using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageKnownCountSmoke
    {
        internal static void Run()
        {
            EmptyInputRemainsValid();
            OversizedKnownCountFailsBeforeEnumeration();
            NegativeKnownCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
        }

        private static void EmptyInputRemainsValid()
        {
            var report = MeasurementWorkItemCoverageReport.Create(Array.Empty<MeasurementWorkItemCoverageFinding>());
            Equal(0, report.TotalCount, "Empty coverage-report input must remain valid.");
            Equal(0, report.ReadyCount, "Empty coverage-report input must have zero ready rows.");
        }

        private static void OversizedKnownCountFailsBeforeEnumeration()
        {
            var source = new MultiCountNeverEnumerated<MeasurementWorkItemCoverageFinding>(10001, 10001, 10001);
            var error = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(source));
            AssertAllCountContractsReadOnce(source, "Oversized coverage-report Count validation");
            Equal(0, source.GetEnumeratorCalls, "Oversized known Count must fail before coverage-report enumeration.");
            Contains("maximum supported finding count of 10000", error.Message, "Oversized coverage-report Count must preserve the existing capacity error.");
        }

        private static void NegativeKnownCountFailsBeforeEnumeration()
        {
            var source = new MultiCountNeverEnumerated<MeasurementWorkItemCoverageFinding>(-1, -1, -1);
            var error = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(source));
            AssertAllCountContractsReadOnce(source, "Negative coverage-report Count validation");
            Equal(0, source.GetEnumeratorCalls, "Negative known Count must fail before coverage-report enumeration.");
            Contains("negative known count", error.Message, "Negative coverage-report Count must fail closed explicitly.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new MultiCountNeverEnumerated<MeasurementWorkItemCoverageFinding>(1, 2, 1);
            var error = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(source));
            AssertAllCountContractsReadOnce(source, "Conflicting coverage-report Count validation");
            Equal(0, source.GetEnumeratorCalls, "Conflicting known Counts must fail before coverage-report enumeration.");
            Contains("conflicting known counts", error.Message, "Conflicting coverage-report Count contracts must fail closed explicitly.");
        }

        private static void AssertAllCountContractsReadOnce<T>(MultiCountNeverEnumerated<T> source, string message)
        {
            Equal(1, source.GenericCountReads, message + " must inspect ICollection<T>.Count exactly once.");
            Equal(1, source.ReadOnlyCountReads, message + " must inspect IReadOnlyCollection<T>.Count exactly once.");
            Equal(1, source.NonGenericCountReads, message + " must inspect ICollection.Count exactly once.");
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
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class MultiCountNeverEnumerated<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal MultiCountNeverEnumerated(int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            int ICollection<T>.Count
            {
                get
                {
                    GenericCountReads++;
                    return _genericCount;
                }
            }

            int IReadOnlyCollection<T>.Count
            {
                get
                {
                    ReadOnlyCountReads++;
                    return _readOnlyCount;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountReads++;
                    return _nonGenericCount;
                }
            }

            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int GenericCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Malformed coverage-report source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => false;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }

    internal static class MeasurementWorkItemCoverageKnownCountRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementWorkItemCoverageKnownCountSmoke.Run();
        }
    }
}
