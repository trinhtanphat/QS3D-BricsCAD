using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class QsSemanticReadinessRuleFamilySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var first = QsSemanticReadinessRuleFamily.CreateProfile();
            var second = QsSemanticReadinessRuleFamily.CreateProfile();

            Equal(QsSemanticReadinessRuleFamily.ProfileId, first.ProfileId, "profile id");
            Equal(13, first.Rules.Count, "rule count");
            Equal(first.Rules.Count, second.Rules.Count, "repeat rule count");
            for (var i = 0; i < first.Rules.Count; i++)
            {
                Equal(first.Rules[i].RuleId, second.Rules[i].RuleId, "rule id " + i);
                Equal(first.Rules[i].HealthIssueCode, second.Rules[i].HealthIssueCode, "health code " + i);
                Equal(first.Rules[i].Severity, second.Rules[i].Severity, "severity " + i);
                Equal(first.Rules[i].Explanation, second.Rules[i].Explanation, "explanation " + i);
            }

            Resolve(first, "AMBIGUOUS_FAMILY", HealthSeverity.Error);
            Resolve(first, "FAMILY_CATEGORY_MISMATCH", HealthSeverity.Warning);
            Resolve(first, "MISSING_FAMILY", HealthSeverity.Error);
            Resolve(first, "FAMILY_REFERENCE_NON_CANONICAL", HealthSeverity.Error);
            Resolve(first, "AMBIGUOUS_FLOOR", HealthSeverity.Error);
            Resolve(first, "MISSING_FLOOR", HealthSeverity.Warning);
            Resolve(first, "FLOOR_REFERENCE_NON_CANONICAL", HealthSeverity.Error);
            Resolve(first, "AMBIGUOUS_ZONE", HealthSeverity.Error);
            Resolve(first, "MISSING_ZONE", HealthSeverity.Warning);
            Resolve(first, "ZONE_REFERENCE_NON_CANONICAL", HealthSeverity.Error);
            Resolve(first, "MISSING_MATERIAL", HealthSeverity.Warning);
            Resolve(first, "MISSING_DIMENSION", HealthSeverity.Error);
            Resolve(first, "INVALID_DIMENSION", HealthSeverity.Error);

            if (first.Resolve(new ModelHealthIssue("ORPHAN_HANDLE", HealthSeverity.Error, "Unrelated.", "E-QSC")) != null)
                throw new InvalidOperationException("Unrelated health families must remain unmapped.");
        }

        private static void Resolve(QsRuleProfile profile, string code, HealthSeverity severity)
        {
            var issue = new ModelHealthIssue(code, severity, "Existing semantic health finding.", "E-QSC");
            var rule = profile.Resolve(issue);
            if (rule == null)
                throw new InvalidOperationException("Missing QSC rule for health code: " + code + ".");
            Equal(code, rule.HealthIssueCode, code + " mapped code");
            Equal(severity, rule.Severity, code + " mapped severity");
            Equal("E-QSC", issue.ElementId, code + " affected identity");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }
    }
}
