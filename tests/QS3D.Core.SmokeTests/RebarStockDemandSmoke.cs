using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockDemandSmoke
    {
        public static void Run()
        {
            DemandKeepsMaterialComponentsSeparate();
            CanonicalIdentityFailsClosed();
            DuplicateCutIdentityFailsClosed();
            TransientMoveNextCountDriftFailsClosed();
            TransientCurrentCountDriftFailsClosed();
            StableCountedListStillSucceeds();
            NonFinitePolicyFailsClosed();
            ProcurementKeepsKerfAndOffCutSeparate();
            ExcessWasteFailsClosed();
        }

        private static void DemandKeepsMaterialComponentsSeparate()
        {
            var demand = NewDemand(new[]
            {
                new RebarCutRequirement("C-01", 4.5d, 2),
                new RebarCutRequirement("C-02", 2d, 1)
            });

            Equal(3L, demand.RequiredCutCount);
            Equal(2, demand.RequiredCuts.Count);
            Near(11d, demand.RequiredCutLengthM);
            Near(0.06d, demand.AllowanceLengthM);
            Near(11.06d, demand.DemandLengthBeforeKerfM);
            Near(0.003d, demand.AllowancePolicy.KerfPerCutM);
            Near(20d, demand.DiameterMm);
            Near(12d, demand.StockLengthM);
            Equal("G-01", demand.GroupId);
            Equal("CB400-V", demand.Grade);
        }

        private static void CanonicalIdentityFailsClosed()
        {
            Throws<ArgumentException>(() => new RebarCutRequirement(" CUT-A", 3d, 1));
            Throws<ArgumentException>(() => new RebarStockDemand(
                "G-02 ", "CB500-V", 16d, 12d,
                new[] { new RebarCutRequirement("CUT-A", 3d, 1) },
                new RebarCutAllowancePolicy()));
        }

        private static void DuplicateCutIdentityFailsClosed()
        {
            Throws<ArgumentException>(() => new RebarStockDemand(
                "G-03", "CB500-V", 16d, 12d,
                new[]
                {
                    new RebarCutRequirement("CUT-A", 3d, 1),
                    new RebarCutRequirement("cut-a", 2d, 1)
                },
                new RebarCutAllowancePolicy()));
        }

        private static void TransientMoveNextCountDriftFailsClosed()
        {
            Throws<InvalidOperationException>(() => NewDemand(new HostileRequiredCuts(DriftBoundary.MoveNext)));
        }

        private static void TransientCurrentCountDriftFailsClosed()
        {
            Throws<InvalidOperationException>(() => NewDemand(new HostileRequiredCuts(DriftBoundary.Current)));
        }

        private static void StableCountedListStillSucceeds()
        {
            var demand = NewDemand(new HostileRequiredCuts(DriftBoundary.None));
            Equal(2, demand.RequiredCuts.Count);
            Equal(2L, demand.RequiredCutCount);
        }

        private static RebarStockDemand NewDemand(IReadOnlyList<RebarCutRequirement> cuts)
        {
            return new RebarStockDemand(
                "G-01", "CB400-V", 20d, 12d, cuts,
                new RebarCutAllowancePolicy(0.003d, 0.02d));
        }

        private static void NonFinitePolicyFailsClosed()
        {
            Throws<ArgumentOutOfRangeException>(() => new RebarCutAllowancePolicy(double.NaN, 0d));
            Throws<ArgumentOutOfRangeException>(() => new RebarCutAllowancePolicy(0d, double.PositiveInfinity));
        }

        private static void ProcurementKeepsKerfAndOffCutSeparate()
        {
            var procurement = new RebarStockProcurementQuantities(12d, 1, 0.009d, 0.931d);
            Equal(1, procurement.StockBarCount);
            Near(12d, procurement.ProcurementLengthM);
            Near(0.009d, procurement.KerfLengthM);
            Near(0.931d, procurement.OffCutLengthM);
        }

        private static void ExcessWasteFailsClosed()
        {
            Throws<ArgumentOutOfRangeException>(() => new RebarStockProcurementQuantities(12d, 1, 0.01d, 12d));
        }

        private enum DriftBoundary { None, MoveNext, Current }

        private sealed class HostileRequiredCuts : IReadOnlyList<RebarCutRequirement>
        {
            private readonly RebarCutRequirement[] _items =
            {
                new RebarCutRequirement("CUT-A", 3d, 1),
                new RebarCutRequirement("CUT-B", 2d, 1)
            };
            private readonly DriftBoundary _boundary;
            private bool _drifting;

            public HostileRequiredCuts(DriftBoundary boundary) { _boundary = boundary; }
            public int Count => _items.Length + (_drifting ? 1 : 0);
            public RebarCutRequirement this[int index] => _items[index];
            public IEnumerator<RebarCutRequirement> GetEnumerator() => new HostileEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class HostileEnumerator : IEnumerator<RebarCutRequirement>
            {
                private readonly HostileRequiredCuts _owner;
                private int _index = -1;

                public HostileEnumerator(HostileRequiredCuts owner) { _owner = owner; }

                public bool MoveNext()
                {
                    if (_owner._boundary == DriftBoundary.Current)
                        _owner._drifting = false;
                    _index++;
                    var hasNext = _index < _owner._items.Length;
                    if (hasNext && _owner._boundary == DriftBoundary.MoveNext)
                        _owner._drifting = true;
                    return hasNext;
                }

                public RebarCutRequirement Current
                {
                    get
                    {
                        if (_owner._boundary == DriftBoundary.MoveNext)
                            _owner._drifting = false;
                        else if (_owner._boundary == DriftBoundary.Current)
                            _owner._drifting = true;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() { throw new NotSupportedException(); }
                public void Dispose() { }
            }
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(long expected, long actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
