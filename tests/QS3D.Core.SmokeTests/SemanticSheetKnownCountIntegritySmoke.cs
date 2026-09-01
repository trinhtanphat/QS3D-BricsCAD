using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PlacementOverrunRejectsBeforeCurrent();
            PlacementPostTraversalCountDriftRejects();
            CatalogOverrunRejectsBeforeCurrent();
            CatalogPostTraversalCountDriftRejects();
            AvailableViewOverrunRejectsBeforeCurrent();
            AvailableViewPostTraversalCountDriftRejects();
        }

        private static void PlacementOverrunRejectsBeforeCurrent()
        {
            var source = new CurrentTrackingCollection<SemanticSheetPlacementDefinition>(
                _ => 1,
                NewPlacement("VIEW-P1"),
                NewPlacement("VIEW-P2"));

            ThrowsCountIntegrity(
                () => new SemanticSheetDefinition("S-P", "P-001", "Placement integrity", 1000d, 1000d, source),
                "placement Count overrun");
            Require(source.MoveNextCalls == 2,
                "Semantic Sheet placement Count overrun must observe the first unexpected successful MoveNext.");
            Require(source.CurrentReads == 1,
                "Semantic Sheet placement Count overrun exposed IEnumerator.Current beyond the admitted Count.");
        }

        private static void PlacementPostTraversalCountDriftRejects()
        {
            var source = new CurrentTrackingCollection<SemanticSheetPlacementDefinition>(
                read => read <= 6 ? 1 : 2,
                NewPlacement("VIEW-P1"));

            ThrowsCountIntegrity(
                () => new SemanticSheetDefinition("S-PD", "PD-001", "Placement drift", 1000d, 1000d, source),
                "placement post-traversal Count drift");
            Require(source.CountReads >= 7,
                "Semantic Sheet placement Count evidence was not rebound after traversal.");
            Require(source.CurrentReads == 1,
                "Semantic Sheet placement Count drift test must traverse exactly one admitted placement.");
        }

        private static void CatalogOverrunRejectsBeforeCurrent()
        {
            var source = new CurrentTrackingCollection<SemanticSheetDefinition>(
                _ => 1,
                NewSheet("S-C1", "C-001"),
                NewSheet("S-C2", "C-002"));

            ThrowsCountIntegrity(
                () => SemanticSheetPlanner.BuildCatalog(source, Array.Empty<SemanticViewPlan>()),
                "catalog Count overrun");
            Require(source.MoveNextCalls == 2,
                "Semantic Sheet catalog Count overrun must observe the first unexpected successful MoveNext.");
            Require(source.CurrentReads == 1,
                "Semantic Sheet catalog Count overrun exposed IEnumerator.Current beyond the admitted Count.");
        }

        private static void CatalogPostTraversalCountDriftRejects()
        {
            var source = new CurrentTrackingCollection<SemanticSheetDefinition>(
                read => read <= 6 ? 1 : 2,
                NewSheet("S-CD", "CD-001"));

            ThrowsCountIntegrity(
                () => SemanticSheetPlanner.BuildCatalog(source, Array.Empty<SemanticViewPlan>()),
                "catalog post-traversal Count drift");
            Require(source.CountReads >= 7,
                "Semantic Sheet catalog Count evidence was not rebound after traversal.");
            Require(source.CurrentReads == 1,
                "Semantic Sheet catalog Count drift test must traverse exactly one admitted sheet.");
        }

        private static void AvailableViewOverrunRejectsBeforeCurrent()
        {
            var source = new CurrentTrackingCollection<SemanticViewPlan>(
                _ => 1,
                NewView("VIEW-V1"),
                NewView("VIEW-V2"));

            ThrowsCountIntegrity(
                () => SemanticSheetPlanner.BuildCatalog(Array.Empty<SemanticSheetDefinition>(), source),
                "available-view Count overrun");
            Require(source.MoveNextCalls == 2,
                "Semantic Sheet available-view Count overrun must observe the first unexpected successful MoveNext.");
            Require(source.CurrentReads == 1,
                "Semantic Sheet available-view Count overrun exposed IEnumerator.Current beyond the admitted Count.");
        }

        private static void AvailableViewPostTraversalCountDriftRejects()
        {
            var source = new CurrentTrackingCollection<SemanticViewPlan>(
                read => read <= 6 ? 1 : 2,
                NewView("VIEW-VD"));

            ThrowsCountIntegrity(
                () => SemanticSheetPlanner.BuildCatalog(Array.Empty<SemanticSheetDefinition>(), source),
                "available-view post-traversal Count drift");
            Require(source.CountReads >= 7,
                "Semantic Sheet available-view Count evidence was not rebound after traversal.");
            Require(source.CurrentReads == 1,
                "Semantic Sheet available-view Count drift test must traverse exactly one admitted view.");
        }

        private static SemanticSheetPlacementDefinition NewPlacement(string viewId) =>
            new SemanticSheetPlacementDefinition(viewId, 0d, 0d, 100d, 100d);

        private static SemanticSheetDefinition NewSheet(string id, string number) =>
            new SemanticSheetDefinition(
                id,
                number,
                "Sheet " + number,
                1000d,
                1000d,
                Array.Empty<SemanticSheetPlacementDefinition>());

        private static SemanticViewPlan NewView(string id)
        {
            var project = new ProjectState("sheet-integrity-" + id, "Sheet integrity " + id);
            return SemanticViewPlanner.Build(project, new SemanticViewDefinition(id, "View " + id));
        }

        private static void ThrowsCountIntegrity(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(
                    "SemanticSheetKnownCountIntegritySmoke " + label + " returned the wrong diagnostic: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException(
                "SemanticSheetKnownCountIntegritySmoke did not reject " + label + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class CurrentTrackingCollection<T> : ICollection<T>
        {
            private readonly Func<int, int> _countForRead;
            private readonly T[] _items;

            public CurrentTrackingCollection(Func<int, int> countForRead, params T[] items)
            {
                _countForRead = countForRead ?? throw new ArgumentNullException(nameof(countForRead));
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _countForRead(CountReads);
                }
            }

            public int CountReads { get; private set; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator() => new TrackingEnumerator(this, _items);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class TrackingEnumerator : IEnumerator<T>
            {
                private readonly CurrentTrackingCollection<T> _owner;
                private readonly T[] _items;
                private int _index = -1;

                public TrackingEnumerator(CurrentTrackingCollection<T> owner, T[] items)
                {
                    _owner = owner;
                    _items = items;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _items.Length)
                            throw new InvalidOperationException("Current requested outside the active Semantic Sheet traversal item.");
                        return _items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
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
    }
}
