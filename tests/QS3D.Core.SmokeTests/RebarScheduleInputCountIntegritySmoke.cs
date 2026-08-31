using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarScheduleInputCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TransientMoveNextCountDriftFailsBeforeCurrent();
            TransientCurrentCountDriftFailsBeforeSemanticAcceptance();
            StableCountedInputRemainsAccepted();
            PureStreamingInputRemainsAccepted();
        }

        private static void TransientMoveNextCountDriftFailsBeforeCurrent()
        {
            var source = new HostileCountedSource(DriftBoundary.MoveNext);
            ExpectInvalidOperation(
                () => RebarScheduleBuilder.Build(source),
                "changed during traversal",
                "Transient MoveNext Count drift must fail closed.");
            Equal(1, source.MoveNextCalls, "MoveNext drift MoveNext calls");
            Equal(0, source.CurrentReads, "MoveNext drift Current reads");
        }

        private static void TransientCurrentCountDriftFailsBeforeSemanticAcceptance()
        {
            var source = new HostileCountedSource(DriftBoundary.Current);
            ExpectInvalidOperation(
                () => RebarScheduleBuilder.Build(source),
                "changed during traversal",
                "Transient Current Count drift must fail before schedule-row acceptance.");
            Equal(1, source.MoveNextCalls, "Current drift MoveNext calls");
            Equal(1, source.CurrentReads, "Current drift Current reads");
        }

        private static void StableCountedInputRemainsAccepted()
        {
            var source = new HostileCountedSource(DriftBoundary.None);
            var rows = RebarScheduleBuilder.Build(source);
            Equal(1, rows.Count, "stable counted row count");
            Equal("COUNT-STABLE", rows[0].ElementId, "stable counted element id");
            Equal(2, source.MoveNextCalls, "stable counted MoveNext calls");
            Equal(1, source.CurrentReads, "stable counted Current reads");
        }

        private static void PureStreamingInputRemainsAccepted()
        {
            var rows = RebarScheduleBuilder.Build(PureStreaming());
            Equal(1, rows.Count, "pure-streaming row count");
            Equal("STREAM-STABLE", rows[0].ElementId, "pure-streaming element id");
        }

        private static IEnumerable<RebarScheduleInput> PureStreaming()
        {
            yield return ValidInput("STREAM-STABLE");
        }

        private static RebarScheduleInput ValidInput(string id)
        {
            return new RebarScheduleInput
            {
                ElementId = id,
                Notation = "1D8",
                CuttingLengthM = 1d
            };
        }

        private static void ExpectInvalidOperation(Action action, string messageFragment, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(messageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(label + " Actual diagnostic: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(label);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private enum DriftBoundary
        {
            None,
            MoveNext,
            Current
        }

        private sealed class HostileCountedSource : IReadOnlyCollection<RebarScheduleInput>
        {
            private readonly DriftBoundary _driftBoundary;
            private bool _pendingDriftRead;

            internal HostileCountedSource(DriftBoundary driftBoundary)
            {
                _driftBoundary = driftBoundary;
            }

            public int Count
            {
                get
                {
                    if (!_pendingDriftRead) return 1;
                    _pendingDriftRead = false;
                    return 2;
                }
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<RebarScheduleInput> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<RebarScheduleInput>
            {
                private readonly HostileCountedSource _owner;
                private int _index = -1;

                internal Enumerator(HostileCountedSource owner)
                {
                    _owner = owner;
                }

                public RebarScheduleInput Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._driftBoundary == DriftBoundary.Current)
                            _owner._pendingDriftRead = true;
                        return ValidInput("COUNT-STABLE");
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    var hasCurrent = _index == 0;
                    if (hasCurrent && _owner._driftBoundary == DriftBoundary.MoveNext)
                        _owner._pendingDriftRead = true;
                    return hasCurrent;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}