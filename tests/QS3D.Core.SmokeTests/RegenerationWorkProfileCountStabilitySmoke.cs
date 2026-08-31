using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfileCountStabilitySmoke
    {
        internal static void Run()
        {
            TargetCountDriftFailsClosed();
            WorkItemCountDriftFailsClosed();
            CategoryCountDriftFailsClosed();
            StableCountedAndStreamingSourcesRemainAccepted();
        }

        private static void TargetCountDriftFailsClosed()
        {
            var source = new DriftingCountCollection<string>(new[] { "E1" }, 1, 2);
            var error = Throws<ArgumentException>(() => NewProfile(source, Array.Empty<RegenerationWorkItem>(), Array.Empty<RegenerationCategoryWork>()));
            Contains(error.Message, "known Count changed during traversal");
        }

        private static void WorkItemCountDriftFailsClosed()
        {
            var source = new DriftingCountCollection<RegenerationWorkItem>(new[] { Item(0, "E1") }, 1, -1);
            var error = Throws<ArgumentException>(() => NewProfile(Array.Empty<string>(), source, Array.Empty<RegenerationCategoryWork>()));
            Contains(error.Message, "invalid negative known Count");
        }

        private static void CategoryCountDriftFailsClosed()
        {
            var source = new DriftingCountCollection<RegenerationCategoryWork>(
                new[] { new RegenerationCategoryWork(ElementCategory.Grid, 1, 0) }, 1, 2, 1);
            var error = Throws<ArgumentException>(() => NewProfile(Array.Empty<string>(), Array.Empty<RegenerationWorkItem>(), source));
            Contains(error.Message, "conflicting known Counts");
        }

        private static void StableCountedAndStreamingSourcesRemainAccepted()
        {
            var counted = new DriftingCountCollection<string>(new[] { "E1" }, 1, 1);
            var countedProfile = NewProfile(counted, Array.Empty<RegenerationWorkItem>(), Array.Empty<RegenerationCategoryWork>());
            Equal(1, countedProfile.TargetElementIds.Count);

            var streamingProfile = NewProfile(Stream("E1"), Array.Empty<RegenerationWorkItem>(), Array.Empty<RegenerationCategoryWork>());
            Equal(1, streamingProfile.TargetElementIds.Count);
        }

        private static RegenerationWorkProfile NewProfile(
            IEnumerable<string> targets,
            IEnumerable<RegenerationWorkItem> items,
            IEnumerable<RegenerationCategoryWork> categories) =>
            new RegenerationWorkProfile("P-COUNT-STABILITY", 0L, RegenerationWorkScope.Subset, targets, 2, 0, items, categories, 0, 0);

        private static RegenerationWorkItem Item(int index, string id) =>
            new RegenerationWorkItem(index, id, ElementCategory.Grid, ElementDirtyFlags.None, 0, 0, 0);

        private static IEnumerable<string> Stream(string value)
        {
            yield return value;
        }

        private sealed class DriftingCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly IReadOnlyList<T> _values;
            private readonly int _before;
            private readonly int _after;
            private readonly int? _afterReadOnly;
            private bool _completed;

            internal DriftingCountCollection(IReadOnlyList<T> values, int before, int after, int? afterReadOnly = null)
            {
                _values = values;
                _before = before;
                _after = after;
                _afterReadOnly = afterReadOnly;
            }

            int ICollection<T>.Count => _completed ? _after : _before;
            int IReadOnlyCollection<T>.Count => _completed ? (_afterReadOnly ?? _after) : _before;
            int ICollection.Count => _completed ? _after : _before;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator() => new CompletingEnumerator(this, _values.GetEnumerator());
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => throw new NotSupportedException();
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class CompletingEnumerator : IEnumerator<T>
            {
                private readonly DriftingCountCollection<T> _owner;
                private readonly IEnumerator<T> _inner;

                internal CompletingEnumerator(DriftingCountCollection<T> owner, IEnumerator<T> inner)
                {
                    _owner = owner;
                    _inner = inner;
                }

                public T Current => _inner.Current;
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    var moved = _inner.MoveNext();
                    if (!moved) _owner._completed = true;
                    return moved;
                }

                public void Reset() => _inner.Reset();
                public void Dispose() => _inner.Dispose();
            }
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException error) { return error; }
            throw new Exception("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string value, string expected)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected text containing '" + expected + "', got '" + (value ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class RegenerationWorkProfileCountStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegenerationWorkProfileCountStabilitySmoke.Run();
    }
}
