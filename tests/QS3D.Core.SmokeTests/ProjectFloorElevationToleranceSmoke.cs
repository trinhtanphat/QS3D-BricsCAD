using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorElevationToleranceSmoke
    {
        public static void Run()
        {
            RenameWithSubToleranceElevationPreservesExactElevation();
            MaterialElevationChangeStillMarksGeometryDirty();
            PureSubToleranceElevationRequestRemainsNoOp();
            LargeCoordinateMaterialChangeDoesNotDisappear();
        }

        private static void RenameWithSubToleranceElevationPreservesExactElevation()
        {
            var fixture = NewFixture("rename-small-delta");
            var beforeElevation = fixture.Floor.ElevationM;
            var beforeVersion = fixture.Project.ChangeVersion;
            var requestedElevation = beforeElevation + 5e-12d;

            ProjectFloorService.Update(fixture.Project, fixture.Floor.Id, "Level 1 renamed", requestedElevation);

            if (!string.Equals(fixture.Floor.Name, "Level 1 renamed", StringComparison.Ordinal))
                throw new Exception("Floor rename did not apply in the sub-tolerance elevation case.");
            if (!fixture.Floor.ElevationM.Equals(beforeElevation))
                throw new Exception("Sub-tolerance Floor elevation was applied even though it was classified as a geometry no-op.");
            if (fixture.Project.ChangeVersion != beforeVersion + 1L)
                throw new Exception("Floor rename should touch the project exactly once.");
            if ((fixture.Element.Dirty & ElementDirtyFlags.Geometry) != 0)
                throw new Exception("Ignored sub-tolerance Floor elevation delta marked referenced geometry dirty.");
            if ((fixture.Element.Dirty & (ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity)) !=
                (ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity))
                throw new Exception("Floor rename did not preserve existing relation/quantity dirty behavior.");
        }

        private static void MaterialElevationChangeStillMarksGeometryDirty()
        {
            var fixture = NewFixture("material-delta");
            var requestedElevation = fixture.Floor.ElevationM + 1e-5d;

            ProjectFloorService.Update(fixture.Project, fixture.Floor.Id, fixture.Floor.Name, requestedElevation);

            if (!fixture.Floor.ElevationM.Equals(requestedElevation))
                throw new Exception("Material Floor elevation change was not applied.");
            if ((fixture.Element.Dirty & ElementDirtyFlags.Geometry) == 0)
                throw new Exception("Material Floor elevation change did not mark referenced geometry dirty.");
        }

        private static void PureSubToleranceElevationRequestRemainsNoOp()
        {
            var fixture = NewFixture("pure-small-delta");
            var beforeElevation = fixture.Floor.ElevationM;
            var beforeVersion = fixture.Project.ChangeVersion;
            var beforeUpdatedUtc = fixture.Element.UpdatedUtc;

            ProjectFloorService.Update(
                fixture.Project,
                fixture.Floor.Id,
                fixture.Floor.Name,
                beforeElevation + 5e-12d);

            if (!fixture.Floor.ElevationM.Equals(beforeElevation))
                throw new Exception("Pure sub-tolerance Floor elevation request changed the stored elevation.");
            if (fixture.Project.ChangeVersion != beforeVersion)
                throw new Exception("Pure sub-tolerance Floor elevation request changed project freshness.");
            if (fixture.Element.Dirty != ElementDirtyFlags.None || fixture.Element.UpdatedUtc != beforeUpdatedUtc)
                throw new Exception("Pure sub-tolerance Floor elevation request mutated referenced element freshness.");
        }

        private static void LargeCoordinateMaterialChangeDoesNotDisappear()
        {
            const double originalElevation = 1e16d;
            var requestedElevation = originalElevation + 2d;
            if (requestedElevation.Equals(originalElevation))
                throw new Exception("Large-coordinate Floor regression fixture requires two distinct representable elevations.");

            var project = new ProjectState("P-FLOOR-TOLERANCE-large-coordinate", "Floor tolerance large coordinate");
            var floor = ProjectFloorService.Create(project, "F1", "Level 1", originalElevation);
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, string.Empty, floor.Id, string.Empty);
            project.Elements.Add(element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            ProjectFloorService.Update(project, floor.Id, floor.Name, requestedElevation);

            if (!floor.ElevationM.Equals(requestedElevation))
                throw new Exception("Representable two-metre Floor elevation change disappeared at a large coordinate magnitude.");
            if (project.ChangeVersion != beforeVersion + 1L)
                throw new Exception("Material large-coordinate Floor elevation change should touch the project exactly once.");
            var requiredDirty = ElementDirtyFlags.Geometry | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity;
            if ((element.Dirty & requiredDirty) != requiredDirty)
                throw new Exception("Material large-coordinate Floor elevation change did not invalidate referenced geometry/relations/quantity.");
        }

        private static Fixture NewFixture(string id)
        {
            var project = new ProjectState("P-FLOOR-TOLERANCE-" + id, "Floor tolerance");
            var floor = ProjectFloorService.Create(project, "F1", "Level 1", 10d);
            var element = new ProjectElement("E1", ElementCategory.ArchitecturalWall, string.Empty, floor.Id, string.Empty);
            project.Elements.Add(element);
            element.MarkClean(ElementDirtyFlags.All);
            return new Fixture(project, floor, element);
        }

        private sealed class Fixture
        {
            public Fixture(ProjectState project, FloorDefinition floor, ProjectElement element)
            {
                Project = project;
                Floor = floor;
                Element = element;
            }

            public ProjectState Project { get; }
            public FloorDefinition Floor { get; }
            public ProjectElement Element { get; }
        }
    }
}
