using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionBoundedEnumerationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RuleTermsAreReadOnlySnapshots();
            KnownCountDriftFailsClosed();

            var rule = new RecognitionRule("bounded-beam", ElementCategory.Beam, new[] { "beam" }, entityTypes: new[] { "line" });
            var engine = new RecognitionEngine(new[] { rule });
            var snapshot = new EntitySnapshot("B1", "line", "beam");

            var ordinary = engine.Suggest(snapshot);
            if (ordinary.Candidates.Count != 1 || ordinary.TopCandidate == null || ordinary.TopCandidate.Category != ElementCategory.Beam)
                throw new InvalidOperationException("Ordinary recognition behavior changed while adding enumeration bounds.");
            if (engine.SuggestBatch(new[] { snapshot }).Results.Count != 1)
                throw new InvalidOperationException("Ordinary recognition batch behavior changed while adding enumeration bounds.");

            var project = new ProjectState("recognition-bound", "Recognition bounds");
            if (new ProjectRecognitionService().SuggestBatch(project, new[] { snapshot }).Results.Count != 1)
                throw new InvalidOperationException("Ordinary project recognition batch behavior changed while adding enumeration bounds.");

            Throws<InvalidOperationException>(() =>
                new RecognitionRule("oversized-terms", ElementCategory.Beam, new OversizedCollection<string>(10001)));
            Throws<InvalidOperationException>(() =>
                new RecognitionEngine(new OversizedCollection<RecognitionRule>(10001)));

            var result = new RecognitionResult(snapshot, Array.Empty<RecognitionCandidate>());
            Throws<InvalidOperationException>(() =>
                new RecognitionBatch(new OversizedCollection<RecognitionResult>(250001)));
            Throws<InvalidOperationException>(() =>
                engine.SuggestBatch(new OversizedCollection<EntitySnapshot>(250001)));
            Throws<InvalidOperationException>(() =>
                new ProjectRecognitionService().SuggestBatch(project, new OversizedCollection<EntitySnapshot>(250001)));

            var observedRules = 0;
            Throws<InvalidOperationException>(() => new RecognitionEngine(OverLimitRules(rule, () => observedRules++)));
            if (observedRules != 10001)
                throw new InvalidOperationException("Lazy recognition rule enumeration did not stop at the cap sentinel.");
        }

        private static void RuleTermsAreReadOnlySnapshots()
        {
            var layerTerms = new List<string> { "  Beam  ", "BEAM" };
            var textTerms = new List<string> { "dam" };
            var entityTypes = new List<string> { "LINE" };
            var rule = new RecognitionRule("readonly-terms", ElementCategory.Beam, layerTerms, textTerms, entityTypes);

            if (rule.LayerTerms.Count != 1 || rule.LayerTerms[0] != "beam")
                throw new InvalidOperationException("Recognition rule layer terms were not normalized deterministically.");
            if (rule.TextTerms.Count != 1 || rule.TextTerms[0] != "dam")
                throw new InvalidOperationException("Recognition rule text terms were not normalized deterministically.");
            if (rule.EntityTypes.Count != 1 || rule.EntityTypes[0] != "line")
                throw new InvalidOperationException("Recognition rule entity types were not normalized deterministically.");

            layerTerms[0] = "column";
            textTerms.Clear();
            entityTypes.Add("solid3d");

            if (rule.LayerTerms.Count != 1 || rule.LayerTerms[0] != "beam" ||
                rule.TextTerms.Count != 1 || rule.TextTerms[0] != "dam" ||
                rule.EntityTypes.Count != 1 || rule.EntityTypes[0] != "line")
                throw new InvalidOperationException("Recognition rule terms changed after constructor source mutation.");

            RejectTermMutation(rule.LayerTerms);
            RejectTermMutation(rule.TextTerms);
            RejectTermMutation(rule.EntityTypes);

            var result = new RecognitionEngine(new[] { rule }).Suggest(new EntitySnapshot("B-READONLY", "line", "beam"));
            if (result.TopCandidate == null || result.TopCandidate.Category != ElementCategory.Beam)
                throw new InvalidOperationException("Read-only term hardening changed ordinary recognition semantics.");
        }

        private static void KnownCountDriftFailsClosed()
        {
            var rule = new RecognitionRule("count-drift-beam", ElementCategory.Beam, new[] { "beam" }, entityTypes: new[] { "line" });

            ThrowsMessage<InvalidOperationException>(
                () => new RecognitionEngine(new DriftingCountCollection<RecognitionRule>(rule, DriftBoundary.MoveNext)),
                "changed its reported Count",
                "recognition rule MoveNext Count drift");

            ThrowsMessage<InvalidOperationException>(
                () => new RecognitionEngine(new DriftingCountCollection<RecognitionRule>(rule, DriftBoundary.Current)),
                "changed its reported Count",
                "recognition rule Current Count drift");

            var stable = new RecognitionEngine(new StableCountCollection<RecognitionRule>(rule));
            if (stable.Suggest(new EntitySnapshot("B-STABLE", "line", "beam")).Candidates.Count != 1)
                throw new InvalidOperationException("Stable counted recognition rule input changed behavior.");

            var streaming = new RecognitionEngine(StreamSingle(rule));
            if (streaming.Suggest(new EntitySnapshot("B-STREAM", "line", "beam")).Candidates.Count != 1)
                throw new InvalidOperationException("Pure-streaming recognition rule input changed behavior.");
        }

        private static void RejectTermMutation(IReadOnlyList<string> terms)
        {
            if (!(terms is IList<string> mutable))
                throw new InvalidOperationException("Recognition rule term collection must expose the standard read-only IList contract.");

            Throws<NotSupportedException>(() => mutable[0] = "mutated");
            Throws<NotSupportedException>(() => mutable.Add("mutated"));
        }

        private static IEnumerable<RecognitionRule> OverLimitRules(RecognitionRule rule, Action observed)
        {
            for (var index = 0; index < 10001; index++)
            {
                observed();
                yield return rule;
            }
            throw new ApplicationException("Recognition rule enumeration exceeded the cap sentinel.");
        }

        private static IEnumerable<T> StreamSingle<T>(T item)
        {
            yield return item;
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

        private static void ThrowsMessage<TException>(Action action, string expected, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(label + ": unexpected error message: " + ex.Message);
                return;
            }

            throw new InvalidOperationException(label + ": expected " + typeof(TException).Name + ".");
        }

        private sealed class OversizedCollection<T> : ICollection<T>
        {
            internal OversizedCollection(int count) => Count = count;

            public int Count { get; }
            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() => throw new ApplicationException("Oversized collection should fail from Count before enumeration.");
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private enum DriftBoundary
        {
            MoveNext,
            Current
        }

        private sealed class DriftingCountCollection<T> : ICollection<T>
        {
            private readonly T _item;
            private readonly DriftBoundary _boundary;
            private int _reportedCount = 1;

            internal DriftingCountCollection(T item, DriftBoundary boundary)
            {
                _item = item;
                _boundary = boundary;
            }

            public int Count => _reportedCount;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly DriftingCountCollection<T> _owner;
                private int _state;

                internal Enumerator(DriftingCountCollection<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        if (_state != 1) throw new InvalidOperationException("Current is unavailable.");
                        if (_owner._boundary == DriftBoundary.Current) _owner._reportedCount = 2;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    if (_state == 0)
                    {
                        _state = 1;
                        if (_owner._boundary == DriftBoundary.MoveNext) _owner._reportedCount = 2;
                        return true;
                    }

                    _owner._reportedCount = 1;
                    _state = 2;
                    return false;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StableCountCollection<T> : ICollection<T>
        {
            private readonly T _item;
            internal StableCountCollection(T item) => _item = item;
            public int Count => 1;
            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator()
            {
                yield return _item;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
