using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthGeneratedSolidHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedHandleFailsVisibleButKeepsLiveLookup();
            CanonicalHandleDoesNotEmitCanonicalityError();
            LowercaseHexDoesNotEmitSpacingError();
            InvalidHandleKeepsInvalidDiagnostic();
        }

        private static void PaddedHandleFailsVisibleButKeepsLiveLookup()
        {
            var setup = Create("PAD", " A ");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new ModelHealthService().Inspect(setup.Project, null, live);
            RequireIssue(issues, setup.Element.Id, "GENERATED_HANDLE_NON_CANONICAL");
            if (issues.Any(x => string.Equals(x.Code, "GENERATED_SOLID_MISSING", StringComparison.Ordinal)))
                throw new InvalidOperationException("Padded GeneratedSolidHandle must keep downstream live lookup on the trimmed handle.");
        }

        private static void CanonicalHandleDoesNotEmitCanonicalityError()
        {
            var setup = Create("CANONICAL", "A");
            var issues = new ModelHealthService().Inspect(setup.Project);
            if (issues.Any(x => string.Equals(x.Code, "GENERATED_HANDLE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Canonical GeneratedSolidHandle must not produce a spacing canonicality error.");
        }

        private static void LowercaseHexDoesNotEmitSpacingError()
        {
            var setup = Create("LOWER", "a");
            var issues = new ModelHealthService().Inspect(setup.Project);
            if (issues.Any(x => string.Equals(x.Code, "GENERATED_HANDLE_NON_CANONICAL", StringComparison.Ordinal)))
                throw new InvalidOperationException("Generated Solid handle canonicality must not impose hex-letter casing beyond the writer-owned Trim contract.");
        }

        private static void InvalidHandleKeepsInvalidDiagnostic()
        {
            var setup = Create("INVALID", " G ");
            var issues = new ModelHealthService().Inspect(setup.Project);
            RequireIssue(issues, setup.Element.Id, "INVALID_GENERATED_HANDLE");
        }

        private static Setup Create(string suffix, string handle)
        {
            var project = new ProjectState("P-GSOLID-HANDLE-" + suffix, "Generated Solid handle canonicality smoke");
            var element = new ProjectElement("E-GSOLID-HANDLE-" + suffix, ElementCategory.Grid);
            element.Properties["GeneratedSolidHandle"] = handle;
            element.Properties["GeneratedSolidCategory"] = ElementCategory.Grid.ToString();
            element.Properties["GeneratedSolidOwnershipVersion"] = "1";
            element.Properties["GeneratedSolidOwnerProjectId"] = project.ProjectId;
            element.Properties["GeneratedSolidOwnerElementId"] = element.Id;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Generated Solid handle health issue was not reported: " + code + ".");
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
