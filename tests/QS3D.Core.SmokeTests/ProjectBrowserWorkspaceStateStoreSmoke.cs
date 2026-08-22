using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserWorkspaceStateStoreSmoke
    {
        public static void Run()
        {
            SaveLoadRoundTripsValidatedState();
            PresentationStateDoesNotInvalidateSemanticVersion();
            RepeatedSaveIsIdempotent();
            CorruptStateFailsClosed();
            UnsupportedSchemaShapeFailsClosed();
            StaleSelectionFailsClosed();
            ClearRemovesPersistedState();
        }

        private static void SaveLoadRoundTripsValidatedState()
        {
            var project = BuildProject();
            var query = ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(query.Root);
            var firstLevel = ProjectBrowserVirtualizationPlanner.BuildViewport(query.Root, new[] { rootPath }, 0, 10);
            var floorPath = firstLevel.Rows.Single(x => x.DisplayName == "L02").Path;
            var state = new ProjectBrowserWorkspaceState(
                ProjectBrowserGrouping.FloorThenCategory,
                "",
                false,
                new[] { ElementCategory.Beam, ElementCategory.Column },
                new[] { "F-02" },
                new[] { "Z-A" },
                new[] { floorPath, rootPath },
                new[] { "B-002", "B-001" },
                "B-002");

            var store = new ProjectBrowserWorkspaceStateStore();
            var beforeVersion = project.ChangeVersion;
            True(store.Save(project, state));
            Equal(beforeVersion, project.ChangeVersion);
            True(project.Metadata.ContainsKey(ProjectBrowserWorkspaceStateStore.MetadataKey));

            var loaded = store.Load(project);
            Equal(ProjectBrowserGrouping.FloorThenCategory, loaded.Grouping);
            Equal(2, loaded.Categories.Count);
            Equal(ElementCategory.Beam, loaded.Categories[0]);
            Equal(ElementCategory.Column, loaded.Categories[1]);
            Equal("F-02", loaded.FloorIds.Single());
            Equal("Z-A", loaded.ZoneIds.Single());
            Equal(2, loaded.ExpandedPaths.Count);
            Equal(rootPath, loaded.ExpandedPaths[0]);
            Equal(floorPath, loaded.ExpandedPaths[1]);
            Equal("B-001", loaded.SelectedElementIds[0]);
            Equal("B-002", loaded.SelectedElementIds[1]);
            Equal("B-002", loaded.PrimaryElementId);
        }

        private static void PresentationStateDoesNotInvalidateSemanticVersion()
        {
            var project = BuildProject();
            project.Touch();
            var semanticVersion = project.ChangeVersion;
            var store = new ProjectBrowserWorkspaceStateStore();
            True(store.Save(project, ValidState(project)));
            Equal(semanticVersion, project.ChangeVersion);
            True(store.Clear(project));
            Equal(semanticVersion, project.ChangeVersion);
        }

        private static void RepeatedSaveIsIdempotent()
        {
            var project = BuildProject();
            var state = ValidState(project);
            var store = new ProjectBrowserWorkspaceStateStore();
            True(store.Save(project, state));
            var version = project.ChangeVersion;
            True(!store.Save(project, state));
            Equal(version, project.ChangeVersion);
        }

        private static void CorruptStateFailsClosed()
        {
            var project = BuildProject();
            var store = new ProjectBrowserWorkspaceStateStore();
            project.Metadata[ProjectBrowserWorkspaceStateStore.MetadataKey] = "<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///tmp/x'>]><x>&e;</x>";
            Throws<InvalidDataException>(() => store.Load(project));

            project.Metadata[ProjectBrowserWorkspaceStateStore.MetadataKey] =
                "<ProjectBrowserWorkspaceState format=\"Wrong\" version=\"1\" grouping=\"Category\" dirtyOnly=\"false\" query=\"\" primaryElementId=\"\"><Categories/><FloorIds/><ZoneIds/><ExpandedPaths/><SelectedElementIds/></ProjectBrowserWorkspaceState>";
            Throws<InvalidDataException>(() => store.Load(project));
        }

        private static void UnsupportedSchemaShapeFailsClosed()
        {
            var project = BuildProject();
            var store = new ProjectBrowserWorkspaceStateStore();
            var valid = store.Serialize(SchemaState());

            Throws<InvalidDataException>(() => store.Deserialize(valid.Replace(" version=\"1\"", " version=\"1\" future=\"x\"")));
            Throws<InvalidDataException>(() => store.Deserialize(valid.Replace(" query=\"beam\"", string.Empty)));
            Throws<InvalidDataException>(() => store.Deserialize(valid.Replace("<FloorIds>", "<FloorIds future=\"x\">")));
            Throws<InvalidDataException>(() => store.Deserialize(valid.Replace("<ZoneIds>", "<ZoneIds>future")));
            Throws<InvalidDataException>(() => store.Deserialize(valid.Replace("<Id>F-02</Id>", "<Id future=\"x\">F-02</Id>")));
            Throws<InvalidDataException>(() => store.Deserialize(valid.Replace("<Id>Z-A</Id>", "<Id><Future>Z-A</Future></Id>")));
            Throws<InvalidDataException>(() => store.Deserialize(valid.Replace("<SelectedElementIds>", "<SelectedElementIds><!--future-->")));
            Throws<InvalidDataException>(() => store.Deserialize(valid.Replace("<Categories>", "<Categories/><future:Categories xmlns:future=\"urn:future\"><future:Category>Beam</future:Category></future:Categories><Categories>")));
        }

        private static void StaleSelectionFailsClosed()
        {
            var project = BuildProject();
            var store = new ProjectBrowserWorkspaceStateStore();
            True(store.Save(project, ValidState(project)));
            var element = project.Elements.Single(x => x.Id == "B-001");
            project.Elements.Remove(element);
            Throws<InvalidOperationException>(() => store.Load(project));
        }

        private static void ClearRemovesPersistedState()
        {
            var project = BuildProject();
            var store = new ProjectBrowserWorkspaceStateStore();
            True(store.Save(project, ValidState(project)));
            var version = project.ChangeVersion;
            True(store.Clear(project));
            Equal(version, project.ChangeVersion);
            True(!project.Metadata.ContainsKey(ProjectBrowserWorkspaceStateStore.MetadataKey));
            True(!store.Clear(project));
            Equal(version, project.ChangeVersion);
        }

        private static ProjectBrowserWorkspaceState ValidState(ProjectState project)
        {
            var query = ProjectBrowserQueryPlanner.Build(project, ProjectBrowserGrouping.FloorThenCategory);
            var rootPath = ProjectBrowserVirtualizationPlanner.GetRootPath(query.Root);
            var firstLevel = ProjectBrowserVirtualizationPlanner.BuildViewport(query.Root, new[] { rootPath }, 0, 10);
            var floorPath = firstLevel.Rows.Single(x => x.DisplayName == "L02").Path;
            return new ProjectBrowserWorkspaceState(
                ProjectBrowserGrouping.FloorThenCategory,
                expandedPaths: new[] { rootPath, floorPath },
                selectedElementIds: new[] { "B-001" },
                primaryElementId: "B-001");
        }

        private static ProjectBrowserWorkspaceState SchemaState()
        {
            return new ProjectBrowserWorkspaceState(
                ProjectBrowserGrouping.FloorThenCategory,
                "beam",
                false,
                new[] { ElementCategory.Beam },
                new[] { "F-02" },
                new[] { "Z-A" },
                Array.Empty<string>(),
                new[] { "B-001" },
                "B-001");
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-BROWSER-STATE", "Browser State");
            project.Floors.Add(new FloorDefinition("F-01", "L01", 0d));
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Elements.Add(new ProjectElement("B-002", ElementCategory.Beam, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("C-001", ElementCategory.Column, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, string.Empty, "F-02", "Z-A"));
            project.Elements.Add(new ProjectElement("W-001", ElementCategory.ArchitecturalWall, string.Empty, "F-01", "Z-A"));
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectBrowserWorkspaceStateStoreSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("ProjectBrowserWorkspaceStateStoreSmoke assertion failed.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("ProjectBrowserWorkspaceStateStoreSmoke expected exception " + typeof(T).Name + ".");
        }
    }
}
