using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BomNegativeQuantityReleaseGuardSmoke
    {
        public static void Run()
        {
            CanonicalNegativeQuantityIsAttributed();
            MalformedKeyKeepsKeyDiagnosticPrecedence();
            ZeroAndPositiveQuantitiesRemainValid();
        }

        private static void CanonicalNegativeQuantityIsAttributed()
        {
            var project = ProjectWithBeam("bom-negative", "beam-negative");
            var element = project.Elements[0];
            element.Quantities["FormworkM2"] = -0.25d;

            var issues = BomReleaseGuardService.Inspect(project);
            var matches = issues.Where(x => x.Code == "BOM_QUANTITY_NEGATIVE").ToList();
            if (matches.Count != 1)
                throw new Exception("Expected exactly one BOM_QUANTITY_NEGATIVE issue.");
            var issue = matches[0];
            if (issue.Severity != HealthSeverity.Error || issue.ElementId != element.Id)
                throw new Exception("Negative quantity must be an Error-level blocker attributed to its owning element.");
            if (issue.Message != "Quantity FormworkM2 không được âm.")
                throw new Exception("Negative quantity diagnostic must identify the canonical quantity key.");
            if (!issues.Any(x => x.Code == "BOM_REPORT_FAILED"))
                throw new Exception("Existing fail-closed report construction must remain in force for negative quantities.");
        }

        private static void MalformedKeyKeepsKeyDiagnosticPrecedence()
        {
            var project = ProjectWithBeam("bom-negative-bad-key", "beam-negative-bad-key", addCanonicalQuantity: false);
            project.Elements[0].Quantities[" BadQuantity "] = -1d;

            var issues = BomReleaseGuardService.Inspect(project);
            if (issues.Count(x => x.Code == "BOM_QUANTITY_KEY_INVALID") != 1)
                throw new Exception("Malformed negative quantity key must retain BOM_QUANTITY_KEY_INVALID precedence.");
            if (issues.Any(x => x.Code == "BOM_QUANTITY_NEGATIVE"))
                throw new Exception("Malformed quantity keys must not emit value diagnostics that reflect an invalid key.");
        }

        private static void ZeroAndPositiveQuantitiesRemainValid()
        {
            var project = ProjectWithBeam("bom-nonnegative", "beam-nonnegative", addCanonicalQuantity: false);
            var element = project.Elements[0];
            element.Quantities["FormworkM2"] = 0d;
            element.Quantities["NetConcreteM3"] = 1.25d;

            var issues = BomReleaseGuardService.Inspect(project);
            if (issues.Any(x => x.Code == "BOM_QUANTITY_NEGATIVE"))
                throw new Exception("Zero and positive quantities must not be classified as negative.");
            if (issues.Any(x => x.Code == "BOM_REPORT_FAILED"))
                throw new Exception("Valid non-negative quantities must remain reportable.");
        }

        private static ProjectState ProjectWithBeam(string projectId, string elementId, bool addCanonicalQuantity = true)
        {
            var project = new ProjectState(projectId, "BOM negative quantity guard");
            project.Families.Add(new ProjectFamily("beam", "Beam", ElementCategory.Beam));
            var element = new ProjectElement(elementId, ElementCategory.Beam, "beam", string.Empty, string.Empty);
            element.SourceHandles.Add("1A");
            if (addCanonicalQuantity) element.SetQuantity("NetConcreteM3", 1.25d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return project;
        }
    }
}
