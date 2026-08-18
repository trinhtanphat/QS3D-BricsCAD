using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;

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
            ConsistentKnownCountsRemainAccepted();
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
    }
}
