using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class HealthSummaryTransientCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            KnownCountOverrunRejectsBeforeSecondCurrent();
            TransientGrowthRejectsBeforeNextMove();
            TransientShrinkRejectsBeforeNextMove();
            TransientNegativeRejectsBeforeNextMove();
            TransientConflictRejectsBeforeNextMove();
            StableMultiInterfaceCountRemainsAccepted();
        }

        private static void KnownCountOverrunRejectsBeforeSecondCurrent()
        {
            var source = new CurrentSensitiveKnownCountCollection(1, 2);
            Throws<InvalidOperationException>(
                () => _ = new HealthSummary(source),
                "Health summary traversal produced more diagnostic issues than its known count of 1.");
            Equal(2, source.MoveNextCalls);
            Equal(1, source.CurrentReads);
        }

        private static void TransientGrowthRejectsBeforeNextMove() => RequireTransientFailure(CountMutation.Growth);
        private static void TransientShrinkRejectsBeforeNextMove() => RequireTransientFailure(CountMutation.Shrink);
        private static void TransientNegativeRejectsBeforeNextMove() => RequireTransientFailure(CountMutation.Negative);
        private static void TransientConflictRejectsBeforeNextMove() => RequireTransientFailure(CountMutation.Conflict);

        private static void RequireTransientFailure(CountMutation mutation)
        {
            var source = new TransientCountCollection(mutation);
            try
            {
                _ = new HealthSummary(source);
            }
            catch (InvalidOperationException)
            {
                Equal(1, source.MoveNextCalls);
                Equal(1, source.CurrentReads);
                return;
            }

            throw new InvalidOperationException("HealthSummary must reject transient Count mutation: " + mutation + ".");
        }

        private static void StableMultiInterfaceCountRemainsAccepted()
        {
            var source = new TransientCountCollection(CountMutation.None);
            var summary = new HealthSummary(source);
            Equal(2, summary.Issues.Count);
            Equal(3, source.MoveNextCalls);
            Equal(2, source.CurrentReads);
        }

        private enum CountMutation
        {
            None,
            Growth,
            Shrink,
            Negative,
            Conflict,
        }

        private sealed class CurrentSensitiveKnownCountCollection : ICollection<ModelHealthIssue>, IReadOnlyCollection<ModelHealthIssue>, ICollection
        {
            private readonly int _knownCount;
            private readonly int _enumeratedCount;

            public CurrentSensitiveKnownCountCollection(int knownCount, int enumeratedCount)
            {
                _knownCount = knownCount;
                _enumeratedCount = enumeratedCount;
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            int ICollection<ModelHealthIssue>.Count => _knownCount;
            int IReadOnlyCollection<ModelHealthIssue>.Count => _knownCount;
            int ICollection.Count => _knownCount;
            bool ICollection<ModelHealthIssue>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<ModelHealthIssue> GetEnumerator() => new Enumerator(this, _enumeratedCount, null);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<ModelHealthIssue>.Add(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection<ModelHealthIssue>.Clear() => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Contains(ModelHealthIssue item) => false;
            void ICollection<ModelHealthIssue>.CopyTo(ModelHealthIssue[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Remove(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<ModelHealthIssue>
            {
                private readonly CurrentSensitiveKnownCountCollection _owner;
                private readonly int _count;
                private readonly Action? _afterCurrent;
                private int _index = -1;

                public Enumerator(CurrentSensitiveKnownCountCollection owner, int count, Action? afterCurrent)
                {
                    _owner = owner;
                    _count = count;
                    _afterCurrent = afterCurrent;
                }

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _count;
                }

                public ModelHealthIssue Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner.CurrentReads > 1)
                            throw new InvalidOperationException("Second Current must never be observed after known-Count admission fails.");
                        _afterCurrent?.Invoke();
                        return Issue();
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class TransientCountCollection : ICollection<ModelHealthIssue>, IReadOnlyCollection<ModelHealthIssue>, ICollection
        {
            private readonly CountMutation _mutation;
            private bool _armed;

            public TransientCountCollection(CountMutation mutation) => _mutation = mutation;
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            int ICollection<ModelHealthIssue>.Count => CountFor(1);
            int IReadOnlyCollection<ModelHealthIssue>.Count => CountFor(2);
            int ICollection.Count => CountFor(3);
            bool ICollection<ModelHealthIssue>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            private int CountFor(int surface)
            {
                if (!_armed || _mutation == CountMutation.None) return 2;
                switch (_mutation)
                {
                    case CountMutation.Growth: return 3;
                    case CountMutation.Shrink: return 1;
                    case CountMutation.Negative: return -1;
                    case CountMutation.Conflict: return surface == 1 ? 3 : 2;
                    default: return 2;
                }
            }

            public IEnumerator<ModelHealthIssue> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<ModelHealthIssue>.Add(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection<ModelHealthIssue>.Clear() => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Contains(ModelHealthIssue item) => false;
            void ICollection<ModelHealthIssue>.CopyTo(ModelHealthIssue[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<ModelHealthIssue>.Remove(ModelHealthIssue item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<ModelHealthIssue>
            {
                private readonly TransientCountCollection _owner;
                private int _index = -1;

                public Enumerator(TransientCountCollection owner) => _owner = owner;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < 2;
                }

                public ModelHealthIssue Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner.CurrentReads == 1) _owner._armed = true;
                        return Issue();
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private static ModelHealthIssue Issue() => new ModelHealthIssue("COUNT_STABILITY", HealthSeverity.Info, "Count stability probe");

        private static void Throws<T>(Action action, string expectedMessage) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (!string.Equals(expectedMessage, ex.Message, StringComparison.Ordinal))
                    throw new InvalidOperationException("Expected message '" + expectedMessage + "', got '" + ex.Message + "'.");
                return;
            }
            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }
    }
}
