using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleSelectionStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RemovedOwnerBeforeEmptySelectionReturnFailsClosed();
            ReplacedOwnerDuringLazySelectionFailsClosed();
            StableLazySelectionStillResolvesOwner();
        }

        private static void RemovedOwnerBeforeEmptySelectionReturnFailsClosed()
        {
            var project = CreateProject("P-HANDLE-STRUCT-1", out var owner);
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(
                () => SemanticHandleOwnershipResolver.Resolve(project, RemoveThenYieldNothing(project, owner)),
                "Project element ownership changed while materializing semantic handle selection");

            Equal(beforeVersion, project.ChangeVersion, "removed-owner project revision");
            False(project.Elements.Contains(owner), "removed-owner caller side effect");
        }

        private static void ReplacedOwnerDuringLazySelectionFailsClosed()
        {
            var project = CreateProject("P-HANDLE-STRUCT-2", out var owner);
            var replacement = new ProjectElement(owner.Id, owner.Category);
            replacement.SourceHandles.Add("A1");
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(
                () => SemanticHandleOwnershipResolver.Resolve(project, YieldThenReplace(project, owner, replacement, "A1")),
                "Project element ownership changed while materializing semantic handle selection");

            Equal(beforeVersion, project.ChangeVersion, "replaced-owner project revision");
            False(project.Elements.Contains(owner), "replaced-owner original instance");
            True(project.Elements.Contains(replacement), "replaced-owner replacement instance");
        }

        private static void StableLazySelectionStillResolvesOwner()
        {
            var project = CreateProject("P-HANDLE-STRUCT-3", out var owner);
            var beforeVersion = project.ChangeVersion;

            var resolved = SemanticHandleOwnershipResolver.Resolve(project, YieldStable("A1"));

            Equal(1, resolved.Count, "stable resolved count");
            True(ReferenceEquals(owner, resolved[0]), "stable resolved owner identity");
            Equal(beforeVersion, project.ChangeVersion, "stable project revision");
        }

        private static ProjectState CreateProject(string id, out ProjectElement owner)
        {
            var project = new ProjectState(id, "Semantic handle structural freshness");
            owner = new ProjectElement("E-HANDLE-STRUCT", ElementCategory.Beam);
            owner.SourceHandles.Add("A1");
            project.Elements.Add(owner);
            return project;
        }

        private static IEnumerable<string> RemoveThenYieldNothing(ProjectState project, ProjectElement owner)
        {
            project.Elements.Remove(owner);
            yield break;
        }

        private static IEnumerable<string> YieldThenReplace(ProjectState project, ProjectElement owner, ProjectElement replacement, string handle)
        {
            yield return handle;
            project.Elements.Remove(owner);
            project.Elements.Add(replacement);
        }

        private static IEnumerable<string> YieldStable(string handle)
        {
            yield return handle;
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new Exception("SemanticHandleSelectionStructuralFreshnessSmoke expected true: " + label + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("SemanticHandleSelectionStructuralFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("SemanticHandleSelectionStructuralFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void ThrowsContaining<TException>(Action action, string expectedText) where TException : Exception
        {
            try { action(); }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("SemanticHandleSelectionStructuralFreshnessSmoke expected message containing '" + expectedText + "', actual='" + ex.Message + "'.");
            }
            throw new Exception("SemanticHandleSelectionStructuralFreshnessSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
