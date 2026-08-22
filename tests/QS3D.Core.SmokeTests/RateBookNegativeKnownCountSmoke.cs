using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBookNegativeKnownCountSmoke
    {
        internal static void Run()
        {
            GenericCountFailsBeforeEnumeration();
            ReadOnlyCountFailsBeforeEnumeration();
            NonGenericCountFailsBeforeEnumeration();
        }

        private static void GenericCountFailsBeforeEnumeration()
        {
            var source = new NegativeGenericCount<RateItem>();
            var error = Capture<InvalidOperationException>(() => new RateBook("NEGATIVE-GENERIC", source));

            Equal(1, source.CountReads, "ICollection<T>.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative ICollection<T>.Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative ICollection<T>.Count must fail closed explicitly.");
        }

        private static void ReadOnlyCountFailsBeforeEnumeration()
        {
            var source = new NegativeReadOnlyCount<RateItem>();
            var error = Capture<InvalidOperationException>(() => new RateBook("NEGATIVE-READONLY", source));

            Equal(1, source.CountReads, "IReadOnlyCollection<T>.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative IReadOnlyCollection<T>.Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative IReadOnlyCollection<T>.Count must fail closed explicitly.");
        }

        private static void NonGenericCountFailsBeforeEnumeration()
        {
            var source = new NegativeNonGenericCount<RateItem>();
            var error = Capture<InvalidOperationException>(() => new RateBook("NEGATIVE-NONGENERIC", source));

            Equal(1, source.CountReads, "ICollection.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative ICollection.Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative ICollection.Count must fail closed explicitly.");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class NegativeGenericCount<T> : ICollection<T>
        {
            public int Count
            {
                get
                {
                    CountReads++;
                    return -1;
                }
            }

            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Negative counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class NegativeReadOnlyCount<T> : IReadOnlyCollection<T>
        {
            public int Count
            {
                get
                {
                    CountReads++;
                    return -1;
                }
            }

            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Negative counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NegativeNonGenericCount<T> : IEnumerable<T>, ICollection
        {
            public int Count
            {
                get
                {
                    CountReads++;
                    return -1;
                }
            }

            public bool IsSynchronized => false;
            public object SyncRoot => this;
            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Negative counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }

    internal static class RateBookNegativeKnownCountRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateBookNegativeKnownCountSmoke.Run();
        }
    }
}
