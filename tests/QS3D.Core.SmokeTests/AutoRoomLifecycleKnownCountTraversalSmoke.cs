using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomLifecycleKnownCountTraversalSmoke
    {
        private static readonly DateTime UtcNow = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            SelectedSetRejectsUnderAndOverYield();
            ActiveSetRejectsUnderAndOverYield();
            ExactKnownCountsRemainAccepted();
            SelectedCapacityPreflightStillPrecedesEnumeration();
        }

        private static void SelectedSetRejectsUnderAndOverYield()
        {
            AssertCountMismatch(
                new MisreportedSet<string>(0),
                new MisreportedSet<string>(2, "A"),
                "Selected-source under-yield must reject advertised Count/traversal disagreement.");
            AssertCountMismatch(
                new MisreportedSet<string>(0),
                new MisreportedSet<string>(1, "A", "B"),
                "Selected-source over-yield must reject advertised Count/traversal disagreement.");
        }

        private static void ActiveSetRejectsUnderAndOverYield()
        {
            AssertCountMismatch(
                new MisreportedSet<string>(2, "ROOM-1"),
                new MisreportedSet<string>(0),
                "Active-room under-yield must reject advertised Count/traversal disagreement.");
            AssertCountMismatch(
                new MisreportedSet<string>(1, "ROOM-1", "ROOM-2"),
                new MisreportedSet<string>(0),
                "Active-room over-yield must reject advertised Count/traversal disagreement.");
        }

        private static void ExactKnownCountsRemainAccepted()
        {
            var project = new ProjectState("P-AUTOROOM-COUNT", "Auto Room Count Smoke");
            var result = AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new MisreportedSet<string>(2, " ROOM-1 ", "ROOM-2"),
                new MisreportedSet<string>(2, " a ", "B"),
                string.Empty,
                string.Empty,
                UtcNow);

            Equal(0, result.Count, "Exact known Count inputs must retain ordinary empty-project behavior.");
            Equal(0L, project.ChangeVersion, "Exact-count validation must not mutate an empty project.");
        }

        private static void SelectedCapacityPreflightStillPrecedesEnumeration()
        {
            var selected = new ThrowingSet<string>(5001);
            var error = Capture<InvalidOperationException>(() =>
                AutoRoomLifecycle.MarkStaleForSelection(
                    new ProjectState("P-AUTOROOM-CAP", "Auto Room Capacity Smoke"),
                    new MisreportedSet<string>(0),
                    selected,
                    string.Empty,
                    string.Empty,
                    UtcNow));

            Contains("cannot exceed 5000", error.Message,
                "Oversized advertised selected-source Count must retain the existing capacity failure.");
            Equal(0, selected.GetEnumeratorCalls,
                "Oversized advertised selected-source Count must fail before enumeration.");
        }

        private static void AssertCountMismatch(ISet<string> active, ISet<string> selected, string message)
        {
            var project = new ProjectState("P-AUTOROOM-MISMATCH", "Auto Room Mismatch Smoke");
            var version = project.ChangeVersion;
            var error = Capture<InvalidOperationException>(() =>
                AutoRoomLifecycle.MarkStaleForSelection(
                    project,
                    active,
                    selected,
                    string.Empty,
                    string.Empty,
                    UtcNow));

            Contains("known count reported", error.Message, message);
            Equal(version, project.ChangeVersion, "Count/traversal mismatch must fail before project mutation.");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class MisreportedSet<T> : ISet<T>
        {
            private readonly T[] _items;
            private readonly HashSet<T> _delegate;

            internal MisreportedSet(int advertisedCount, params T[] items)
            {
                Count = advertisedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _delegate = new HashSet<T>(_items);
            }

            public int Count { get; }
            public bool IsReadOnly => false;
            public bool Add(T item) => _delegate.Add(item);
            void ICollection<T>.Add(T item) => _delegate.Add(item);
            public void ExceptWith(IEnumerable<T> other) => _delegate.ExceptWith(other);
            public void IntersectWith(IEnumerable<T> other) => _delegate.IntersectWith(other);
            public bool IsProperSubsetOf(IEnumerable<T> other) => _delegate.IsProperSubsetOf(other);
            public bool IsProperSupersetOf(IEnumerable<T> other) => _delegate.IsProperSupersetOf(other);
            public bool IsSubsetOf(IEnumerable<T> other) => _delegate.IsSubsetOf(other);
            public bool IsSupersetOf(IEnumerable<T> other) => _delegate.IsSupersetOf(other);
            public bool Overlaps(IEnumerable<T> other) => _delegate.Overlaps(other);
            public bool SetEquals(IEnumerable<T> other) => _delegate.SetEquals(other);
            public void SymmetricExceptWith(IEnumerable<T> other) => _delegate.SymmetricExceptWith(other);
            public void UnionWith(IEnumerable<T> other) => _delegate.UnionWith(other);
            public void Clear() => _delegate.Clear();
            public bool Contains(T item) => _delegate.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _delegate.CopyTo(array, arrayIndex);
            public bool Remove(T item) => _delegate.Remove(item);
            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                    yield return _items[i];
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ThrowingSet<T> : ISet<T>
        {
            internal ThrowingSet(int count) { Count = count; }
            public int Count { get; }
            public bool IsReadOnly => true;
            internal int GetEnumeratorCalls { get; private set; }
            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized set must not be enumerated.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            public void ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
            public void IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();
            public bool IsProperSubsetOf(IEnumerable<T> other) => throw new NotSupportedException();
            public bool IsProperSupersetOf(IEnumerable<T> other) => throw new NotSupportedException();
            public bool IsSubsetOf(IEnumerable<T> other) => throw new NotSupportedException();
            public bool IsSupersetOf(IEnumerable<T> other) => throw new NotSupportedException();
            public bool Overlaps(IEnumerable<T> other) => throw new NotSupportedException();
            public bool SetEquals(IEnumerable<T> other) => throw new NotSupportedException();
            public void SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
            public void UnionWith(IEnumerable<T> other) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }

    internal static class AutoRoomLifecycleKnownCountTraversalRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AutoRoomLifecycleKnownCountTraversalSmoke.Run();
        }
    }
}
