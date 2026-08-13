using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class QsActiveContextIntegrityRuleFamilySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProfileIsDeterministicAndBounded();
            RealActiveContextFindingsResolve();
        }

        private static void ProfileIsDeterministicAndBounded()
        {
            var profile = QsActiveContextIntegrityRuleFamily.Profile;
            Equal("QSC.ACTIVE-CONTEXT.INTEGRITY.V1", profile.ProfileId, "profile id");
            Equal(6, profile.Rules.Count, "rule count");
            Equal("QSC.ACTIVE.FLOOR.AMBIGUOUS", profile.Rules[0].RuleId, "rule 0");
            Equal("QSC.ACTIVE.FLOOR.INVALID", profile.Rules[1].RuleId, "rule 1");
            Equal("QSC.ACTIVE.FLOOR.NON_CANONICAL", profile.Rules[2].RuleId, "rule 2");
            Equal("QSC.ACTIVE.ZONE.AMBIGUOUS", profile.Rules[3].RuleId, "rule 3");
            Equal("QSC.ACTIVE.ZONE.INVALID", profile.Rules[4].RuleId, "rule 4");
            Equal("QSC.ACTIVE.ZONE.NON_CANONICAL", profile.Rules[5].RuleId, "rule 5");
        }

        private static void RealActiveContextFindingsResolve()
        {
            var missing = new ProjectState("ACTIVE-MISSING", "Missing active context");
            missing.Elements.Add(new ProjectElement("E1", ElementCategory.ArchitecturalWall));
            var missingIssues = new ModelHealthService().Inspect(missing);
            AssertResolved(missingIssues, "INVALID_ACTIVE_FLOOR", "QSC.ACTIVE.FLOOR.INVALID", HealthSeverity.Warning);
            AssertResolved(missingIssues, "INVALID_ACTIVE_ZONE", "QSC.ACTIVE.ZONE.INVALID", HealthSeverity.Warning);

            var unrelated = missingIssues.FirstOrDefault(x => string.Equals(x.Code, "MISSING_FAMILY", StringComparison.Ordinal));
            if (unrelated == null)
                throw new InvalidOperationException("Fixture must emit an unrelated semantic-readiness finding.");
            if (QsActiveContextIntegrityRuleFamily.Profile.Resolve(unrelated) != null)
                throw new InvalidOperationException("Active-context profile must not absorb element semantic-readiness findings.");

            var nonCanonical = new ProjectState("ACTIVE-NONCANONICAL", "Non-canonical active context");
            nonCanonical.Zones.Add(new ZoneDefinition("ZONE-1", "Zone 1"));
            nonCanonical.Floors.Add(new FloorDefinition("FLOOR-1", "Floor 1", 0d));
            nonCanonical.ActiveZoneId = "zone-1";
            nonCanonical.ActiveFloorId = "floor-1";
            var nonCanonicalIssues = new ModelHealthService().Inspect(nonCanonical);
            AssertResolved(nonCanonicalIssues, "ACTIVE_FLOOR_NON_CANONICAL", "QSC.ACTIVE.FLOOR.NON_CANONICAL", HealthSeverity.Error);
            AssertResolved(nonCanonicalIssues, "ACTIVE_ZONE_NON_CANONICAL", "QSC.ACTIVE.ZONE.NON_CANONICAL", HealthSeverity.Error);

            var ambiguous = new ProjectState("ACTIVE-AMBIGUOUS", "Ambiguous active context");
            ambiguous.Zones.Add(new ZoneDefinition("ZONE-1", "Zone A"));
            ambiguous.Zones.Add(new ZoneDefinition("ZONE-1", "Zone B"));
            ambiguous.Floors.Add(new FloorDefinition("FLOOR-1", "Floor A", 0d));
            ambiguous.Floors.Add(new FloorDefinition("FLOOR-1", "Floor B", 1d));
            ambiguous.ActiveZoneId = "ZONE-1";
            ambiguous.ActiveFloorId = "FLOOR-1";
            var ambiguousIssues = new ModelHealthService().Inspect(ambiguous);
            AssertResolved(ambiguousIssues, "AMBIGUOUS_ACTIVE_FLOOR", "QSC.ACTIVE.FLOOR.AMBIGUOUS", HealthSeverity.Error);
            AssertResolved(ambiguousIssues, "AMBIGUOUS_ACTIVE_ZONE", "QSC.ACTIVE.ZONE.AMBIGUOUS", HealthSeverity.Error);
        }

        private static void AssertResolved(
            IReadOnlyList<ModelHealthIssue> issues,
            string healthCode,
            string expectedRuleId,
            HealthSeverity expectedSeverity)
        {
            var matches = issues.Where(x => string.Equals(x.Code, healthCode, StringComparison.Ordinal)).ToList();
            Equal(1, matches.Count, healthCode + " finding count");
            var issue = matches[0];
            Equal(expectedSeverity, issue.Severity, healthCode + " source severity");

            var rule = QsActiveContextIntegrityRuleFamily.Profile.Resolve(issue);
            if (rule == null)
                throw new InvalidOperationException("Expected active-context rule mapping for " + healthCode + ".");
            Equal(expectedRuleId, rule.RuleId, healthCode + " rule id");
            Equal(issue.Severity, rule.Severity, healthCode + " profile severity");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }
    }
}
