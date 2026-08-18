using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace QS3D.Core.Measurement
{
    public enum MeasurementTraceAdjustmentKind
    {
        Deduction = 0,
        Addition = 1
    }

    public sealed class MeasurementTraceFact : IEquatable<MeasurementTraceFact>
    {
        public MeasurementTraceFact(string name, double value, string unit, string? sourceIdentity = null)
        {
            Name = MeasurementTraceContract.RequireToken(name, nameof(name));
            Value = MeasurementTraceContract.RequireFinite(value, nameof(value));
            Unit = MeasurementTraceContract.RequireUnit(unit, nameof(unit));
            SourceIdentity = sourceIdentity == null ? null : MeasurementTraceContract.RequireToken(sourceIdentity, nameof(sourceIdentity));
        }

        public string Name { get; }
        public double Value { get; }
        public string Unit { get; }
        public string? SourceIdentity { get; }

        public bool Equals(MeasurementTraceFact? other)
        {
            return other != null &&
                   string.Equals(Name, other.Name, StringComparison.Ordinal) &&
                   Value.Equals(other.Value) &&
                   string.Equals(Unit, other.Unit, StringComparison.Ordinal) &&
                   string.Equals(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as MeasurementTraceFact);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = MeasurementTraceContract.AddHash(hash, Name);
                hash = MeasurementTraceContract.AddHash(hash, Value);
                hash = MeasurementTraceContract.AddHash(hash, Unit);
                hash = MeasurementTraceContract.AddHash(hash, SourceIdentity);
                return hash;
            }
        }
    }

    public sealed class MeasurementTraceAdjustment : IEquatable<MeasurementTraceAdjustment>
    {
        public MeasurementTraceAdjustment(
            MeasurementTraceAdjustmentKind kind,
            double amount,
            string unit,
            string reason,
            string sourceIdentity)
            : this(kind, amount, unit, reason, sourceIdentity, null, null)
        {
        }

        public MeasurementTraceAdjustment(
            MeasurementTraceAdjustmentKind kind,
            double amount,
            string unit,
            string reason,
            string sourceIdentity,
            string? ruleId = null,
            string? ruleVersion = null)
        {
            if (!Enum.IsDefined(typeof(MeasurementTraceAdjustmentKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if ((ruleId == null) != (ruleVersion == null))
                throw new ArgumentException("Measurement trace adjustment rule identity and version must be supplied together.");

            Kind = kind;
            Amount = MeasurementTraceContract.RequireNonNegativeFinite(amount, nameof(amount));
            Unit = MeasurementTraceContract.RequireUnit(unit, nameof(unit));
            Reason = MeasurementTraceContract.RequireText(reason, nameof(reason));
            SourceIdentity = MeasurementTraceContract.RequireToken(sourceIdentity, nameof(sourceIdentity));
            RuleId = ruleId == null ? null : MeasurementTraceContract.RequireToken(ruleId, nameof(ruleId));
            RuleVersion = ruleVersion == null ? null : MeasurementTraceContract.RequireToken(ruleVersion, nameof(ruleVersion));
        }

        public MeasurementTraceAdjustmentKind Kind { get; }
        public double Amount { get; }
        public string Unit { get; }
        public string Reason { get; }
        public string SourceIdentity { get; }
        public string? RuleId { get; }
        public string? RuleVersion { get; }

        public bool Equals(MeasurementTraceAdjustment? other)
        {
            return other != null &&
                   Kind == other.Kind &&
                   Amount.Equals(other.Amount) &&
                   string.Equals(Unit, other.Unit, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal) &&
                   string.Equals(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal) &&
                   string.Equals(RuleId, other.RuleId, StringComparison.Ordinal) &&
                   string.Equals(RuleVersion, other.RuleVersion, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as MeasurementTraceAdjustment);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = MeasurementTraceContract.AddHash(hash, (int)Kind);
                hash = MeasurementTraceContract.AddHash(hash, Amount);
                hash = MeasurementTraceContract.AddHash(hash, Unit);
                hash = MeasurementTraceContract.AddHash(hash, Reason);
                hash = MeasurementTraceContract.AddHash(hash, SourceIdentity);
                if (RuleId != null)
                {
                    hash = MeasurementTraceContract.AddHash(hash, RuleId);
                    hash = MeasurementTraceContract.AddHash(hash, RuleVersion);
                }
                return hash;
            }
        }
    }

    public sealed class MeasurementTrace : IEquatable<MeasurementTrace>
    {
        public MeasurementTrace(
            string semanticIdentity,
            string sourceIdentity,
            string quantityKey,
            IEnumerable<MeasurementTraceFact> inputFacts,
            double grossValue,
            IEnumerable<MeasurementTraceAdjustment> adjustments,
            double netValue,
            string unit,
            string roundingPolicy,
            IEnumerable<string>? warnings = null,
            IEnumerable<string>? assumptions = null,
            string? ruleId = null,
            string? ruleVersion = null)
        {
            SemanticIdentity = MeasurementTraceContract.RequireToken(semanticIdentity, nameof(semanticIdentity));
            SourceIdentity = MeasurementTraceContract.RequireToken(sourceIdentity, nameof(sourceIdentity));
            QuantityKey = MeasurementTraceContract.RequireToken(quantityKey, nameof(quantityKey));
            GrossValue = MeasurementTraceContract.RequireNonNegativeFinite(grossValue, nameof(grossValue));
            NetValue = MeasurementTraceContract.RequireNonNegativeFinite(netValue, nameof(netValue));
            Unit = MeasurementTraceContract.RequireUnit(unit, nameof(unit));
            RoundingPolicy = MeasurementTraceContract.RequireRoundingPolicy(roundingPolicy, nameof(roundingPolicy));

            if ((ruleId == null) != (ruleVersion == null))
                throw new ArgumentException("Measurement trace rule identity and version must be supplied together.");
            RuleId = ruleId == null ? null : MeasurementTraceContract.RequireToken(ruleId, nameof(ruleId));
            RuleVersion = ruleVersion == null ? null : MeasurementTraceContract.RequireToken(ruleVersion, nameof(ruleVersion));

            InputFacts = MeasurementTraceContract.SnapshotFacts(inputFacts, nameof(inputFacts));
            Adjustments = MeasurementTraceContract.SnapshotAdjustments(adjustments, nameof(adjustments));
            Warnings = MeasurementTraceContract.SnapshotMessages(warnings);
            Assumptions = MeasurementTraceContract.SnapshotMessages(assumptions);

            for (var i = 0; i < Adjustments.Count; i++)
            {
                if (!string.Equals(Adjustments[i].Unit, Unit, StringComparison.Ordinal))
                    throw new ArgumentException("Measurement trace adjustment unit must match the trace unit.", nameof(adjustments));
            }

            if (string.Equals(RoundingPolicy, "none", StringComparison.Ordinal))
            {
                var reconciledNetValue = ReconcileNetValue(GrossValue, Adjustments);
                if (double.IsNaN(reconciledNetValue) ||
                    double.IsInfinity(reconciledNetValue) ||
                    !reconciledNetValue.Equals(NetValue))
                {
                    throw new ArgumentException(
                        "Measurement trace with rounding policy 'none' must reconcile gross value, adjustments, and net value.",
                        nameof(netValue));
                }
            }
        }

        private static double ReconcileNetValue(
            double grossValue,
            IReadOnlyList<MeasurementTraceAdjustment> adjustments)
        {
            var sum = grossValue;
            var compensation = 0d;
            for (var i = 0; i < adjustments.Count; i++)
            {
                var adjustment = adjustments[i];
                var term = adjustment.Kind == MeasurementTraceAdjustmentKind.Deduction
                    ? -adjustment.Amount
                    : adjustment.Amount;
                var next = sum + term;
                if (double.IsNaN(next) || double.IsInfinity(next))
                    return ReconcileNetValueScaled(grossValue, adjustments);

                compensation += Math.Abs(sum) >= Math.Abs(term)
                    ? (sum - next) + term
                    : (term - next) + sum;
                if (double.IsNaN(compensation) || double.IsInfinity(compensation))
                    return ReconcileNetValueScaled(grossValue, adjustments);

                sum = next;
            }

            var reconciled = sum + compensation;
            if (double.IsNaN(reconciled) || double.IsInfinity(reconciled))
                return ReconcileNetValueScaled(grossValue, adjustments);
            return reconciled == 0d ? 0d : reconciled;
        }

        private static double ReconcileNetValueScaled(
            double grossValue,
            IReadOnlyList<MeasurementTraceAdjustment> adjustments)
        {
            var pending = new List<double>();
            pending.Add(grossValue);
            for (var i = 0; i < adjustments.Count; i++)
            {
                var adjustment = adjustments[i];
                pending.Add(adjustment.Kind == MeasurementTraceAdjustmentKind.Deduction
                    ? -adjustment.Amount
                    : adjustment.Amount);
            }

            var reconciled = 0d;
            while (pending.Count > 0)
            {
                var scale = 0d;
                for (var i = 0; i < pending.Count; i++)
                    scale = Math.Max(scale, Math.Abs(pending[i]));
                if (scale == 0d) break;

                var residuals = new List<double>();
                var sum = 0d;
                var compensation = 0d;
                for (var i = 0; i < pending.Count; i++)
                {
                    var value = pending[i];
                    var scaled = value / scale;
                    if (value != 0d && scaled == 0d)
                    {
                        residuals.Add(value);
                        continue;
                    }

                    var corrected = scaled - compensation;
                    var next = sum + corrected;
                    compensation = (next - sum) - corrected;
                    sum = next;
                }

                reconciled += sum * scale;
                if (double.IsNaN(reconciled) || double.IsInfinity(reconciled))
                    return reconciled;
                pending = residuals;
            }

            return reconciled == 0d ? 0d : reconciled;
        }

        public string SemanticIdentity { get; }
        public string SourceIdentity { get; }
        public string QuantityKey { get; }
        public IReadOnlyList<MeasurementTraceFact> InputFacts { get; }
        public double GrossValue { get; }
        public IReadOnlyList<MeasurementTraceAdjustment> Adjustments { get; }
        public double NetValue { get; }
        public string Unit { get; }
        public string RoundingPolicy { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyList<string> Assumptions { get; }
        public string? RuleId { get; }
        public string? RuleVersion { get; }

        public string ToCanonicalString()
        {
            var hasAdjustmentRuleIdentity = false;
            for (var i = 0; i < Adjustments.Count; i++)
            {
                if (Adjustments[i].RuleId == null) continue;
                hasAdjustmentRuleIdentity = true;
                break;
            }

            var builder = new StringBuilder();
            MeasurementTraceContract.AppendToken(builder, hasAdjustmentRuleIdentity ? "MTR2" : "MTR1");
            MeasurementTraceContract.AppendToken(builder, SemanticIdentity);
            MeasurementTraceContract.AppendToken(builder, SourceIdentity);
            MeasurementTraceContract.AppendToken(builder, QuantityKey);
            MeasurementTraceContract.AppendNumber(builder, GrossValue);
            MeasurementTraceContract.AppendNumber(builder, NetValue);
            MeasurementTraceContract.AppendToken(builder, Unit);
            MeasurementTraceContract.AppendToken(builder, RoundingPolicy);
            MeasurementTraceContract.AppendNullableToken(builder, RuleId);
            MeasurementTraceContract.AppendNullableToken(builder, RuleVersion);

            MeasurementTraceContract.AppendCount(builder, InputFacts.Count);
            for (var i = 0; i < InputFacts.Count; i++)
            {
                var fact = InputFacts[i];
                MeasurementTraceContract.AppendToken(builder, fact.Name);
                MeasurementTraceContract.AppendNumber(builder, fact.Value);
                MeasurementTraceContract.AppendToken(builder, fact.Unit);
                MeasurementTraceContract.AppendNullableToken(builder, fact.SourceIdentity);
            }

            MeasurementTraceContract.AppendCount(builder, Adjustments.Count);
            for (var i = 0; i < Adjustments.Count; i++)
            {
                var adjustment = Adjustments[i];
                MeasurementTraceContract.AppendCount(builder, (int)adjustment.Kind);
                MeasurementTraceContract.AppendNumber(builder, adjustment.Amount);
                MeasurementTraceContract.AppendToken(builder, adjustment.Unit);
                MeasurementTraceContract.AppendToken(builder, adjustment.Reason);
                MeasurementTraceContract.AppendToken(builder, adjustment.SourceIdentity);
                if (hasAdjustmentRuleIdentity)
                {
                    MeasurementTraceContract.AppendNullableToken(builder, adjustment.RuleId);
                    MeasurementTraceContract.AppendNullableToken(builder, adjustment.RuleVersion);
                }
            }

            MeasurementTraceContract.AppendMessages(builder, Warnings);
            MeasurementTraceContract.AppendMessages(builder, Assumptions);
            return builder.ToString();
        }

        public bool Equals(MeasurementTrace? other)
        {
            if (other == null ||
                !string.Equals(SemanticIdentity, other.SemanticIdentity, StringComparison.Ordinal) ||
                !string.Equals(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal) ||
                !string.Equals(QuantityKey, other.QuantityKey, StringComparison.Ordinal) ||
                !GrossValue.Equals(other.GrossValue) ||
                !NetValue.Equals(other.NetValue) ||
                !string.Equals(Unit, other.Unit, StringComparison.Ordinal) ||
                !string.Equals(RoundingPolicy, other.RoundingPolicy, StringComparison.Ordinal) ||
                !string.Equals(RuleId, other.RuleId, StringComparison.Ordinal) ||
                !string.Equals(RuleVersion, other.RuleVersion, StringComparison.Ordinal))
                return false;

            return MeasurementTraceContract.SequenceEqual(InputFacts, other.InputFacts) &&
                   MeasurementTraceContract.SequenceEqual(Adjustments, other.Adjustments) &&
                   MeasurementTraceContract.SequenceEqual(Warnings, other.Warnings) &&
                   MeasurementTraceContract.SequenceEqual(Assumptions, other.Assumptions);
        }

        public override bool Equals(object? obj) => Equals(obj as MeasurementTrace);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = MeasurementTraceContract.AddHash(hash, SemanticIdentity);
                hash = MeasurementTraceContract.AddHash(hash, SourceIdentity);
                hash = MeasurementTraceContract.AddHash(hash, QuantityKey);
                hash = MeasurementTraceContract.AddHash(hash, GrossValue);
                hash = MeasurementTraceContract.AddHash(hash, NetValue);
                hash = MeasurementTraceContract.AddHash(hash, Unit);
                hash = MeasurementTraceContract.AddHash(hash, RoundingPolicy);
                hash = MeasurementTraceContract.AddHash(hash, RuleId);
                hash = MeasurementTraceContract.AddHash(hash, RuleVersion);
                hash = MeasurementTraceContract.AddSequenceHash(hash, InputFacts);
                hash = MeasurementTraceContract.AddSequenceHash(hash, Adjustments);
                hash = MeasurementTraceContract.AddStringSequenceHash(hash, Warnings);
                hash = MeasurementTraceContract.AddStringSequenceHash(hash, Assumptions);
                return hash;
            }
        }
    }

    internal static class MeasurementTraceContract
    {
        private const int MaximumCollectionEntries = 10000;

        internal static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Measurement trace text is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Measurement trace text must be canonical without surrounding whitespace.", parameterName);
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                    throw new ArgumentException("Measurement trace text cannot contain control characters.", parameterName);
            }
            return value;
        }

        internal static string RequireToken(string value, string parameterName)
        {
            value = RequireText(value, parameterName);
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                    throw new ArgumentException("Measurement trace token cannot contain whitespace.", parameterName);
            }
            return value;
        }

        internal static string RequireUnit(string value, string parameterName)
        {
            value = RequireToken(value, parameterName);
            if (!string.Equals(value, value.ToLowerInvariant(), StringComparison.Ordinal))
                throw new ArgumentException("Measurement trace unit must use canonical lower-case text.", parameterName);
            return value;
        }

        internal static string RequireRoundingPolicy(string value, string parameterName)
        {
            value = RequireToken(value, parameterName);
            if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(value, "none", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Measurement trace rounding policy 'none' must use canonical lower-case text.",
                    parameterName);
            }
            return value;
        }

        internal static double RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "Measurement trace numeric values must be finite.");
            return value == 0d ? 0d : value;
        }

        internal static double RequireNonNegativeFinite(double value, string parameterName)
        {
            value = RequireFinite(value, parameterName);
            if (value < 0d)
                throw new ArgumentOutOfRangeException(parameterName, "Measurement trace quantity values must be non-negative.");
            return value;
        }

        internal static IReadOnlyList<MeasurementTraceFact> SnapshotFacts(IEnumerable<MeasurementTraceFact> source, string parameterName)
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            RequireSupportedCount(source, parameterName, "facts");
            var items = new List<MeasurementTraceFact>();
            foreach (var item in source)
            {
                if (items.Count >= MaximumCollectionEntries)
                    throw CollectionCountError(parameterName, "facts");
                if (item == null) throw new ArgumentException("Measurement trace facts cannot contain null entries.", parameterName);
                items.Add(item);
            }
            items.Sort(CompareFacts);
            for (var i = 1; i < items.Count; i++)
            {
                var previous = items[i - 1];
                var current = items[i];
                if (!string.Equals(previous.Name, current.Name, StringComparison.Ordinal) ||
                    !string.Equals(previous.SourceIdentity, current.SourceIdentity, StringComparison.Ordinal))
                    continue;

                if (previous.Equals(current))
                    throw new ArgumentException("Measurement trace facts must not contain duplicates.", parameterName);

                throw new ArgumentException(
                    "Measurement trace facts must not contain conflicting payloads for the same evidence identity.",
                    parameterName);
            }
            return new ReadOnlyCollection<MeasurementTraceFact>(items.ToArray());
        }

        internal static IReadOnlyList<MeasurementTraceAdjustment> SnapshotAdjustments(IEnumerable<MeasurementTraceAdjustment> source, string parameterName)
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            RequireSupportedCount(source, parameterName, "adjustments");
            var items = new List<MeasurementTraceAdjustment>();
            foreach (var item in source)
            {
                if (items.Count >= MaximumCollectionEntries)
                    throw CollectionCountError(parameterName, "adjustments");
                if (item == null) throw new ArgumentException("Measurement trace adjustments cannot contain null entries.", parameterName);
                items.Add(item);
            }
            items.Sort(CompareAdjustments);
            for (var i = 1; i < items.Count; i++)
            {
                if (items[i - 1].Equals(items[i]))
                    throw new ArgumentException("Measurement trace adjustments must not contain duplicates.", parameterName);
            }
            return new ReadOnlyCollection<MeasurementTraceAdjustment>(items.ToArray());
        }

        internal static IReadOnlyList<string> SnapshotMessages(IEnumerable<string>? source)
        {
            if (source == null) return new ReadOnlyCollection<string>(Array.Empty<string>());
            RequireSupportedCount(source, nameof(source), "messages");
            var items = new List<string>();
            foreach (var item in source)
            {
                if (items.Count >= MaximumCollectionEntries)
                    throw CollectionCountError(nameof(source), "messages");
                items.Add(RequireText(item, nameof(source)));
            }
            items.Sort(StringComparer.Ordinal);
            for (var i = 1; i < items.Count; i++)
            {
                if (string.Equals(items[i - 1], items[i], StringComparison.Ordinal))
                    throw new ArgumentException("Measurement trace messages must not contain duplicates.", nameof(source));
            }
            return new ReadOnlyCollection<string>(items.ToArray());
        }

        private static void RequireSupportedCount<T>(IEnumerable<T> source, string parameterName, string collectionName)
        {
            int? knownCount = null;
            if (source is ICollection<T> collection)
                ValidateKnownCount(collection.Count, ref knownCount, parameterName, collectionName);
            if (source is IReadOnlyCollection<T> readOnlyCollection)
                ValidateKnownCount(readOnlyCollection.Count, ref knownCount, parameterName, collectionName);
            if (source is System.Collections.ICollection nonGenericCollection)
                ValidateKnownCount(nonGenericCollection.Count, ref knownCount, parameterName, collectionName);
        }

        private static void ValidateKnownCount(
            int count,
            ref int? knownCount,
            string parameterName,
            string collectionName)
        {
            if (count < 0)
                throw new ArgumentException(
                    "Measurement trace " + collectionName + " count cannot be negative.",
                    parameterName);
            if (count > MaximumCollectionEntries)
                throw CollectionCountError(parameterName, collectionName);
            if (knownCount.HasValue && knownCount.Value != count)
                throw new ArgumentException(
                    "Measurement trace " + collectionName + " count contracts disagree.",
                    parameterName);
            knownCount = count;
        }

        private static ArgumentException CollectionCountError(string parameterName, string collectionName)
        {
            return new ArgumentException(
                "Measurement trace " + collectionName + " accepts at most " + MaximumCollectionEntries + " entries.",
                parameterName);
        }

        private static int CompareFacts(MeasurementTraceFact left, MeasurementTraceFact right)
        {
            var compare = StringComparer.Ordinal.Compare(left.Name, right.Name);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.SourceIdentity ?? string.Empty, right.SourceIdentity ?? string.Empty);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.Unit, right.Unit);
            if (compare != 0) return compare;
            return left.Value.CompareTo(right.Value);
        }

        private static int CompareAdjustments(MeasurementTraceAdjustment left, MeasurementTraceAdjustment right)
        {
            var compare = ((int)left.Kind).CompareTo((int)right.Kind);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.SourceIdentity, right.SourceIdentity);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.Reason, right.Reason);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.Unit, right.Unit);
            if (compare != 0) return compare;
            compare = left.Amount.CompareTo(right.Amount);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.RuleId ?? string.Empty, right.RuleId ?? string.Empty);
            if (compare != 0) return compare;
            return StringComparer.Ordinal.Compare(left.RuleVersion ?? string.Empty, right.RuleVersion ?? string.Empty);
        }

        internal static bool SequenceEqual<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        {
            if (left.Count != right.Count) return false;
            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < left.Count; i++)
            {
                if (!comparer.Equals(left[i], right[i])) return false;
            }
            return true;
        }

        internal static void AppendToken(StringBuilder builder, string value)
        {
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }

        internal static void AppendNullableToken(StringBuilder builder, string? value)
        {
            if (value == null)
            {
                builder.Append("-;");
                return;
            }
            AppendToken(builder, value);
        }

        internal static void AppendNumber(StringBuilder builder, double value)
        {
            AppendToken(builder, value.ToString("R", CultureInfo.InvariantCulture));
        }

        internal static void AppendCount(StringBuilder builder, int value)
        {
            AppendToken(builder, value.ToString(CultureInfo.InvariantCulture));
        }

        internal static void AppendMessages(StringBuilder builder, IReadOnlyList<string> values)
        {
            AppendCount(builder, values.Count);
            for (var i = 0; i < values.Count; i++) AppendToken(builder, values[i]);
        }

        internal static int AddHash(int hash, string? value)
        {
            unchecked
            {
                var valueHash = 17;
                if (value != null)
                {
                    for (var i = 0; i < value.Length; i++)
                        valueHash = (valueHash * 31) + value[i];
                }
                return (hash * 31) + (value == null ? 0 : valueHash);
            }
        }

        internal static int AddHash(int hash, double value) => unchecked((hash * 31) + value.GetHashCode());
        internal static int AddHash(int hash, int value) => unchecked((hash * 31) + value);

        internal static int AddSequenceHash<T>(int hash, IReadOnlyList<T> values)
        {
            unchecked
            {
                for (var i = 0; i < values.Count; i++)
                    hash = (hash * 31) + (values[i] == null ? 0 : values[i]!.GetHashCode());
                return hash;
            }
        }

        internal static int AddStringSequenceHash(int hash, IReadOnlyList<string> values)
        {
            unchecked
            {
                for (var i = 0; i < values.Count; i++)
                    hash = AddHash(hash, values[i]);
                return hash;
            }
        }
    }
}
