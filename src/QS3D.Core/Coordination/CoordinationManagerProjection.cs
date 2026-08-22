using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Coordination
{
    public enum CoordinationFindingKind
    {
        HardClash = 0,
        Clearance = 1,
        Duplicate = 2
    }

    public enum CoordinationFindingStatus
    {
        Open = 0,
        Reviewed = 1,
        Resolved = 2,
        Ignored = 3
    }

    public enum CoordinationFindingSeverity
    {
        Info = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public sealed class CoordinationManagerFinding
    {
        public CoordinationManagerFinding(
            string id,
            CoordinationFindingKind kind,
            CoordinationFindingStatus status,
            CoordinationFindingSeverity severity,
            string floorId,
            string categoryA,
            string categoryB,
            string ruleId,
            bool referenceAResolved,
            bool referenceBResolved,
            bool isStale,
            string nonActionableReason = null)
        {
            RequireDefined(kind, nameof(kind));
            RequireDefined(status, nameof(status));
            RequireDefined(severity, nameof(severity));

            Id = RequireToken(id, nameof(id));
            Kind = kind;
            Status = status;
            Severity = severity;
            FloorId = NormalizeOptional(floorId);
            CategoryA = RequireToken(categoryA, nameof(categoryA));
            CategoryB = RequireToken(categoryB, nameof(categoryB));
            RuleId = NormalizeOptional(ruleId);
            ReferenceAResolved = referenceAResolved;
            ReferenceBResolved = referenceBResolved;
            IsStale = isStale;
            NonActionableReason = NormalizeOptional(nonActionableReason);

            if (!IsActionable && string.IsNullOrEmpty(NonActionableReason))
                NonActionableReason = BuildNonActionableReason(referenceAResolved, referenceBResolved, isStale);
            if (IsActionable && !string.IsNullOrEmpty(NonActionableReason))
                throw new ArgumentException("Actionable findings cannot carry a non-actionable reason.", nameof(nonActionableReason));
        }

        public string Id { get; }
        public CoordinationFindingKind Kind { get; }
        public CoordinationFindingStatus Status { get; }
        public CoordinationFindingSeverity Severity { get; }
        public string FloorId { get; }
        public string CategoryA { get; }
        public string CategoryB { get; }
        public string RuleId { get; }
        public bool ReferenceAResolved { get; }
        public bool ReferenceBResolved { get; }
        public bool IsStale { get; }
        public string NonActionableReason { get; private set; }
        public bool IsActionable => ReferenceAResolved && ReferenceBResolved && !IsStale;

        private static void RequireDefined<TEnum>(TEnum value, string parameterName) where TEnum : struct
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
                throw new ArgumentOutOfRangeException(parameterName, value, "Unsupported enum value.");
        }

        private static string RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value is required.", parameterName);
            var normalized = value.Trim();
            if (!string.Equals(value, normalized, StringComparison.Ordinal))
                throw new ArgumentException("Value must already be canonical without surrounding whitespace.", parameterName);
            return normalized;
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string BuildNonActionableReason(bool aResolved, bool bResolved, bool stale)
        {
            var reasons = new List<string>();
            if (!aResolved) reasons.Add("REFERENCE_A_UNRESOLVED");
            if (!bResolved) reasons.Add("REFERENCE_B_UNRESOLVED");
            if (stale) reasons.Add("STALE");
            return string.Join("+", reasons);
        }
    }

    public sealed class CoordinationManagerFilter
    {
        public CoordinationFindingStatus? Status { get; set; }
        public CoordinationFindingSeverity? MinimumSeverity { get; set; }
        public string FloorId { get; set; }
        public string Category { get; set; }
        public string RuleId { get; set; }
        public CoordinationFindingKind? Kind { get; set; }
        public bool IncludeNonActionable { get; set; } = true;
    }

    public static class CoordinationManagerProjection
    {
        public static IReadOnlyList<CoordinationManagerFinding> Build(
            IEnumerable<CoordinationManagerFinding> findings,
            CoordinationManagerFilter filter = null)
        {
            if (findings == null) throw new ArgumentNullException(nameof(findings));
            filter = filter ?? new CoordinationManagerFilter();
            ValidateFilter(filter);

            var byId = new Dictionary<string, CoordinationManagerFinding>(StringComparer.OrdinalIgnoreCase);
            foreach (var finding in findings)
            {
                if (finding == null)
                    throw new ArgumentException("Finding collection cannot contain null entries.", nameof(findings));
                if (byId.ContainsKey(finding.Id))
                    throw new InvalidOperationException("Duplicate coordination finding ID: " + finding.Id);
                byId.Add(finding.Id, finding);
            }

            IEnumerable<CoordinationManagerFinding> query = byId.Values;
            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);
            if (filter.MinimumSeverity.HasValue)
            {
                var minimum = (int)filter.MinimumSeverity.Value;
                query = query.Where(x => (int)x.Severity >= minimum);
            }
            if (filter.Kind.HasValue)
                query = query.Where(x => x.Kind == filter.Kind.Value);
            if (!string.IsNullOrWhiteSpace(filter.FloorId))
            {
                var floorId = filter.FloorId.Trim();
                query = query.Where(x => string.Equals(x.FloorId, floorId, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                var category = filter.Category.Trim();
                query = query.Where(x =>
                    string.Equals(x.CategoryA, category, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.CategoryB, category, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(filter.RuleId))
            {
                var ruleId = filter.RuleId.Trim();
                query = query.Where(x => string.Equals(x.RuleId, ruleId, StringComparison.OrdinalIgnoreCase));
            }
            if (!filter.IncludeNonActionable)
                query = query.Where(x => x.IsActionable);

            var rows = query
                .OrderByDescending(x => (int)x.Severity)
                .ThenBy(x => x.FloorId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => (int)x.Kind)
                .ThenBy(x => x.RuleId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ReadOnlyCollection<CoordinationManagerFinding>(rows);
        }

        private static void ValidateFilter(CoordinationManagerFilter filter)
        {
            if (filter.Status.HasValue && !Enum.IsDefined(typeof(CoordinationFindingStatus), filter.Status.Value))
                throw new ArgumentOutOfRangeException(nameof(filter.Status), filter.Status.Value, "Unsupported status filter.");
            if (filter.MinimumSeverity.HasValue && !Enum.IsDefined(typeof(CoordinationFindingSeverity), filter.MinimumSeverity.Value))
                throw new ArgumentOutOfRangeException(nameof(filter.MinimumSeverity), filter.MinimumSeverity.Value, "Unsupported severity filter.");
            if (filter.Kind.HasValue && !Enum.IsDefined(typeof(CoordinationFindingKind), filter.Kind.Value))
                throw new ArgumentOutOfRangeException(nameof(filter.Kind), filter.Kind.Value, "Unsupported kind filter.");
        }
    }
}
