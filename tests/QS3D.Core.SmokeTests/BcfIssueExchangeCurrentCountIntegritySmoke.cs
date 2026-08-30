using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfIssueExchangeCurrentCountIntegritySmoke
    {
        internal static void Run()
        {
            StableCountIsReboundImmediatelyAfterCurrent();
            CurrentInducedCountDriftIsRejectedImmediately();
        }

        private static void StableCountIsReboundImmediatelyAfterCurrent()
        {
            var source = new CurrentCountProbe<BcfTopic>(Topic(), 1, false);
            var exchange = BcfIssueExchange.Create(source);

            Equal(1, exchange.Topics.Count, "Stable BCF topic collection should remain accepted.");
            Equal(1, source.CurrentReads, "BCF materializer should read Current exactly once for one topic.");
            Equal(7, source.CountReads, "BCF materializer must rebind known Count at admission, around traversal, immediately after Current, at termination, and before publication.");
            Equal(4, source.FirstCountReadAfterCurrent, "The first Count rebound after Current must happen before the next loop edge.");
        }

        private static void CurrentInducedCountDriftIsRejectedImmediately()
        {
            var source = new CurrentCountProbe<BcfTopic>(Topic(), 1, true);
            var error = Capture<ArgumentException>(() => BcfIssueExchange.Create(source));

            Contains("Count changed during enumeration", error.Message,
                "Current-induced BCF Count drift must be rejected by the canonical stability guard.");
            Equal(1, source.CurrentReads, "The hostile BCF collection should expose exactly one Current value.");
            Equal(4, source.CountReads, "Count drift must be observed by the immediate post-Current rebound, before another loop edge can run.");
            Equal(4, source.FirstCountReadAfterCurrent, "The first Count read after Current must be the rejecting rebound.");
            Equal(1, source.MoveNextCalls, "Current-induced Count drift must be rejected before another MoveNext call.");
        }

        private static BcfTopic Topic() => new BcfTopic(
            "11111111-1111-1111-1111-111111111111",
            "Count integrity",
            "Open",
            "Issue",
            "",
            "c01@example.invalid",
            new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
            Array.Empty<BcfComment>(),
            Array.Empty<BcfViewpoint>());

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CurrentCountProbe<T> : IReadOnlyCollection<T>
        {
            private readonly T _item;
            private readonly int _stableCount;
            private readonly bool _driftFromCurrent;
            private bool _currentWasRead;

            internal CurrentCountProbe(T item, int stableCount, bool driftFromCurrent)
            {
                _item = item;
                _stableCount = stableCount;
                _driftFromCurrent = driftFromCurrent;
            }

            public int Count
            {
                get
                {
                    CountReads++;
                    if (_currentWasRead && FirstCountReadAfterCurrent == 0)
                        FirstCountReadAfterCurrent = CountReads;
                    return _currentWasRead && _driftFromCurrent ? _stableCount + 1 : _stableCount;
                }
            }

            internal int CountReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            internal int FirstCountReadAfterCurrent { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentCountProbe<T> _owner;
                private int _index = -1;

                internal Enumerator(CurrentCountProbe<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index == 0;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._currentWasRead = true;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class BcfIssueExchangeCurrentCountIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            BcfIssueExchangeCurrentCountIntegritySmoke.Run();
        }
    }
}
