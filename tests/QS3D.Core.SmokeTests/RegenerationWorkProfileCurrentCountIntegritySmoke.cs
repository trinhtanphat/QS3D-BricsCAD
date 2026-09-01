using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfileCurrentCountIntegritySmoke
    {
        internal static void Run()
        {
            TargetCurrentGrowthRejectsImmediately();
            ItemCurrentShrinkRejectsImmediately();
            CategoryCurrentNegativeCountRejectsImmediately();
            CurrentCanExposeCrossInterfaceConflict();
            StableCountedInputKeepsObservationBudget();
            PureStreamingInputRemainsAccepted();
        }

        private static void TargetCurrentGrowthRejectsImmediately()
        {
            var source = new CurrentDriftCollection<string>(1, 2, "A");
            var error = Capture<ArgumentException>(() => NewProfile(source, EmptyItems(), EmptyCategories(), 3));
            Contains("known Count changed during traversal", error.Message,
                "Target Count growth triggered by Current must fail closed before retention.");
            Equal(1, source.MoveNextCalls, "Target Current drift must stop in the first item.");
            Equal(1, source.CurrentReads, "Target Current drift requires exactly one detached Current read.");
            Equal(4, source.CountReads, "Target Count must rebound immediately after Current.");
        }

        private static void ItemCurrentShrinkRejectsImmediately()
        {
            var source = new CurrentDriftCollection<RegenerationWorkItem>(1, 0, Item(0, "A"));
            var error = Capture<ArgumentException>(() => NewProfile(Array.Empty<string>(), source, EmptyCategories(), 3));
            Contains("known Count changed during traversal", error.Message,
                "Work-item Count shrink triggered by Current must fail closed before retention.");
            Equal(1, source.MoveNextCalls, "Work-item Current drift must stop in the first item.");
            Equal(1, source.CurrentReads, "Work-item Current drift requires exactly one detached Current read.");
            Equal(4, source.CountReads, "Work-item Count must rebound immediately after Current.");
        }

        private static void CategoryCurrentNegativeCountRejectsImmediately()
        {
            var source = new CurrentDriftCollection<RegenerationCategoryWork>(
                1,
                -1,
                Category(ElementCategory.ArchitecturalWall));
            var error = Capture<ArgumentException>(() => NewProfile(Array.Empty<string>(), EmptyItems(), source, 3));
            Contains("reports an invalid negative known Count", error.Message,
                "Category Count becoming negative from Current must fail before retention.");
            Equal(1, source.MoveNextCalls, "Category Current drift must stop in the first item.");
            Equal(1, source.CurrentReads, "Category Current drift requires exactly one detached Current read.");
            Equal(4, source.CountReads, "Category Count must rebound immediately after Current.");
        }

        private static void CurrentCanExposeCrossInterfaceConflict()
        {
            var source = new CrossInterfaceCurrentDriftCollection<string>("A");
            var error = Capture<ArgumentException>(() => NewProfile(source, EmptyItems(), EmptyCategories(), 3));
            Contains("reports conflicting known Counts", error.Message,
                "Post-Current validation must rebind every admitted Count interface, not only ICollection<T>.");
            Equal(1, source.CurrentReads, "Cross-interface drift must be detected after exactly one Current read.");
            Equal(1, source.MoveNextCalls, "Cross-interface drift must not advance past the offending item.");
        }

        private static void StableCountedInputKeepsObservationBudget()
        {
            var source = new CurrentDriftCollection<string>(1, 1, "A");
            var profile = NewProfile(source, EmptyItems(), EmptyCategories(), 1);
            Equal(1, profile.TargetElementIds.Count, "Stable counted target must remain accepted.");
            Equal("A", profile.TargetElementIds[0], "Stable counted target must retain its value.");
            Equal(5, source.CountReads,
                "One-item stable Count observation budget must remain admission, pre-traversal, post-MoveNext, post-Current and final publication.");
            Equal(2, source.MoveNextCalls, "Stable counted traversal must include the terminal MoveNext.");
            Equal(1, source.CurrentReads, "Stable counted traversal must read Current once.");
        }

        private static void PureStreamingInputRemainsAccepted()
        {
            var source = new StreamingProbe<string>("A");
            var profile = NewProfile(source, EmptyItems(), EmptyCategories(), 1);
            Equal(1, profile.TargetElementIds.Count, "Pure streaming input must remain accepted inside the ceiling.");
            Equal(2, source.MoveNextCalls, "Streaming traversal must include terminal MoveNext.");
            Equal(1, source.CurrentReads, "Streaming traversal must read the one retained item exactly once.");
        }

        private static RegenerationWorkProfile NewProfile(
            IEnumerable<string> targets,
            IEnumerable<RegenerationWorkItem> items,
            IEnumerable<RegenerationCategoryWork> categories,
            int projectElementCount)
        {
            return new RegenerationWorkProfile(
                "P-PROFILE-CURRENT-COUNT",
                0L,
                RegenerationWorkScope.Subset,
                targets,
                projectElementCount,
                0,
                items,
                categories,
                0,
                0);
        }

        private static RegenerationWorkItem Item(int index, string id) =>
            new RegenerationWorkItem(index, id, ElementCategory.ArchitecturalWall, ElementDirtyFlags.None, 0, 0, 0);

        private static RegenerationCategoryWork Category(ElementCategory category) =>
            new RegenerationCategoryWork(category, 0, 0);

        private static IEnumerable<RegenerationWorkItem> EmptyItems() => Array.Empty<RegenerationWorkItem>();
        private static IEnumerable<RegenerationCategoryWork> EmptyCategories() => Array.Empty<RegenerationCategoryWork>();

        private static TException Capture<TException>(Action action) where TException : Exception
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

        private sealed class CurrentDriftCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _postCurrentCount;
            private bool _currentObserved;

            internal CurrentDriftCollection(int initialCount, int postCurrentCount, params T[] items)
            {
                _initialCount = initialCount;
                _postCurrentCount = postCurrentCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _currentObserved ? _postCurrentCount : _initialCount;
                }
            }

            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly CurrentDriftCollection<T> _owner;
                private int _index = -1;

                internal ProbeEnumerator(CurrentDriftCollection<T> owner) { _owner = owner; }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._currentObserved = true;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class CrossInterfaceCurrentDriftCollection<T> :
            ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private bool _currentObserved;

            internal CrossInterfaceCurrentDriftCollection(params T[] items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count => 1;
            int IReadOnlyCollection<T>.Count => _currentObserved ? 2 : 1;
            int ICollection.Count => 1;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly CrossInterfaceCurrentDriftCollection<T> _owner;
                private int _index = -1;

                internal ProbeEnumerator(CrossInterfaceCurrentDriftCollection<T> owner) { _owner = owner; }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._currentObserved = true;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class StreamingProbe<T> : IEnumerable<T>
        {
            private readonly T[] _items;

            internal StreamingProbe(params T[] items) { _items = items; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly StreamingProbe<T> _owner;
                private int _index = -1;

                internal ProbeEnumerator(StreamingProbe<T> owner) { _owner = owner; }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class RegenerationWorkProfileCurrentCountIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RegenerationWorkProfileCurrentCountIntegritySmoke.Run();
        }
    }
}
