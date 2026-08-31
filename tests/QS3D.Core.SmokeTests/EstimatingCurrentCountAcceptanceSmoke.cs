using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimatingCurrentCountAcceptanceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PortfolioRejectsCurrentInducedCountDriftBeforeNullAcceptance();
            SelectedLineRejectsCurrentInducedCountDriftBeforeTokenAcceptance();
            UnitRateRejectsCurrentInducedCountDriftBeforeNullAcceptance();
            StableCountedControlsRemainAccepted();
            Console.WriteLine("PASS estimating Current-induced Count acceptance boundary");
        }

        private static void PortfolioRejectsCurrentInducedCountDriftBeforeNullAcceptance()
        {
            var source = new CurrentDriftCollection<EstimatingLine>(null!);
            ExpectCountDrift(
                () => new EstimatingPortfolio(source),
                "known line count changed during enumeration",
                "portfolio");
            Equal(1, source.CurrentReads, "portfolio Current reads");
        }

        private static void SelectedLineRejectsCurrentInducedCountDriftBeforeTokenAcceptance()
        {
            var source = new CurrentDriftCollection<string>(null!);
            ExpectCountDrift(
                () => new BulkRateAssignmentRequest(
                    source,
                    "CC-1",
                    "RATE-SOURCE",
                    "R1",
                    new[] { new UnitRateAssignment("m", 10m) }),
                "selected-line known count changed during enumeration",
                "selected-line");
            Equal(1, source.CurrentReads, "selected-line Current reads");
        }

        private static void UnitRateRejectsCurrentInducedCountDriftBeforeNullAcceptance()
        {
            var source = new CurrentDriftCollection<UnitRateAssignment>(null!);
            ExpectCountDrift(
                () => new BulkRateAssignmentRequest(
                    new[] { "L-1" },
                    "CC-1",
                    "RATE-SOURCE",
                    "R1",
                    source),
                "unit-rate known count changed during enumeration",
                "unit-rate");
            Equal(1, source.CurrentReads, "unit-rate Current reads");
        }

        private static void StableCountedControlsRemainAccepted()
        {
            var portfolio = new EstimatingPortfolio(new[]
            {
                new EstimatingLine("L-1", "Q-1", "R1", 1m, "m")
            });
            Equal(1, portfolio.Lines.Count, "stable portfolio count");

            var request = new BulkRateAssignmentRequest(
                new[] { "L-1" },
                "CC-1",
                "RATE-SOURCE",
                "R1",
                new[] { new UnitRateAssignment("m", 10m) });
            Equal(1, request.LineIds.Count, "stable selected-line count");
            Equal(1, request.UnitRates.Count, "stable unit-rate count");
        }

        private static void ExpectCountDrift(Action action, string expectedFragment, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception(
                    "EstimatingCurrentCountAcceptanceSmoke: " + label +
                    " failed for the wrong reason: " + ex.Message,
                    ex);
            }
            catch (ArgumentException ex)
            {
                throw new Exception(
                    "EstimatingCurrentCountAcceptanceSmoke: " + label +
                    " reached ordinary item acceptance before Count stability was rebound: " + ex.Message,
                    ex);
            }

            throw new Exception(
                "EstimatingCurrentCountAcceptanceSmoke: " + label +
                " accepted Current-induced Count drift.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(
                    "EstimatingCurrentCountAcceptanceSmoke: " + label +
                    ". Expected " + expected + ", got " + actual + ".");
        }

        private sealed class CurrentDriftCollection<T> : ICollection<T>
        {
            private readonly T _item;
            private bool _emitDrift;

            internal CurrentDriftCollection(T item)
            {
                _item = item;
            }

            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    if (_emitDrift)
                    {
                        _emitDrift = false;
                        return 2;
                    }
                    return 1;
                }
            }

            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
            public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentDriftCollection<T> _owner;
                private int _state;

                internal Enumerator(CurrentDriftCollection<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    if (_state != 0) return false;
                    _state = 1;
                    return true;
                }

                public T Current
                {
                    get
                    {
                        if (_state != 1) throw new InvalidOperationException("Enumerator is not positioned on an item.");
                        _owner.CurrentReads++;
                        _owner._emitDrift = true;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
