using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SafeGeneratedHandleOwnershipStructuralIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DelimiterCollisionRemainsAConflict();
            SameLogicalHostAliasRemainsDeduplicated();
        }

        private static void DelimiterCollisionRemainsAConflict()
        {
            var project = new ProjectState("SAFE-OWN-STRUCTURAL", "Safe ownership structural identity");
            var first = new ProjectElement("E", ElementCategory.Beam);
            first.Properties["GeneratedA/GeneratedBHandles"] = "AA11";
            var second = new ProjectElement("E/GeneratedA", ElementCategory.Column);
            second.Properties["GeneratedBHandles"] = "AA11";
            project.Elements.Add(first);
            project.Elements.Add(second);
            var beforeVersion = project.ChangeVersion;

            var issues = new SafeGeneratedHandleOwnershipHealthService().Inspect(project);

            Equal(beforeVersion, project.ChangeVersion);
            var conflicts = issues
                .Where(x => string.Equals(x.Code, "GENERATED_HANDLE_OWNERSHIP_CONFLICT", StringComparison.Ordinal))
                .ToArray();
            Equal(2, conflicts.Length);
            True(conflicts.All(x => x.Severity == HealthSeverity.Error));
            True(conflicts.Any(x => string.Equals(x.ElementId, first.Id, StringComparison.Ordinal)));
            True(conflicts.Any(x => string.Equals(x.ElementId, second.Id, StringComparison.Ordinal)));
        }

        private static void SameLogicalHostAliasRemainsDeduplicated()
        {
            var project = new ProjectState("SAFE-OWN-ALIAS", "Safe ownership host alias");
            var element = new ProjectElement("E-ALIAS", ElementCategory.ArchitecturalWall);
            element.Properties["GeneratedSolidHandle"] = "BB22";
            element.Properties["PhysicalOpeningCutSolidHandle"] = "BB22";
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            var issues = new SafeGeneratedHandleOwnershipHealthService().Inspect(project);

            Equal(beforeVersion, project.ChangeVersion);
            Equal(0, issues.Count(x => string.Equals(x.Code, "GENERATED_HANDLE_OWNERSHIP_CONFLICT", StringComparison.Ordinal)));
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected condition to be true.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }
    }
}
