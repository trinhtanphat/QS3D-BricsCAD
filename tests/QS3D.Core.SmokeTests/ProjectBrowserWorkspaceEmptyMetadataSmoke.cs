using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceEmptyMetadataSmoke
    {
        public static void Run()
        {
            MissingMetadataReturnsDefaultWithoutMutation();
            NullMetadataFailsWithoutMutation();
            EmptyMetadataFailsWithoutMutation();
            WhitespaceMetadataFailsWithoutMutation();
            CanonicalMetadataStillLoadsWithoutMutation();
        }

        private static void MissingMetadataReturnsDefaultWithoutMutation()
        {
            var project = NewProject("missing");
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;

            var state = new ProjectBrowserWorkspaceStateStore().Load(project);

            if (state.Grouping != ProjectBrowserGrouping.FloorThenCategory ||
                state.Query.Length != 0 || state.DirtyOnly ||
                state.Categories.Count != 0 || state.FloorIds.Count != 0 ||
                state.ZoneIds.Count != 0 || state.ExpandedPaths.Count != 0 ||
                state.SelectedElementIds.Count != 0 || state.PrimaryElementId.Length != 0)
                throw new Exception("Missing workspace metadata did not return the canonical default state.");
            if (project.Metadata.ContainsKey(ProjectBrowserWorkspaceStateStore.MetadataKey))
                throw new Exception("Loading missing workspace metadata created persisted state.");
            AssertFreshnessUnchanged(project, beforeUpdatedUtc, beforeVersion, "missing metadata load");
        }

        private static void NullMetadataFailsWithoutMutation()
        {
            AssertCorruptMetadataFailsWithoutMutation(null, "null");
        }

        private static void EmptyMetadataFailsWithoutMutation()
        {
            AssertCorruptMetadataFailsWithoutMutation(string.Empty, "empty");
        }

        private static void WhitespaceMetadataFailsWithoutMutation()
        {
            AssertCorruptMetadataFailsWithoutMutation("   ", "whitespace");
        }

        private static void AssertCorruptMetadataFailsWithoutMutation(string? serialized, string label)
        {
            var project = NewProject(label);
            project.Metadata[ProjectBrowserWorkspaceStateStore.MetadataKey] = serialized!;
            if (!project.Metadata.TryGetValue(ProjectBrowserWorkspaceStateStore.MetadataKey, out var storedBeforeLoad))
                throw new Exception("Requested " + label + " workspace metadata did not remain present before Load.");
            if (serialized == null && storedBeforeLoad.Length != 0)
                throw new Exception("Null workspace metadata was not canonicalized to empty text before Load.");
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;

            var rejected = false;
            try
            {
                new ProjectBrowserWorkspaceStateStore().Load(project);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            if (!rejected)
                throw new Exception("Present " + label + " workspace metadata was silently treated as missing state.");
            if (!project.Metadata.TryGetValue(ProjectBrowserWorkspaceStateStore.MetadataKey, out var after) ||
                !string.Equals(storedBeforeLoad, after, StringComparison.Ordinal))
                throw new Exception("Failed " + label + " workspace load mutated the persisted metadata value.");
            AssertFreshnessUnchanged(project, beforeUpdatedUtc, beforeVersion, label + " metadata failure");
        }

        private static void CanonicalMetadataStillLoadsWithoutMutation()
        {
            var project = NewProject("canonical");
            var store = new ProjectBrowserWorkspaceStateStore();
            var expected = new ProjectBrowserWorkspaceState(ProjectBrowserGrouping.Category);
            var serialized = store.Serialize(expected);
            project.Metadata[ProjectBrowserWorkspaceStateStore.MetadataKey] = serialized;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeVersion = project.ChangeVersion;

            var loaded = store.Load(project);

            if (loaded.Grouping != ProjectBrowserGrouping.Category)
                throw new Exception("Canonical workspace metadata did not preserve grouping through Load.");
            if (!string.Equals(serialized, store.Serialize(loaded), StringComparison.Ordinal))
                throw new Exception("Canonical workspace metadata did not round-trip unchanged through Load.");
            if (!project.Metadata.TryGetValue(ProjectBrowserWorkspaceStateStore.MetadataKey, out var after) ||
                !string.Equals(serialized, after, StringComparison.Ordinal))
                throw new Exception("Canonical workspace Load rewrote persisted metadata.");
            AssertFreshnessUnchanged(project, beforeUpdatedUtc, beforeVersion, "canonical metadata load");
        }

        private static ProjectState NewProject(string suffix) =>
            new ProjectState("P-WORKSPACE-EMPTY-" + suffix, "Workspace metadata load smoke");

        private static void AssertFreshnessUnchanged(ProjectState project, DateTime expectedUpdatedUtc, long expectedVersion, string label)
        {
            if (project.ChangeVersion != expectedVersion)
                throw new Exception(label + " changed project ChangeVersion.");
            if (project.UpdatedUtc != expectedUpdatedUtc)
                throw new Exception(label + " changed project UpdatedUtc.");
        }
    }
}
