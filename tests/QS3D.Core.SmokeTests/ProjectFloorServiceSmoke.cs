using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorServiceSmoke
    {
        public static void Run()
        {
            CreateUpdateAssignAndDelete();
            ElevationChangeMarksGeneratedGeometryStale();
            DeleteGuardsActiveAndReferencedFloors();
            CorruptElementCollectionFailsClosed();
            RejectsDuplicateNamesAndInvalidElevation();
            RejectsDetachedSameIdElements();
        }

        private static void CreateUpdateAssignAndDelete()
        {
            var project = new ProjectState("p", "Floors");
            var f1 = ProjectFloorService.Create(project, "f1", "Tầng 1", 0d);
            var f2 = ProjectFloorService.Create(project, "f2", "Tầng 2", 3.6d);
            if (project.ActiveFloorId != f1.Id) throw new Exception("First created floor should become active when none was active.");
            ProjectFloorService.SetActive(project, f2.Id);
            if (project.ActiveFloorId != f2.Id) throw new Exception("SetActive failed.");

            var element = new ProjectElement("e", ElementCategory.Column, "fam", f1.Id, "z");
            project.Elements.Add(element);
            var changed = ProjectFloorService.Assign(project, f2.Id, new[] { element, element });
            if (changed != 1 || element.FloorId != f2.Id) throw new Exception("Floor assignment must be distinct and deterministic.");
            if ((element.Dirty & ElementDirtyFlags.Relations) == 0) throw new Exception("Floor assignment must dirty semantic relations.");

            ProjectFloorService.Assign(project, f1.Id, new[] { element });
            ProjectFloorService.SetActive(project, f1.Id);
            ProjectFloorService.Update(project, f2.Id, "Tầng mái", 7.2d);
            if (f2.Name != "Tầng mái" || Math.Abs(f2.ElevationM - 7.2d) > 1e-12d) throw new Exception("Floor update failed.");
            if (!ProjectFloorService.Delete(project, f2.Id)) throw new Exception("Unused non-active floor delete failed.");
        }

        private static void ElevationChangeMarksGeneratedGeometryStale()
        {
            var project = new ProjectState("p2", "Floor stale");
            var floor = ProjectFloorService.Create(project, "f1", "Tầng 1", 0d);
            var element = new ProjectElement("wall", ElementCategory.ArchitecturalWall, "fam", floor.Id, "z");
            element.Properties["GeneratedSolidHandle"] = "ABCD";
            element.ClearGeneratedGeometryStale();
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            ProjectFloorService.Update(project, floor.Id, floor.Name, 0.15d);
            if (!element.IsGeneratedSolidStale()) throw new Exception("Floor elevation change must stale generated solid output.");
            if ((element.Dirty & ElementDirtyFlags.Geometry) == 0) throw new Exception("Floor elevation change must dirty geometry.");
        }

        private static void DeleteGuardsActiveAndReferencedFloors()
        {
            var project = new ProjectState("p3", "Delete guards");
            var f1 = ProjectFloorService.Create(project, "f1", "Tầng 1", 0d);
            var f2 = ProjectFloorService.Create(project, "f2", "Tầng 2", 3.6d);
            Throws<InvalidOperationException>(() => ProjectFloorService.Delete(project, f1.Id));
            ProjectFloorService.SetActive(project, f2.Id);
            var element = new ProjectElement("e", ElementCategory.Slab, "fam", f1.Id, "z");
            project.Elements.Add(element);
            Throws<InvalidOperationException>(() => ProjectFloorService.Delete(project, f1.Id));
        }

        private static void CorruptElementCollectionFailsClosed()
        {
            var project = new ProjectState("p-corrupt", "Floor atomicity");
            var f1 = ProjectFloorService.Create(project, "f1", "Tầng 1", 0d);
            var f2 = ProjectFloorService.Create(project, "f2", "Tầng 2", 3.6d);
            project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => ProjectFloorService.Update(project, f2.Id, "Tầng 2 mới", 4d));
            if (f2.Name != "Tầng 2" || Math.Abs(f2.ElevationM - 3.6d) > 1e-12d)
                throw new Exception("Rejected floor update must not partially mutate floor state.");

            Throws<InvalidOperationException>(() => ProjectFloorService.ReferenceCount(project, f2.Id));
            Throws<InvalidOperationException>(() => ProjectFloorService.Delete(project, f2.Id));
            if (!ReferenceEquals(project.FindFloor(f2.Id), f2))
                throw new Exception("Rejected floor delete must preserve project ownership.");

            Throws<InvalidOperationException>(() => ProjectFloorService.Assign(project, f2.Id, Array.Empty<ProjectElement>()));
            if (project.ActiveFloorId != f1.Id)
                throw new Exception("Rejected floor operations must not change active floor.");
        }

        private static void RejectsDuplicateNamesAndInvalidElevation()
        {
            var project = new ProjectState("p4", "Bad floors");
            ProjectFloorService.Create(project, "f1", "Tầng 1", 0d);
            Throws<InvalidOperationException>(() => ProjectFloorService.Create(project, "f2", "tầng 1", 3d));
            Throws<ArgumentOutOfRangeException>(() => ProjectFloorService.Create(project, "f3", "Tầng 3", double.NaN));
        }

        private static void RejectsDetachedSameIdElements()
        {
            var project = new ProjectState("p5", "Detached member guard");
            var f1 = ProjectFloorService.Create(project, "f1", "Tầng 1", 0d);
            var f2 = ProjectFloorService.Create(project, "f2", "Tầng 2", 3.6d);
            var owned = new ProjectElement("e", ElementCategory.Beam, "fam", f1.Id, "z");
            project.Elements.Add(owned);
            var detached = new ProjectElement("e", ElementCategory.Beam, "fam", f1.Id, "z");

            Throws<InvalidOperationException>(() => ProjectFloorService.Assign(project, f2.Id, new[] { detached }));
            if (owned.FloorId != f1.Id || detached.FloorId != f1.Id)
                throw new Exception("Rejected detached assignment must not mutate either object.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
