using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleRootFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LateAddedRootFailsClosed();
            ExistingRootTouchFailsClosed();
            StableRootStillResolvesDirectHandle();
        }

        private static void LateAddedRootFailsClosed()
        {
            var project = new ProjectState("P-SOURCE-HANDLE-LATE", "Source Handle freshness");
            var beforeVersion = project.ChangeVersion;

            ThrowsFreshness(() => SourceHandleResolver.Resolve(project, AddAndYieldRoot(project)));

            Equal(checked(beforeVersion + 1L), project.ChangeVersion, "Caller root enumeration should be the only project version change.");
            if (project.FindElement("E-LATE") == null)
                throw new InvalidOperationException("Regression setup failed to add the late semantic root.");
        }

        private static void ExistingRootTouchFailsClosed()
        {
            var project = new ProjectState("P-SOURCE-HANDLE-TOUCH", "Source Handle freshness");
            var element = new ProjectElement("E-EXISTING", ElementCategory.ArchitecturalWall);
            element.SourceHandles.Add("ABC");
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            ThrowsFreshness(() => SourceHandleResolver.Resolve(project, TouchAndYield(project, element.Id)));

            Equal(checked(beforeVersion + 1L), project.ChangeVersion, "Caller root enumeration should be the only existing-root version change.");
        }

        private static void StableRootStillResolvesDirectHandle()
        {
            var project = new ProjectState("P-SOURCE-HANDLE-STABLE", "Source Handle freshness");
            var element = new ProjectElement("E-STABLE", ElementCategory.ArchitecturalWall);
            element.SourceHandles.Add("ABC");
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            var handles = SourceHandleResolver.Resolve(project, new[] { element.Id });

            Equal(beforeVersion, project.ChangeVersion, "Read-only Locate must not advance the project version.");
            if (handles.Count != 1 || !string.Equals(handles[0], "ABC", StringComparison.Ordinal))
                throw new InvalidOperationException("Stable Source Handle roots must preserve direct-handle resolution.");
        }

        private static IEnumerable<string> AddAndYieldRoot(ProjectState project)
        {
            var element = new ProjectElement("E-LATE", ElementCategory.ArchitecturalWall);
            element.SourceHandles.Add("DEF");
            project.Elements.Add(element);
            project.Touch();
            yield return element.Id;
        }

        private static IEnumerable<string> TouchAndYield(ProjectState project, string elementId)
        {
            project.Touch();
            yield return elementId;
        }

        private static void ThrowsFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("changed", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    ex.Message.IndexOf("root", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("Locate rejected stale root enumeration with the wrong contract message.", ex);
            }
            throw new InvalidOperationException("Expected Locate to reject a root enumerable that changes ProjectState during enumeration.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + "; Actual=" + actual + ".");
        }
    }
}