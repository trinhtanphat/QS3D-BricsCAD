using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class BomReleaseGuardGeneratedHandleNumericIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NumericEquivalentLiveHandleIsAccepted();
            TrulyMissingLiveHandleIsRejected();
        }

        private static void NumericEquivalentLiveHandleIsAccepted()
        {
            var project = CreateProject();
            var beforeVersion = project.ChangeVersion;
            var liveHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "0x000A" };

            var issues = BomReleaseGuardService.Inspect(project, liveHandles);

            Equal(beforeVersion, project.ChangeVersion);
            Equal(0, MissingHandleIssues(issues).Length);
        }

        private static void TrulyMissingLiveHandleIsRejected()
        {
            var project = CreateProject();
            var beforeVersion = project.ChangeVersion;
            var liveHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "B" };

            var issues = BomReleaseGuardService.Inspect(project, liveHandles);

            Equal(beforeVersion, project.ChangeVersion);
            var missing = MissingHandleIssues(issues);
            Equal(1, missing.Length);
            Equal("BOM-HANDLE-ELEMENT", missing[0].ElementId);
            Equal(HealthSeverity.Error, missing[0].Severity);
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("BOM-HANDLE-NUMERIC", "BOM generated handle numeric identity");
            var element = new ProjectElement("BOM-HANDLE-ELEMENT", ElementCategory.Beam);
            element.Properties["GeneratedSolidHandle"] = "A";
            project.Elements.Add(element);
            return project;
        }

        private static ModelHealthIssue[] MissingHandleIssues(IReadOnlyList<ModelHealthIssue> issues) =>
            issues
                .Where(x => string.Equals(x.Code, "BOM_GENERATED_HANDLE_MISSING", StringComparison.Ordinal))
                .ToArray();

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }
    }
}
