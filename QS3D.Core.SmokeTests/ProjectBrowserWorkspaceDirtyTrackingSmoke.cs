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
            DirtyTrackingFollowsWorkspaceMutations();
            SaveOverflowLeavesWorkspaceMetadataUnchanged();
            ClearOverflowLeavesWorkspaceMetadataUnchanged();
        }

        private static void DirtyTrackingFollowsWorkspaceMutations()
        {
            var project = new ProjectState("workspace-dirty-project", "Workspace Dirty Project");
            var store = new ProjectBrowserWorkspaceStateStore();
            var stamp = new ProjectPersistenceStamp(project);
            var initialVersion = project.ChangeVersion;

            False(stamp.RequiresSave(project), "new persistence stamp should start clean");

            var initialState = new ProjectBrowserWorkspaceState();
            True(store.Save(project, initialState), "first workspace save should mutate metadata");
            Equal(initialVersion + 1L, project.ChangeVersion, "first workspace save change version");
            True(project.Metadata.ContainsKey(ProjectBrowserWorkspaceStateStore.MetadataKey), "workspace metadata should be present after save");
            True(stamp.RequiresSave(project), "workspace save should mark project dirty");

            stamp.MarkSaved(project);
            False(stamp.RequiresSave(project), "mark saved should reset workspace dirty state");

            var savedVersion = project.ChangeVersion;
            False(store.Save(project, initialState), "identical workspace save should be a no-op");
            Equal(savedVersion, project.ChangeVersion, "identical workspace save change version");
            False(stamp.RequiresSave(project), "identical workspace save should remain clean");

            var changedState = new ProjectBrowserWorkspaceState(query: "wall", dirtyOnly: true);
            True(store.Save(project, changedState), "changed workspace save should mutate metadata");
            Equal(savedVersion + 1L, project.ChangeVersion, "changed workspace save change version");
            True(stamp.RequiresSave(project), "changed workspace save should mark project dirty");

            stamp.MarkSaved(project);
            var beforeClearVersion = project.ChangeVersion;
            True(store.Clear(project), "clear should remove existing workspace metadata");
            Equal(beforeClearVersion + 1L, project.ChangeVersion, "workspace clear change version");
            False(project.Metadata.ContainsKey(ProjectBrowserWorkspaceStateStore.MetadataKey), "workspace metadata should be absent after clear");
            True(stamp.RequiresSave(project), "workspace clear should mark project dirty");

            stamp.MarkSaved(project);
            var clearedVersion = project.ChangeVersion;
            False(store.Clear(project), "second clear should be a no-op");
            Equal(clearedVersion, project.ChangeVersion, "second workspace clear change version");
            False(stamp.RequiresSave(project), "second workspace clear should remain clean");
        }

        private static void SaveOverflowLeavesWorkspaceMetadataUnchanged()
        {
            var project = AtVersion(new ProjectState("workspace-save-overflow", "Workspace Save Overflow"), long.MaxValue);
            var beforeUtc = project.UpdatedUtc;
            var store = new ProjectBrowserWorkspaceStateStore();

            Throws<OverflowException>(
                () => store.Save(project, new ProjectBrowserWorkspaceState(query: "wall")),
                "workspace save at maximum project version");

            False(project.Metadata.ContainsKey(ProjectBrowserWorkspaceStateStore.MetadataKey), "failed workspace save must not add metadata");
            Equal(long.MaxValue, project.ChangeVersion, "failed workspace save change version");
            Equal(beforeUtc, project.UpdatedUtc, "failed workspace save UpdatedUtc");
        }

        private static void ClearOverflowLeavesWorkspaceMetadataUnchanged()
        {
            var source = new ProjectState("workspace-clear-overflow", "Workspace Clear Overflow");
            var store = new ProjectBrowserWorkspaceStateStore();
            True(store.Save(source, new ProjectBrowserWorkspaceState(query: "wall")), "clear overflow fixture workspace save");
            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;
            var beforeMetadata = project.Metadata[ProjectBrowserWorkspaceStateStore.MetadataKey];

            Throws<OverflowException>(() => store.Clear(project), "workspace clear at maximum project version");

            True(project.Metadata.TryGetValue(ProjectBrowserWorkspaceStateStore.MetadataKey, out var afterMetadata), "failed workspace clear must preserve metadata");
            Equal(beforeMetadata, afterMetadata, "failed workspace clear metadata");
            Equal(long.MaxValue, project.ChangeVersion, "failed workspace clear change version");
            Equal(beforeUtc, project.UpdatedUtc, "failed workspace clear UpdatedUtc");
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

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
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
