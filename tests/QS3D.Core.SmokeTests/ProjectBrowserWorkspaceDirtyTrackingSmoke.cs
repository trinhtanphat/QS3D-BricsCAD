using System;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceDirtyTrackingSmoke
    {
        internal static void Run()
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
