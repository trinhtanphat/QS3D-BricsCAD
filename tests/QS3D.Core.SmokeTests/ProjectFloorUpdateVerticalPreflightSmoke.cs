using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorUpdateVerticalPreflightSmoke
    {
        internal static void Run()
        {
            BottomReferenceOverflowFailsBeforeFloorMutation();
            TopReferenceOverflowFailsBeforeFloorMutation();
            BottomReferenceInversionFailsBeforeFloorMutation();
            TopReferenceInversionFailsBeforeFloorMutation();
            LegacyFloorIdOnlyReferenceDoesNotAcquireVerticalPairValidation();
            ValidVerticalReferenceUpdateStillPropagatesDirtyState();
        }

        private static void BottomReferenceOverflowFailsBeforeFloorMutation()
        {
            var project = ProjectWithFloors(("B", -double.MaxValue), ("T", 10d));
            var element = Element("E-BOTTOM-OVERFLOW");
            SetVertical(element, "B", double.MaxValue, "T", 0d);
            project.Elements.Add(element);
            var floor = project.FindFloor("B")!;
            var snapshot = Snapshot(project, element, floor);

            Throws<InvalidOperationException>(() => ProjectFloorService.Update(project, "B", "Renamed Bottom", double.MaxValue));

            SnapshotUnchanged(snapshot, project, element, floor, "bottom overflow");
        }

        private static void TopReferenceOverflowFailsBeforeFloorMutation()
        {
            var project = ProjectWithFloors(("B", -10d), ("T", -double.MaxValue));
            var element = Element("E-TOP-OVERFLOW");
            SetVertical(element, "B", 0d, "T", double.MaxValue);
            project.Elements.Add(element);
            var floor = project.FindFloor("T")!;
            var snapshot = Snapshot(project, element, floor);

            Throws<InvalidOperationException>(() => ProjectFloorService.Update(project, "T", "Renamed Top", double.MaxValue));

            SnapshotUnchanged(snapshot, project, element, floor, "top overflow");
        }

        private static void BottomReferenceInversionFailsBeforeFloorMutation()
        {
            var project = ProjectWithFloors(("B", 0d), ("T", 10d));
            var element = Element("E-BOTTOM-INVERT");
            SetVertical(element, "B", 0d, "T", 0d);
            project.Elements.Add(element);
            var floor = project.FindFloor("B")!;
            var snapshot = Snapshot(project, element, floor);

            Throws<InvalidOperationException>(() => ProjectFloorService.Update(project, "B", "Renamed Bottom", 10d));

            SnapshotUnchanged(snapshot, project, element, floor, "bottom inversion");
        }

        private static void TopReferenceInversionFailsBeforeFloorMutation()
        {
            var project = ProjectWithFloors(("B", 0d), ("T", 10d));
            var element = Element("E-TOP-INVERT");
            SetVertical(element, "B", 0d, "T", 0d);
            project.Elements.Add(element);
            var floor = project.FindFloor("T")!;
            var snapshot = Snapshot(project, element, floor);

            Throws<InvalidOperationException>(() => ProjectFloorService.Update(project, "T", "Renamed Top", 0d));

            SnapshotUnchanged(snapshot, project, element, floor, "top inversion");
        }

        private static void LegacyFloorIdOnlyReferenceDoesNotAcquireVerticalPairValidation()
        {
            var project = ProjectWithFloors(("L", 0d));
            var element = new ProjectElement("E-FLOOR-ONLY", ElementCategory.Beam, string.Empty, "L", string.Empty);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            var floor = ProjectFloorService.Update(project, "L", "Level Renamed", double.MaxValue);

            Equal(double.MaxValue, floor.ElevationM, "FloorId-only elevation update");
            Equal("Level Renamed", floor.Name, "FloorId-only rename");
            Equal(beforeVersion + 1, project.ChangeVersion, "FloorId-only ChangeVersion");
            var expectedDirty = ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity;
            Equal(expectedDirty, element.Dirty, "FloorId-only referenced element dirty flags");
        }

        private static void ValidVerticalReferenceUpdateStillPropagatesDirtyState()
        {
            var project = ProjectWithFloors(("B", 0d), ("T", 10d));
            var element = Element("E-VALID-UPDATE");
            SetVertical(element, "B", 0.5d, "T", -0.5d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            var floor = ProjectFloorService.Update(project, "B", "Bottom Renamed", 2d);

            Equal(2d, floor.ElevationM, "valid updated floor elevation");
            Equal("Bottom Renamed", floor.Name, "valid updated floor name");
            Equal(beforeVersion + 1, project.ChangeVersion, "valid update ChangeVersion");
            var expectedDirty = ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity;
            Equal(expectedDirty, element.Dirty, "valid vertical referenced element dirty flags");
            var placement = ElementVerticalPlacementService.Resolve(project, element, 0d, 1d, 0d);
            Equal(2.5d, placement.BottomElevationM, "valid updated bottom placement");
            Equal(9.5d, placement.TopElevationM, "valid retained top placement");
        }

        private static ProjectState ProjectWithFloors(params (string Id, double Elevation)[] floors)
        {
            var project = new ProjectState("P-FLOOR-UPDATE-PREFLIGHT", "Floor update preflight");
            foreach (var floor in floors)
                project.Floors.Add(new FloorDefinition(floor.Id, floor.Id, floor.Elevation));
            return project;
        }

        private static ProjectElement Element(string id) =>
            new ProjectElement(id, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);

        private static void SetVertical(ProjectElement element, string bottomId, double bottomOffset, string topId, double topOffset)
        {
            element.Properties[ProjectFloorService.BottomLevelIdKey] = bottomId;
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = bottomOffset.ToString("R", CultureInfo.InvariantCulture);
            element.Properties[ProjectFloorService.TopLevelIdKey] = topId;
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = topOffset.ToString("R", CultureInfo.InvariantCulture);
        }

        private static StateSnapshot Snapshot(ProjectState project, ProjectElement element, FloorDefinition floor) =>
            new StateSnapshot(project.ChangeVersion, project.UpdatedUtc, element.UpdatedUtc, element.Dirty, floor.Name, floor.ElevationM);

        private static void SnapshotUnchanged(StateSnapshot snapshot, ProjectState project, ProjectElement element, FloorDefinition floor, string label)
        {
            Equal(snapshot.ChangeVersion, project.ChangeVersion, label + " ChangeVersion");
            Equal(snapshot.ProjectUpdatedUtc, project.UpdatedUtc, label + " project UpdatedUtc");
            Equal(snapshot.ElementUpdatedUtc, element.UpdatedUtc, label + " element UpdatedUtc");
            Equal(snapshot.Dirty, element.Dirty, label + " Dirty");
            Equal(snapshot.FloorName, floor.Name, label + " Floor name");
            Equal(snapshot.FloorElevation, floor.ElevationM, label + " Floor elevation");
        }

        private readonly struct StateSnapshot
        {
            public StateSnapshot(long changeVersion, DateTime projectUpdatedUtc, DateTime elementUpdatedUtc, ElementDirtyFlags dirty, string floorName, double floorElevation)
            {
                ChangeVersion = changeVersion;
                ProjectUpdatedUtc = projectUpdatedUtc;
                ElementUpdatedUtc = elementUpdatedUtc;
                Dirty = dirty;
                FloorName = floorName;
                FloorElevation = floorElevation;
            }

            public long ChangeVersion { get; }
            public DateTime ProjectUpdatedUtc { get; }
            public DateTime ElementUpdatedUtc { get; }
            public ElementDirtyFlags Dirty { get; }
            public string FloorName { get; }
            public double FloorElevation { get; }
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("ProjectFloorUpdateVerticalPreflightSmoke expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectFloorUpdateVerticalPreflightSmoke: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class ProjectFloorUpdateVerticalPreflightSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFloorUpdateVerticalPreflightSmoke.Run();
    }
}
