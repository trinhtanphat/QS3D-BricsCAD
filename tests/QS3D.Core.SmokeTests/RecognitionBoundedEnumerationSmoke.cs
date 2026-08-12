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

        private static IEnumerable<RecognitionRule> OverLimitRules(RecognitionRule rule, Action observed)
        {
            for (var index = 0; index < 10001; index++)
            {
                observed();
                yield return rule;
            }
            throw new ApplicationException("Recognition rule enumeration exceeded the cap sentinel.");
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
    }
}
