using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class DuplicateDetectionCurrentCountAcceptanceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectElementCurrentInducedCountDriftBeforeNullAcceptance();
            RejectCandidateCurrentInducedCountDriftBeforeNullAcceptance();
            AcceptStableCountAfterElementCurrent();
            AcceptStableCountAfterCandidateCurrent();
        }

        private static void RejectElementCurrentInducedCountDriftBeforeNullAcceptance()
        {
            var source = new CurrentMutatingCollection<CoordinationElement>(null!, driftAfterCurrent: true);
            ExpectCountDrift(() => new DuplicateDetectionService().Detect(source), "element Current-induced Count drift");
            Equal(1, source.CurrentReads, "element hostile Current reads");
        }

        private static void RejectCandidateCurrentInducedCountDriftBeforeNullAcceptance()
        {
            var source = new CurrentMutatingCollection<DuplicateCandidate>(null!, driftAfterCurrent: true);
            ExpectCountDrift(() => new DuplicateDetectionService().Detect(source), "candidate Current-induced Count drift");
            Equal(1, source.CurrentReads, "candidate hostile Current reads");
        }

        private static void AcceptStableCountAfterElementCurrent()
        {
            var source = new CurrentMutatingCollection<CoordinationElement>(Element("E1"), driftAfterCurrent: false);
            var result = new DuplicateDetectionService().Detect(source);
            Equal(0, result.Pairs.Count, "stable element pair count");
            Equal(1, source.CurrentReads, "stable element Current reads");
        }

        private static void AcceptStableCountAfterCandidateCurrent()
        {
            var source = new CurrentMutatingCollection<DuplicateCandidate>(new DuplicateCandidate(Element("C1"), "stable"), driftAfterCurrent: false);
            var result = new DuplicateDetectionService().Detect(source);
            Equal(0, result.Pairs.Count, "stable candidate pair count");
            Equal(1, source.CurrentReads, "stable candidate Current reads");
        }

        private static CoordinationElement Element(string id) => new CoordinationElement(
            id, "Structure", "Beam", "S", "R1", new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d));

        private static void ExpectCountDrift(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.Contains("known element Count changed during snapshot", StringComparison.Ordinal))
                    throw new Exception(label + " wrong InvalidOperationException: " + ex.Message);
                return;
            }
            catch (ArgumentException ex)
            {
                throw new Exception(label + " must be rejected before null/identity acceptance.", ex);
            }

            throw new Exception(label + " expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CurrentMutatingCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T _item;
            private readonly bool _driftAfterCurrent;
            private bool _currentObserved;

            internal CurrentMutatingCollection(T item, bool driftAfterCurrent)
            {
                _item = item;
                _driftAfterCurrent = driftAfterCurrent;
            }

            public int Count => _currentObserved && _driftAfterCurrent ? 2 : 1;
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentMutatingCollection<T> _owner;
                private int _index = -1;

                internal Enumerator(CurrentMutatingCollection<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._currentObserved = true;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;

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
