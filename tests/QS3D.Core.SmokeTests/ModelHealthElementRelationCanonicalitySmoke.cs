using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthElementRelationCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedFamilyNormalizesBeforeHealth();
            PaddedFloorNormalizesBeforeHealth();
            PaddedZoneNormalizesBeforeHealth();
            WhitespaceOnlyFamilyBecomesMissing();
            CanonicalRelationsDoNotEmitCanonicalityErrors();
        }

        private static void PaddedFamilyNormalizesBeforeHealth()
        {
            var setup = Create("FAMILY-PAD");
            setup.Element.FamilyId = " F1 ";
            Equal("F1", setup.Element.FamilyId);
            EnsureNoRelationCanonicality(new ModelHealthService().Inspect(setup.Project), "Padded FamilyId setter input");
        }

        private static void PaddedFloorNormalizesBeforeHealth()
        {
            var setup = Create("FLOOR-PAD");
            setup.Element.FloorId = " L1 ";
            Equal("L1", setup.Element.FloorId);
            EnsureNoRelationCanonicality(new ModelHealthService().Inspect(setup.Project), "Padded FloorId setter input");
        }

        private static void PaddedZoneNormalizesBeforeHealth()
        {
            var setup = Create("ZONE-PAD");
            setup.Element.ZoneId = " Z1 ";
            Equal("Z1", setup.Element.ZoneId);
            EnsureNoRelationCanonicality(new ModelHealthService().Inspect(setup.Project), "Padded ZoneId setter input");
        }

        private static void WhitespaceOnlyFamilyBecomesMissing()
        {
            var setup = Create("FAMILY-BLANK");
            setup.Element.FamilyId = "   ";
            Equal(string.Empty, setup.Element.FamilyId);
            var issues = new ModelHealthService().Inspect(setup.Project);
            RequireIssue(issues, setup.Element.Id, "MISSING_FAMILY");
            EnsureNoRelationCanonicality(issues, "Whitespace-only FamilyId setter input");
        }

        private static void CanonicalRelationsDoNotEmitCanonicalityErrors()
        {
            var setup = Create("CANONICAL");
            EnsureNoRelationCanonicality(
                new ModelHealthService().Inspect(setup.Project),
                "Canonical element relations");
        }

        private static void EnsureNoRelationCanonicality(
            System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues,
            string label)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, "FAMILY_REFERENCE_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "FLOOR_REFERENCE_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "ZONE_REFERENCE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException(label + " must not produce relation canonicality errors.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-REL-" + suffix, "Relation canonicality smoke");
            ProjectFamilyService.Create(project, "F1", "Grid Family", ElementCategory.Grid);
            ProjectFloorService.Create(project, "L1", "Level 1", 0d);
            ProjectZoneService.Create(project, "Z1", "Zone 1");
            var element = new ProjectElement("E-REL-" + suffix, ElementCategory.Grid, "F1", "L1", "Z1");
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void RequireIssue(
            System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues,
            string elementId,
            string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Model Health relation issue was not reported: " + code + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Expected stored relation " + expected + " but got " + actual + ".");
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectElement Element { get; }
        }
    }
}
