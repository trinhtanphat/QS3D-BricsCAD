using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceKnownCountIntegritySmoke
    {
        internal static void Run()
        {
            NegativeGenericFactsCountFailsBeforeEnumeration();
            NegativeReadOnlyAdjustmentsCountFailsBeforeEnumeration();
            NegativeNonGenericMessagesCountFailsBeforeEnumeration();
            GenericReadOnlyFactsCountConflictFailsBeforeEnumeration();
            GenericNonGenericMessagesCountConflictFailsBeforeEnumeration();
            ReadOnlyNonGenericAdjustmentsCountConflictFailsBeforeEnumeration();
            ConsistentMultiContractCountsRemainAccepted();
        }

        private static void NegativeGenericFactsCountFailsBeforeEnumeration()
        {
            var source = new GenericCountedNeverEnumerated<MeasurementTraceFact>(-1);
            var error = Capture<ArgumentException>(() => Trace(inputFacts: source));

            Equal(0, source.GetEnumeratorCalls, "Negative ICollection facts Count must fail before enumeration.");
            Contains("count cannot be negative", error.Message, "Negative facts Count must be diagnosed explicitly.");
        }

        private static void NegativeReadOnlyAdjustmentsCountFailsBeforeEnumeration()
        {
            var source = new ReadOnlyCountedNeverEnumerated<MeasurementTraceAdjustment>(-1);
            var error = Capture<ArgumentException>(() => Trace(adjustments: source));

            Equal(0, source.GetEnumeratorCalls, "Negative IReadOnlyCollection adjustments Count must fail before enumeration.");
            Contains("count cannot be negative", error.Message, "Negative adjustments Count must be diagnosed explicitly.");
        }

        private static void NegativeNonGenericMessagesCountFailsBeforeEnumeration()
        {
            var source = new NonGenericCountedNeverEnumerated(-1);
            var error = Capture<ArgumentException>(() => Trace(warnings: source));

            Equal(0, source.GetEnumeratorCalls, "Negative non-generic ICollection message Count must fail before enumeration.");
            Contains("count cannot be negative", error.Message, "Negative message Count must be diagnosed explicitly.");
        }

        private static void GenericReadOnlyFactsCountConflictFailsBeforeEnumeration()
        {
            var source = new GenericReadOnlyCountedNeverEnumerated<MeasurementTraceFact>(1, 2);
            var error = Capture<ArgumentException>(() => Trace(inputFacts: source));

            Equal(0, source.GetEnumeratorCalls, "Conflicting generic/read-only facts Counts must fail before enumeration.");
            Contains("count contracts disagree", error.Message, "Conflicting facts Counts must be diagnosed explicitly.");
        }

        private static void GenericNonGenericMessagesCountConflictFailsBeforeEnumeration()
        {
            var source = new GenericNonGenericCountedNeverEnumerated<string>(1, 2);
            var error = Capture<ArgumentException>(() => Trace(assumptions: source));

            Equal(0, source.GetEnumeratorCalls, "Conflicting generic/non-generic message Counts must fail before enumeration.");
            Contains("count contracts disagree", error.Message, "Conflicting message Counts must be diagnosed explicitly.");
        }

        private static void ReadOnlyNonGenericAdjustmentsCountConflictFailsBeforeEnumeration()
        {
            var source = new ReadOnlyNonGenericCountedNeverEnumerated<MeasurementTraceAdjustment>(1, 2);
            var error = Capture<ArgumentException>(() => Trace(adjustments: source));

            Equal(0, source.GetEnumeratorCalls, "Conflicting read-only/non-generic adjustment Counts must fail before enumeration.");
            Contains("count contracts disagree", error.Message, "Conflicting adjustment Counts must be diagnosed explicitly.");
        }

        private static void ConsistentMultiContractCountsRemainAccepted()
        {
            var fact = new MeasurementTraceFact("fact", 1d, "m", "fact-source");
            var source = new ConsistentMultiCountSource<MeasurementTraceFact>(fact);

            var trace = Trace(inputFacts: source);

            Equal(1, source.GetEnumeratorCalls, "Consistent known Count contracts must remain enumerable.");
            Equal(1, trace.InputFacts.Count, "Consistent known Count contracts must preserve the input fact.");
            Equal("fact", trace.InputFacts[0].Name, "Consistent known Count contracts changed canonical fact payload.");
        }

        private static MeasurementTrace Trace(
            IEnumerable<MeasurementTraceFact>? inputFacts = null,
            IEnumerable<MeasurementTraceAdjustment>? adjustments = null,
            IEnumerable<string>? warnings = null,
            IEnumerable<string>? assumptions = null)
        {
            return new MeasurementTrace(
                "SEM-TRACE-COUNT-INTEGRITY",
                "SRC-TRACE-COUNT-INTEGRITY",
                "QTY-TRACE-COUNT-INTEGRITY",
                inputFacts ?? Array.Empty<MeasurementTraceFact>(),
                1d,
                adjustments ?? Array.Empty<MeasurementTraceAdjustment>(),
                1d,
                "m",
                "none",
                warnings,
                assumptions);
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

        private sealed class GenericCountedNeverEnumerated<T> : ICollection<T>
        {
            private readonly int _count;

            internal GenericCountedNeverEnumerated(int count)
            {
                _count = count;
            }

            public int Count => _count;
            public bool IsReadOnly => true;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Invalid known Count must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyCountedNeverEnumerated<T> : IReadOnlyCollection<T>
        {
            private readonly int _count;

            internal ReadOnlyCountedNeverEnumerated(int count)
            {
                _count = count;
            }

            public int Count => _count;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Invalid known Count must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericCountedNeverEnumerated : ICollection, IEnumerable<string>
        {
            private readonly int _count;

            internal NonGenericCountedNeverEnumerated(int count)
            {
                _count = count;
            }

            public int Count => _count;
            public bool IsSynchronized => false;
            public object SyncRoot { get; } = new object();
            internal int GetEnumeratorCalls { get; private set; }

            public void CopyTo(Array array, int index) => throw new NotSupportedException();

            public IEnumerator<string> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Invalid known Count must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class GenericReadOnlyCountedNeverEnumerated<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;

            internal GenericReadOnlyCountedNeverEnumerated(int genericCount, int readOnlyCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
            }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            bool ICollection<T>.IsReadOnly => true;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Conflicting known Counts must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => throw new NotSupportedException();
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        }

        private sealed class GenericNonGenericCountedNeverEnumerated<T> : ICollection<T>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _nonGenericCount;

            internal GenericNonGenericCountedNeverEnumerated(int genericCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _nonGenericCount = nonGenericCount;
            }

            int ICollection<T>.Count => _genericCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot { get; } = new object();
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Conflicting known Counts must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => throw new NotSupportedException();
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyNonGenericCountedNeverEnumerated<T> : IReadOnlyCollection<T>, ICollection
        {
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal ReadOnlyNonGenericCountedNeverEnumerated(int readOnlyCount, int nonGenericCount)
            {
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot { get; } = new object();
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Conflicting known Counts must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class ConsistentMultiCountSource<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;

            internal ConsistentMultiCountSource(params T[] items)
            {
                _items = items;
            }

            int ICollection<T>.Count => _items.Length;
            int IReadOnlyCollection<T>.Count => _items.Length;
            int ICollection.Count => _items.Length;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot { get; } = new object();
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => ((ICollection<T>)_items).Contains(item);
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }
    }

    internal static class MeasurementTraceKnownCountIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementTraceKnownCountIntegritySmoke.Run();
        }
    }
}
