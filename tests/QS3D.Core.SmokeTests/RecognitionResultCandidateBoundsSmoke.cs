using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionResultCandidateBoundsSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var snapshot = new EntitySnapshot("REC-CANDIDATE-BOUNDS", "line", "beam");
            var ordinaryCandidate = new RecognitionCandidate
            {
                RuleId = "ordinary",
                Category = ElementCategory.Beam,
                Confidence = 0.75d
            };
            var ordinary = new RecognitionResult(snapshot, new[] { ordinaryCandidate });
            if (ordinary.Candidates.Count != 1 || !ReferenceEquals(ordinary.Candidates[0], ordinaryCandidate))
                throw new InvalidOperationException("Ordinary RecognitionResult candidate semantics changed while adding input bounds.");

            Throws<InvalidOperationException>(() =>
                new RecognitionResult(snapshot, new OversizedReadOnlyList()));

            var observed = 0;
            Throws<InvalidOperationException>(() =>
                new RecognitionResult(snapshot, new LyingCountReadOnlyList(() => observed++)));
            if (observed != 2)
                throw new InvalidOperationException("RecognitionResult known-Count overrun must fail on the second MoveNext before traversing toward the hard-cap sentinel.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private sealed class OversizedReadOnlyList : IReadOnlyList<RecognitionCandidate>
        {
            public int Count => 10001;
            public RecognitionCandidate this[int index] => throw new ApplicationException("Oversized candidate list should fail from Count before access.");
            public IEnumerator<RecognitionCandidate> GetEnumerator() =>
                throw new ApplicationException("Oversized candidate list should fail from Count before enumeration.");
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class LyingCountReadOnlyList : IReadOnlyList<RecognitionCandidate>
        {
            private readonly Action _observed;

            internal LyingCountReadOnlyList(Action observed)
            {
                _observed = observed ?? throw new ArgumentNullException(nameof(observed));
            }

            public int Count => 1;
            public RecognitionCandidate this[int index] => throw new NotSupportedException();
            public IEnumerator<RecognitionCandidate> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<RecognitionCandidate> Enumerate()
            {
                for (var index = 0; index < 10001; index++)
                {
                    _observed();
                    yield return new RecognitionCandidate
                    {
                        RuleId = "candidate-" + index,
                        Category = ElementCategory.Beam,
                        Confidence = 0.5d
                    };
                }

                throw new ApplicationException("RecognitionResult enumerated beyond the 10,001-item cap sentinel.");
            }
        }
    }
}
