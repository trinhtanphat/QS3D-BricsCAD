using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class QsHostOpeningIntegrityRuleFamilySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProfileIsDeterministicAndBounded();
            RealHostFindingsResolveThroughExistingHealthCodes();
        }

        private static void ProfileIsDeterministicAndBounded()
        {
            var profile = QsHostOpeningIntegrityRuleFamily.Profile;
            Equal("QSC.HOST-OPENING.INTEGRITY.V1", profile.ProfileId, "profile id");
            Equal(5, profile.Rules.Count, "host/opening rule count");
            Equal("QSC.HOST.AMBIGUOUS", profile.Rules[0].RuleId, "rule 0");
            Equal("QSC.HOST.CATEGORY", profile.Rules[1].RuleId, "rule 1");
            Equal("QSC.HOST.INVALID", profile.Rules[2].RuleId, "rule 2");
            Equal("QSC.HOST.MISSING", profile.Rules[3].RuleId, "rule 3");
            Equal("QSC.HOST.NON_CANONICAL", profile.Rules[4].RuleId, "rule 4");
        }

        private static void RealHostFindingsResolveThroughExistingHealthCodes()
        {
            var project = new ProjectState("QSC-HOST", "QSC host/opening integrity smoke");

            var wall = new ProjectElement("WALL-1", ElementCategory.ArchitecturalWall);
            var room = new ProjectElement("ROOM-1", ElementCategory.Room);
            var duplicateWallA = new ProjectElement("DUP-WALL", ElementCategory.ArchitecturalWall);
            var duplicateWallB = new ProjectElement("DUP-WALL", ElementCategory.StructuralWall);

            var missingDoor = new ProjectElement("D-MISSING", ElementCategory.Door);

            var invalidOpening = new ProjectElement("O-INVALID", ElementCategory.WallOpening);
            invalidOpening.SetProperty("HostWallId", "NO-SUCH-WALL");

            var categoryDoor = new ProjectElement("D-CATEGORY", ElementCategory.Door);
            categoryDoor.SetProperty("HostWallId", room.Id);

            var nonCanonicalOpening = new ProjectElement("O-NONCANONICAL", ElementCategory.WallOpening);
            nonCanonicalOpening.SetProperty("HostWallId", " " + wall.Id + " ");

            var ambiguousDoor = new ProjectElement("D-AMBIGUOUS", ElementCategory.Door);
            ambiguousDoor.SetProperty("HostWallId", duplicateWallA.Id);

            project.Elements.Add(wall);
            project.Elements.Add(room);
            project.Elements.Add(duplicateWallA);
            project.Elements.Add(duplicateWallB);
            project.Elements.Add(missingDoor);
            project.Elements.Add(invalidOpening);
            project.Elements.Add(categoryDoor);
            project.Elements.Add(nonCanonicalOpening);
            project.Elements.Add(ambiguousDoor);

            var issues = new ModelHealthService().Inspect(project);

            AssertResolved(issues, "MISSING_HOST", "QSC.HOST.MISSING", missingDoor.Id);
            AssertResolved(issues, "INVALID_HOST", "QSC.HOST.INVALID", invalidOpening.Id);
            AssertResolved(issues, "INVALID_HOST_CATEGORY", "QSC.HOST.CATEGORY", categoryDoor.Id);
            AssertResolved(issues, "HOST_REFERENCE_NON_CANONICAL", "QSC.HOST.NON_CANONICAL", nonCanonicalOpening.Id);
            AssertResolved(issues, "AMBIGUOUS_HOST", "QSC.HOST.AMBIGUOUS", ambiguousDoor.Id);

            var unrelated = issues.FirstOrDefault(x => string.Equals(x.Code, "MISSING_FAMILY", StringComparison.Ordinal));
            if (unrelated == null)
                throw new InvalidOperationException("Smoke fixture must also emit an unrelated health finding.");
            if (QsHostOpeningIntegrityRuleFamily.Profile.Resolve(unrelated) != null)
                throw new InvalidOperationException("Host/opening profile must not absorb unrelated semantic-readiness findings.");
        }

        private static void AssertResolved(
            IReadOnlyList<ModelHealthIssue> issues,
            string healthCode,
            string expectedRuleId,
            string expectedElementId)
        {
            var matches = issues.Where(x => string.Equals(x.Code, healthCode, StringComparison.Ordinal)).ToList();
            Equal(1, matches.Count, healthCode + " finding count");

            var issue = matches[0];
            Equal(expectedElementId, issue.ElementId, healthCode + " element id");

            var rule = QsHostOpeningIntegrityRuleFamily.Profile.Resolve(issue);
            if (rule == null)
                throw new InvalidOperationException("Expected host/opening rule mapping for health code " + healthCode + ".");
            Equal(expectedRuleId, rule.RuleId, healthCode + " rule id");
            Equal(issue.Severity, rule.Severity, healthCode + " severity parity");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }
    }
}
