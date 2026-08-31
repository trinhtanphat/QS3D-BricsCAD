using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace QS3D.Core.Coordination
{
    public enum CoordinationRuleKind
    {
        HardClash = 0,
        Clearance = 1
    }

    /// <summary>
    /// Host-neutral, immutable coordination rule definition. Category pairs are symmetric:
    /// a Pipe/Beam rule matches both Pipe→Beam and Beam→Pipe inputs.
    /// </summary>
    public sealed class CoordinationRule
    {
        public const string WildcardCategory = "*";

        public CoordinationRule(
            string ruleId,
            int ruleVersion,
            string leftCategory,
            string rightCategory,
            CoordinationRuleKind kind,
            string severity,
            double clearance,
            bool enabled = true)
        {
            RuleId = RequiredToken(ruleId, nameof(ruleId));
            if (ruleVersion <= 0) throw new ArgumentOutOfRangeException(nameof(ruleVersion), "Rule version must be positive.");
            RuleVersion = ruleVersion;
            LeftCategory = Category(leftCategory, nameof(leftCategory));
            RightCategory = Category(rightCategory, nameof(rightCategory));
            if (!Enum.IsDefined(typeof(CoordinationRuleKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind), "Unknown coordination rule kind.");
            Kind = kind;
            Severity = RequiredToken(severity, nameof(severity));
            if (double.IsNaN(clearance) || double.IsInfinity(clearance) || clearance < 0d)
                throw new ArgumentOutOfRangeException(nameof(clearance), "Clearance must be finite and non-negative.");
            if (kind == CoordinationRuleKind.HardClash && clearance != 0d)
                throw new ArgumentException("Hard-clash rules must use zero clearance.", nameof(clearance));
            Clearance = clearance;
            Enabled = enabled;
        }

        public string RuleId { get; }
        public int RuleVersion { get; }
        public string LeftCategory { get; }
        public string RightCategory { get; }
        public CoordinationRuleKind Kind { get; }
        public string Severity { get; }
        public double Clearance { get; }
        public bool Enabled { get; }

        internal int Specificity
        {
            get
            {
                var value = 0;
                if (!IsWildcard(LeftCategory)) value++;
                if (!IsWildcard(RightCategory)) value++;
                return value;
            }
        }

        internal bool Matches(string leftCategory, string rightCategory)
        {
            return (CategoryMatches(LeftCategory, leftCategory) && CategoryMatches(RightCategory, rightCategory)) ||
                   (CategoryMatches(LeftCategory, rightCategory) && CategoryMatches(RightCategory, leftCategory));
        }

        private static bool CategoryMatches(string ruleCategory, string actualCategory)
        {
            return IsWildcard(ruleCategory) || string.Equals(ruleCategory, actualCategory, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWildcard(string category)
        {
            return string.Equals(category, WildcardCategory, StringComparison.Ordinal);
        }

        internal static string NormalizeCategory(string value, string parameterName)
        {
            var normalized = Category(value, parameterName);
            if (IsWildcard(normalized))
                throw new ArgumentException("Actual coordination category cannot be the rule wildcard '*'.", parameterName);
            return normalized;
        }

        private static string Category(string value, string parameterName)
        {
            var normalized = RequiredToken(value, parameterName);
            if (normalized.IndexOf('*') >= 0 && !IsWildcard(normalized))
                throw new ArgumentException("Category wildcard must be exactly '*'.", parameterName);
            return normalized;
        }

        private static string RequiredToken(string value, string parameterName)
        {
            var raw = value ?? string.Empty;
            if (raw.Any(char.IsControl)) throw new ArgumentException("Control characters are not allowed.", parameterName);
            var normalized = raw.Trim();
            if (normalized.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            return normalized;
        }
    }

    internal static class CoordinationRuleCollectionContract
    {
        internal const int MaximumEntries = 10000;

        internal static T[] MaterializeBounded<T>(IEnumerable<T> items, string collectionLabel)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            var hasKnownCount = TryGetKnownCount(items, out var knownCount);
            if (hasKnownCount && knownCount > MaximumEntries)
                ThrowTooManyEntries(collectionLabel);

            var snapshot = hasKnownCount ? new List<T>(knownCount) : new List<T>();
            var observedCount = 0;
            using (var enumerator = items.GetEnumerator())
            {
                while (true)
                {
                    if (hasKnownCount)
                        RequireStableKnownCount(items, knownCount, collectionLabel);

                    var moved = enumerator.MoveNext();

                    if (hasKnownCount)
                        RequireStableKnownCount(items, knownCount, collectionLabel);
                    if (!moved)
                        break;

                    if (hasKnownCount && observedCount >= knownCount)
                    {
                        throw new InvalidOperationException(
                            collectionLabel + " traversal produced more entries than its admitted known count of " + knownCount + ".");
                    }
                    if (observedCount >= MaximumEntries)
                        ThrowTooManyEntries(collectionLabel);

                    var item = enumerator.Current;
                    if (hasKnownCount)
                        RequireStableKnownCount(items, knownCount, collectionLabel);
                    snapshot.Add(item);
                    observedCount++;
                }
            }

            if (hasKnownCount && knownCount != observedCount)
            {
                throw new InvalidOperationException(
                    collectionLabel + " traversal produced " + observedCount +
                    " entries but its known count reported " + knownCount + ".");
            }

            if (hasKnownCount)
                RequireStableKnownCount(items, knownCount, collectionLabel);

            return snapshot.ToArray();
        }

        private static void RequireStableKnownCount<T>(IEnumerable<T> items, int admittedCount, string collectionLabel)
        {
            var stillHasKnownCount = TryGetKnownCount(items, out var reboundKnownCount);
            if (!stillHasKnownCount || reboundKnownCount != admittedCount)
            {
                throw new InvalidOperationException(
                    collectionLabel + " known Count changed during traversal from " + admittedCount +
                    " to " + (stillHasKnownCount ? reboundKnownCount.ToString(CultureInfo.InvariantCulture) : "<unavailable>") + ".");
            }
        }

        private static bool TryGetKnownCount<T>(IEnumerable<T> items, out int count)
        {
            var counts = new List<int>(3);
            if (items is ICollection<T> collection)
                counts.Add(collection.Count);
            if (items is IReadOnlyCollection<T> readOnlyCollection)
                counts.Add(readOnlyCollection.Count);
            if (items is ICollection nonGenericCollection)
                counts.Add(nonGenericCollection.Count);

            if (counts.Count == 0)
            {
                count = 0;
                return false;
            }

            count = counts[0];
            var maximumCount = count;
            var hasConflict = false;
            var hasNegative = count < 0;
            for (var i = 1; i < counts.Count; i++)
            {
                if (counts[i] < 0)
                    hasNegative = true;
                if (counts[i] != count)
                    hasConflict = true;
                if (counts[i] > maximumCount)
                    maximumCount = counts[i];
            }

            if (maximumCount > MaximumEntries)
            {
                count = maximumCount;
                return true;
            }

            if (hasNegative)
                throw new InvalidOperationException(collectionLabelForCountError + " reports an invalid negative known count.");

            if (hasConflict)
                throw new InvalidOperationException(collectionLabelForCountError + " reports conflicting known counts.");

            return true;
        }

        private const string collectionLabelForCountError = "Coordination collection";

        private static void ThrowTooManyEntries(string collectionLabel)
        {
            throw new InvalidOperationException(
                collectionLabel + " supports at most " + MaximumEntries + " entries.");
        }
    }

    /// <summary>
    /// Immutable profile. Profile and rule versions are carried into every resolution so
    /// downstream issue/workbook projections can retain the exact classification provenance.
    /// </summary>
    public sealed class CoordinationRuleProfile
    {
        private readonly ReadOnlyCollection<CoordinationRule> _rules;

        public CoordinationRuleProfile(string profileId, int profileVersion, IEnumerable<CoordinationRule> rules)
        {
            ProfileId = Required(profileId, nameof(profileId));
            if (profileVersion <= 0) throw new ArgumentOutOfRangeException(nameof(profileVersion), "Profile version must be positive.");
            ProfileVersion = profileVersion;
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            var snapshot = CoordinationRuleCollectionContract.MaterializeBounded(rules, "Coordination rule profile");
            if (snapshot.Any(rule => rule == null))
                throw new ArgumentException("Rule profile cannot contain null rules.", nameof(rules));

            var duplicate = snapshot
                .GroupBy(rule => rule.RuleId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new ArgumentException("Rule profile contains duplicate RuleId: " + duplicate.Key + ".", nameof(rules));

            _rules = Array.AsReadOnly(snapshot);
        }

        public string ProfileId { get; }
        public int ProfileVersion { get; }
        public IReadOnlyList<CoordinationRule> Rules => _rules;

        public CoordinationRuleResolution? Resolve(string leftCategory, string rightCategory)
        {
            var left = CoordinationRule.NormalizeCategory(leftCategory, nameof(leftCategory));
            var right = CoordinationRule.NormalizeCategory(rightCategory, nameof(rightCategory));

            var candidates = _rules
                .Where(rule => rule.Enabled && rule.Matches(left, right))
                .ToArray();
            if (candidates.Length == 0) return null;

            var specificity = candidates.Max(rule => rule.Specificity);
            var best = candidates.Where(rule => rule.Specificity == specificity).ToArray();
            if (best.Length != 1)
            {
                var ids = string.Join(", ", best.Select(rule => rule.RuleId).OrderBy(id => id, StringComparer.OrdinalIgnoreCase));
                throw new InvalidOperationException(
                    "Ambiguous coordination rules for category pair '" + left + "'/'" + right +
                    "' at specificity " + specificity.ToString(CultureInfo.InvariantCulture) + ": " + ids + ".");
            }

            return new CoordinationRuleResolution(ProfileId, ProfileVersion, best[0]);
        }

        private static string Required(string value, string parameterName)
        {
            var raw = value ?? string.Empty;
            if (raw.Any(char.IsControl)) throw new ArgumentException("Control characters are not allowed.", parameterName);
            var normalized = raw.Trim();
            if (normalized.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            if (!string.Equals(raw, normalized, StringComparison.Ordinal))
                throw new ArgumentException("Value must not contain leading or trailing whitespace.", parameterName);
            return raw;
        }
    }

    public sealed class CoordinationRuleResolution
    {
        internal CoordinationRuleResolution(string profileId, int profileVersion, CoordinationRule rule)
        {
            ProfileId = profileId;
            ProfileVersion = profileVersion;
            RuleId = rule.RuleId;
            RuleVersion = rule.RuleVersion;
            Kind = rule.Kind;
            Severity = rule.Severity;
            Clearance = rule.Clearance;
        }

        public string ProfileId { get; }
        public int ProfileVersion { get; }
        public string RuleId { get; }
        public int RuleVersion { get; }
        public CoordinationRuleKind Kind { get; }
        public string Severity { get; }
        public double Clearance { get; }
    }
}