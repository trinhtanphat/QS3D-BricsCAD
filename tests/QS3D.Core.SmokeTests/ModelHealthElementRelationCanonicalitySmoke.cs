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
            PaddedFamilyFailsVisible();
            PaddedFloorFailsVisible();
            PaddedZoneFailsVisible();
            WhitespaceOnlyRelationFailsVisible();
            CanonicalRelationsDoNotEmitCanonicalityErrors();
        }

        private static void PaddedFamilyFailsVisible()
        {
            var setup = Create("FAMILY-PAD");
            setup.Element.FamilyId = " F1 ";
            RequireIssue(setup.Project, setup.Element.Id, "FAMILY_REFERENCE_NON_CANONICAL");
        }

        private static void PaddedFloorFailsVisible()
        {
            var setup = Create("FLOOR-PAD");
            setup.Element.FloorId = " L1 ";
            RequireIssue(setup.Project, setup.Element.Id, "FLOOR_REFERENCE_NON_CANONICAL");
        }

        private static void PaddedZoneFailsVisible()
        {
            var setup = Create("ZONE-PAD");
            setup.Element.ZoneId = " Z1 ";
            RequireIssue(setup.Project, setup.Element.Id, "ZONE_REFERENCE_NON_CANONICAL");
        }

        private static void WhitespaceOnlyRelationFailsVisible()
        {
            var setup = Create("FAMILY-BLANK");
            setup.Element.FamilyId = "   ";
            RequireIssue(setup.Project, setup.Element.Id, "FAMILY_REFERENCE_NON_CANONICAL");
        }

        private static void CanonicalRelationsDoNotEmitCanonicalityErrors()
        {
            var setup = Create("CANONICAL");
            var issues = new ModelHealthService().Inspect(setup.Project);
            if (issues.Any(x =>
                string.Equals(x.Code, "FAMILY_REFERENCE_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "FLOOR_REFERENCE_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "ZONE_REFERENCE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Canonical element relations must not produce relation canonicality errors.");
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

        private static void RequireIssue(ProjectState project, string elementId, string code)
        {
            var issues = new ModelHealthService().Inspect(project);
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Model Health relation canonicality error was not reported: " + code + ".");
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
