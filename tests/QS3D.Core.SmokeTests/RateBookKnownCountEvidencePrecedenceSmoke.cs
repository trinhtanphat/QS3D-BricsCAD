using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBookKnownCountEvidencePrecedenceSmoke
    {
        internal static void Run()
        {
            NegativeEvidenceOutranksOversizedCount();
            ConflictingEvidenceOutranksOversizedCount();
            HonestOversizedCountKeepsCardinalityDiagnostic();
        }

        private static void NegativeEvidenceOutranksOversizedCount()
        {
            var items = new HostileCountCollection(10001, -1, 10001);
            ExpectInvalid(
                () => new RateBook("BOOK-NEGATIVE", items),
                "Rate book item source reports an invalid negative known count.");
            Equal(0, items.EnumerationAttempts, "Negative Count evidence must fail before enumeration.");
        }

        private static void ConflictingEvidenceOutranksOversizedCount()
        {
            var items = new HostileCountCollection(10001, 1, 10001);
            ExpectInvalid(
                () => new RateBook("BOOK-CONFLICT", items),
                "Rate book item source reports conflicting known counts.");
            Equal(0, items.EnumerationAttempts, "Conflicting Count evidence must fail before enumeration.");
        }

        private static void HonestOversizedCountKeepsCardinalityDiagnostic()
        {
            var items = new HostileCountCollection(10001, 10001, 10001);
            ExpectInvalid(
                () => new RateBook("BOOK-OVERSIZED", items),
                "Rate book supports at most 10000 rate items.");
            Equal(0, items.EnumerationAttempts, "Oversized known Count must fail before enumeration.");
        }

        private static void ExpectInvalid(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(expectedMessage, ex.Message, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Unexpected RateBook failure. Expected: " + expectedMessage + " Actual: " + ex.Message,
                        ex);
                return;
            }

            throw new InvalidOperationException("Expected RateBook construction to fail closed: " + expectedMessage);
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual + ".");
        }

        private sealed class HostileCountCollection : ICollection<RateItem>, IReadOnlyCollection<RateItem>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal HostileCountCollection(int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            internal int EnumerationAttempts { get; private set; }

            int ICollection<RateItem>.Count => _genericCount;
            int IReadOnlyCollection<RateItem>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<RateItem>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            IEnumerator<RateItem> IEnumerable<RateItem>.GetEnumerator()
            {
                EnumerationAttempts++;
                throw new InvalidOperationException("RateBook enumerated before hostile Count evidence was rejected.");
            }

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<RateItem>)this).GetEnumerator();

            void ICollection<RateItem>.Add(RateItem item) => throw new NotSupportedException();
            void ICollection<RateItem>.Clear() => throw new NotSupportedException();
            bool ICollection<RateItem>.Contains(RateItem item) => false;
            void ICollection<RateItem>.CopyTo(RateItem[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<RateItem>.Remove(RateItem item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
