using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedCurtainFrameHandleCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedHandleFailsVisibleButKeepsLiveLookup();
            CanonicalHandleStaysCanonical();
            EmptyTokenKeepsInvalidPrecedence();
            PaddedDuplicateKeepsDuplicateVisible();
            LowercaseHexDoesNotEmitCanonicality();
        }

        private static void PaddedHandleFailsVisibleButKeepsLiveLookup()
        {
            var setup = Create("PAD", " A ", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = Inspect(setup, live);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_GENERATED_HANDLE_NON_CANONICAL");
            EnsureAbsent(issues, "INVALID_CURTAIN_FRAME_GENERATED_HANDLE", "Padded valid Curtain Frame handles must remain valid after normalization.");
            EnsureAbsent(issues, "CURTAIN_FRAME_GENERATED_SOLID_MISSING", "Trimmed Curtain Frame handles must continue to drive live-solid lookup.");
        }

        private static void CanonicalHandleStaysCanonical()
        {
            var setup = Create("CANONICAL", "A", "1");
            EnsureAbsent(Inspect(setup), "CURTAIN_FRAME_GENERATED_HANDLE_NON_CANONICAL", "Canonical Curtain Frame handles must not produce canonicality evidence.");
        }

        private static void EmptyTokenKeepsInvalidPrecedence()
        {
            var setup = Create("EMPTY", "A;;B", "2");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "INVALID_CURTAIN_FRAME_GENERATED_HANDLE");
            EnsureAbsent(issues, "CURTAIN_FRAME_GENERATED_HANDLE_NON_CANONICAL", "Empty Curtain Frame handle tokens must keep invalid-token precedence without canonicality noise.");
        }

        private static void PaddedDuplicateKeepsDuplicateVisible()
        {
            var setup = Create("DUP", "A; A", "1");
            var issues = Inspect(setup);
            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_GENERATED_HANDLE_NON_CANONICAL");
            RequireIssue(issues, setup.Element.Id, "DUPLICATE_CURTAIN_FRAME_GENERATED_HANDLE");
        }

        private static void LowercaseHexDoesNotEmitCanonicality()
        {
            var setup = Create("LOWER", "a", "1");
            EnsureAbsent(Inspect(setup), "CURTAIN_FRAME_GENERATED_HANDLE_NON_CANONICAL", "Curtain Frame handle canonicality must not impose hex-letter casing.");
        }

        private static Setup Create(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-Curtain-Frame-" + suffix, "Curtain Frame handle canonicality smoke");
            var element = new ProjectElement("Curtain-Frame-" + suffix, ElementCategory.GlassWall);
            element.Properties["GeneratedCurtainFrameHandles"] = handles;
            element.Properties["GeneratedCurtainFrameCount"] = count;
            project.Elements.Add(element);
            return new Setup(project, element);
        }

        private static IReadOnlyList<ModelHealthIssue> Inspect(Setup setup, ISet<string>? live = null) =>
            new GeneratedCurtainFrameHealthService().Inspect(setup.Project, live);

        private static void RequireIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (issues.Any(x =>
                string.Equals(x.Code, code, StringComparison.Ordinal) &&
                x.Severity == HealthSeverity.Error &&
                string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("Expected Curtain Frame health issue was not reported: " + code + ".");
        }

        private static void EnsureAbsent(IReadOnlyList<ModelHealthIssue> issues, string code, string message)
        {
            if (issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new InvalidOperationException(message);
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
