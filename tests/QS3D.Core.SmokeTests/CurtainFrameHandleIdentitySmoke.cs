using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainFrameHandleIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NumericEquivalentLiveHandleIsAccepted();
            NumericEquivalentDuplicateSpellingsAreRejected();
            NumericEquivalentSourceHandleIsRejected();
            NumericEquivalentCrossOwnerConflictIsRejected();
        }

        private static void NumericEquivalentLiveHandleIsAccepted()
        {
            var setup = Create("LIVE-ALIAS", "000A", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var version = setup.Project.ChangeVersion;

            var issues = new GeneratedCurtainFrameHealthService().Inspect(setup.Project, live);

            ForbidIssue(issues, setup.Element.Id, "CURTAIN_FRAME_GENERATED_SOLID_MISSING");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_FRAME_COUNT_MISMATCH");
            RequireUnchangedVersion(setup.Project, version);
        }

        private static void NumericEquivalentDuplicateSpellingsAreRejected()
        {
            var setup = Create("DUP-ALIAS", "A;000A", "1");
            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var issues = new GeneratedCurtainFrameHealthService().Inspect(setup.Project, live);

            RequireIssue(issues, setup.Element.Id, "DUPLICATE_CURTAIN_FRAME_GENERATED_HANDLE");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_FRAME_COUNT_MISMATCH");
            ForbidIssue(issues, setup.Element.Id, "CURTAIN_FRAME_GENERATED_SOLID_MISSING");
        }

        private static void NumericEquivalentSourceHandleIsRejected()
        {
            var setup = Create("SOURCE-ALIAS", "000A", "1");
            setup.Element.SourceHandles.Add("A");
            var issues = new GeneratedCurtainFrameHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_GENERATED_HANDLE_IN_SOURCE");
        }

        private static void NumericEquivalentCrossOwnerConflictIsRejected()
        {
            var setup = Create("OWNER-ALIAS", "000A", "1");
            var other = new ProjectElement("E-CURTAIN-FRAME-OTHER", ElementCategory.GlassWall);
            other.Properties["GeneratedFutureFrameHandles"] = "A";
            setup.Project.Elements.Add(other);

            var issues = new GeneratedCurtainFrameHealthService().Inspect(setup.Project);

            RequireIssue(issues, setup.Element.Id, "CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT");
        }

        private static Setup Create(string suffix, string handles, string count)
        {
            var project = new ProjectState("P-CURTAIN-FRAME-IDENTITY-" + suffix, "Curtain Frame handle identity");
            var element = new ProjectElement("E-CURTAIN-FRAME-IDENTITY-" + suffix, ElementCategory.GlassWall);
            element.Properties["GeneratedCurtainFrameHandles"] = handles;
            element.Properties["GeneratedCurtainFrameCount"] = count;
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
            throw new InvalidOperationException("CurtainFrameHandleIdentitySmoke expected issue was not reported: " + code + ".");
        }

        private static void ForbidIssue(IReadOnlyList<ModelHealthIssue> issues, string elementId, string code)
        {
            if (!issues.Any(x => string.Equals(x.Code, code, StringComparison.Ordinal) && string.Equals(x.ElementId, elementId, StringComparison.Ordinal)))
                return;
            throw new InvalidOperationException("CurtainFrameHandleIdentitySmoke reported unexpected issue: " + code + ".");
        }

        private static void RequireUnchangedVersion(ProjectState project, long version)
        {
            if (project.ChangeVersion == version) return;
            throw new InvalidOperationException("CurtainFrameHandleIdentitySmoke changed ProjectState.ChangeVersion during inspection.");
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