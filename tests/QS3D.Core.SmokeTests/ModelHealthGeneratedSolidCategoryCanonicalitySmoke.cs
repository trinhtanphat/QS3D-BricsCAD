using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthGeneratedSolidCategoryCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CaseVariantFailsVisible();
            PaddedCategoryFailsVisible();
            NumericAliasFailsVisible();
            CanonicalMismatchStillFailsVisible();
            CanonicalCategoryDoesNotEmitCanonicalityError();
        }

        private static void CaseVariantFailsVisible()
        {
            var setup = Create("CASE");
            setup.Element.Properties["GeneratedSolidCategory"] = ElementCategory.Grid.ToString().ToLowerInvariant();
            RequireIssue(setup.Project, setup.Element.Id, "GENERATED_CATEGORY_NON_CANONICAL");
        }

        private static void PaddedCategoryFailsVisible()
        {
            var setup = Create("PAD");
            setup.Element.Properties["GeneratedSolidCategory"] = " " + ElementCategory.Grid + " ";
            RequireIssue(setup.Project, setup.Element.Id, "GENERATED_CATEGORY_NON_CANONICAL");
        }

        private static void NumericAliasFailsVisible()
        {
            var setup = Create("NUMERIC");
            setup.Element.Properties["GeneratedSolidCategory"] = ((int)ElementCategory.Grid).ToString(CultureInfo.InvariantCulture);
            RequireIssue(setup.Project, setup.Element.Id, "GENERATED_CATEGORY_NON_CANONICAL");
        }

        private static void CanonicalMismatchStillFailsVisible()
        {
            var setup = Create("MISMATCH");
            setup.Element.Properties["GeneratedSolidCategory"] = ElementCategory.Beam.ToString();
            var issues = new ModelHealthService().Inspect(setup.Project);
            RequireIssue(issues, setup.Element.Id, "GENERATED_CATEGORY_MISMATCH");
            if (issues.Any(x => string.Equals(x.Code, "GENERATED_CATEGORY_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("A canonical category token for a different category must remain a mismatch, not a canonicality error.");
        }

        private static void CanonicalCategoryDoesNotEmitCanonicalityError()
        {
            var setup = Create("CANONICAL");
            var issues = new ModelHealthService().Inspect(setup.Project);
            if (issues.Any(x => string.Equals(x.Code, "GENERATED_CATEGORY_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Canonical GeneratedSolidCategory metadata must not produce a canonicality error.");
        }

        private static Setup Create(string suffix)
        {
            var project = new ProjectState("P-GSOLID-CATEGORY-" + suffix, "Generated Solid category canonicality smoke");
            var element = new ProjectElement("E-GSOLID-CATEGORY-" + suffix, ElementCategory.Grid);
            element.Properties["GeneratedSolidHandle"] = "A";
            element.Properties["GeneratedSolidCategory"] = ElementCategory.Grid.ToString();
            element.Properties["GeneratedSolidOwnershipVersion"] = "1";
            element.Properties["GeneratedSolidOwnerProjectId"] = project.ProjectId;
            element.Properties["GeneratedSolidOwnerElementId"] = element.Id;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void RequireIssue(ProjectState project, string elementId, string code)
        {
            RequireIssue(new ModelHealthService().Inspect(project), elementId, code);
        }

        private static void RequireIssue(System.Collections.Generic.IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Generated Solid category health issue was not reported: " + code + ".");
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
