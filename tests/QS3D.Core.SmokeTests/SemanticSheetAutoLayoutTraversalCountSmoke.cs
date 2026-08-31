using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetAutoLayoutTraversalCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ItemMoveNextCountDriftFailsBeforeCurrent();
            ItemCurrentCountDriftFailsBeforeRetention();
            AvailableViewMoveNextCountDriftFailsBeforeCurrent();
            AvailableViewCurrentCountDriftFailsBeforeRetention();
            StableMultiInterfaceSourcesRemainAccepted();
        }

        private static void ItemMoveNextCountDriftFailsBeforeCurrent()
        {
            var item = new SemanticSheetAutoLayoutItem("V1", 100d, 80d);
            var source = new TransientCountCollection<SemanticSheetAutoLayoutItem>(item, DriftPoint.MoveNext);

            ThrowsCountDrift(() => SemanticSheetAutoLayoutPlanner.Build(source, StableViews(), Options()));
            Equal(1, source.MoveNextCalls, "item MoveNext drift MoveNext calls");
            Equal(0, source.CurrentReads, "item MoveNext drift Current reads");
        }

        private static void ItemCurrentCountDriftFailsBeforeRetention()
        {
            var item = new SemanticSheetAutoLayoutItem("V1", 100d, 80d);
            var source = new TransientCountCollection<SemanticSheetAutoLayoutItem>(item, DriftPoint.Current);

            ThrowsCountDrift(() => SemanticSheetAutoLayoutPlanner.Build(source, StableViews(), Options()));
            Equal(1, source.MoveNextCalls, "item Current drift MoveNext calls");
            Equal(1, source.CurrentReads, "item Current drift Current reads");
        }

        private static void AvailableViewMoveNextCountDriftFailsBeforeCurrent()
        {
            var source = new TransientCountCollection<SemanticViewPlan>(BuildView("V1"), DriftPoint.MoveNext);

            ThrowsCountDrift(() => SemanticSheetAutoLayoutPlanner.Build(Array.Empty<SemanticSheetAutoLayoutItem>(), source, Options()));
            Equal(1, source.MoveNextCalls, "available-view MoveNext drift MoveNext calls");
            Equal(0, source.CurrentReads, "available-view MoveNext drift Current reads");
        }

        private static void AvailableViewCurrentCountDriftFailsBeforeRetention()
        {
            var source = new TransientCountCollection<SemanticViewPlan>(BuildView("V1"), DriftPoint.Current);

            ThrowsCountDrift(() => SemanticSheetAutoLayoutPlanner.Build(Array.Empty<SemanticSheetAutoLayoutItem>(), source, Options()));
            Equal(1, source.MoveNextCalls, "available-view Current drift MoveNext calls");
            Equal(1, source.CurrentReads, "available-view Current drift Current reads");
        }

        private static void StableMultiInterfaceSourcesRemainAccepted()
        {
            var items = new TransientCountCollection<SemanticSheetAutoLayoutItem>(
                new SemanticSheetAutoLayoutItem("V1", 100d, 80d),
                DriftPoint.None);
            var views = new TransientCountCollection<SemanticViewPlan>(BuildView("V1"), DriftPoint.None);

            var sheets = SemanticSheetAutoLayoutPlanner.Build(items, views, Options());

            Equal(1, sheets.Count, "stable sheet count");
            Equal(1, sheets[0].Placements.Count, "stable placement count");
            Equal("V1", sheets[0].Placements[0].ViewId, "stable placement view id");
            Equal(2, items.MoveNextCalls, "stable item MoveNext calls");
            Equal(1, items.CurrentReads, "stable item Current reads");
            Equal(2, views.MoveNextCalls, "stable available-view MoveNext calls");
            Equal(1, views.CurrentReads, "stable available-view Current reads");
            if (items.GenericCountReads < 7 || items.ReadOnlyCountReads < 7 || items.NonGenericCountReads < 7)
                throw new InvalidOperationException("Stable item source must have every admitted Count channel rebound around traversal and before publication.");
            if (views.GenericCountReads < 7 || views.ReadOnlyCountReads < 7 || views.NonGenericCountReads < 7)
                throw new InvalidOperationException("Stable available-view source must have every admitted Count channel rebound around traversal and before publication.");
        }

        private static SemanticViewPlan BuildView(string id)
        {
            var project = new ProjectState("P-AUTO-LAYOUT-COUNT", "Auto Layout Count Integrity");
            return SemanticViewPlanner.Build(project, new SemanticViewDefinition(id, "View " + id));
        }

        private static IReadOnlyCollection<SemanticViewPlan> StableViews()
        {
            return new[] { BuildView("V1") };
        }

        private static SemanticSheetAutoLayoutOptions Options()
        {
            return new SemanticSheetAutoLayoutOptions("COUNT", "COUNT-", "Count Integrity", 297d, 210d);
        }

        private static void ThrowsCountDrift(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException failure)
            {
                if (failure.Message.IndexOf("known Count", StringComparison.Ordinal) >= 0 ||
                    failure.Message.IndexOf("conflicting known counts", StringComparison.Ordinal) >= 0)
                    return;
                throw new InvalidOperationException("Unexpected automatic sheet Count-drift failure: " + failure.Message, failure);
            }

            throw new InvalidOperationException("Expected automatic sheet layout transient Count drift to fail closed.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private enum DriftPoint
        {
            None,
            MoveNext,
            Current
        }

        private sealed class TransientCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T _item;
            private readonly DriftPoint _driftPoint;
            private int _genericDriftReadsRemaining;

            internal TransientCountCollection(T item, DriftPoint driftPoint)
            {
                _item = item;
                _driftPoint = driftPoint;
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            internal int GenericCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }

            int ICollection<T>.Count
            {
                get
                {
                    GenericCountReads++;
                    if (_genericDriftReadsRemaining > 0)
                    {
                        _genericDriftReadsRemaining--;
                        return 2;
                    }
                    return 1;
                }
            }

            int IReadOnlyCollection<T>.Count
            {
                get
                {
                    ReadOnlyCountReads++;
                    return 1;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountReads++;
                    return 1;
                }
            }

            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_item, index);

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly TransientCountCollection<T> _owner;
                private int _state;

                internal Enumerator(TransientCountCollection<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._driftPoint == DriftPoint.Current)
                            _owner._genericDriftReadsRemaining = 1;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _state++;
                    if (_state != 1) return false;
                    if (_owner._driftPoint == DriftPoint.MoveNext)
                        _owner._genericDriftReadsRemaining = 1;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
