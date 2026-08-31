using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthBaselineCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TransientMoveNextCountDriftFailsBeforeCurrent();
            TransientCurrentCountDriftFailsBeforeRetention();
            StableCountedInputRemainsAccepted();
            PureStreamingInputRemainsAccepted();
        }

        private static void TransientMoveNextCountDriftFailsBeforeCurrent()
        {
            var source = new HostileIssues(DriftBoundary.MoveNext);
            ExpectInvalid(() => Capture(source), "changed during enumeration", "MoveNext Count drift must fail closed.");
            Equal(1, source.MoveNextCalls, "MoveNext drift calls");
            Equal(0, source.CurrentReads, "MoveNext drift Current reads");
        }

        private static void TransientCurrentCountDriftFailsBeforeRetention()
        {
            var source = new HostileIssues(DriftBoundary.Current);
            ExpectInvalid(() => Capture(source), "changed during enumeration", "Current Count drift must fail before retention.");
            Equal(1, source.MoveNextCalls, "Current drift MoveNext calls");
            Equal(1, source.CurrentReads, "Current drift Current reads");
        }

        private static void StableCountedInputRemainsAccepted()
        {
            var source = new HostileIssues(DriftBoundary.None);
            var baseline = Capture(source);
            Equal(1, baseline.Issues.Count, "stable baseline issue count");
            Equal("COUNT-STABLE", baseline.Issues[0].Code, "stable baseline issue code");
            Equal(2, source.MoveNextCalls, "stable MoveNext calls");
            Equal(1, source.CurrentReads, "stable Current reads");
        }

        private static void PureStreamingInputRemainsAccepted()
        {
            var baseline = Capture(Stream());
            Equal(1, baseline.Issues.Count, "stream baseline issue count");
            Equal("STREAM-STABLE", baseline.Issues[0].Code, "stream baseline issue code");
        }

        private static ModelHealthBaseline Capture(IEnumerable<ModelHealthIssue> issues) =>
            new ModelHealthBaselineService().Capture(new ProjectState("baseline-count-integrity", "Baseline Count Integrity"), issues);

        private static IEnumerable<ModelHealthIssue> Stream()
        {
            yield return Issue("STREAM-STABLE");
        }

        private static ModelHealthIssue Issue(string code) =>
            new ModelHealthIssue(code, HealthSeverity.Warning, "count integrity", "E1");

        private static void ExpectInvalid(Action action, string fragment, string label)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException(label + " Actual diagnostic: " + ex.Message, ex);
            }
            throw new InvalidOperationException(label);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private enum DriftBoundary { None, MoveNext, Current }

        private sealed class HostileIssues : IReadOnlyCollection<ModelHealthIssue>
        {
            private readonly DriftBoundary _boundary;
            private bool _pendingDriftRead;

            internal HostileIssues(DriftBoundary boundary) { _boundary = boundary; }

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

            public IEnumerator<ModelHealthIssue> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<ModelHealthIssue>
            {
                private readonly HostileIssues _owner;
                private int _index = -1;

                internal Enumerator(HostileIssues owner) { _owner = owner; }

                public ModelHealthIssue Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._boundary == DriftBoundary.Current) _owner._pendingDriftRead = true;
                        return Issue("COUNT-STABLE");
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    var moved = _index == 0;
                    if (moved && _owner._boundary == DriftBoundary.MoveNext) _owner._pendingDriftRead = true;
                    return moved;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
