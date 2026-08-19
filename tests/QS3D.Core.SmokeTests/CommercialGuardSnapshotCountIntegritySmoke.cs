using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialGuardSnapshotCountIntegritySmoke
    {
        private const int MaximumSourceRevisions = 64;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            MalformedKnownCountsFailBeforeEnumeration();
            KnownCountMismatchesFailClosed();
            HonestKnownCountRemainsAccepted();
            PureStreamsPreserveBoundary();
            NullItemsRemainRejected();
        }

        private static void MalformedKnownCountsFailBeforeEnumeration()
        {
            AssertPreEnumerationFailure(
                new MultiCountCollection<CommercialRevisionRef>(new[] { Revision(0) }, -1, -1, -1, true),
                "invalid negative known Count");

            AssertPreEnumerationFailure(
                new MultiCountCollection<CommercialRevisionRef>(
                    new[] { Revision(0) },
                    MaximumSourceRevisions + 1,
                    MaximumSourceRevisions + 1,
                    MaximumSourceRevisions + 1,
                    true),
                "supports at most 64 entries");

            AssertPreEnumerationFailure(
                new MultiCountCollection<CommercialRevisionRef>(new[] { Revision(0) }, 1, 2, 1, true),
                "conflicting known Count");
        }

        private static void KnownCountMismatchesFailClosed()
        {
            var under = new MultiCountCollection<CommercialRevisionRef>(new[] { Revision(0) }, 2, 2, 2, false);
            ExpectInvalidOperation(() => Record(under), "known Count does not match completed traversal cardinality");
            Equal(1, under.EnumerationRequestCount, "Under-enumerating counted source must be traversed exactly once.");

            var over = new MultiCountCollection<CommercialRevisionRef>(new[] { Revision(0), Revision(1) }, 1, 1, 1, false);
            ExpectInvalidOperation(() => Record(over), "known Count does not match completed traversal cardinality");
            Equal(1, over.EnumerationRequestCount, "Over-enumerating counted source must be traversed exactly once.");
        }

        private static void HonestKnownCountRemainsAccepted()
        {
            var source = new MultiCountCollection<CommercialRevisionRef>(
                new[] { Revision(1), Revision(0) },
                2,
                2,
                2,
                false);
            var record = Record(source);

            Equal(2, record.SourceRevisions.Count, "Honest counted source must remain accepted.");
            Equal("REV-1", record.SourceRevisions[0].RevisionId, "Snapshot must preserve caller order.");
            Equal("REV-0", record.SourceRevisions[1].RevisionId, "Snapshot must preserve caller order.");
        }

        private static void PureStreamsPreserveBoundary()
        {
            var exact = new StreamingEnumerable<CommercialRevisionRef>(MaximumSourceRevisions, Revision);
            var accepted = Record(exact);
            Equal(MaximumSourceRevisions, accepted.SourceRevisions.Count, "Pure stream must accept the exact 64-entry boundary.");

            var oversized = new StreamingEnumerable<CommercialRevisionRef>(MaximumSourceRevisions + 1, Revision);
            ExpectInvalidOperation(() => Record(oversized), "supports at most 64 entries");
            Equal(MaximumSourceRevisions + 1, oversized.YieldCount, "Pure stream must stop immediately after observing item 65.");
        }

        private static void NullItemsRemainRejected()
        {
            ExpectArgument(() => Record(new CommercialRevisionRef[] { null }), "contains a null item");
        }

        private static CommercialAuditRecord Record(IEnumerable<CommercialRevisionRef> revisions)
        {
            return new CommercialAuditRecord(
                "EVENT-1",
                "estimate-line",
                "LINE-1",
                "rate-assigned",
                "tester",
                new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc),
                "reason",
                "CORR-1",
                "before",
                "after",
                revisions);
        }

        private static CommercialRevisionRef Revision(int index)
        {
            return new CommercialRevisionRef("rate", "SOURCE-1", "REV-" + index);
        }

        private static void AssertPreEnumerationFailure(
            MultiCountCollection<CommercialRevisionRef> source,
            string expectedMessageFragment)
        {
            ExpectInvalidOperation(() => Record(source), expectedMessageFragment);
            if (source.EnumerationRequested)
                throw new InvalidOperationException("Malformed CommercialGuard known Count requested enumeration before failing closed.");
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("Unexpected CommercialGuard Count-integrity diagnostic: " + ex.Message);
            }

            throw new InvalidOperationException("Expected CommercialGuard Count-integrity failure.");
        }

        private static void ExpectArgument(Action action, string expectedMessageFragment)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException("Unexpected CommercialGuard argument diagnostic: " + ex.Message);
            }

            throw new InvalidOperationException("Expected CommercialGuard argument failure.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private sealed class MultiCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            internal MultiCountCollection(
                T[] items,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                bool throwOnEnumeration)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            internal bool EnumerationRequested => EnumerationRequestCount != 0;
            internal int EnumerationRequestCount { get; private set; }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationRequestCount++;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Enumerator must not be requested for malformed known Count input.");
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        }

        private sealed class StreamingEnumerable<T> : IEnumerable<T>
        {
            private readonly int _count;
            private readonly Func<int, T> _factory;

            internal StreamingEnumerable(int count, Func<int, T> factory)
            {
                _count = count;
                _factory = factory;
            }

            internal int YieldCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldCount++;
                    yield return _factory(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
