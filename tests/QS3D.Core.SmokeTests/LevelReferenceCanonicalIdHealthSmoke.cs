using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class LevelReferenceCanonicalIdHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedBottomFailsVisible();
            PaddedTopFailsVisible();
            WhitespaceOnlyTopFailsVisible();
            CanonicalReferencesDoNotEmitCanonicalityErrors();
        }

        private static void PaddedBottomFailsVisible()
        {
            var project = ProjectWithLevels("P-LEVEL-BOTTOM-PAD");
            var element = new ProjectElement("E-BOTTOM-PAD", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = " L1 ";
            project.Elements.Add(element);

            RequireIssue(
                new LevelReferenceHealthService().Inspect(project),
                "BOTTOM_LEVEL_REFERENCE_NON_CANONICAL",
                element.Id);
        }

        private static void PaddedTopFailsVisible()
        {
            var project = ProjectWithLevels("P-LEVEL-TOP-PAD");
            var element = new ProjectElement("E-TOP-PAD", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
            element.Properties[ProjectFloorService.TopLevelIdKey] = " L2 ";
            project.Elements.Add(element);

            RequireIssue(
                new LevelReferenceHealthService().Inspect(project),
                "TOP_LEVEL_REFERENCE_NON_CANONICAL",
                element.Id);
        }

        private static void WhitespaceOnlyTopFailsVisible()
        {
            var project = ProjectWithLevels("P-LEVEL-TOP-BLANK");
            var element = new ProjectElement("E-TOP-BLANK", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
            element.Properties[ProjectFloorService.TopLevelIdKey] = "   ";
            project.Elements.Add(element);

            RequireIssue(
                new LevelReferenceHealthService().Inspect(project),
                "TOP_LEVEL_REFERENCE_NON_CANONICAL",
                element.Id);
        }

        private static void CanonicalReferencesDoNotEmitCanonicalityErrors()
        {
            var project = ProjectWithLevels("P-LEVEL-CANONICAL");
            var element = new ProjectElement("E-CANONICAL", ElementCategory.Beam);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
            element.Properties[ProjectFloorService.TopLevelIdKey] = "L2";
            project.Elements.Add(element);

            var issues = new LevelReferenceHealthService().Inspect(project);
            if (issues.Any(x =>
                string.Equals(x.Code, "BOTTOM_LEVEL_REFERENCE_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "TOP_LEVEL_REFERENCE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Canonical Level references must not produce canonicality errors.");
        }

        private static ProjectState ProjectWithLevels(string id)
        {
            var project = new ProjectState(id, "Level canonicality smoke");
            ProjectFloorService.Create(project, "L1", "Level 1", 0d);
            ProjectFloorService.Create(project, "L2", "Level 2", 3d);
            return project;
        }

        private static void RequireIssue(
            System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues,
            string code,
            string elementId)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Level Reference canonicality error was not reported: " + code + ".");
        }
    }
}
