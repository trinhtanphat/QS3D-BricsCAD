using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneServiceSmoke
    {
        public static void Run()
        {
            CreateUpdateAssignAndDelete();
            AssignmentMarksGeneratedGeometryStale();
            AssignmentRejectsSpoofedSameIdElement();
            DeleteGuardsActiveAndReferencedZones();
            CorruptElementCollectionFailsClosed();
            NoncanonicalSemanticReferencesFailClosed();
            RejectsDuplicateNames();
        }

        private static void CreateUpdateAssignAndDelete()
        {
            var project = new ProjectState("p", "Zones");
            var z1 = ProjectZoneService.Create(project, "z1", "Khu A");
            var z2 = ProjectZoneService.Create(project, "z2", "Khu B");
            if (project.ActiveZoneId != z1.Id) throw new Exception("First created zone should become active when none was active.");
            ProjectZoneService.SetActive(project, z2.Id);
            if (project.ActiveZoneId != z2.Id) throw new Exception("SetActive failed.");

            var element = new ProjectElement("e", ElementCategory.Room, "fam", "floor", z1.Id);
            project.Elements.Add(element);
            var changed = ProjectZoneService.Assign(project, "Z2", new[] { element, element });
            if (changed != 1 || element.ZoneId != z2.Id) throw new Exception("Zone assignment must be distinct, case-insensitive, and deterministic.");
            ProjectZoneService.Assign(project, z1.Id, new[] { element });
            ProjectZoneService.SetActive(project, z1.Id);
            ProjectZoneService.Update(project, z2.Id, "Khu kỹ thuật");
            if (z2.Name != "Khu kỹ thuật") throw new Exception("Zone update failed.");
            if (!ProjectZoneService.Delete(project, z2.Id)) throw new Exception("Unused non-active zone delete failed.");
        }

        private static void AssignmentMarksGeneratedGeometryStale()
        {
            var project = new ProjectState("p2", "Zone stale");
            var z1 = ProjectZoneService.Create(project, "z1", "Khu A");
            var z2 = ProjectZoneService.Create(project, "z2", "Khu B");
            var element = new ProjectElement("wall", ElementCategory.ArchitecturalWall, "fam", "floor", z1.Id);
            element.Properties["GeneratedSolidHandle"] = "ABCD";
            element.ClearGeneratedGeometryStale();
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            ProjectZoneService.Assign(project, z2.Id, new[] { element });
            if (!element.IsGeneratedSolidStale()) throw new Exception("Zone assignment must stale generated solid output.");
            if ((element.Dirty & ElementDirtyFlags.Relations) == 0) throw new Exception("Zone assignment must dirty relations.");
        }

        private static void AssignmentRejectsSpoofedSameIdElement()
        {
            var project = new ProjectState("p-spoof", "Zone ownership");
            var z1 = ProjectZoneService.Create(project, "z1", "Khu A");
            var z2 = ProjectZoneService.Create(project, "z2", "Khu B");
            var owned = new ProjectElement("same-id", ElementCategory.Room, "fam", "floor", z1.Id);
            project.Elements.Add(owned);
            var spoofed = new ProjectElement("same-id", ElementCategory.Room, "fam", "floor", z1.Id);

            Throws<InvalidOperationException>(() => ProjectZoneService.Assign(project, z2.Id, new[] { spoofed }));
            if (owned.ZoneId != z1.Id) throw new Exception("Rejected spoofed assignment must not mutate the project-owned element.");
            if (spoofed.ZoneId != z1.Id) throw new Exception("Rejected spoofed assignment must not mutate the foreign element.");
        }

        private static void DeleteGuardsActiveAndReferencedZones()
        {
            var project = new ProjectState("p3", "Delete guards");
            var z1 = ProjectZoneService.Create(project, "z1", "Khu A");
            var z2 = ProjectZoneService.Create(project, "z2", "Khu B");
            Throws<InvalidOperationException>(() => ProjectZoneService.Delete(project, z1.Id));
            ProjectZoneService.SetActive(project, z2.Id);
            var element = new ProjectElement("e", ElementCategory.Slab, "fam", "floor", z1.Id);
            project.Elements.Add(element);
            Throws<InvalidOperationException>(() => ProjectZoneService.Delete(project, z1.Id));
        }

        private static void CorruptElementCollectionFailsClosed()
        {
            var project = new ProjectState("p-corrupt", "Zone atomicity");
            var z1 = ProjectZoneService.Create(project, "z1", "Khu A");
            var z2 = ProjectZoneService.Create(project, "z2", "Khu B");
            project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => ProjectZoneService.Update(project, z2.Id, "Khu B mới"));
            if (z2.Name != "Khu B") throw new Exception("Rejected zone update must not partially mutate the zone name.");

            Throws<InvalidOperationException>(() => ProjectZoneService.ReferenceCount(project, z2.Id));
            Throws<InvalidOperationException>(() => ProjectZoneService.Delete(project, z2.Id));
            if (!ReferenceEquals(project.FindZone(z2.Id), z2)) throw new Exception("Rejected zone delete must leave the zone owned by the project.");

            Throws<InvalidOperationException>(() => ProjectZoneService.Assign(project, z2.Id, Array.Empty<ProjectElement>()));
            if (project.ActiveZoneId != z1.Id) throw new Exception("Rejected zone operations must not change the active zone.");
        }

        private static void NoncanonicalSemanticReferencesFailClosed()
        {
            RejectsPaddedProjectElementId();
            RejectsPaddedPersistedZoneReference();
            RejectsPaddedActiveZoneReference();
        }

        private static void RejectsPaddedProjectElementId()
        {
            foreach (var id in new[] { " e1", "e1 ", " e1 ", "\te1" })
            {
                var project = new ProjectState("p-id", "Zone canonical element id");
                var z1 = ProjectZoneService.Create(project, "z1", "Khu A");
                var z2 = ProjectZoneService.Create(project, "z2", "Khu B");
                var element = new ProjectElement(id, ElementCategory.Room, "fam", "floor", z1.Id);
                project.Elements.Add(element);
                var before = project.ChangeVersion;

                Throws<InvalidOperationException>(() => ProjectZoneService.Assign(project, z2.Id, new[] { element }));
                if (project.ChangeVersion != before || element.ZoneId != z1.Id)
                    throw new Exception("Rejected padded semantic id must not mutate Zone assignment state.");
                Throws<InvalidOperationException>(() => ProjectZoneService.ReferenceCount(project, z1.Id));
            }
        }

        private static void RejectsPaddedPersistedZoneReference()
        {
            foreach (var zoneId in new[] { " z1", "z1 ", " z1 ", "\tz1" })
            {
                var project = new ProjectState("p-ref", "Zone canonical reference");
                var z1 = ProjectZoneService.Create(project, "z1", "Khu A");
                var z2 = ProjectZoneService.Create(project, "z2", "Khu B");
                var element = new ProjectElement("e1", ElementCategory.Room, "fam", "floor", zoneId);
                project.Elements.Add(element);
                var before = project.ChangeVersion;

                Throws<InvalidOperationException>(() => ProjectZoneService.ReferenceCount(project, z1.Id));
                Throws<InvalidOperationException>(() => ProjectZoneService.Delete(project, z1.Id));
                Throws<InvalidOperationException>(() => ProjectZoneService.Assign(project, z2.Id, new[] { element }));
                if (project.ChangeVersion != before || element.ZoneId != zoneId)
                    throw new Exception("Rejected padded Zone reference must remain atomic.");
            }
        }

        private static void RejectsPaddedActiveZoneReference()
        {
            var project = new ProjectState("p-active", "Zone canonical active reference");
            var z1 = ProjectZoneService.Create(project, "z1", "Khu A");
            var z2 = ProjectZoneService.Create(project, "z2", "Khu B");
            ProjectZoneService.SetActive(project, z2.Id);
            project.ActiveZoneId = " z2 ";
            var before = project.ChangeVersion;

            Throws<InvalidOperationException>(() => ProjectZoneService.Delete(project, z1.Id));
            if (project.ChangeVersion != before || !ReferenceEquals(project.FindZone(z1.Id), z1))
                throw new Exception("Rejected noncanonical active Zone reference must not mutate project state.");
        }

        private static void RejectsDuplicateNames()
        {
            var project = new ProjectState("p4", "Bad zones");
            ProjectZoneService.Create(project, "z1", "Khu A");
            Throws<InvalidOperationException>(() => ProjectZoneService.Create(project, "z2", "khu a"));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}