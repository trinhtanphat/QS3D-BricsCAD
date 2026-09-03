using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class EstimatingInputGenerationStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PortfolioRejectsSameCountContentDrift();
            BulkLineIdsRejectSameCountContentDrift();
            BulkUnitRatesRejectSameCountContentDrift();
            StableControlsRemainAccepted();
            StreamingControlsRemainAccepted();
            Console.WriteLine("PASS estimating input generation stability");
        }

        private static void PortfolioRejectsSameCountContentDrift()
        {
            var first = new[]
            {
                Line("LINE-A", 1m),
                Line("LINE-B", 2m)
            };
            var second = new[]
            {
                Line("LINE-A", 10m),
                Line("LINE-B", 20m)
            };
            var source = new SameCountDriftCollection<EstimatingLine>(first, second);
            ExpectContentDrift(() => new EstimatingPortfolio(source), "estimating portfolio same-count content drift");
        }

        private static void BulkLineIdsRejectSameCountContentDrift()
        {
            var source = new SameCountDriftCollection<string>(
                new[] { "LINE-A", "LINE-B" },
                new[] { "LINE-C", "LINE-D" });
            ExpectContentDrift(
                () => new BulkRateAssignmentRequest(
                    source,
                    "COST-1",
                    "RATE-SOURCE",
                    "REV-1",
                    new[] { new UnitRateAssignment("ea", 1m) }),
                "bulk selected-line same-count content drift");
        }

        private static void BulkUnitRatesRejectSameCountContentDrift()
        {
            var source = new SameCountDriftCollection<UnitRateAssignment>(
                new[]
                {
                    new UnitRateAssignment("ea", 1m),
                    new UnitRateAssignment("m", 2m)
                },
                new[]
                {
                    new UnitRateAssignment("ea", 10m),
                    new UnitRateAssignment("m", 20m)
                });
            ExpectContentDrift(
                () => new BulkRateAssignmentRequest(
                    new[] { "LINE-A" },
                    "COST-1",
                    "RATE-SOURCE",
                    "REV-1",
                    source),
                "bulk unit-rate same-count content drift");
        }

        private static void StableControlsRemainAccepted()
        {
            var portfolio = new EstimatingPortfolio(new[] { Line("LINE-A", 2m), Line("LINE-B", 3m) });
            Require(portfolio.Lines.Count == 2, "stable counted estimating portfolio changed");

            var request = new BulkRateAssignmentRequest(
                new[] { "LINE-A", "LINE-B" },
                "COST-1",
                "RATE-SOURCE",
                "REV-1",
                new[] { new UnitRateAssignment("ea", 4m) });
            Require(request.LineIds.Count == 2 && request.UnitRates.Count == 1,
                "stable counted bulk-rate request changed");
        }

        private static void StreamingControlsRemainAccepted()
        {
            var portfolio = new EstimatingPortfolio(Yield(Line("LINE-A", 2m)));
            Require(portfolio.Lines.Count == 1, "streaming estimating portfolio changed");

            var request = new BulkRateAssignmentRequest(
                Yield("LINE-A"),
                "COST-1",
                "RATE-SOURCE",
                "REV-1",
                Yield(new UnitRateAssignment("ea", 4m)));
            Require(request.LineIds.Count == 1 && request.UnitRates.Count == 1,
                "streaming bulk-rate request changed");
        }

        private static EstimatingLine Line(string id, decimal quantity)
        {
            return new EstimatingLine(id, "QTY-SOURCE", "QTY-REV", quantity, "ea");
        }

        private static IEnumerable<T> Yield<T>(T value)
        {
            yield return value;
        }

        private static void ExpectContentDrift(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("content changed during enumeration", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(label + " failed for the wrong reason: " + ex.Message, ex);
            }

            throw new InvalidOperationException(label + " was accepted unexpectedly.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class SameCountDriftCollection<T> : ICollection<T>
        {
            private readonly T[] _first;
            private readonly T[] _second;
            private int _enumerations;

            internal SameCountDriftCollection(T[] first, T[] second)
            {
                if (first == null) throw new ArgumentNullException(nameof(first));
                if (second == null) throw new ArgumentNullException(nameof(second));
                if (first.Length != second.Length) throw new ArgumentException("Generations must have equal cardinality.");
                _first = first;
                _second = second;
            }

            public int Count => _first.Length;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                var enumeration = _enumerations++;
                return new Enumerator(this, enumeration == 0);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly SameCountDriftCollection<T> _owner;
                private readonly bool _driftFirstPass;
                private int _index = -1;

                internal Enumerator(SameCountDriftCollection<T> owner, bool driftFirstPass)
                {
                    _owner = owner;
                    _driftFirstPass = driftFirstPass;
                }

                public bool MoveNext()
                {
                    _index++;
                    return _index < _owner.Count;
                }

                public T Current
                {
                    get
                    {
                        if (_index < 0 || _index >= _owner.Count) throw new InvalidOperationException();
                        if (_driftFirstPass && _index == 0)
                            return _owner._first[_index];
                        return _owner._second[_index];
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
