using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneServiceSmoke
    {
        private const int MaxAssignmentTargets = 10000;

        public static void Run()
        {
            CreateUpdateAssignAndDelete();
            AssignmentMarksGeneratedGeometryStale();
            AssignmentRejectsSpoofedSameIdElement();
            AssignmentBoundsCallerEnumeration();
            DeleteGuardsActiveAndReferencedZones();
            CorruptElementCollectionFailsClosed();
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
            var changed = ProjectZoneService.Assign(project, z2.Id, new[] { element, element });
            if (changed != 1 || element.ZoneId != z2.Id) throw new Exception("Zone assignment must be distinct and deterministic.");
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

        private static void AssignmentBoundsCallerEnumeration()
        {
            var project = new ProjectState("p-bound", "Zone target bound");
            var z1 = ProjectZoneService.Create(project, "z1", "Khu A");
            var z2 = ProjectZoneService.Create(project, "z2", "Khu B");
            var element = new ProjectElement("bounded", ElementCategory.Room, "fam", "floor", z1.Id);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var originalVersion = project.ChangeVersion;
            var originalDirty = element.Dirty;

            var counted = new GuardedTargetCollection(element, MaxAssignmentTargets + 1);
            Throws<InvalidOperationException>(() => ProjectZoneService.Assign(project, z2.Id, counted));
            if (counted.GetEnumeratorCalls != 0)
                throw new Exception("Known-count ICollection overflow must reject before target enumeration.");
            AssertAssignmentUnchanged(project, element, z1.Id, originalVersion, originalDirty, "ICollection overflow");

            var readOnlyCounted = new GuardedReadOnlyTargetCollection(element, MaxAssignmentTargets + 1);
            Throws<InvalidOperationException>(() => ProjectZoneService.Assign(project, z2.Id, readOnlyCounted));
            if (readOnlyCounted.GetEnumeratorCalls != 0)
                throw new Exception("Known-count IReadOnlyCollection overflow must reject before target enumeration.");
            AssertAssignmentUnchanged(project, element, z1.Id, originalVersion, originalDirty, "IReadOnlyCollection overflow");

            var streamed = 0;
            Throws<InvalidOperationException>(() => ProjectZoneService.Assign(
                project,
                z2.Id,
                StreamTargets(element, MaxAssignmentTargets + 2, () => streamed++)));
            if (streamed != MaxAssignmentTargets + 1)
                throw new Exception("Streaming Zone assignment must stop immediately after observing target 10,001.");
            AssertAssignmentUnchanged(project, element, z1.Id, originalVersion, originalDirty, "streaming overflow");

            var exactObserved = 0;
            var changed = ProjectZoneService.Assign(
                project,
                z2.Id,
                StreamTargets(element, MaxAssignmentTargets, () => exactObserved++));
            if (exactObserved != MaxAssignmentTargets || changed != 1 || element.ZoneId != z2.Id)
                throw new Exception("Exactly 10,000 Zone assignment target entries must remain accepted with duplicate-target collapse.");
            if (project.ChangeVersion != originalVersion + 1L)
                throw new Exception("Exact-bound Zone assignment must touch the project exactly once when one unique target changes.");

            var noOpVersion = project.ChangeVersion;
            var noOpObserved = 0;
            var noOpChanged = ProjectZoneService.Assign(
                project,
                z2.Id,
                StreamTargets(element, MaxAssignmentTargets, () => noOpObserved++));
            if (noOpObserved != MaxAssignmentTargets || noOpChanged != 0 || project.ChangeVersion != noOpVersion)
                throw new Exception("Exact-bound duplicate no-op assignment must preserve no-Touch semantics.");
        }

        private static void AssertAssignmentUnchanged(
            ProjectState project,
            ProjectElement element,
            string expectedZoneId,
            long expectedVersion,
            ElementDirtyFlags expectedDirty,
            string scenario)
        {
            if (element.ZoneId != expectedZoneId || project.ChangeVersion != expectedVersion || element.Dirty != expectedDirty)
                throw new Exception("Rejected Zone assignment must be atomic for " + scenario + ".");
        }

        private static IEnumerable<ProjectElement> StreamTargets(ProjectElement element, int count, Action onYield)
        {
            for (var i = 0; i < count; i++)
            {
                onYield();
                yield return element;
            }
        }

        private sealed class GuardedTargetCollection : ICollection<ProjectElement>
        {
            public GuardedTargetCollection(ProjectElement element, int count)
            {
                Element = element;
                Count = count;
            }

            private ProjectElement Element { get; }
            public int Count { get; }
            public bool IsReadOnly => true;
            public int GetEnumeratorCalls { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new Exception("Oversized ICollection must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(ProjectElement item) => ReferenceEquals(item, Element);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
        }

        private sealed class GuardedReadOnlyTargetCollection : IReadOnlyCollection<ProjectElement>
        {
            public GuardedReadOnlyTargetCollection(ProjectElement element, int count)
            {
                Element = element;
                Count = count;
            }

            private ProjectElement Element { get; }
            public int Count { get; }
            public int GetEnumeratorCalls { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new Exception("Oversized IReadOnlyCollection must not be enumerated: " + Element.Id);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
