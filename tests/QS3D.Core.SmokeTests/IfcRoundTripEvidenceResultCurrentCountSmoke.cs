using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripEvidenceResultCurrentCountSmoke
    {
        private const string EvidenceCountChanged = "IFC round-trip quantity evidence source Count changed during traversal.";
        private const string ResultCountChanged = "IFC exchange result source Count changed during traversal.";

        [ModuleInitializer]
        internal static void Initialize()
        {
            EvidenceMoveNextTransientDriftFailsBeforeCurrent();
            EvidenceCurrentDriftFailsBeforeNullValidation();
            ResultMoveNextTransientDriftFailsBeforeCurrent();
            ResultCurrentDriftFailsBeforeNullValidation();
            StableCountedInputsRemainAccepted();
            PureStreamingInputsRemainAccepted();
        }

        private static void EvidenceMoveNextTransientDriftFailsBeforeCurrent()
        {
            var input = new HostileCountCollection<IfcRoundTripQuantityEvidence>(
                new IfcRoundTripQuantityEvidence("Q", 1d, "m", "SRC", "P"),
                DriftPoint.MoveNext);

            ExpectFailure(
                () => IfcRoundTripQuantityEvidenceSet.Create(input),
                EvidenceCountChanged,
                "evidence MoveNext transient drift");
            Equal(1, input.MoveNextCalls, "evidence MoveNext transient drift MoveNext calls");
            Equal(0, input.CurrentReads, "evidence MoveNext transient drift Current reads");
        }

        private static void EvidenceCurrentDriftFailsBeforeNullValidation()
        {
            var input = new HostileCountCollection<IfcRoundTripQuantityEvidence>(null!, DriftPoint.Current);

            ExpectFailure(
                () => IfcRoundTripQuantityEvidenceSet.Create(input),
                EvidenceCountChanged,
                "evidence Current drift");
            Equal(1, input.MoveNextCalls, "evidence Current drift MoveNext calls");
            Equal(1, input.CurrentReads, "evidence Current drift Current reads");
        }

        private static void ResultMoveNextTransientDriftFailsBeforeCurrent()
        {
            var input = new HostileCountCollection<IfcRoundTripExchangeResult>(
                new IfcRoundTripExchangeResult("IFC-1", IfcRoundTripResultState.Unsupported, null),
                DriftPoint.MoveNext);

            ExpectFailure(
                () => IfcRoundTripExchangeResultSet.Create(input),
                ResultCountChanged,
                "result MoveNext transient drift");
            Equal(1, input.MoveNextCalls, "result MoveNext transient drift MoveNext calls");
            Equal(0, input.CurrentReads, "result MoveNext transient drift Current reads");
        }

        private static void ResultCurrentDriftFailsBeforeNullValidation()
        {
            var input = new HostileCountCollection<IfcRoundTripExchangeResult>(null!, DriftPoint.Current);

            ExpectFailure(
                () => IfcRoundTripExchangeResultSet.Create(input),
                ResultCountChanged,
                "result Current drift");
            Equal(1, input.MoveNextCalls, "result Current drift MoveNext calls");
            Equal(1, input.CurrentReads, "result Current drift Current reads");
        }

        private static void StableCountedInputsRemainAccepted()
        {
            var evidence = IfcRoundTripQuantityEvidenceSet.Create(new[]
            {
                new IfcRoundTripQuantityEvidence("Q", 1d, "m", "SRC", "P")
            });
            Equal(1, evidence.CandidateCount, "stable counted evidence candidate count");

            var results = IfcRoundTripExchangeResultSet.Create(new[]
            {
                new IfcRoundTripExchangeResult("IFC-1", IfcRoundTripResultState.Unsupported, null)
            });
            Equal(1, results.Items.Count, "stable counted result count");
        }

        private static void PureStreamingInputsRemainAccepted()
        {
            var evidence = IfcRoundTripQuantityEvidenceSet.Create(Stream(
                new IfcRoundTripQuantityEvidence("Q", 1d, "m", "SRC", "P")));
            Equal(1, evidence.CandidateCount, "streaming evidence candidate count");

            var results = IfcRoundTripExchangeResultSet.Create(Stream(
                new IfcRoundTripExchangeResult("IFC-1", IfcRoundTripResultState.Unsupported, null)));
            Equal(1, results.Items.Count, "streaming result count");
        }

        private static IEnumerable<T> Stream<T>(params T[] items)
        {
            for (var index = 0; index < items.Length; index++)
                yield return items[index];
        }

        private static void ExpectFailure(Action action, string expectedMessage, string label)
        {
            try
            {
                action();
                throw new InvalidOperationException(label + ": expected Count-integrity rejection.");
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(expectedMessage, ex.Message, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        label + ": wrong failure. Expected '" + expectedMessage + "', got '" + ex.Message + "'.");
            }
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private enum DriftPoint
        {
            MoveNext,
            Current
        }

        private sealed class HostileCountCollection<T> : ICollection<T>
        {
            private readonly T _item;
            private readonly DriftPoint _driftPoint;
            private bool _drifted;

            internal HostileCountCollection(T item, DriftPoint driftPoint)
            {
                _item = item;
                _driftPoint = driftPoint;
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count => _drifted ? 2 : 1;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly HostileCountCollection<T> _owner;
                private int _state;

                internal Enumerator(HostileCountCollection<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._driftPoint == DriftPoint.Current)
                            _owner._drifted = true;
                        else
                            _owner._drifted = false;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_state != 0) return false;
                    _state = 1;
                    if (_owner._driftPoint == DriftPoint.MoveNext)
                        _owner._drifted = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
