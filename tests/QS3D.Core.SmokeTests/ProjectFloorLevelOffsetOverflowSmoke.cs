using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorLevelOffsetOverflowSmoke
    {
        internal static void Run()
        {
            AssignBottomRejectsCandidateBottomOverflowBeforeMutation();
            AssignBottomRejectsExistingTopOverflowBeforeMutation();
            AssignTopRejectsExistingBottomOverflowBeforeMutation();
            AssignTopRejectsCandidateTopOverflowBeforeMutation();
            ValidFiniteEffectiveElevationsStillAssign();
        }

        private static void AssignBottomRejectsCandidateBottomOverflowBeforeMutation()
        {
            var project = ProjectWithFloors(("B", double.MaxValue));
            var element = Element("E-BOTTOM-CANDIDATE");
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = double.MaxValue.ToString("R", CultureInfo.InvariantCulture);
            project.Elements.Add(element);
            var snapshot = Snapshot(project, element);

            Throws<InvalidOperationException>(() => ProjectFloorService.AssignBottomLevel(project, "B", new[] { element }));

            SnapshotUnchanged(snapshot, project, element, "candidate bottom overflow");
            False(element.Properties.ContainsKey(ProjectFloorService.BottomLevelIdKey), "candidate bottom overflow wrote BottomLevelId");
        }

        private static void AssignBottomRejectsExistingTopOverflowBeforeMutation()
        {
            var project = ProjectWithFloors(("B", 0d), ("T", double.MaxValue));
            var element = Element("E-BOTTOM-TOP");
            element.Properties[ProjectFloorService.TopLevelIdKey] = "T";
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = double.MaxValue.ToString("R", CultureInfo.InvariantCulture);
            project.Elements.Add(element);
            var snapshot = Snapshot(project, element);

            Throws<InvalidOperationException>(() => ProjectFloorService.AssignBottomLevel(project, "B", new[] { element }));

            SnapshotUnchanged(snapshot, project, element, "existing top overflow");
            False(element.Properties.ContainsKey(ProjectFloorService.BottomLevelIdKey), "existing top overflow wrote BottomLevelId");
        }

        private static void AssignTopRejectsExistingBottomOverflowBeforeMutation()
        {
            var project = ProjectWithFloors(("B", double.MaxValue), ("T", 0d));
            var element = Element("E-TOP-BOTTOM");
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "B";
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = double.MaxValue.ToString("R", CultureInfo.InvariantCulture);
            project.Elements.Add(element);
            var snapshot = Snapshot(project, element);

            Throws<InvalidOperationException>(() => ProjectFloorService.AssignTopLevel(project, "T", new[] { element }));

            SnapshotUnchanged(snapshot, project, element, "existing bottom overflow");
            False(element.Properties.ContainsKey(ProjectFloorService.TopLevelIdKey), "existing bottom overflow wrote TopLevelId");
        }

        private static void AssignTopRejectsCandidateTopOverflowBeforeMutation()
        {
            var project = ProjectWithFloors(("B", 0d), ("T", double.MaxValue));
            var element = Element("E-TOP-CANDIDATE");
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "B";
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "0";
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = double.MaxValue.ToString("R", CultureInfo.InvariantCulture);
            project.Elements.Add(element);
            var snapshot = Snapshot(project, element);

            Throws<InvalidOperationException>(() => ProjectFloorService.AssignTopLevel(project, "T", new[] { element }));

            SnapshotUnchanged(snapshot, project, element, "candidate top overflow");
            False(element.Properties.ContainsKey(ProjectFloorService.TopLevelIdKey), "candidate top overflow wrote TopLevelId");
        }

        private static void ValidFiniteEffectiveElevationsStillAssign()
        {
            var project = ProjectWithFloors(("B", 10d), ("T", 15d));
            var element = Element("E-VALID");
            element.Properties[ProjectFloorService.BottomLevelOffsetKey] = "0.5";
            element.Properties[ProjectFloorService.TopLevelOffsetKey] = "-0.25";
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;

            Equal(1, ProjectFloorService.AssignBottomLevel(project, "B", new[] { element }), "valid bottom assignment count");
            Equal(1, ProjectFloorService.AssignTopLevel(project, "T", new[] { element }), "valid top assignment count");

            Equal("B", element.Properties[ProjectFloorService.BottomLevelIdKey], "valid BottomLevelId");
            Equal("T", element.Properties[ProjectFloorService.TopLevelIdKey], "valid TopLevelId");
            Equal(beforeVersion + 2, project.ChangeVersion, "valid assignment ChangeVersion");
            var expectedDirty = ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity;
            Equal(expectedDirty, element.Dirty, "valid assignment dirty flags");
            var resolved = ElementVerticalPlacementService.Resolve(project, element, 0d, 1d, 0d);
            Equal(10.5d, resolved.BottomElevationM, "resolved valid bottom elevation");
            Equal(14.75d, resolved.TopElevationM, "resolved valid top elevation");
        }

        private static ProjectState ProjectWithFloors(params (string Id, double Elevation)[] floors)
        {
            var project = new ProjectState("P-LEVEL-OVERFLOW", "Level overflow");
            foreach (var floor in floors)
                project.Floors.Add(new FloorDefinition(floor.Id, floor.Id, floor.Elevation));
            return project;
        }

        private static ProjectElement Element(string id) =>
            new ProjectElement(id, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);

        private static ElementSnapshot Snapshot(ProjectState project, ProjectElement element) =>
            new ElementSnapshot(project.ChangeVersion, project.UpdatedUtc, element.UpdatedUtc, element.Dirty, element.Properties.Count);

        private static void SnapshotUnchanged(ElementSnapshot snapshot, ProjectState project, ProjectElement element, string label)
        {
            Equal(snapshot.ChangeVersion, project.ChangeVersion, label + " ChangeVersion");
            Equal(snapshot.ProjectUpdatedUtc, project.UpdatedUtc, label + " project UpdatedUtc");
            Equal(snapshot.ElementUpdatedUtc, element.UpdatedUtc, label + " element UpdatedUtc");
            Equal(snapshot.Dirty, element.Dirty, label + " Dirty");
            Equal(snapshot.PropertyCount, element.Properties.Count, label + " property count");
        }

        private readonly struct ElementSnapshot
        {
            public ElementSnapshot(long changeVersion, DateTime projectUpdatedUtc, DateTime elementUpdatedUtc, ElementDirtyFlags dirty, int propertyCount)
            {
                ChangeVersion = changeVersion;
                ProjectUpdatedUtc = projectUpdatedUtc;
                ElementUpdatedUtc = elementUpdatedUtc;
                Dirty = dirty;
                PropertyCount = propertyCount;
            }

            public long ChangeVersion { get; }
            public DateTime ProjectUpdatedUtc { get; }
            public DateTime ElementUpdatedUtc { get; }
            public ElementDirtyFlags Dirty { get; }
            public int PropertyCount { get; }
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("ProjectFloorLevelOffsetOverflowSmoke expected " + typeof(TException).Name + ".");
        }

        private static void False(bool condition, string message)
        {
            if (condition) throw new InvalidOperationException("ProjectFloorLevelOffsetOverflowSmoke: " + message + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectFloorLevelOffsetOverflowSmoke: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class ProjectFloorLevelOffsetOverflowSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFloorLevelOffsetOverflowSmoke.Run();
    }
}
