using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditCanonicalizationSmoke
    {
        public static void Run()
        {
            SetPropertyUsesCanonicalKeyAndGeometryDirtyPolicy();
            MultiplyNumericPropertyUsesCanonicalKey();
            CorruptProjectFailsBeforeBulkMutation();
            ObjectBasedBulkEditsRejectNullTargets();
            IdBasedBulkEditsRejectIncompleteTargetSets();
            KnownCountContractsFailClosedBeforeEnumeration();
            KnownCountTraversalMismatchFailsClosed();
            CurrentRebindsKnownCountBeforeAcceptance();
            HonestKnownAndStreamingInputsRemainAccepted();
            FamilyAssignmentRejectsIncompatibleBatch();
        }

        private static void SetPropertyUsesCanonicalKeyAndGeometryDirtyPolicy()
        {
            var project = new ProjectState("P1", "Bulk");
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);

            var changed = new BulkEditService().SetProperty(project, new[] { wall }, " WidthM ", "0.25");
            if (changed.Count != 1 || changed[0] != "W1") throw new Exception("Bulk set must report the canonical owned element once.");
            if (!wall.Properties.TryGetValue("WidthM", out var width) || width != "0.25") throw new Exception("Bulk set must write the canonical trimmed property key.");
            if (wall.Properties.Keys.Any(key => key != key.Trim())) throw new Exception("Bulk set must not create padded property keys.");
            if ((wall.Dirty & ElementDirtyFlags.Geometry) == 0) throw new Exception("Canonical geometry property bulk set must mark generated geometry dirty.");
        }

        private static void MultiplyNumericPropertyUsesCanonicalKey()
        {
            var project = new ProjectState("P1", "Bulk");
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);

            var changed = new BulkEditService().MultiplyNumericProperty(project, new[] { wall }, " WidthM ", 2d);
            if (changed.Count != 1 || changed[0] != "W1") throw new Exception("Bulk multiply must report the canonical owned element once.");
            if (!wall.Properties.TryGetValue("WidthM", out var width) || width != "0.4") throw new Exception("Bulk multiply must read/write the canonical trimmed property key.");
            if (wall.Properties.Keys.Any(key => key != key.Trim())) throw new Exception("Bulk multiply must not create padded property keys.");
            if ((wall.Dirty & ElementDirtyFlags.Geometry) == 0) throw new Exception("Canonical geometry property bulk multiply must mark generated geometry dirty.");
        }

        private static void CorruptProjectFailsBeforeBulkMutation()
        {
            var project = new ProjectState("P-CORRUPT", "Bulk atomicity");
            var familyA = new ProjectFamily("F-A", "Tường A", ElementCategory.ArchitecturalWall);
            var familyB = new ProjectFamily("F-B", "Tường B", ElementCategory.ArchitecturalWall);
            project.Families.Add(familyA);
            project.Families.Add(familyB);
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, familyA.Id, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);
            project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => new BulkEditService().SetProperty(project, new[] { wall }, "WidthM", "0.25"));
            if (wall.Properties["WidthM"] != "0.2") throw new Exception("Rejected bulk set must not partially mutate a target.");

            Throws<InvalidOperationException>(() => new BulkEditService().MultiplyNumericProperty(project, new[] { wall }, "WidthM", 2d));
            if (wall.Properties["WidthM"] != "0.2") throw new Exception("Rejected bulk multiply must not partially mutate a target.");

            Throws<InvalidOperationException>(() => new BulkEditService().AssignFamily(project, new[] { wall.Id }, familyB.Id));
            if (wall.FamilyId != familyA.Id) throw new Exception("Rejected bulk family assignment must not partially mutate a target.");
        }

        private static void ObjectBasedBulkEditsRejectNullTargets()
        {
            var project = new ProjectState("P-OBJECT-NULL", "Bulk object target atomicity");
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);
            var service = new BulkEditService();
            var version = project.ChangeVersion;
            var dirty = wall.Dirty;
            var targets = new ProjectElement[] { wall, null! };

            Throws<InvalidOperationException>(() => service.SetProperty(project, targets, "WidthM", "0.25"));
            if (wall.Properties["WidthM"] != "0.2" || wall.Dirty != dirty || project.ChangeVersion != version)
                throw new Exception("Null object target must reject bulk set before any semantic mutation.");

            Throws<InvalidOperationException>(() => service.MultiplyNumericProperty(project, targets, "WidthM", 2d));
            if (wall.Properties["WidthM"] != "0.2" || wall.Dirty != dirty || project.ChangeVersion != version)
                throw new Exception("Null object target must reject bulk multiply before any semantic mutation.");
        }

        private static void IdBasedBulkEditsRejectIncompleteTargetSets()
        {
            var project = new ProjectState("P-ID", "Bulk target identity");
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);
            var service = new BulkEditService();
            var version = project.ChangeVersion;

            Throws<KeyNotFoundException>(() => service.SetProperty(project, new[] { "W1", "W404" }, "WidthM", "0.25"));
            if (wall.Properties["WidthM"] != "0.2" || project.ChangeVersion != version)
                throw new Exception("Missing bulk target must reject the whole batch before mutation.");

            Throws<ArgumentException>(() => service.SetProperty(project, new[] { "W1", "   " }, "WidthM", "0.25"));
            if (wall.Properties["WidthM"] != "0.2" || project.ChangeVersion != version)
                throw new Exception("Blank bulk target must reject the whole batch before mutation.");

            Throws<InvalidOperationException>(() => service.SetProperty(project, new[] { "W1", "w1" }, "WidthM", "0.25"));
            if (wall.Properties["WidthM"] != "0.2" || project.ChangeVersion != version)
                throw new Exception("Duplicate bulk target must reject the whole batch before mutation.");
        }

        private static void KnownCountContractsFailClosedBeforeEnumeration()
        {
            var project = ProjectWithTwoWalls(out var wall1, out _);
            var service = new BulkEditService();
            var version = project.ChangeVersion;
            var dirty = wall1.Dirty;

            var negativeIds = new MultiCountCollection<string>(new[] { wall1.Id }, -1, -1, -1, throwOnEnumeration: true);
            ThrowsMessage<InvalidOperationException>(() => service.SetProperty(project, negativeIds, "WidthM", "0.25"), "invalid negative input count");
            if (negativeIds.EnumerationRequested) throw new Exception("Negative known BulkEdit ID count must fail before enumeration.");

            var conflictingObjects = new MultiCountCollection<ProjectElement>(new[] { wall1 }, 1, 2, 1, throwOnEnumeration: true);
            ThrowsMessage<InvalidOperationException>(() => service.SetProperty(project, conflictingObjects, "WidthM", "0.25"), "conflicting known input counts");
            if (conflictingObjects.EnumerationRequested) throw new Exception("Conflicting known BulkEdit object counts must fail before enumeration.");

            var oversizedNonGeneric = new MultiCountCollection<string>(new[] { wall1.Id }, 1, 1, 10001, throwOnEnumeration: true);
            ThrowsMessage<InvalidOperationException>(() => service.SetProperty(project, oversizedNonGeneric, "WidthM", "0.25"), "cannot exceed 10000");
            if (oversizedNonGeneric.EnumerationRequested) throw new Exception("Oversized non-generic BulkEdit ID count must fail before enumeration.");

            if (wall1.Properties["WidthM"] != "0.2" || wall1.Dirty != dirty || project.ChangeVersion != version)
                throw new Exception("Malformed known BulkEdit counts must not mutate semantic state.");
        }

        private static void KnownCountTraversalMismatchFailsClosed()
        {
            var project = ProjectWithTwoWalls(out var wall1, out var wall2);
            var service = new BulkEditService();
            var version = project.ChangeVersion;
            var dirty1 = wall1.Dirty;
            var dirty2 = wall2.Dirty;

            var underIds = new MultiCountCollection<string>(new[] { wall1.Id }, 2, 2, 2, throwOnEnumeration: false);
            ThrowsMessage<InvalidOperationException>(() => service.SetProperty(project, underIds, "WidthM", "0.25"), "input count changed during enumeration");

            var overObjects = new MultiCountCollection<ProjectElement>(new[] { wall1, wall2 }, 1, 1, 1, throwOnEnumeration: false);
            ThrowsMessage<InvalidOperationException>(() => service.SetProperty(project, overObjects, "WidthM", "0.25"), "input count changed during enumeration");

            if (wall1.Properties["WidthM"] != "0.2" || wall2.Properties["WidthM"] != "0.3" ||
                wall1.Dirty != dirty1 || wall2.Dirty != dirty2 || project.ChangeVersion != version)
                throw new Exception("Known Count traversal mismatch must fail before BulkEdit mutation.");
        }

        private static void CurrentRebindsKnownCountBeforeAcceptance()
        {
            var project = ProjectWithTwoWalls(out var wall1, out var wall2);
            var service = new BulkEditService();

            var objectTargets = new CountReadCollection<ProjectElement>(new[] { wall1 });
            var changed = service.SetProperty(project, objectTargets, "WidthM", "0.25");
            if (changed.Count != 1 || changed[0] != wall1.Id || wall1.Properties["WidthM"] != "0.25")
                throw new Exception("Count-instrumented BulkEdit object target must remain accepted.");
            if (objectTargets.CurrentReads != 1)
                throw new Exception("BulkEdit object target Current must be read exactly once.");
            if (objectTargets.CountReads != 7)
                throw new Exception("BulkEdit object target Count must rebound immediately after Current; expected 7 Count reads, got " + objectTargets.CountReads + ".");

            var idTargets = new CountReadCollection<string>(new[] { wall2.Id });
            if (service.SetProperty(project, idTargets, "WidthM", "0.35") != 1 || wall2.Properties["WidthM"] != "0.35")
                throw new Exception("Count-instrumented BulkEdit id target must remain accepted.");
            if (idTargets.CurrentReads != 1)
                throw new Exception("BulkEdit id target Current must be read exactly once.");
            if (idTargets.CountReads != 7)
                throw new Exception("BulkEdit id target Count must rebound immediately after Current; expected 7 Count reads, got " + idTargets.CountReads + ".");
        }

        private static void HonestKnownAndStreamingInputsRemainAccepted()
        {
            var project = ProjectWithTwoWalls(out var wall1, out var wall2);
            var service = new BulkEditService();

            var countedIds = new MultiCountCollection<string>(new[] { wall1.Id }, 1, 1, 1, throwOnEnumeration: false);
            if (service.SetProperty(project, countedIds, "WidthM", "0.25") != 1 || wall1.Properties["WidthM"] != "0.25")
                throw new Exception("Honest counted BulkEdit ID input must remain accepted.");

            var countedObjects = new MultiCountCollection<ProjectElement>(new[] { wall2 }, 1, 1, 1, throwOnEnumeration: false);
            var changed = service.SetProperty(project, countedObjects, "WidthM", "0.35");
            if (changed.Count != 1 || changed[0] != wall2.Id || wall2.Properties["WidthM"] != "0.35")
                throw new Exception("Honest counted BulkEdit object input must remain accepted.");

            if (service.SetProperty(project, Stream(wall1.Id), "WidthM", "0.4") != 1 || wall1.Properties["WidthM"] != "0.4")
                throw new Exception("Pure streaming BulkEdit ID input must remain supported.");
        }

        private static void FamilyAssignmentRejectsIncompatibleBatch()
        {
            var project = new ProjectState("P-CATEGORY", "Bulk family category atomicity");
            var wallA = new ProjectFamily("FW-A", "Wall A", ElementCategory.ArchitecturalWall);
            var wallB = new ProjectFamily("FW-B", "Wall B", ElementCategory.ArchitecturalWall);
            var columnFamily = new ProjectFamily("FC", "Column", ElementCategory.Column);
            project.Families.Add(wallA);
            project.Families.Add(wallB);
            project.Families.Add(columnFamily);
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, wallA.Id, string.Empty, string.Empty);
            var column = new ProjectElement("C1", ElementCategory.Column, columnFamily.Id, string.Empty, string.Empty);
            project.Elements.Add(wall);
            project.Elements.Add(column);
            var version = project.ChangeVersion;

            Throws<InvalidOperationException>(() => new BulkEditService().AssignFamily(project, new[] { wall.Id, column.Id }, wallB.Id));
            if (wall.FamilyId != wallA.Id || column.FamilyId != columnFamily.Id || project.ChangeVersion != version)
                throw new Exception("Incompatible family assignment must reject the whole batch without silently skipping targets.");
        }

        private static ProjectState ProjectWithTwoWalls(out ProjectElement wall1, out ProjectElement wall2)
        {
            var project = new ProjectState("P-COUNT", "Bulk known Count");
            wall1 = new ProjectElement("W1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall2 = new ProjectElement("W2", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            wall1.Properties["WidthM"] = "0.2";
            wall2.Properties["WidthM"] = "0.3";
            wall1.MarkClean(ElementDirtyFlags.All);
            wall2.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall1);
            project.Elements.Add(wall2);
            return project;
        }

        private static IEnumerable<string> Stream(string id)
        {
            yield return id;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void ThrowsMessage<T>(Action action, string messageFragment) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(messageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Expected " + typeof(T).Name + " containing '" + messageFragment + "', got: " + ex.Message);
                return;
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private sealed class CountReadCollection<T> : ICollection<T>
        {
            private readonly T[] _items;

            public CountReadCollection(T[] items)
            {
                _items = items;
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _items.Length;
                }
            }

            public int CountReads { get; private set; }
            public int CurrentReads { get; private set; }
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator() => new CountingEnumerator(this, _items);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class CountingEnumerator : IEnumerator<T>
            {
                private readonly CountReadCollection<T> _owner;
                private readonly T[] _items;
                private int _index = -1;

                public CountingEnumerator(CountReadCollection<T> owner, T[] items)
                {
                    _owner = owner;
                    _items = items;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    if (_index + 1 >= _items.Length)
                    {
                        _index = _items.Length;
                        return false;
                    }
                    _index++;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class MultiCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            public MultiCountCollection(T[] items, int genericCount, int readOnlyCount, int nonGenericCount, bool throwOnEnumeration)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            public bool EnumerationRequested { get; private set; }
            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationRequested = true;
                if (_throwOnEnumeration) throw new Exception("Enumerator must not be requested.");
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }
    }
}
