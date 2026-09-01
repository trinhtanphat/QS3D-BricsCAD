using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetTransientCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PlacementMoveNextDriftFailsBeforeCurrent();
            PlacementCurrentDriftFailsBeforeRetention();
            CatalogMoveNextDriftFailsBeforeCurrent();
            CatalogCurrentDriftFailsBeforeRetention();
            AvailableViewMoveNextDriftFailsBeforeCurrent();
            AvailableViewCurrentDriftFailsBeforeRetention();
            StableCountedControlsRemainAccepted();
        }

        private static void PlacementMoveNextDriftFailsBeforeCurrent()
        {
            var source = new TransientCountCollection<SemanticSheetPlacementDefinition>(
                DriftMode.MoveNext,
                NewPlacement("VIEW-P-MOVE"));

            ThrowsCountIntegrity(
                () => new SemanticSheetDefinition("S-P-MOVE", "P-MOVE", "Placement MoveNext drift", 1000d, 1000d, source),
                "placement MoveNext-induced Count drift");
            Require(source.MoveNextCalls == 1, "placement MoveNext drift must fail on the first successful MoveNext");
            Require(source.CurrentReads == 0, "placement MoveNext drift must fail before Current");
        }

        private static void PlacementCurrentDriftFailsBeforeRetention()
        {
            var source = new TransientCountCollection<SemanticSheetPlacementDefinition>(
                DriftMode.Current,
                NewPlacement("VIEW-P-CURRENT"));

            ThrowsCountIntegrity(
                () => new SemanticSheetDefinition("S-P-CURRENT", "P-CURRENT", "Placement Current drift", 1000d, 1000d, source),
                "placement Current-induced Count drift");
            Require(source.CurrentReads == 1, "placement Current drift must read exactly one admitted Current");
            Require(source.MoveNextCalls == 1, "placement Current drift must fail before the next MoveNext can restore Count");
        }

        private static void CatalogMoveNextDriftFailsBeforeCurrent()
        {
            var source = new TransientCountCollection<SemanticSheetDefinition>(
                DriftMode.MoveNext,
                NewSheet("S-C-MOVE", "C-MOVE"));

            ThrowsCountIntegrity(
                () => SemanticSheetPlanner.BuildCatalog(source, Array.Empty<SemanticViewPlan>()),
                "catalog MoveNext-induced Count drift");
            Require(source.MoveNextCalls == 1, "catalog MoveNext drift must fail on the first successful MoveNext");
            Require(source.CurrentReads == 0, "catalog MoveNext drift must fail before Current");
        }

        private static void CatalogCurrentDriftFailsBeforeRetention()
        {
            var source = new TransientCountCollection<SemanticSheetDefinition>(
                DriftMode.Current,
                NewSheet("S-C-CURRENT", "C-CURRENT"));

            ThrowsCountIntegrity(
                () => SemanticSheetPlanner.BuildCatalog(source, Array.Empty<SemanticViewPlan>()),
                "catalog Current-induced Count drift");
            Require(source.CurrentReads == 1, "catalog Current drift must read exactly one admitted Current");
            Require(source.MoveNextCalls == 1, "catalog Current drift must fail before the next MoveNext can restore Count");
        }

        private static void AvailableViewMoveNextDriftFailsBeforeCurrent()
        {
            var source = new TransientCountCollection<SemanticViewPlan>(
                DriftMode.MoveNext,
                NewView("VIEW-V-MOVE"));

            ThrowsCountIntegrity(
                () => SemanticSheetPlanner.BuildCatalog(Array.Empty<SemanticSheetDefinition>(), source),
                "available-view MoveNext-induced Count drift");
            Require(source.MoveNextCalls == 1, "available-view MoveNext drift must fail on the first successful MoveNext");
            Require(source.CurrentReads == 0, "available-view MoveNext drift must fail before Current");
        }

        private static void AvailableViewCurrentDriftFailsBeforeRetention()
        {
            var source = new TransientCountCollection<SemanticViewPlan>(
                DriftMode.Current,
                NewView("VIEW-V-CURRENT"));

            ThrowsCountIntegrity(
                () => SemanticSheetPlanner.BuildCatalog(Array.Empty<SemanticSheetDefinition>(), source),
                "available-view Current-induced Count drift");
            Require(source.CurrentReads == 1, "available-view Current drift must read exactly one admitted Current");
            Require(source.MoveNextCalls == 1, "available-view Current drift must fail before the next MoveNext can restore Count");
        }

        private static void StableCountedControlsRemainAccepted()
        {
            var placementSource = new TransientCountCollection<SemanticSheetPlacementDefinition>(
                DriftMode.None,
                NewPlacement("VIEW-STABLE-P"));
            var sheet = new SemanticSheetDefinition(
                "S-STABLE",
                "STABLE-001",
                "Stable sheet",
                1000d,
                1000d,
                placementSource);
            Require(sheet.Placements.Count == 1, "stable counted placement source must remain accepted");

            var catalogSource = new TransientCountCollection<SemanticSheetDefinition>(DriftMode.None, sheet);
            var catalog = SemanticSheetPlanner.BuildCatalog(catalogSource, new[] { NewView("VIEW-STABLE-P") });
            Require(catalog.Count == 1, "stable counted catalog source must remain accepted");

            var viewSource = new TransientCountCollection<SemanticViewPlan>(DriftMode.None, NewView("VIEW-STABLE-V"));
            var emptyCatalog = SemanticSheetPlanner.BuildCatalog(Array.Empty<SemanticSheetDefinition>(), viewSource);
            Require(emptyCatalog.Count == 0, "stable counted available-view source must remain accepted");
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
            var project = new ProjectState("sheet-transient-" + id, "Sheet transient " + id);
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
                    "SemanticSheetTransientCountStabilitySmoke " + label + " returned the wrong diagnostic: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException(
                "SemanticSheetTransientCountStabilitySmoke did not reject " + label + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private enum DriftMode
        {
            None,
            MoveNext,
            Current
        }

        private sealed class TransientCountCollection<T> : ICollection<T>
        {
            private readonly DriftMode _mode;
            private readonly T[] _items;
            private int _countState;

            public TransientCountCollection(DriftMode mode, params T[] items)
            {
                _mode = mode;
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _countState = _items.Length;
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    return _countState;
                }
            }

            public int CountReads { get; private set; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator() => new TrackingEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private void RestoreCount() => _countState = _items.Length;
            private void DriftCount() => _countState = _items.Length + 1;

            private sealed class TrackingEnumerator : IEnumerator<T>
            {
                private readonly TransientCountCollection<T> _owner;
                private int _index = -1;

                public TrackingEnumerator(TransientCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _owner._items.Length)
                            throw new InvalidOperationException("Current requested outside the active Semantic Sheet traversal item.");

                        if (_owner._mode == DriftMode.MoveNext)
                            _owner.RestoreCount();
                        else if (_owner._mode == DriftMode.Current)
                            _owner.DriftCount();

                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_owner._mode == DriftMode.Current)
                        _owner.RestoreCount();

                    if (_index + 1 >= _owner._items.Length)
                    {
                        _index = _owner._items.Length;
                        return false;
                    }

                    _index++;
                    if (_owner._mode == DriftMode.MoveNext)
                        _owner.DriftCount();
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
