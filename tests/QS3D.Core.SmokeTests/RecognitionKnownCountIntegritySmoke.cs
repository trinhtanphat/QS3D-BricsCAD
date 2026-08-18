using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class RecognitionKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var first = new RecognitionRule("count-one", ElementCategory.Beam);
            var second = new RecognitionRule("count-two", ElementCategory.Column);

            RejectsNegativeCountBeforeEnumeration(first);
            RejectsConflictingCountsBeforeEnumeration(first);
            RejectsOversizedNonGenericCountBeforeEnumeration(first);
            RejectsUnderYield(first);
            RejectsOverYield(first, second);
            AcceptsHonestCount(first);
            AcceptsPureStreaming(first);
        }

        private static void RejectsNegativeCountBeforeEnumeration(RecognitionRule rule)
        {
            var source = new KnownCountCollection<RecognitionRule>(-1, -1, -1, new[] { rule });
            Throws<InvalidOperationException>(() => new RecognitionEngine(source));
            if (source.EnumeratorCalls != 0)
                throw new InvalidOperationException("Recognition negative Count reached enumeration.");
        }

        private static void RejectsConflictingCountsBeforeEnumeration(RecognitionRule rule)
        {
            var source = new KnownCountCollection<RecognitionRule>(1, 2, 1, new[] { rule });
            Throws<InvalidOperationException>(() => new RecognitionEngine(source));
            if (source.EnumeratorCalls != 0)
                throw new InvalidOperationException("Recognition conflicting Count contracts reached enumeration.");
        }

        private static void RejectsOversizedNonGenericCountBeforeEnumeration(RecognitionRule rule)
        {
            var source = new KnownCountCollection<RecognitionRule>(1, 1, 10001, new[] { rule });
            Throws<InvalidOperationException>(() => new RecognitionEngine(source));
            if (source.EnumeratorCalls != 0)
                throw new InvalidOperationException("Recognition oversized non-generic Count reached enumeration.");
        }

        private static void RejectsUnderYield(RecognitionRule rule)
        {
            var source = new KnownCountCollection<RecognitionRule>(2, 2, 2, new[] { rule });
            Throws<InvalidOperationException>(() => new RecognitionEngine(source));
            if (source.EnumeratorCalls != 1)
                throw new InvalidOperationException("Recognition under-yield traversal was not evaluated exactly once.");
        }

        private static void RejectsOverYield(RecognitionRule first, RecognitionRule second)
        {
            var source = new KnownCountCollection<RecognitionRule>(1, 1, 1, new[] { first, second });
            Throws<InvalidOperationException>(() => new RecognitionEngine(source));
            if (source.EnumeratorCalls != 1)
                throw new InvalidOperationException("Recognition over-yield traversal was not evaluated exactly once.");
        }

        private static void AcceptsHonestCount(RecognitionRule rule)
        {
            var source = new KnownCountCollection<RecognitionRule>(1, 1, 1, new[] { rule });
            _ = new RecognitionEngine(source);
            if (source.EnumeratorCalls != 1)
                throw new InvalidOperationException("Recognition honest counted input was not enumerated exactly once.");
        }

        private static void AcceptsPureStreaming(RecognitionRule rule)
        {
            var observed = 0;
            _ = new RecognitionEngine(Stream(rule, () => observed++));
            if (observed != 1)
                throw new InvalidOperationException("Recognition pure streaming input changed while hardening known Count integrity.");
        }

        private static IEnumerable<RecognitionRule> Stream(RecognitionRule rule, Action observed)
        {
            observed();
            yield return rule;
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

        private sealed class KnownCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly List<T> _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal KnownCountCollection(int genericCount, int readOnlyCount, int nonGenericCount, IEnumerable<T> items)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _items = new List<T>(items);
            }

            internal int EnumeratorCalls { get; private set; }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                EnumeratorCalls++;
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => _items.Contains(item);
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}