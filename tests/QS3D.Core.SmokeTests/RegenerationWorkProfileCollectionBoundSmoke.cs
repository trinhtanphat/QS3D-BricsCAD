using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationWorkProfileCollectionBoundSmoke
    {
        internal static void Run()
        {
            TargetCollectionStopsAtProjectCardinality();
            ItemCollectionStopsAtProjectCardinality();
            CategoryCollectionStopsAtProjectCardinality();
            MalformedKnownCountsFailBeforeEnumeration();
            KnownCountTraversalMismatchFailsClosed();
            HonestKnownCountsRemainAccepted();
            ExactProjectCardinalityRemainsAccepted();
        }

        private static void TargetCollectionStopsAtProjectCardinality()
        {
            var error = Throws<ArgumentException>(() => NewProfile(ThreeTargetsThenSentinel(), Array.Empty<RegenerationWorkItem>(), Array.Empty<RegenerationCategoryWork>()));
            Contains(error.Message, "target element collection cannot exceed project element count of 2");
        }

        private static void ItemCollectionStopsAtProjectCardinality()
        {
            var error = Throws<ArgumentException>(() => NewProfile(Array.Empty<string>(), ThreeItemsThenSentinel(), Array.Empty<RegenerationCategoryWork>()));
            Contains(error.Message, "work item collection cannot exceed project element count of 2");
        }

        private static void CategoryCollectionStopsAtProjectCardinality()
        {
            var error = Throws<ArgumentException>(() => NewProfile(Array.Empty<string>(), Array.Empty<RegenerationWorkItem>(), ThreeCategoriesThenSentinel()));
            Contains(error.Message, "category collection cannot exceed project element count of 2");
        }

        private static void MalformedKnownCountsFailBeforeEnumeration()
        {
            var negativeTargets = new CountContractCollection<string>(-1, -1, -1, new[] { "E1" });
            var negativeError = Throws<ArgumentException>(() => NewProfile(negativeTargets, Array.Empty<RegenerationWorkItem>(), Array.Empty<RegenerationCategoryWork>()));
            Contains(negativeError.Message, "invalid negative known Count");
            Equal(false, negativeTargets.EnumeratorRequested);

            var oversizedItems = new CountContractCollection<RegenerationWorkItem>(3, 3, 3, new[] { Item(0, "E1", ElementCategory.Grid) });
            var oversizedError = Throws<ArgumentException>(() => NewProfile(Array.Empty<string>(), oversizedItems, Array.Empty<RegenerationCategoryWork>()));
            Contains(oversizedError.Message, "work item collection cannot exceed project element count of 2");
            Equal(false, oversizedItems.EnumeratorRequested);

            var conflictingCategories = new CountContractCollection<RegenerationCategoryWork>(1, 2, 1, new[]
            {
                new RegenerationCategoryWork(ElementCategory.Grid, 1, 0)
            });
            var conflictError = Throws<ArgumentException>(() => NewProfile(Array.Empty<string>(), Array.Empty<RegenerationWorkItem>(), conflictingCategories));
            Contains(conflictError.Message, "conflicting known Counts");
            Equal(false, conflictingCategories.EnumeratorRequested);
        }

        private static void KnownCountTraversalMismatchFailsClosed()
        {
            var shortTargets = new CountContractCollection<string>(2, 2, 2, new[] { "E1" });
            var shortError = Throws<ArgumentException>(() => NewProfile(shortTargets, Array.Empty<RegenerationWorkItem>(), Array.Empty<RegenerationCategoryWork>()));
            Contains(shortError.Message, "known Count reported 2 entries but traversal produced 1");
            Equal(true, shortTargets.EnumeratorRequested);

            var longItems = new CountContractCollection<RegenerationWorkItem>(1, 1, 1, new[]
            {
                Item(0, "E1", ElementCategory.Grid),
                Item(1, "E2", ElementCategory.Room)
            });
            var longError = Throws<ArgumentException>(() => NewProfile(Array.Empty<string>(), longItems, Array.Empty<RegenerationCategoryWork>()));
            Contains(longError.Message, "known Count reported 1 entries but traversal produced 2");
            Equal(true, longItems.EnumeratorRequested);
        }

        private static void HonestKnownCountsRemainAccepted()
        {
            var targets = new CountContractCollection<string>(2, 2, 2, new[] { "E2", "E1" });
            var items = new CountContractCollection<RegenerationWorkItem>(1, 1, 1, new[]
            {
                Item(0, "E1", ElementCategory.Grid)
            });
            var categories = new CountContractCollection<RegenerationCategoryWork>(1, 1, 1, new[]
            {
                new RegenerationCategoryWork(ElementCategory.Grid, 1, 0)
            });

            var profile = NewProfile(targets, items, categories);
            Equal(2, profile.TargetElementIds.Count);
            Equal("E2", profile.TargetElementIds[0]);
            Equal("E1", profile.TargetElementIds[1]);
            Equal(1, profile.Items.Count);
            Equal("E1", profile.Items[0].ElementId);
            Equal(1, profile.Categories.Count);
            Equal(ElementCategory.Grid, profile.Categories[0].Category);
            Equal(true, targets.EnumeratorRequested);
            Equal(true, items.EnumeratorRequested);
            Equal(true, categories.EnumeratorRequested);
        }

        private static void ExactProjectCardinalityRemainsAccepted()
        {
            var targets = new[] { "E2", "E1" };
            var items = new[]
            {
                Item(0, "E1", ElementCategory.Grid),
                Item(1, "E2", ElementCategory.Room)
            };
            var categories = new[]
            {
                new RegenerationCategoryWork(ElementCategory.Grid, 1, 0),
                new RegenerationCategoryWork(ElementCategory.Room, 1, 0)
            };
            var profile = NewProfile(targets, items, categories);
            Equal(2, profile.TargetElementIds.Count);
            Equal("E2", profile.TargetElementIds[0]);
            Equal("E1", profile.TargetElementIds[1]);
            Equal(2, profile.Items.Count);
            Equal("E1", profile.Items[0].ElementId);
            Equal("E2", profile.Items[1].ElementId);
            Equal(2, profile.Categories.Count);
            Equal(ElementCategory.Grid, profile.Categories[0].Category);
            Equal(ElementCategory.Room, profile.Categories[1].Category);
        }

        private static RegenerationWorkProfile NewProfile(
            IEnumerable<string> targets,
            IEnumerable<RegenerationWorkItem> items,
            IEnumerable<RegenerationCategoryWork> categories)
        {
            return new RegenerationWorkProfile(
                "P-PROFILE-BOUND",
                0L,
                RegenerationWorkScope.Subset,
                targets,
                2,
                0,
                items,
                categories,
                0,
                0);
        }

        private static RegenerationWorkItem Item(int orderIndex, string elementId, ElementCategory category) =>
            new RegenerationWorkItem(orderIndex, elementId, category, ElementDirtyFlags.None, 0, 0, 0);

        private static IEnumerable<string> ThreeTargetsThenSentinel()
        {
            yield return "E1";
            yield return "E2";
            yield return "E3";
            throw new InvalidOperationException("Target DTO enumeration continued beyond project cardinality.");
        }

        private static IEnumerable<RegenerationWorkItem> ThreeItemsThenSentinel()
        {
            yield return Item(0, "E1", ElementCategory.Grid);
            yield return Item(1, "E2", ElementCategory.Room);
            yield return Item(2, "E3", ElementCategory.Beam);
            throw new InvalidOperationException("Work-item DTO enumeration continued beyond project cardinality.");
        }

        private static IEnumerable<RegenerationCategoryWork> ThreeCategoriesThenSentinel()
        {
            yield return new RegenerationCategoryWork(ElementCategory.Grid, 1, 0);
            yield return new RegenerationCategoryWork(ElementCategory.Room, 1, 0);
            yield return new RegenerationCategoryWork(ElementCategory.Beam, 1, 0);
            throw new InvalidOperationException("Category DTO enumeration continued beyond project cardinality.");
        }

        private sealed class CountContractCollection<T> : ICollection<T>, IReadOnlyCollection<T>, System.Collections.ICollection
        {
            private readonly IReadOnlyList<T> _values;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal CountContractCollection(int genericCount, int readOnlyCount, int nonGenericCount, IReadOnlyList<T> values)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _values = values ?? throw new ArgumentNullException(nameof(values));
            }

            internal bool EnumeratorRequested { get; private set; }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int System.Collections.ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool System.Collections.ICollection.IsSynchronized => false;
            object System.Collections.ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                EnumeratorRequested = true;
                return _values.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => throw new NotSupportedException();
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void System.Collections.ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T error) { return error; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string value, string expected)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected text containing '" + expected + "', got '" + (value ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class RegenerationWorkProfileCollectionBoundSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegenerationWorkProfileCollectionBoundSmoke.Run();
    }
}
