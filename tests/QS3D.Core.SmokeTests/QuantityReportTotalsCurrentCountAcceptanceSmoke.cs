using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportTotalsCurrentCountAcceptanceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectCurrentInducedCountDriftBeforeNullAcceptance();
            AcceptStableCountAfterCurrent();
        }

        private static void RejectCurrentInducedCountDriftBeforeNullAcceptance()
        {
            var source = new CurrentMutatingRows(driftAfterCurrent: true, returnNull: true);
            try
            {
                QuantityReportTotals.FromRows(source);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("Count changed during enumeration", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Current-induced Count drift must preempt ordinary row acceptance. Actual diagnostic: " + ex.Message, ex);
                if (source.CurrentReads != 1)
                    throw new InvalidOperationException("Current-induced Count drift regression must observe exactly one Current read.");
                return;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("Current-induced Count drift must be rejected before null-row validation begins.", ex);
            }

            throw new InvalidOperationException("Quantity report totals must reject Count drift induced by IEnumerator.Current.");
        }

        private static void AcceptStableCountAfterCurrent()
        {
            var source = new CurrentMutatingRows(driftAfterCurrent: false, returnNull: false);
            var totals = QuantityReportTotals.FromRows(source);
            if (source.CurrentReads != 1 || totals.Count != 2 || totals.GrossConcreteM3 != 2d)
                throw new InvalidOperationException("Stable counted quantity-report rows must remain accepted after the post-Current Count rebound.");
        }

        private sealed class CurrentMutatingRows : IReadOnlyCollection<QuantityReportRow>
        {
            private readonly bool _driftAfterCurrent;
            private readonly bool _returnNull;
            private bool _currentObserved;

            internal CurrentMutatingRows(bool driftAfterCurrent, bool returnNull)
            {
                _driftAfterCurrent = driftAfterCurrent;
                _returnNull = returnNull;
            }

            public int Count => _currentObserved && _driftAfterCurrent ? 2 : 1;
            internal int CurrentReads { get; private set; }

            public IEnumerator<QuantityReportRow> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<QuantityReportRow>
            {
                private readonly CurrentMutatingRows _owner;
                private int _index = -1;

                internal Enumerator(CurrentMutatingRows owner) { _owner = owner; }

                public QuantityReportRow Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._currentObserved = true;
                        if (_owner._returnNull)
                            return null!;
                        return new QuantityReportRow
                        {
                            Count = 2,
                            GrossConcreteM3 = 2d,
                            DeductionM3 = 0d,
                            NetConcreteM3 = 2d,
                            FormworkM2 = 2d,
                            LengthM = 2d,
                            DoorAreaM2 = 2d
                        };
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _index++;
                    return _index == 0;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
