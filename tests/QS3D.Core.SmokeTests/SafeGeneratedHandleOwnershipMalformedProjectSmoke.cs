using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SafeGeneratedHandleOwnershipMalformedProjectSmoke
    {
        private const string InvalidProjectIssueCode = "GENERATED_HANDLE_OWNERSHIP_INVALID_PROJECT";

        internal static void Run()
        {
            NullElementIsVisibleAsError();
            DuplicateElementIdIsVisibleAsError();
            ValidProjectRemainsClean();
            ValidOwnershipConflictStillReports();
        }

        private static void NullElementIsVisibleAsError()
        {
            var project = new ProjectState("SAFE-OWN-NULL", "Safe ownership null");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam));
            project.Elements.Add(null!);
            var beforeVersion = project.ChangeVersion;

            var issues = new SafeGeneratedHandleOwnershipHealthService().Inspect(project);

            Equal(beforeVersion, project.ChangeVersion);
            Equal(1, issues.Count);
            Equal(InvalidProjectIssueCode, issues[0].Code);
            Equal(HealthSeverity.Error, issues[0].Severity);
        }

        private static void DuplicateElementIdIsVisibleAsError()
        {
            var project = new ProjectState("SAFE-OWN-DUP", "Safe ownership duplicate");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Column));
            var beforeVersion = project.ChangeVersion;

            var issues = new SafeGeneratedHandleOwnershipHealthService().Inspect(project);

            Equal(beforeVersion, project.ChangeVersion);
            Equal(1, issues.Count);
            Equal(InvalidProjectIssueCode, issues[0].Code);
            Equal(HealthSeverity.Error, issues[0].Severity);
        }

        private static void ValidProjectRemainsClean()
        {
            var project = new ProjectState("SAFE-OWN-CLEAN", "Safe ownership clean");
            var element = new ProjectElement("E1", ElementCategory.Beam);
            element.Properties["GeneratedSolidHandle"] = "AA11";
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            var issues = new SafeGeneratedHandleOwnershipHealthService().Inspect(project);

            Equal(beforeVersion, project.ChangeVersion);
            Equal(0, issues.Count);
        }

        private static void ValidOwnershipConflictStillReports()
        {
            var project = new ProjectState("SAFE-OWN-CONFLICT", "Safe ownership conflict");
            var source = new ProjectElement("A", ElementCategory.ArchitecturalWall);
            source.SourceHandles.Add("AA11");
            var generated = new ProjectElement("B", ElementCategory.Beam);
            generated.Properties["GeneratedSolidHandle"] = "AA11";
            project.Elements.Add(source);
            project.Elements.Add(generated);
            var beforeVersion = project.ChangeVersion;

            var issues = new SafeGeneratedHandleOwnershipHealthService().Inspect(project);

            Equal(beforeVersion, project.ChangeVersion);
            Equal(2, issues.Count(x => string.Equals(x.Code, "GENERATED_HANDLE_OWNERSHIP_CONFLICT", StringComparison.Ordinal)));
            True(issues.Where(x => string.Equals(x.Code, "GENERATED_HANDLE_OWNERSHIP_CONFLICT", StringComparison.Ordinal))
                .All(x => x.Severity == HealthSeverity.Error));
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class SafeGeneratedHandleOwnershipMalformedProjectSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SafeGeneratedHandleOwnershipMalformedProjectSmoke.Run();
    }
}
