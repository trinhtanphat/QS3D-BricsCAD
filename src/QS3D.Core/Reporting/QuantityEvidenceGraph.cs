using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.Reporting;

/// <summary>
/// Host-neutral selector for the model evidence behind a quantity value.
/// The selector deliberately contains no BricsCAD/ODA runtime types so the
/// same identity can be consumed by Core reporting, XLSX export and host UI.
/// </summary>
public sealed class QuantityEvidenceSelector
{
    private QuantityEvidenceSelector(
        QuantityEvidenceSelectorKind kind,
        string? entityKey,
        int? faceIndex,
        string? faceKey,
        string? sourceEntityKey,
        string? targetEntityKey,
        string? intersectionKey)
    {
        Kind = kind;
        EntityKey = entityKey;
        FaceIndex = faceIndex;
        FaceKey = faceKey;
        SourceEntityKey = sourceEntityKey;
        TargetEntityKey = targetEntityKey;
        IntersectionKey = intersectionKey;
        CanonicalKey = BuildCanonicalKey();
    }

    public QuantityEvidenceSelectorKind Kind { get; }
    public string? EntityKey { get; }
    public int? FaceIndex { get; }
    public string? FaceKey { get; }
    public string? SourceEntityKey { get; }
    public string? TargetEntityKey { get; }
    public string? IntersectionKey { get; }
    public string CanonicalKey { get; }

    public static QuantityEvidenceSelector ForEntity(string entityKey)
    {
        return new QuantityEvidenceSelector(
            QuantityEvidenceSelectorKind.Entity,
            QuantityEvidenceIdentity.RequireKey(entityKey, nameof(entityKey)),
            null,
            null,
            null,
            null,
            null);
    }

    public static QuantityEvidenceSelector ForFace(string entityKey, int faceIndex)
    {
        if (faceIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(faceIndex), "Face index must be zero or greater.");
        }

        return new QuantityEvidenceSelector(
            QuantityEvidenceSelectorKind.Face,
            QuantityEvidenceIdentity.RequireKey(entityKey, nameof(entityKey)),
            faceIndex,
            null,
            null,
            null,
            null);
    }

    /// <summary>
    /// Selects a stable host-neutral face identifier such as
    /// SOLID-01/FACE-03 without reducing it to a transient numeric index.
    /// Existing numeric face selector canonical keys remain unchanged.
    /// </summary>
    public static QuantityEvidenceSelector ForFaceKey(string entityKey, string faceKey)
    {
        return new QuantityEvidenceSelector(
            QuantityEvidenceSelectorKind.Face,
            QuantityEvidenceIdentity.RequireKey(entityKey, nameof(entityKey)),
            null,
            QuantityEvidenceIdentity.RequireKey(faceKey, nameof(faceKey)),
            null,
            null,
            null);
    }

    public static QuantityEvidenceSelector ForIntersection(
        string sourceEntityKey,
        string targetEntityKey,
        string intersectionKey)
    {
        return new QuantityEvidenceSelector(
            QuantityEvidenceSelectorKind.Intersection,
            null,
            null,
            null,
            QuantityEvidenceIdentity.RequireKey(sourceEntityKey, nameof(sourceEntityKey)),
            QuantityEvidenceIdentity.RequireKey(targetEntityKey, nameof(targetEntityKey)),
            QuantityEvidenceIdentity.RequireKey(intersectionKey, nameof(intersectionKey)));
    }

    private string BuildCanonicalKey()
    {
        return Kind switch
        {
            QuantityEvidenceSelectorKind.Entity => QuantityEvidenceIdentity.Join("entity", EntityKey!),
            QuantityEvidenceSelectorKind.Face when FaceIndex.HasValue => QuantityEvidenceIdentity.Join(
                "face",
                EntityKey!,
                FaceIndex.Value.ToString(CultureInfo.InvariantCulture)),
            QuantityEvidenceSelectorKind.Face when !string.IsNullOrWhiteSpace(FaceKey) => QuantityEvidenceIdentity.Join(
                "face-key",
                EntityKey!,
                FaceKey!),
            QuantityEvidenceSelectorKind.Face => throw new InvalidOperationException("Face selector requires a face index or stable face key."),
            QuantityEvidenceSelectorKind.Intersection => QuantityEvidenceIdentity.Join(
                "intersection",
                SourceEntityKey!,
                TargetEntityKey!,
                IntersectionKey!),
            _ => throw new InvalidOperationException($"Unsupported selector kind: {Kind}.")
        };
    }
}

public enum QuantityEvidenceSelectorKind
{
    Entity = 0,
    Face = 1,
    Intersection = 2
}

public enum QuantityEvidenceOperation
{
    Add = 0,
    Deduct = 1,
    Ignore = 2
}

/// <summary>
/// One deterministic operand used to explain a quantity contribution.
/// </summary>
public sealed class QuantityEvidenceOperand
{
    public QuantityEvidenceOperand(string key, decimal value, string unit)
    {
        Key = QuantityEvidenceIdentity.RequireKey(key, nameof(key));
        Value = value;
        Unit = QuantityEvidenceIdentity.NormalizeText(unit);
    }

    public string Key { get; }
    public decimal Value { get; }
    public string Unit { get; }

    internal string CanonicalKey => QuantityEvidenceIdentity.Join(
        Key,
        QuantityEvidenceIdentity.Decimal(Value),
        Unit);
}

/// <summary>
/// Evidence for a gross quantity contribution. Value is copied from the
/// canonical quantity result; this type does not re-run geometry takeoff.
/// </summary>
public sealed class QuantityContribution
{
    private QuantityContribution(
        string semanticKey,
        string label,
        QuantityEvidenceOperation operation,
        string formula,
        decimal value,
        QuantityEvidenceSelector selector,
        IReadOnlyList<QuantityEvidenceOperand> operands)
    {
        SemanticKey = semanticKey;
        Label = label;
        Operation = operation;
        Formula = formula;
        Value = value;
        Selector = selector;
        Operands = operands;
        EvidenceId = QuantityEvidenceIdentity.CreateId(
            "contribution",
            SemanticKey,
            Operation.ToString(),
            QuantityEvidenceIdentity.Decimal(Value),
            Selector.CanonicalKey,
            QuantityEvidenceIdentity.Join(Operands.Select(static operand => operand.CanonicalKey).ToArray()));
    }

    public string EvidenceId { get; }
    public string SemanticKey { get; }
    public string Label { get; }
    public QuantityEvidenceOperation Operation { get; }
    public string Formula { get; }
    public decimal Value { get; }
    public QuantityEvidenceSelector Selector { get; }
    public IReadOnlyList<QuantityEvidenceOperand> Operands { get; }

    public static QuantityContribution Create(
        string semanticKey,
        string label,
        QuantityEvidenceOperation operation,
        string formula,
        decimal value,
        QuantityEvidenceSelector selector,
        IEnumerable<QuantityEvidenceOperand>? operands = null)
    {
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        ValidateSignedValue(operation, value, nameof(value));

        var orderedOperands = (operands ?? Array.Empty<QuantityEvidenceOperand>())
            .Select(static operand => operand ?? throw new ArgumentException("Operands cannot contain null values.", nameof(operands)))
            .OrderBy(static operand => operand.CanonicalKey, StringComparer.Ordinal)
            .ToArray();

        return new QuantityContribution(
            QuantityEvidenceIdentity.RequireKey(semanticKey, nameof(semanticKey)),
            QuantityEvidenceIdentity.RequireText(label, nameof(label)),
            operation,
            QuantityEvidenceIdentity.RequireText(formula, nameof(formula)),
            value,
            selector,
            orderedOperands);
    }

    internal static void ValidateSignedValue(QuantityEvidenceOperation operation, decimal value, string parameterName)
    {
        switch (operation)
        {
            case QuantityEvidenceOperation.Add when value < 0m:
                throw new ArgumentOutOfRangeException(parameterName, "Add evidence cannot have a negative value.");
            case QuantityEvidenceOperation.Deduct when value > 0m:
                throw new ArgumentOutOfRangeException(parameterName, "Deduct evidence must be zero or negative.");
            case QuantityEvidenceOperation.Ignore when value != 0m:
                throw new ArgumentOutOfRangeException(parameterName, "Ignore evidence must have a zero value.");
        }
    }
}

/// <summary>
/// Signed adjustment from gross to net quantity with explicit source/target
/// provenance. Intersection selectors are required whenever both source and
/// target participate in a deduction/addition.
/// </summary>
public sealed class QuantityAdjustment
{
    private QuantityAdjustment(
        string semanticKey,
        string ruleKey,
        string reason,
        QuantityEvidenceOperation operation,
        string sourceReference,
        string targetReference,
        decimal delta,
        QuantityEvidenceSelector selector)
    {
        SemanticKey = semanticKey;
        RuleKey = ruleKey;
        Reason = reason;
        Operation = operation;
        SourceReference = sourceReference;
        TargetReference = targetReference;
        Delta = delta;
        Selector = selector;
        EvidenceId = QuantityEvidenceIdentity.CreateId(
            "adjustment",
            SemanticKey,
            RuleKey,
            Operation.ToString(),
            SourceReference,
            TargetReference,
            QuantityEvidenceIdentity.Decimal(Delta),
            Selector.CanonicalKey);
    }

    public string EvidenceId { get; }
    public string SemanticKey { get; }
    public string RuleKey { get; }
    public string Reason { get; }
    public QuantityEvidenceOperation Operation { get; }
    public string SourceReference { get; }
    public string TargetReference { get; }
    public decimal Delta { get; }
    public QuantityEvidenceSelector Selector { get; }

    public static QuantityAdjustment Create(
        string semanticKey,
        string ruleKey,
        string reason,
        QuantityEvidenceOperation operation,
        string sourceReference,
        string targetReference,
        decimal delta,
        QuantityEvidenceSelector selector)
    {
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        QuantityContribution.ValidateSignedValue(operation, delta, nameof(delta));

        var normalizedSource = QuantityEvidenceIdentity.RequireKey(sourceReference, nameof(sourceReference));
        var normalizedTarget = QuantityEvidenceIdentity.RequireKey(targetReference, nameof(targetReference));

        if (selector.Kind != QuantityEvidenceSelectorKind.Intersection)
        {
            throw new ArgumentException(
                "Quantity adjustments with source/target provenance require an intersection selector.",
                nameof(selector));
        }

        if (!string.Equals(selector.SourceEntityKey, normalizedSource, StringComparison.Ordinal)
            || !string.Equals(selector.TargetEntityKey, normalizedTarget, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Adjustment source/target references must match the intersection selector.",
                nameof(selector));
        }

        return new QuantityAdjustment(
            QuantityEvidenceIdentity.RequireKey(semanticKey, nameof(semanticKey)),
            QuantityEvidenceIdentity.RequireKey(ruleKey, nameof(ruleKey)),
            QuantityEvidenceIdentity.RequireText(reason, nameof(reason)),
            operation,
            normalizedSource,
            normalizedTarget,
            delta,
            selector);
    }
}

/// <summary>
/// Canonical review/export graph for a single quantity metric. Gross and net
/// are authoritative values supplied by the quantity engine. Adjustments are
/// validated against those values; no geometry is recalculated here.
/// </summary>
public sealed class QuantityExplanation
{
    private QuantityExplanation(
        string subjectKey,
        string category,
        string metric,
        string unit,
        decimal grossValue,
        decimal netValue,
        IReadOnlyList<QuantityContribution> contributions,
        IReadOnlyList<QuantityAdjustment> adjustments)
    {
        SubjectKey = subjectKey;
        Category = category;
        Metric = metric;
        Unit = unit;
        GrossValue = grossValue;
        NetValue = netValue;
        Contributions = contributions;
        Adjustments = adjustments;
        EvidenceId = QuantityEvidenceIdentity.CreateId(
            "explanation",
            SubjectKey,
            Category,
            Metric,
            Unit,
            QuantityEvidenceIdentity.Decimal(GrossValue),
            QuantityEvidenceIdentity.Decimal(NetValue),
            QuantityEvidenceIdentity.Join(Contributions.Select(static item => item.EvidenceId).ToArray()),
            QuantityEvidenceIdentity.Join(Adjustments.Select(static item => item.EvidenceId).ToArray()));
    }

    public string EvidenceId { get; }
    public string SubjectKey { get; }
    public string Category { get; }
    public string Metric { get; }
    public string Unit { get; }
    public decimal GrossValue { get; }
    public decimal NetValue { get; }
    public IReadOnlyList<QuantityContribution> Contributions { get; }
    public IReadOnlyList<QuantityAdjustment> Adjustments { get; }

    public static QuantityExplanation Create(
        string subjectKey,
        string category,
        string metric,
        string unit,
        decimal grossValue,
        decimal netValue,
        IEnumerable<QuantityContribution>? contributions = null,
        IEnumerable<QuantityAdjustment>? adjustments = null)
    {
        if (grossValue < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(grossValue), "Gross quantity cannot be negative.");
        }

        if (netValue < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(netValue), "Net quantity cannot be negative.");
        }

        var orderedContributions = (contributions ?? Array.Empty<QuantityContribution>())
            .Select(static item => item ?? throw new ArgumentException("Contributions cannot contain null values.", nameof(contributions)))
            .OrderBy(static item => item.EvidenceId, StringComparer.Ordinal)
            .ToArray();

        var orderedAdjustments = (adjustments ?? Array.Empty<QuantityAdjustment>())
            .Select(static item => item ?? throw new ArgumentException("Adjustments cannot contain null values.", nameof(adjustments)))
            .OrderBy(static item => item.EvidenceId, StringComparer.Ordinal)
            .ToArray();

        var adjustmentTotal = orderedAdjustments.Sum(static adjustment => adjustment.Delta);
        var expectedNet = grossValue + adjustmentTotal;
        if (expectedNet != netValue)
        {
            throw new ArgumentException(
                $"Quantity evidence arithmetic mismatch: gross {QuantityEvidenceIdentity.Decimal(grossValue)} + adjustments {QuantityEvidenceIdentity.Decimal(adjustmentTotal)} != net {QuantityEvidenceIdentity.Decimal(netValue)}.",
                nameof(netValue));
        }

        return new QuantityExplanation(
            QuantityEvidenceIdentity.RequireKey(subjectKey, nameof(subjectKey)),
            QuantityEvidenceIdentity.RequireText(category, nameof(category)),
            QuantityEvidenceIdentity.RequireText(metric, nameof(metric)),
            QuantityEvidenceIdentity.RequireText(unit, nameof(unit)),
            grossValue,
            netValue,
            orderedContributions,
            orderedAdjustments);
    }
}

internal static class QuantityEvidenceIdentity
{
    public static string CreateId(params string[] fields)
    {
        var canonical = Join(fields);
        using (var sha = SHA256.Create())
        {
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            var builder = new StringBuilder(hash.Length * 2);
            for (var index = 0; index < hash.Length; index++)
            {
                builder.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
            }

            return "qe_" + builder.ToString().Substring(0, 24);
        }
    }

    public static string Join(params string[] fields)
    {
        var builder = new StringBuilder();
        foreach (var field in fields)
        {
            var value = field ?? string.Empty;
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }

        return builder.ToString();
    }

    public static string Decimal(decimal value)
    {
        return value.ToString("G29", CultureInfo.InvariantCulture);
    }

    public static string RequireKey(string value, string parameterName)
    {
        var normalized = NormalizeText(value);
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A non-empty semantic key is required.", parameterName);
        }

        return normalized;
    }

    public static string RequireText(string value, string parameterName)
    {
        var normalized = NormalizeText(value);
        if (normalized.Length == 0)
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return normalized;
    }

    public static string NormalizeText(string? value)
    {
        var candidate = value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return string.Empty;
        }

        return string.Join(
            " ",
            candidate.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
