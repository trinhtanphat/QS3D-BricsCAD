using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserQueryKnownCountContractSmoke
    {
        private const int Limit = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsMalformedKnownCountsBeforeEnumeration();
            AcceptsConsistentKnownCounts();
            KeepsStreamingLimit();
        }

        private static void RejectsMalformedKnownCountsBeforeEnumeration()
        {
            AssertRejectedBeforeEnumeration(
                new MultiCountSource<string>(-1, -1, -1),
                source => new ProjectBrowserQueryOptions(floorIds: source),
                "negative Count");
            AssertRejectedBeforeEnumeration(
                new MultiCountSource<string>(Limit + 1, Limit + 1, Limit + 1),
                source => new ProjectBrowserQueryOptions(zoneIds: source),
                "oversized Count");
            AssertRejectedBeforeEnumeration(
                new MultiCountSource<ElementCategory>(1, 2, 1),
                source => new ProjectBrowserQueryOptions(categories: source),
                "conflicting Count contracts");
            AssertRejectedBeforeEnumeration(
                new NonGenericCountSource<string>(Limit + 1),
                source => new ProjectBrowserQueryOptions(floorIds: source),
                "non-generic Count");
        }

        private static void AcceptsConsistentKnownCounts()
        {
            var source = new MultiCountSource<string>(0, 0, 0, allowEnumeration: true);
            var options = new ProjectBrowserQueryOptions(zoneIds: source);
            if (options.ZoneIds.Count != 0 || !source.Enumerated)
                throw new InvalidOperationException("ProjectBrowserQueryKnownCountContractSmoke expected consistent counts to enumerate normally.");
        }

        private static void KeepsStreamingLimit()
        {
            var source = new StreamingSource<string>(Limit + 1, "F1");
            try
            {
                _ = new ProjectBrowserQueryOptions(floorIds: source);
            }
            catch (InvalidOperationException)
            {
                if (source.Yielded != Limit + 1)
                    throw new InvalidOperationException("ProjectBrowserQueryKnownCountContractSmoke expected rejection at the 10001st streaming item.");
                return;
            }
            throw new InvalidOperationException("ProjectBrowserQueryKnownCountContractSmoke did not enforce the streaming filter bound.");
        }

        private static void AssertRejectedBeforeEnumeration<T>(TrackingEnumerable<T> source, Action<IEnumerable<T>> action, string label)
        {
            try
            {
                action(source);
            }
            catch (InvalidOperationException)
            {
                if (source.Enumerated)
                    throw new InvalidOperationException("ProjectBrowserQueryKnownCountContractSmoke enumerated " + label + " before rejecting it.");
                return;
            }
            throw new InvalidOperationException("ProjectBrowserQueryKnownCountContractSmoke did not reject " + label + ".");
        }

        private abstract class TrackingEnumerable<T> : IEnumerable<T>
        {
            public bool Enumerated { get; protected set; }
            public abstract IEnumerator<T> GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class MultiCountSource<T> : TrackingEnumerable<T>, ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _allowEnumeration;

            public MultiCountSource(int genericCount, int readOnlyCount, int nonGenericCount, bool allowEnumeration = false)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _allowEnumeration = allowEnumeration;
            }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public override IEnumerator<T> GetEnumerator()
            {
                Enumerated = true;
                if (!_allowEnumeration)
                    throw new InvalidOperationException("Malformed known Count evidence must fail before enumeration.");
                return ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();
            }

            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class NonGenericCountSource<T> : TrackingEnumerable<T>, ICollection
        {
            public NonGenericCountSource(int count) { Count = count; }
            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public override IEnumerator<T> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Known Count evidence must fail before enumeration.");
            }
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class StreamingSource<T> : IEnumerable<T>
        {
            private readonly int _count;
            private readonly T _value;
            public StreamingSource(int count, T value) { _count = count; _value = value; }
            public int Yielded { get; private set; }
            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    Yielded++;
                    yield return _value;
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}