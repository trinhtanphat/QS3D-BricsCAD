using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingCanonicalityHealthSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedLabelFailsVisible();
            PaddedSequenceFailsVisible();
            LeadingZeroSequenceFailsVisible();
            CanonicalNamingDoesNotEmitCanonicalityErrors();
        }

        private static void PaddedLabelFailsVisible()
        {
            var element = Grid("E-GRID-LABEL-PAD", " A ", "1");
            RequireIssue(element, "GRID_LABEL_NON_CANONICAL");
        }

        private static void PaddedSequenceFailsVisible()
        {
            var element = Grid("E-GRID-SEQ-PAD", "A", " 1 ");
            RequireIssue(element, "GRID_SEQUENCE_NON_CANONICAL");
        }

        private static void LeadingZeroSequenceFailsVisible()
        {
            var element = Grid("E-GRID-SEQ-ZERO", "A", "01");
            RequireIssue(element, "GRID_SEQUENCE_NON_CANONICAL");
        }

        private static void CanonicalNamingDoesNotEmitCanonicalityErrors()
        {
            var project = new ProjectState("P-GRID-CANONICAL", "Grid canonicality smoke");
            project.Elements.Add(Grid("E-GRID-CANONICAL", "A", "1"));

            var issues = new GridNamingHealthService().Inspect(project);
            if (issues.Any(x =>
                string.Equals(x.Code, "GRID_LABEL_NON_CANONICAL", StringComparison.Ordinal) ||
                string.Equals(x.Code, "GRID_SEQUENCE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Canonical Grid naming metadata must not produce canonicality errors.");
        }

        private static ProjectElement Grid(string id, string label, string sequence)
        {
            var element = new ProjectElement(id, ElementCategory.Grid);
            element.Properties[GridNamingService.GridLabelKey] = label;
            element.Properties[GridNamingService.GridSequenceIndexKey] = sequence;
            return element;
        }

        private static void RequireIssue(ProjectElement element, string code)
        {
            var project = new ProjectState("P-" + element.Id, "Grid canonicality smoke");
            project.Elements.Add(element);
            var issues = new GridNamingHealthService().Inspect(project);
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, element.Id, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Grid naming canonicality error was not reported: " + code + ".");
        }
    }
}
