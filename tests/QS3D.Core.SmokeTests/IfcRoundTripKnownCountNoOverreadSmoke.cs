using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripKnownCountNoOverreadSmoke
    {
        internal static void Run()
        {
            DimensionKnownCountOverrunDoesNotReadUnexpectedCurrent();
            ProvenanceKnownCountOverrunDoesNotReadUnexpectedCurrent();
            ProjectionKnownCountOverrunDoesNotReadUnexpectedCurrent();
            QuantityEvidenceKnownCountOverrunDoesNotReadUnexpectedCurrent();
            ExchangeResultKnownCountOverrunDoesNotReadUnexpectedCurrent();
        }

        private static void DimensionKnownCountOverrunDoesNotReadUnexpectedCurrent()
        {
            var source = new CurrentCountingCollection<IfcRoundTripNumericProperty>(1,
                new IfcRoundTripNumericProperty("Length", 1d, "m"), null!);
            Capture(() => NewProjection("Q-D", "IFC-D", source, new[] { "SRC" }));
            Equal(2, source.MoveNextCalls, "dimension boundary MoveNext count");
            Equal(1, source.CurrentReads, "dimension boundary must reject before unexpected Current");
        }

        private static void ProvenanceKnownCountOverrunDoesNotReadUnexpectedCurrent()
        {
            var source = new CurrentCountingCollection<string>(1, "SRC", null!);
            Capture(() => NewProjection("Q-P", "IFC-P",
                new[] { new IfcRoundTripNumericProperty("Length", 1d, "m") }, source));
            Equal(2, source.MoveNextCalls, "provenance boundary MoveNext count");
            Equal(1, source.CurrentReads, "provenance boundary must reject before unexpected Current");
        }

        private static void ProjectionKnownCountOverrunDoesNotReadUnexpectedCurrent()
        {
            var source = new CurrentCountingCollection<IfcRoundTripProjection>(1,
                NewProjection("Q-1", "IFC-1"), null!);
            Capture(() => IfcRoundTripProjectionSet.Create(source));
            Equal(2, source.MoveNextCalls, "projection boundary MoveNext count");
            Equal(1, source.CurrentReads, "projection boundary must reject before unexpected Current");
        }

        private static void QuantityEvidenceKnownCountOverrunDoesNotReadUnexpectedCurrent()
        {
            var source = new CurrentCountingCollection<IfcRoundTripQuantityEvidence>(1,
                new IfcRoundTripQuantityEvidence("NetVolume", 1d, "m3", "IFC", "SRC"), null!);
            Capture(() => IfcRoundTripQuantityEvidenceSet.Create(source));
            Equal(2, source.MoveNextCalls, "quantity-evidence boundary MoveNext count");
            Equal(1, source.CurrentReads, "quantity-evidence boundary must reject before unexpected Current");
        }

        private static void ExchangeResultKnownCountOverrunDoesNotReadUnexpectedCurrent()
        {
            var source = new CurrentCountingCollection<IfcRoundTripExchangeResult>(1,
                new IfcRoundTripExchangeResult("IFC-R", IfcRoundTripResultState.Unsupported, null, "unsupported"), null!);
            Capture(() => IfcRoundTripExchangeResultSet.Create(source));
            Equal(2, source.MoveNextCalls, "exchange-result boundary MoveNext count");
            Equal(1, source.CurrentReads, "exchange-result boundary must reject before unexpected Current");
        }

        private static IfcRoundTripProjection NewProjection(
            string qs3dId,
            string ifcId,
            IEnumerable<IfcRoundTripNumericProperty>? dimensions = null,
            IEnumerable<string>? provenance = null)
        {
            return new IfcRoundTripProjection(
                qs3dId, ifcId, "Wall",
                dimensions ?? new[] { new IfcRoundTripNumericProperty("Length", 1d, "m") },
                1d, "m3", provenance ?? new[] { "SRC" });
        }

        private static void Capture(Action action)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new InvalidOperationException("Expected known-Count overrun rejection.");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CurrentCountingCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            internal CurrentCountingCollection(int advertisedCount, params T[] items)
            {
                Count = advertisedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count { get; }
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentCountingCollection<T> _owner;
                private int _index = -1;
                internal Enumerator(CurrentCountingCollection<T> owner) { _owner = owner; }
                public T Current { get { _owner.CurrentReads++; return _owner._items[_index]; } }
                object IEnumerator.Current => Current!;
                public bool MoveNext() { _owner.MoveNextCalls++; _index++; return _index < _owner._items.Length; }
                public void Reset() { _index = -1; }
                public void Dispose() { }
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }

    internal static class IfcRoundTripKnownCountNoOverreadRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => IfcRoundTripKnownCountNoOverreadSmoke.Run();
    }
}
