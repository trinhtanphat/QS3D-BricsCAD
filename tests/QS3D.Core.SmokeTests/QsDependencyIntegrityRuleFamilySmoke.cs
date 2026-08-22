using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class QsDependencyIntegrityRuleFamilySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var profile = QsDependencyIntegrityRuleFamily.CreateProfile();
            Equal(8, profile.Rules.Count, "rule count");

            var project = new ProjectState("QSC-DEPENDENCY", "Dependency integrity");
            var element = new ProjectElement("A", ElementCategory.CustomQuantity);
            element.DependsOn.Add("MISSING");
            project.Elements.Add(element);

            var issue = new DependencyHealthService().Inspect(project)
                .Single(x => string.Equals(x.Code, "DEPENDENCY_TARGET_MISSING", StringComparison.Ordinal));
            Equal(HealthSeverity.Error, issue.Severity, "emitted severity");
            Equal("A", issue.ElementId, "affected identity");
            RequireResolved(profile, issue);

            var codes = new[]
            {
                "DEPENDENCY_ELEMENT_ID_DUPLICATE",
                "DEPENDENCY_TARGET_NON_CANONICAL",
                "DEPENDENCY_TARGET_DUPLICATE",
                "DEPENDENCY_TARGET_AMBIGUOUS",
                "DEPENDENCY_TARGET_BLANK",
                "DEPENDENCY_TARGET_MISSING",
                "DEPENDENCY_SELF_REFERENCE",
                "DEPENDENCY_CYCLE"
            };
            foreach (var code in codes)
                RequireResolved(profile, new ModelHealthIssue(code, HealthSeverity.Error, "Dependency integrity finding.", "A"));

            if (profile.Resolve(new ModelHealthIssue("MISSING_FAMILY", HealthSeverity.Error, "Unrelated.", "A")) != null)
                throw new InvalidOperationException("Unrelated health code must remain unmapped.");
        }

        private static void RequireResolved(QsRuleProfile profile, ModelHealthIssue issue)
        {
            var rule = profile.Resolve(issue);
            if (rule == null)
                throw new InvalidOperationException("Missing dependency QSC mapping: " + issue.Code + ".");
            Equal(issue.Code, rule.HealthIssueCode, issue.Code + " mapped code");
            Equal(HealthSeverity.Error, rule.Severity, issue.Code + " mapped severity");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }
    }
}
