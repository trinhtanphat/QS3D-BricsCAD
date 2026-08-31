using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomLifecycleKnownCountNoOverreadSmoke
    {
        private static readonly DateTime UtcNow = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            ActiveOverrunDoesNotReadUnexpectedCurrent();
            SelectedCountDriftDoesNotReadIgnoredCurrent();
            SelectedHardCapRejectsBeforeCurrent5001();
        }

        private static void ActiveOverrunDoesNotReadUnexpectedCurrent()
        {
            var active = new CurrentCountingSet<string>(1, "ROOM-1", "ROOM-2");
            var error = Capture<InvalidOperationException>(() =>
                AutoRoomLifecycle.MarkStaleForSelection(
                    NewProject("ACTIVE"), active, new CurrentCountingSet<string>(0),
                    string.Empty, string.Empty, UtcNow));

            Contains("known count reported 1", error.Message, "Active overrun must retain Count mismatch verdict.");
            Equal(2, active.MoveNextCalls, "Active overrun must prove the first unexpected item.");
            Equal(1, active.CurrentReads, "Active overrun must reject before reading unexpected Current.");
        }

        private static void SelectedCountDriftDoesNotReadIgnoredCurrent()
        {
            var selected = new CurrentCountingSet<string>(1, "A", "B", "C");
            var error = Capture<InvalidOperationException>(() =>
                AutoRoomLifecycle.MarkStaleForSelection(
                    NewProject("SELECTED-DRIFT"), new CurrentCountingSet<string>(0), selected,
                    string.Empty, string.Empty, UtcNow));

            Contains("known count reported 1", error.Message, "Selected drift must retain final Count mismatch verdict.");
            Equal(4, selected.MoveNextCalls, "Selected drift must remain cardinality-only and exhaust the source.");
            Equal(1, selected.CurrentReads, "Selected drift must not observe ignored post-Count Current values.");
        }

        private static void SelectedHardCapRejectsBeforeCurrent5001()
        {
            var items = Enumerable.Range(0, 5001).Select(i => "H" + i.ToString()).ToArray();
            var selected = new CurrentCountingSet<string>(5000, items);
            var error = Capture<InvalidOperationException>(() =>
                AutoRoomLifecycle.MarkStaleForSelection(
                    NewProject("SELECTED-CAP"), new CurrentCountingSet<string>(0), selected,
                    string.Empty, string.Empty, UtcNow));

            Contains("cannot exceed 5000", error.Message, "Selected hard cap must retain precedence over final Count mismatch.");
            Equal(5001, selected.MoveNextCalls, "Selected hard cap must prove item 5001 exists.");
            Equal(5000, selected.CurrentReads, "Selected hard cap must reject before reading Current 5001.");
        }

        private static ProjectState NewProject(string suffix) =>
            new ProjectState("P-AUTOROOM-NO-OVERREAD-" + suffix, "Auto Room no-overread smoke " + suffix);

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
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

        private sealed class CurrentCountingSet<T> : ISet<T>
        {
            private readonly T[] _items;
            private readonly HashSet<T> _delegate;

            internal CurrentCountingSet(int advertisedCount, params T[] items)
            {
                Count = advertisedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _delegate = new HashSet<T>(_items);
            }

            public int Count { get; }
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentCountingSet<T> _owner;
                private int _index = -1;
                internal Enumerator(CurrentCountingSet<T> owner) { _owner = owner; }
                public T Current { get { _owner.CurrentReads++; return _owner._items[_index]; } }
                object IEnumerator.Current => Current!;
                public bool MoveNext() { _owner.MoveNextCalls++; _index++; return _index < _owner._items.Length; }
                public void Reset() { _index = -1; }
                public void Dispose() { }
            }

            public bool Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            public void ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
            public void IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();
            public bool IsProperSubsetOf(IEnumerable<T> other) => _delegate.IsProperSubsetOf(other);
            public bool IsProperSupersetOf(IEnumerable<T> other) => _delegate.IsProperSupersetOf(other);
            public bool IsSubsetOf(IEnumerable<T> other) => _delegate.IsSubsetOf(other);
            public bool IsSupersetOf(IEnumerable<T> other) => _delegate.IsSupersetOf(other);
            public bool Overlaps(IEnumerable<T> other) => _delegate.Overlaps(other);
            public bool SetEquals(IEnumerable<T> other) => _delegate.SetEquals(other);
            public void SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();
            public void UnionWith(IEnumerable<T> other) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => _delegate.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _delegate.CopyTo(array, arrayIndex);
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }

    internal static class AutoRoomLifecycleKnownCountNoOverreadRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => AutoRoomLifecycleKnownCountNoOverreadSmoke.Run();
    }
}
