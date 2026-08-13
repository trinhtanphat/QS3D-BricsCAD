using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class QsSemanticReadinessRuleFamilySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProfileIsDeterministic();
            ResolvesExistingSemanticHealthFindings();
        }

        private static void ProfileIsDeterministic()
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
        }

        private static void ResolvesExistingSemanticHealthFindings()
        {
            var project = new ProjectState("QSC-READINESS", "QSC readiness");
            var element = new ProjectElement("E-QSC", ElementCategory.Beam, string.Empty, "MISSING-FLOOR", "MISSING-ZONE")
            {
                FamilyId = "MISSING-FAMILY"
            };
            element.Properties["LengthM"] = "0";
            project.Elements.Add(element);

            var issues = new ModelHealthService().Inspect(project);
            var profile = QsSemanticReadinessRuleFamily.CreateProfile();

            RequireResolved(issues, profile, "MISSING_FAMILY", HealthSeverity.Error);
            RequireResolved(issues, profile, "MISSING_FLOOR", HealthSeverity.Warning);
            RequireResolved(issues, profile, "MISSING_ZONE", HealthSeverity.Warning);
            RequireResolved(issues, profile, "MISSING_MATERIAL", HealthSeverity.Warning);
            RequireResolved(issues, profile, "INVALID_DIMENSION", HealthSeverity.Error);
            RequireResolved(issues, profile, "MISSING_DIMENSION", HealthSeverity.Error);

            if (profile.Resolve(new ModelHealthIssue("ORPHAN_HANDLE", HealthSeverity.Error, "Unrelated.", "E-QSC")) != null)
                throw new InvalidOperationException("Unrelated health families must remain unmapped.");
        }

        private static void RequireResolved(
            System.Collections.Generic.IEnumerable<ModelHealthIssue> issues,
            QsRuleProfile profile,
            string code,
            HealthSeverity severity)
        {
            var issue = issues.FirstOrDefault(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                string.Equals(x.ElementId, "E-QSC", StringComparison.Ordinal));
            if (issue == null)
                throw new InvalidOperationException("Expected Semantic Health finding: " + code + ".");

            Equal(severity, issue.Severity, code + " emitted severity");
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
