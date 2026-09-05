using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotDetacherIndexerCountStabilitySmoke
    {
        internal static void Run()
        {
            RejectsNestedListIndexerDriftBeforeDestinationPublication();
            RejectsElementIndexerDriftBeforeNestedCopy();
            StableListCopyRemainsAccepted();
        }

        private static void RejectsNestedListIndexerDriftBeforeDestinationPublication()
        {
            var source = new HostileList<string>("HANDLE-1", driftOnIndexer: true);
            var destination = new TrackingList<string>();
            var method = typeof(RevisionService).Assembly
                .GetType("QS3D.Core.Revisions.RevisionSnapshotDetacher", throwOnError: true)!
                .GetMethod("CopyList", BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof(string));

            try
            {
                method.Invoke(null, new object[] { source, destination, "hostile list" });
                throw new Exception("Revision detacher accepted indexer-induced list Count drift.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException inner)
            {
                if (inner.Message.IndexOf("changed during snapshot capture", StringComparison.OrdinalIgnoreCase) < 0)
                    throw;
            }

            if (source.IndexerReads != 1)
                throw new Exception("Revision detacher list indexer observation budget changed unexpectedly.");
            if (destination.AddCalls != 0)
                throw new Exception("Revision detacher published a list item after indexer-induced Count drift.");
        }

        private static void RejectsElementIndexerDriftBeforeNestedCopy()
        {
            var nestedSourceHandles = new ProbeList<string>();
            var element = new RevisionElementSnapshot
            {
                ElementId = "E-1",
                Category = "StructuralWall"
            };
            SetBackingField(element, "<SourceHandles>k__BackingField", nestedSourceHandles);

            var hostileElements = new HostileList<RevisionElementSnapshot>(element, driftOnIndexer: true);
            var before = new RevisionSnapshot
            {
                Id = "REV-1",
                CreatedUtc = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                ProjectId = "PROJECT-1"
            };
            SetBackingField(before, "<Elements>k__BackingField", hostileElements);

            try
            {
                new RevisionService().Compare(before, StableSnapshot());
                throw new Exception("Revision detacher accepted element-indexer Count drift.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("changed during snapshot capture", StringComparison.OrdinalIgnoreCase) < 0)
                    throw;
            }

            if (hostileElements.IndexerReads != 1)
                throw new Exception("Revision detacher element indexer observation budget changed unexpectedly.");
            if (nestedSourceHandles.CountReads != 0)
                throw new Exception("Revision detacher entered nested collection copying after element-indexer Count drift.");
        }

        private static void StableListCopyRemainsAccepted()
        {
            var source = new HostileList<string>("HANDLE-1", driftOnIndexer: false);
            var destination = new TrackingList<string>();
            var method = typeof(RevisionService).Assembly
                .GetType("QS3D.Core.Revisions.RevisionSnapshotDetacher", throwOnError: true)!
                .GetMethod("CopyList", BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof(string));

            method.Invoke(null, new object[] { source, destination, "stable list" });
            if (destination.AddCalls != 1 || destination[0] != "HANDLE-1")
                throw new Exception("Revision detacher changed stable list-copy semantics.");
        }

        private static RevisionSnapshot StableSnapshot()
        {
            var snapshot = new RevisionSnapshot
            {
                Id = "REV-1",
                CreatedUtc = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                ProjectId = "PROJECT-1"
            };
            snapshot.Elements.Add(new RevisionElementSnapshot
            {
                ElementId = "E-1",
                Category = "StructuralWall"
            });
            return snapshot;
        }

        private static void SetBackingField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception(target.GetType().Name + " backing field was not found: " + name);
            field.SetValue(target, value);
        }

        private sealed class HostileList<T> : IList<T>
        {
            private readonly T _item;
            private readonly bool _driftOnIndexer;
            private int _reportedCount = 1;

            internal HostileList(T item, bool driftOnIndexer)
            {
                _item = item;
                _driftOnIndexer = driftOnIndexer;
            }

            internal int IndexerReads { get; private set; }
            public int Count => _reportedCount;
            public bool IsReadOnly => true;
            public T this[int index]
            {
                get
                {
                    if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                    IndexerReads++;
                    if (_driftOnIndexer) _reportedCount = 2;
                    return _item;
                }
                set => throw new NotSupportedException();
            }

            public IEnumerator<T> GetEnumerator() { yield return _item; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int IndexOf(T item) => EqualityComparer<T>.Default.Equals(item, _item) ? 0 : -1;
            public bool Contains(T item) => IndexOf(item) == 0;
            public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
            public void Add(T item) => throw new NotSupportedException();
            public void Insert(int index, T item) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private sealed class ProbeList<T> : IList<T>
        {
            internal int CountReads { get; private set; }
            public int Count { get { CountReads++; return 0; } }
            public bool IsReadOnly => true;
            public T this[int index] { get => throw new ArgumentOutOfRangeException(nameof(index)); set => throw new NotSupportedException(); }
            public IEnumerator<T> GetEnumerator() { yield break; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int IndexOf(T item) => -1;
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) { }
            public void Add(T item) => throw new NotSupportedException();
            public void Insert(int index, T item) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
            public void RemoveAt(int index) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private sealed class TrackingList<T> : IList<T>
        {
            private readonly List<T> _items = new List<T>();
            internal int AddCalls { get; private set; }
            public int Count => _items.Count;
            public bool IsReadOnly => false;
            public T this[int index] { get => _items[index]; set => _items[index] = value; }
            public void Add(T item) { AddCalls++; _items.Add(item); }
            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int IndexOf(T item) => _items.IndexOf(item);
            public bool Contains(T item) => _items.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Insert(int index, T item) => _items.Insert(index, item);
            public bool Remove(T item) => _items.Remove(item);
            public void RemoveAt(int index) => _items.RemoveAt(index);
            public void Clear() => _items.Clear();
        }
    }

    internal static class RevisionSnapshotDetacherIndexerCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RevisionSnapshotDetacherIndexerCountStabilitySmoke.Run();
    }
}
