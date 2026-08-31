using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class HealthSummaryCurrentCountDriftSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CurrentTimeKnownCountDriftIsRejectedBeforeRetention();
            StableCountedCurrentRemainsAccepted();
        }

        private static void CurrentTimeKnownCountDriftIsRejectedBeforeRetention()
        {
            var source = new CurrentDriftCollection(driftOnCurrent: true);

            try
            {
                _ = new HealthSummary(source);
            }
            catch (InvalidOperationException ex)
            {
                Equal("Health summary received conflicting known issue counts.", ex.Message);
                Equal(1, source.CurrentReads);
                Equal(1, source.MoveNextCalls);
                return;
            }

            throw new InvalidOperationException("HealthSummary must reject transient known Count drift triggered by Current before retaining the issue.");
        }

        private static void StableCountedCurrentRemainsAccepted()
        {
            var source = new CurrentDriftCollection(driftOnCurrent: false);
            var summary = new HealthSummary(source);

            Equal(1, summary.Issues.Count);
            Equal(1, source.CurrentReads);
            True(source.MoveNextCalls >= 2);
            Equal("CURRENT_STABLE", summary.Issues[0].Code);
        }

        private sealed class CurrentDriftCollection :
            ICollection<ModelHealthIssue>,
            IReadOnlyCollection<ModelHealthIssue>,
            ICollection
        {
            private readonly bool _driftOnCurrent;
            private bool _returnTransientGenericCount;

            public CurrentDriftCollection(bool driftOnCurrent)
            {
                _driftOnCurrent = driftOnCurrent;
            }

            public int CurrentReads { get; private set; }
            public int MoveNextCalls { get; private set; }

            int ICollection<ModelHealthIssue>.Count
            {
                get
                {
                    if (_returnTransientGenericCount)
                    {
                        _returnTransientGenericCount = false;
                        return 2;
                    }
                    return 1;
                }
            }

            int IReadOnlyCollection<ModelHealthIssue>.Count => 1;
            int ICollection.Count => 1;
            bool ICollection<ModelHealthIssue>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ModelHealthIssue> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ModelHealthIssue>
            {
                private readonly CurrentDriftCollection _owner;
                private int _state;

                public Enumerator(CurrentDriftCollection owner)
                {
                    _owner = owner;
                }

                public ModelHealthIssue Current
                {
                    get
                    {
                        if (_state != 1) throw new InvalidOperationException("Current is unavailable outside the active row.");
                        _owner.CurrentReads++;
                        if (_owner._driftOnCurrent) _owner._returnTransientGenericCount = true;
                        return new ModelHealthIssue(
                            _owner._driftOnCurrent ? "CURRENT_DRIFT" : "CURRENT_STABLE",
                            HealthSeverity.Info,
                            "Current-boundary issue");
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_state == 0)
                    {
                        _state = 1;
                        return true;
                    }
                    _state = 2;
                    return false;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }

            void ICollection<ModelHealthIssue>.Add(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection<ModelHealthIssue>.Clear() => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Contains(ModelHealthIssue item) => false;
            void ICollection<ModelHealthIssue>.CopyTo(ModelHealthIssue[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Remove(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new InvalidOperationException("Expected condition to be true.");
        }
    }
}
