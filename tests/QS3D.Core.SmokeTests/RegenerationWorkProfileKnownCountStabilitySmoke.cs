using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfileKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            TargetOverrunRejectsBeforeSecondCurrent();
            ItemOverrunRejectsBeforeSecondCurrent();
            CategoryOverrunRejectsBeforeSecondCurrent();
            UnderYieldStillFailsClosed();
            PostTraversalCountDriftStillFailsClosed();
            TransientTargetCountGrowthRejectsBeforeCurrent();
            TransientItemCountShrinkRejectsBeforeCurrent();
            TransientCategoryNegativeCountRejectsBeforeCurrent();
            StreamingCeilingRejectsBeforeOverflowCurrent();
            HonestCountedInputsRemainAccepted();
        }

        private static void TargetOverrunRejectsBeforeSecondCurrent()
        {
            var source = new CountProbeCollection<string>(1, 1, "A", "B");
            var error = Capture<ArgumentException>(() => NewProfile(source, EmptyItems(), EmptyCategories(), 3));
            Contains("known Count reported 1 entries but traversal produced 2", error.Message,
                "Target Count overrun must fail closed.");
            Equal(2, source.MoveNextCalls, "Target overrun must observe the boundary MoveNext.");
            Equal(1, source.CurrentReads, "Target overrun must reject before reading Current beyond admitted Count.");
        }

        private static void ItemOverrunRejectsBeforeSecondCurrent()
        {
            var source = new CountProbeCollection<RegenerationWorkItem>(
                1,
                1,
                Item(0, "A"),
                Item(1, "B"));
            var error = Capture<ArgumentException>(() => NewProfile(Array.Empty<string>(), source, EmptyCategories(), 3));
            Contains("work item collection known Count reported 1 entries but traversal produced 2", error.Message,
                "Work-item Count overrun must fail closed.");
            Equal(2, source.MoveNextCalls, "Work-item overrun must observe the boundary MoveNext.");
            Equal(1, source.CurrentReads, "Work-item overrun must reject before reading Current beyond admitted Count.");
        }

        private static void CategoryOverrunRejectsBeforeSecondCurrent()
        {
            var source = new CountProbeCollection<RegenerationCategoryWork>(
                1,
                1,
                Category(ElementCategory.ArchitecturalWall),
                Category(ElementCategory.Slab));
            var error = Capture<ArgumentException>(() => NewProfile(Array.Empty<string>(), EmptyItems(), source, 3));
            Contains("category collection known Count reported 1 entries but traversal produced 2", error.Message,
                "Category Count overrun must fail closed.");
            Equal(2, source.MoveNextCalls, "Category overrun must observe the boundary MoveNext.");
            Equal(1, source.CurrentReads, "Category overrun must reject before reading Current beyond admitted Count.");
        }

        private static void UnderYieldStillFailsClosed()
        {
            var source = new CountProbeCollection<string>(2, 2, "A");
            var error = Capture<ArgumentException>(() => NewProfile(source, EmptyItems(), EmptyCategories(), 3));
            Contains("known Count reported 2 entries but traversal produced 1", error.Message,
                "Known Count under-yield must remain rejected.");
            Equal(1, source.CurrentReads, "Under-yield must read only the item actually produced.");
        }

        private static void PostTraversalCountDriftStillFailsClosed()
        {
            var source = new CountProbeCollection<string>(1, 2, "A");
            var error = Capture<ArgumentException>(() => NewProfile(source, EmptyItems(), EmptyCategories(), 3));
            Contains("known Count changed during traversal", error.Message,
                "Post-traversal Count drift must remain rejected.");
            Equal(5, source.CountReads, "Count evidence must be rebound at traversal boundaries and after traversal.");
            Equal(1, source.CurrentReads, "Drift validation must not introduce extra Current reads.");
        }

        private static void TransientTargetCountGrowthRejectsBeforeCurrent()
        {
            var source = new TransientCountProbeCollection<string>(1, 2, "A");
            var error = Capture<ArgumentException>(() => NewProfile(source, EmptyItems(), EmptyCategories(), 3));
            Contains("known Count changed during traversal", error.Message,
                "Target Count growth after MoveNext must fail closed immediately.");
            Equal(1, source.MoveNextCalls, "Target transient drift must stop at the first successful MoveNext.");
            Equal(0, source.CurrentReads, "Target transient drift must reject before Current under changed Count evidence.");
            Equal(3, source.CountReads, "Target Count must be sampled at admission, before MoveNext, and after successful MoveNext.");
        }

        private static void TransientItemCountShrinkRejectsBeforeCurrent()
        {
            var source = new TransientCountProbeCollection<RegenerationWorkItem>(2, 1, Item(0, "A"));
            var error = Capture<ArgumentException>(() => NewProfile(Array.Empty<string>(), source, EmptyCategories(), 3));
            Contains("known Count changed during traversal", error.Message,
                "Work-item Count shrink after MoveNext must fail closed immediately.");
            Equal(1, source.MoveNextCalls, "Work-item transient drift must stop at the first successful MoveNext.");
            Equal(0, source.CurrentReads, "Work-item transient drift must reject before Current under changed Count evidence.");
            Equal(3, source.CountReads, "Work-item Count must be sampled at admission, before MoveNext, and after successful MoveNext.");
        }

        private static void TransientCategoryNegativeCountRejectsBeforeCurrent()
        {
            var source = new TransientCountProbeCollection<RegenerationCategoryWork>(
                1,
                -1,
                Category(ElementCategory.ArchitecturalWall));
            var error = Capture<ArgumentException>(() => NewProfile(Array.Empty<string>(), EmptyItems(), source, 3));
            Contains("reports an invalid negative known Count", error.Message,
                "Category Count becoming negative after MoveNext must fail closed immediately.");
            Equal(1, source.MoveNextCalls, "Category transient negative Count must stop at the first successful MoveNext.");
            Equal(0, source.CurrentReads, "Category transient negative Count must reject before Current.");
            Equal(3, source.CountReads, "Category Count must be sampled at admission, before MoveNext, and after successful MoveNext.");
        }

        private static void StreamingCeilingRejectsBeforeOverflowCurrent()
        {
            var source = new StreamingProbe<string>("A", "B");
            var error = Capture<ArgumentException>(() => NewProfile(source, EmptyItems(), EmptyCategories(), 1));
            Contains("cannot exceed project element count of 1", error.Message,
                "Pure streaming inputs must respect the project-element ceiling.");
            Equal(2, source.MoveNextCalls, "Streaming ceiling must observe the overflow MoveNext.");
            Equal(1, source.CurrentReads, "Streaming ceiling must reject before overflow Current is read.");
        }

        private static void HonestCountedInputsRemainAccepted()
        {
            var targets = new CountProbeCollection<string>(1, 1, "A");
            var items = new CountProbeCollection<RegenerationWorkItem>(1, 1, Item(0, "A"));
            var categories = new CountProbeCollection<RegenerationCategoryWork>(1, 1, Category(ElementCategory.ArchitecturalWall));
            var profile = NewProfile(targets, items, categories, 1);

            Equal(1, profile.TargetElementIds.Count, "Honest target input must remain accepted.");
            Equal(1, profile.Items.Count, "Honest work-item input must remain accepted.");
            Equal(1, profile.Categories.Count, "Honest category input must remain accepted.");
            Equal(5, targets.CountReads, "Honest target Count must remain stable across every traversal boundary.");
            Equal(5, items.CountReads, "Honest item Count must remain stable across every traversal boundary.");
            Equal(5, categories.CountReads, "Honest category Count must remain stable across every traversal boundary.");
        }

        private static RegenerationWorkProfile NewProfile(
            IEnumerable<string> targets,
            IEnumerable<RegenerationWorkItem> items,
            IEnumerable<RegenerationCategoryWork> categories,
            int projectElementCount)
        {
            return new RegenerationWorkProfile(
                "P-COUNT-STABILITY",
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

        private sealed class CountProbeCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _postTraversalCount;
            private bool _completed;

            internal CountProbeCollection(int initialCount, int postTraversalCount, params T[] items)
            {
                _initialCount = initialCount;
                _postTraversalCount = postTraversalCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _completed ? _postTraversalCount : _initialCount;
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
                private readonly CountProbeCollection<T> _owner;
                private int _index = -1;

                internal ProbeEnumerator(CountProbeCollection<T> owner) { _owner = owner; }

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
                    if (_index < _owner._items.Length) return true;
                    _owner._completed = true;
                    return false;
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

        private sealed class TransientCountProbeCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int _initialCount;
            private readonly int _transientCount;
            private bool _afterMoveNext;

            internal TransientCountProbeCollection(int initialCount, int transientCount, params T[] items)
            {
                _initialCount = initialCount;
                _transientCount = transientCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _afterMoveNext ? _transientCount : _initialCount;
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
                private readonly TransientCountProbeCollection<T> _owner;
                private int _index = -1;

                internal ProbeEnumerator(TransientCountProbeCollection<T> owner) { _owner = owner; }

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
                    _owner._afterMoveNext = true;
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

    internal static class RegenerationWorkProfileKnownCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RegenerationWorkProfileKnownCountStabilitySmoke.Run();
        }
    }
}