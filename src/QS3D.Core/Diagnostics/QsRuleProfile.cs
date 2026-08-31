using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Diagnostics
{
    public sealed class QsRuleDefinition
    {
        public QsRuleDefinition(string ruleId, string healthIssueCode, HealthSeverity severity, string explanation)
        {
            RuleId = RequireIdentity(ruleId, nameof(ruleId));
            HealthIssueCode = RequireIdentity(healthIssueCode, nameof(healthIssueCode));
            if (!Enum.IsDefined(typeof(HealthSeverity), severity))
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "QS rule severity must be defined.");
            Severity = severity;
            Explanation = RequireExplanation(explanation);
        }

        public string RuleId { get; }
        public string HealthIssueCode { get; }
        public HealthSeverity Severity { get; }
        public string Explanation { get; }

        private static string RequireIdentity(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("QS rule/profile identity is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("QS rule/profile identity must be canonical without surrounding whitespace.", parameterName);

            foreach (var ch in value)
            {
                if (char.IsControl(ch) || char.IsWhiteSpace(ch) ||
                    !(char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_' || ch == ':'))
                    throw new ArgumentException("QS rule/profile identity contains unsupported characters.", parameterName);
            }

            return value;
        }

        internal static string RequireProfileId(string value) => RequireIdentity(value, nameof(value));

        private static string RequireExplanation(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("QS rule explanation is required.", nameof(value));

            var normalized = value.Trim();
            if (normalized.Any(char.IsControl))
                throw new ArgumentException("QS rule explanation cannot contain control characters.", nameof(value));
            return normalized;
        }
    }

    public sealed class QsRuleProfile
    {
        private const int MaximumRules = 10000;
        private readonly IReadOnlyList<QsRuleDefinition> _rules;
        private readonly Dictionary<string, QsRuleDefinition> _byHealthIssueCode;

        public QsRuleProfile(string profileId, IEnumerable<QsRuleDefinition> rules)
        {
            ProfileId = QsRuleDefinition.RequireProfileId(profileId);
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            var materialized = MaterializeRules(rules);
            if (materialized.Any(rule => rule == null))
                throw new ArgumentException("QS rule profile cannot contain null rules.", nameof(rules));

            var duplicateRuleId = materialized
                .GroupBy(rule => rule.RuleId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateRuleId != null)
                throw new ArgumentException("Duplicate QS rule id: " + duplicateRuleId.Key, nameof(rules));

            var duplicateHealthCode = materialized
                .GroupBy(rule => rule.HealthIssueCode, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateHealthCode != null)
                throw new ArgumentException("Multiple QS rules map to health issue code: " + duplicateHealthCode.Key, nameof(rules));

            var ordered = materialized
                .OrderBy(rule => rule.RuleId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(rule => rule.RuleId, StringComparer.Ordinal)
                .ToArray();

            _rules = Array.AsReadOnly(ordered);
            _byHealthIssueCode = ordered.ToDictionary(
                rule => rule.HealthIssueCode,
                rule => rule,
                StringComparer.OrdinalIgnoreCase);
        }

        public string ProfileId { get; }
        public IReadOnlyList<QsRuleDefinition> Rules => _rules;

        public bool TryResolve(ModelHealthIssue issue, out QsRuleDefinition? rule)
        {
            if (issue == null) throw new ArgumentNullException(nameof(issue));
            return _byHealthIssueCode.TryGetValue(issue.Code, out rule);
        }

        public QsRuleDefinition? Resolve(ModelHealthIssue issue)
        {
            return TryResolve(issue, out var rule) ? rule : null;
        }

        private static List<QsRuleDefinition> MaterializeRules(IEnumerable<QsRuleDefinition> rules)
        {
            var admittedCount = ReadKnownCount(rules);
            var materialized = new List<QsRuleDefinition>();
            using (var enumerator = rules.GetEnumerator())
            {
                RequireKnownCountStable(rules, admittedCount);
                while (true)
                {
                    RequireKnownCountStable(rules, admittedCount);
                    var moved = enumerator.MoveNext();
                    RequireKnownCountStable(rules, admittedCount);
                    if (!moved) break;

                    if (admittedCount.HasValue && materialized.Count >= admittedCount.Value)
                        throw new InvalidOperationException("QS rule profile enumerated more rules than its reported Count " + admittedCount.Value + ".");
                    if (materialized.Count >= MaximumRules)
                        throw new InvalidOperationException("QS rule profile supports at most " + MaximumRules + " rules.");

                    var current = enumerator.Current;
                    RequireKnownCountStable(rules, admittedCount);
                    materialized.Add(current);
                }
            }

            RequireKnownCountStable(rules, admittedCount);
            if (admittedCount.HasValue && materialized.Count != admittedCount.Value)
                throw new InvalidOperationException(
                    "QS rule profile reported Count " + admittedCount.Value + " but enumerated " + materialized.Count + " rules.");
            return materialized;
        }

        private static void RequireKnownCountStable(IEnumerable<QsRuleDefinition> rules, int? admittedCount)
        {
            var currentCount = ReadKnownCount(rules);
            if (currentCount != admittedCount)
                throw new InvalidOperationException("QS rule profile rule Count changed during materialization.");
        }

        private static int? ReadKnownCount(IEnumerable<QsRuleDefinition> rules)
        {
            var counts = new List<int>(3);
            if (rules is ICollection<QsRuleDefinition> collection) counts.Add(collection.Count);
            if (rules is IReadOnlyCollection<QsRuleDefinition> readOnlyCollection) counts.Add(readOnlyCollection.Count);
            if (rules is ICollection nonGenericCollection) counts.Add(nonGenericCollection.Count);

            if (counts.Any(count => count < 0))
                throw new InvalidOperationException("QS rule profile reported a negative rule Count.");
            if (counts.Any(count => count > MaximumRules))
                throw new InvalidOperationException("QS rule profile supports at most " + MaximumRules + " rules.");
            if (counts.Count > 1 && counts.Any(count => count != counts[0]))
                throw new InvalidOperationException("QS rule profile reported conflicting rule Count values.");
            return counts.Count == 0 ? (int?)null : counts[0];
        }
    }
}
