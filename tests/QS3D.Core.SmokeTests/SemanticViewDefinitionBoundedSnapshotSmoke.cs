using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewDefinitionBoundedSnapshotSmoke
    {
        internal static void Run()
        {
            CategoriesStopAtFirstOverBoundItem();
            IncludeIdsStopAtFirstOverBoundItem();
            ExcludeIdsStopAtFirstOverBoundItem();
            CurrentInducedKnownCountDriftFailsBeforeRetention();
            AcceptedCollectionsRemainDefensiveSnapshots();
        }

        private static void CategoriesStopAtFirstOverBoundItem()
        {
            MustFailCapacity(
                () => new SemanticViewDefinition(
                    "V-CATEGORIES",
                    "Category bound",
                    categories: OverBoundedCategories()),
                "Semantic view supports at most 100000 categories.");
        }

        private static void IncludeIdsStopAtFirstOverBoundItem()
        {
            MustFailCapacity(
                () => new SemanticViewDefinition(
                    "V-INCLUDE",
                    "Include bound",
                    categories: new[] { ElementCategory.Beam },
                    includeElementIds: OverBoundedIds("Include source enumerated beyond the first over-bound id.")),
                "Semantic view supports at most 100000 includeElementIds.");
        }

        private static void ExcludeIdsStopAtFirstOverBoundItem()
        {
            MustFailCapacity(
                () => new SemanticViewDefinition(
                    "V-EXCLUDE",
                    "Exclude bound",
                    categories: new[] { ElementCategory.Beam },
                    excludeElementIds: OverBoundedIds("Exclude source enumerated beyond the first over-bound id.")),
                "Semantic view supports at most 100000 excludeElementIds.");
        }

        private static void CurrentInducedKnownCountDriftFailsBeforeRetention()
        {
            var source = new CurrentCountDriftingCollection<string>("E-1");
            MustFailCapacity(
                () => new SemanticViewDefinition(
                    "V-COUNT-DRIFT",
                    "Count drift",
                    includeElementIds: source),
                "Semantic view includeElementIds source Count changed during snapshot.");
            Equal(1, source.CurrentReads);
        }

        private static void AcceptedCollectionsRemainDefensiveSnapshots()
        {
            var categories = new List<ElementCategory> { ElementCategory.Beam };
            var include = new List<string> { "E-1" };
            var exclude = new List<string> { "E-2" };
            var definition = new SemanticViewDefinition(
                "V-SNAPSHOT",
                "Snapshot",
                categories: categories,
                includeElementIds: include,
                excludeElementIds: exclude);

            categories.Clear();
            include.Clear();
            exclude.Clear();

            Equal(1, definition.Categories.Count);
            Equal(ElementCategory.Beam, definition.Categories[0]);
            Equal(1, definition.IncludeElementIds.Count);
            Equal("E-1", definition.IncludeElementIds[0]);
            Equal(1, definition.ExcludeElementIds.Count);
            Equal("E-2", definition.ExcludeElementIds[0]);
        }

        private static IEnumerable<ElementCategory> OverBoundedCategories()
        {
            for (var i = 0; i <= 100000; i++) yield return ElementCategory.Beam;
            throw new ApplicationException("Category source enumerated beyond the first over-bound item.");
        }

        private static IEnumerable<string> OverBoundedIds(string sentinelMessage)
        {
            for (var i = 0; i <= 100000; i++) yield return "E";
            throw new ApplicationException(sentinelMessage);
        }

        private static void MustFailCapacity(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(ex.Message, expectedMessage, StringComparison.Ordinal))
                    throw new Exception("Unexpected Semantic View capacity error: " + ex.Message);
                return;
            }
            catch (Exception ex)
            {
                throw new Exception("Expected bounded Semantic View capacity failure, got " + ex.GetType().Name + ".", ex);
            }
            throw new Exception("Expected bounded Semantic View capacity failure.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private sealed class CurrentCountDriftingCollection<T> : IReadOnlyCollection<T>, ICollection
        {
            private readonly T _value;
            private int _count = 1;

            public CurrentCountDriftingCollection(T value)
            {
                _value = value;
            }

            public int CurrentReads { get; private set; }
            public int Count => _count;
            public object SyncRoot => this;
            public bool IsSynchronized => false;

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentCountDriftingCollection<T> _owner;
                private bool _moved;

                public Enumerator(CurrentCountDriftingCollection<T> owner)
                {
                    _owner = owner;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._count = 2;
                        return _owner._value;
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
