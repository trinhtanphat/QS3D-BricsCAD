using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetPlannerKnownCountContractSmoke
    {
        private const int Limit = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            SheetDefinitionsRejectMalformedKnownCountsBeforeEnumeration();
            AvailableViewsRejectMalformedKnownCountsBeforeEnumeration();
            NonGenericKnownCountRejectsBeforeEnumeration();
            SheetDefinitionsRejectKnownCountTraversalMismatch();
            AvailableViewsRejectKnownCountTraversalMismatch();
            ConsistentKnownCountsRemainAccepted();
            HonestCountedInputsRemainAccepted();
            PureStreamingInputsRemainAccepted();
        }

        private static void SheetDefinitionsRejectMalformedKnownCountsBeforeEnumeration()
        {
            AssertRejectedBeforeEnumeration(
                new MultiCountSource<SemanticSheetDefinition>(Limit + 1, Limit + 1, Limit + 1),
                source => SemanticSheetPlanner.BuildCatalog(source, Array.Empty<SemanticViewPlan>()),
                "oversized sheet definitions");
            AssertRejectedBeforeEnumeration(
                new MultiCountSource<SemanticSheetDefinition>(-1, -1, -1),
                source => SemanticSheetPlanner.BuildCatalog(source, Array.Empty<SemanticViewPlan>()),
                "negative sheet-definition Count");
            AssertRejectedBeforeEnumeration(
                new MultiCountSource<SemanticSheetDefinition>(1, 2, 1),
                source => SemanticSheetPlanner.BuildCatalog(source, Array.Empty<SemanticViewPlan>()),
                "conflicting sheet-definition Count contracts");
        }

        private static void AvailableViewsRejectMalformedKnownCountsBeforeEnumeration()
        {
            AssertRejectedBeforeEnumeration(
                new MultiCountSource<SemanticViewPlan>(Limit + 1, Limit + 1, Limit + 1),
                source => SemanticSheetPlanner.BuildCatalog(Array.Empty<SemanticSheetDefinition>(), source),
                "oversized available views");
            AssertRejectedBeforeEnumeration(
                new MultiCountSource<SemanticViewPlan>(-1, -1, -1),
                source => SemanticSheetPlanner.BuildCatalog(Array.Empty<SemanticSheetDefinition>(), source),
                "negative available-view Count");
            AssertRejectedBeforeEnumeration(
                new MultiCountSource<SemanticViewPlan>(1, 2, 1),
                source => SemanticSheetPlanner.BuildCatalog(Array.Empty<SemanticSheetDefinition>(), source),
                "conflicting available-view Count contracts");
        }

        private static void NonGenericKnownCountRejectsBeforeEnumeration()
        {
            var sheets = new NonGenericCountSource<SemanticSheetDefinition>(Limit + 1);
            AssertRejectedBeforeEnumeration(
                sheets,
                source => SemanticSheetPlanner.BuildCatalog(source, Array.Empty<SemanticViewPlan>()),
                "non-generic sheet-definition Count");

            var views = new NonGenericCountSource<SemanticViewPlan>(Limit + 1);
            AssertRejectedBeforeEnumeration(
                views,
                source => SemanticSheetPlanner.BuildCatalog(Array.Empty<SemanticSheetDefinition>(), source),
                "non-generic available-view Count");
        }

        private static void SheetDefinitionsRejectKnownCountTraversalMismatch()
        {
            ThrowsTraversalMismatch(
                () => SemanticSheetPlanner.BuildCatalog(
                    new CountTraversalSource<SemanticSheetDefinition>(2, NewSheet("SHEET-U1", "U-001")),
                    Array.Empty<SemanticViewPlan>()),
                "sheet-definition under-enumeration");

            ThrowsTraversalMismatch(
                () => SemanticSheetPlanner.BuildCatalog(
                    new CountTraversalSource<SemanticSheetDefinition>(
                        1,
                        NewSheet("SHEET-O1", "O-001"),
                        NewSheet("SHEET-O2", "O-002")),
                    Array.Empty<SemanticViewPlan>()),
                "sheet-definition over-enumeration");
        }

        private static void AvailableViewsRejectKnownCountTraversalMismatch()
        {
            var view1 = NewView("VIEW-U1");
            ThrowsTraversalMismatch(
                () => SemanticSheetPlanner.BuildCatalog(
                    Array.Empty<SemanticSheetDefinition>(),
                    new CountTraversalSource<SemanticViewPlan>(2, view1)),
                "available-view under-enumeration");

            var view2 = NewView("VIEW-O1");
            var view3 = NewView("VIEW-O2");
            ThrowsTraversalMismatch(
                () => SemanticSheetPlanner.BuildCatalog(
                    Array.Empty<SemanticSheetDefinition>(),
                    new CountTraversalSource<SemanticViewPlan>(1, view2, view3)),
                "available-view over-enumeration");
        }

        private static void ConsistentKnownCountsRemainAccepted()
        {
            var sheets = new MultiCountSource<SemanticSheetDefinition>(0, 0, 0, allowEnumeration: true);
            var views = new MultiCountSource<SemanticViewPlan>(0, 0, 0, allowEnumeration: true);
            var result = SemanticSheetPlanner.BuildCatalog(sheets, views);
            if (result.Count != 0)
                throw new InvalidOperationException("SemanticSheetPlannerKnownCountContractSmoke expected an empty catalog for consistent zero counts.");
            if (!sheets.Enumerated || !views.Enumerated)
                throw new InvalidOperationException("SemanticSheetPlannerKnownCountContractSmoke must continue to enumerate valid consistent inputs.");
        }

        private static void HonestCountedInputsRemainAccepted()
        {
            var result = SemanticSheetPlanner.BuildCatalog(
                new CountTraversalSource<SemanticSheetDefinition>(1, NewSheet("SHEET-H1", "H-001")),
                new CountTraversalSource<SemanticViewPlan>(1, NewView("VIEW-H1")));

            if (result.Count != 1 || result[0].Id != "SHEET-H1")
                throw new InvalidOperationException("SemanticSheetPlannerKnownCountContractSmoke rejected or changed honest counted inputs.");
        }

        private static void PureStreamingInputsRemainAccepted()
        {
            var result = SemanticSheetPlanner.BuildCatalog(
                Stream(NewSheet("SHEET-S2", "S-002"), NewSheet("SHEET-S1", "S-001")),
                Stream(NewView("VIEW-S1")));

            if (result.Count != 2 || result[0].Id != "SHEET-S1" || result[1].Id != "SHEET-S2")
                throw new InvalidOperationException("SemanticSheetPlannerKnownCountContractSmoke changed pure streaming support or deterministic ordering.");
        }

        private static SemanticSheetDefinition NewSheet(string id, string number)
        {
            return new SemanticSheetDefinition(
                id,
                number,
                "Sheet " + number,
                1000d,
                1000d,
                Array.Empty<SemanticSheetPlacementDefinition>());
        }

        private static SemanticViewPlan NewView(string id)
        {
            var project = new ProjectState("sheet-planner-count-" + id, "Sheet planner count " + id);
            return SemanticViewPlanner.Build(project, new SemanticViewDefinition(id, "View " + id));
        }

        private static IEnumerable<T> Stream<T>(params T[] items)
        {
            for (var i = 0; i < items.Length; i++) yield return items[i];
        }

        private static void ThrowsTraversalMismatch(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("traversal count does not match", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(
                    "SemanticSheetPlannerKnownCountContractSmoke " + label + " returned the wrong diagnostic: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException(
                "SemanticSheetPlannerKnownCountContractSmoke did not reject " + label + ".");
        }

        private static void AssertRejectedBeforeEnumeration<T>(
            TrackingEnumerable<T> source,
            Action<IEnumerable<T>> action,
            string label)
        {
            try
            {
                action(source);
            }
            catch (InvalidOperationException)
            {
                if (source.Enumerated)
                    throw new InvalidOperationException(
                        "SemanticSheetPlannerKnownCountContractSmoke enumerated " + label + " before rejecting its known Count contract.");
                return;
            }

            throw new InvalidOperationException(
                "SemanticSheetPlannerKnownCountContractSmoke did not fail closed for " + label + ".");
        }

        private abstract class TrackingEnumerable<T> : IEnumerable<T>
        {
            public bool Enumerated { get; protected set; }
            public abstract IEnumerator<T> GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class MultiCountSource<T> : TrackingEnumerable<T>, ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly bool _allowEnumeration;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            public MultiCountSource(int genericCount, int readOnlyCount, int nonGenericCount, bool allowEnumeration = false)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _allowEnumeration = allowEnumeration;
            }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public override IEnumerator<T> GetEnumerator()
            {
                Enumerated = true;
                if (!_allowEnumeration)
                    throw new InvalidOperationException("Malformed known Count evidence must be rejected before enumeration.");
                return ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();
            }

            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class NonGenericCountSource<T> : TrackingEnumerable<T>, ICollection
        {
            public NonGenericCountSource(int count)
            {
                Count = count;
            }

            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public override IEnumerator<T> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Oversized non-generic Count evidence must be rejected before enumeration.");
            }

            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class CountTraversalSource<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int _knownCount;

            public CountTraversalSource(int knownCount, params T[] items)
            {
                _knownCount = knownCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count => _knownCount;
            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
