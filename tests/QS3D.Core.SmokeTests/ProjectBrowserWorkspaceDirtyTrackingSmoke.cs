using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceDirtyTrackingSmoke
    {
        internal static void Run()
        {
            PresentationStateDoesNotDirtySemanticProject();
            SaveAtMaximumVersionSucceeds();
            ClearAtMaximumVersionSucceeds();
        }

        private static void PresentationStateDoesNotDirtySemanticProject()
        {
            var project = new ProjectState("workspace-dirty-project", "Workspace Dirty Project");
            var store = new ProjectBrowserWorkspaceStateStore();
            var stamp = new ProjectPersistenceStamp(project);
            var initialVersion = project.ChangeVersion;
            var initialUpdatedUtc = project.UpdatedUtc;

            False(stamp.RequiresSave(project), "new persistence stamp should start clean");

            var initialState = new ProjectBrowserWorkspaceState();
            True(store.Save(project, initialState), "first workspace save should mutate metadata");
            Equal(initialVersion, project.ChangeVersion, "first workspace save semantic version");
            Equal(initialUpdatedUtc, project.UpdatedUtc, "first workspace save UpdatedUtc");
            True(project.Metadata.ContainsKey(ProjectBrowserWorkspaceStateStore.MetadataKey), "workspace metadata should be present after save");
            False(stamp.RequiresSave(project), "workspace save should not mark semantic project dirty");

            stamp.MarkSaved(project);
            False(stamp.RequiresSave(project), "mark saved should preserve clean semantic state");

            var savedVersion = project.ChangeVersion;
            False(store.Save(project, initialState), "identical workspace save should be a no-op");
            Equal(savedVersion, project.ChangeVersion, "identical workspace save semantic version");
            Equal(initialUpdatedUtc, project.UpdatedUtc, "identical workspace save UpdatedUtc");
            False(stamp.RequiresSave(project), "identical workspace save should keep semantic project clean");

            var changedState = new ProjectBrowserWorkspaceState(query: "wall", dirtyOnly: true);
            True(store.Save(project, changedState), "changed workspace save should mutate metadata");
            Equal(savedVersion, project.ChangeVersion, "changed workspace save semantic version");
            Equal(initialUpdatedUtc, project.UpdatedUtc, "changed workspace save UpdatedUtc");
            False(stamp.RequiresSave(project), "changed workspace save should not mark semantic project dirty");

            stamp.MarkSaved(project);
            var beforeClearVersion = project.ChangeVersion;
            True(store.Clear(project), "clear should remove existing workspace metadata");
            Equal(beforeClearVersion, project.ChangeVersion, "workspace clear semantic version");
            Equal(initialUpdatedUtc, project.UpdatedUtc, "workspace clear UpdatedUtc");
            False(project.Metadata.ContainsKey(ProjectBrowserWorkspaceStateStore.MetadataKey), "workspace metadata should be absent after clear");
            False(stamp.RequiresSave(project), "workspace clear should not mark semantic project dirty");

            stamp.MarkSaved(project);
            var clearedVersion = project.ChangeVersion;
            False(store.Clear(project), "second clear should be a no-op");
            Equal(clearedVersion, project.ChangeVersion, "second workspace clear semantic version");
            Equal(initialUpdatedUtc, project.UpdatedUtc, "second workspace clear UpdatedUtc");
            False(stamp.RequiresSave(project), "second workspace clear should keep semantic project clean");
        }

        private static void SaveAtMaximumVersionSucceeds()
        {
            var project = AtVersion(new ProjectState("workspace-save-overflow", "Workspace Save Overflow"), long.MaxValue);
            var beforeUtc = project.UpdatedUtc;
            var store = new ProjectBrowserWorkspaceStateStore();

            True(store.Save(project, new ProjectBrowserWorkspaceState(query: "wall")), "workspace save at maximum project version");

            True(project.Metadata.ContainsKey(ProjectBrowserWorkspaceStateStore.MetadataKey), "workspace save at maximum version must add metadata");
            Equal(long.MaxValue, project.ChangeVersion, "maximum-version workspace save semantic version");
            Equal(beforeUtc, project.UpdatedUtc, "maximum-version workspace save UpdatedUtc");
        }

        private static void ClearAtMaximumVersionSucceeds()
        {
            var source = new ProjectState("workspace-clear-overflow", "Workspace Clear Overflow");
            var store = new ProjectBrowserWorkspaceStateStore();
            True(store.Save(source, new ProjectBrowserWorkspaceState(query: "wall")), "clear overflow fixture workspace save");
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;

            True(store.Clear(project), "workspace clear at maximum project version");

            False(project.Metadata.ContainsKey(ProjectBrowserWorkspaceStateStore.MetadataKey), "workspace clear at maximum version must remove metadata");
            Equal(long.MaxValue, project.ChangeVersion, "maximum-version workspace clear semantic version");
            Equal(beforeUtc, project.UpdatedUtc, "maximum-version workspace clear UpdatedUtc");
        }

        private static ProjectState AtVersion(ProjectState source, long version)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-workspace-revision-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(source, path);
                var document = XDocument.Load(path);
                var root = document.Root ?? throw new InvalidOperationException("Workspace revision fixture has no QSDB root.");
                root.SetAttributeValue("changeVersion", version.ToString(CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);
                return store.Load(path);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new InvalidOperationException(label + ": expected false.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
