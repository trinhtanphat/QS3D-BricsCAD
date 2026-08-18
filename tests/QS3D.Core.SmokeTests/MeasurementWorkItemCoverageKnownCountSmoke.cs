using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
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
            AdvertisedCountGreaterThanTraversalFailsClosed();
            AdvertisedCountSmallerThanTraversalFailsClosed();
            MatchingKnownCountRemainsValid();
            PureStreamingInputRemainsValid();
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

        private static void AdvertisedCountGreaterThanTraversalFailsClosed()
        {
            var finding = CreateValidFinding();
            var source = new CountedSequence<MeasurementWorkItemCoverageFinding>(2, new[] { finding });
            var error = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(source));
            Contains("traversal count", error.Message, "Coverage-report under-enumeration must fail on the Count/traversal contract.");
        }

        private static void AdvertisedCountSmallerThanTraversalFailsClosed()
        {
            var finding = CreateValidFinding();
            var source = new CountedSequence<MeasurementWorkItemCoverageFinding>(0, new[] { finding });
            var error = Capture<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(source));
            Contains("traversal count", error.Message, "Coverage-report over-enumeration must fail on the Count/traversal contract.");
        }

        private static void MatchingKnownCountRemainsValid()
        {
            var finding = CreateValidFinding();
            var source = new CountedSequence<MeasurementWorkItemCoverageFinding>(1, new[] { finding });
            var report = MeasurementWorkItemCoverageReport.Create(source);
            Equal(1, report.TotalCount, "An honest known Count must remain accepted.");
        }

        private static void PureStreamingInputRemainsValid()
        {
            var finding = CreateValidFinding();
            var report = MeasurementWorkItemCoverageReport.Create(Stream(finding));
            Equal(1, report.TotalCount, "A pure streaming source without a known Count must remain accepted.");
        }

        private static MeasurementWorkItemCoverageFinding CreateValidFinding()
        {
            var project = new ProjectState("coverage-count-project", "Coverage Count Project");
            var element = new ProjectElement("coverage-element", ElementCategory.Column);
            element.SetQuantity("LengthM", 1d);
            project.Elements.Add(element);

            var findings = MeasurementWorkItemCoverageEvaluator.Evaluate(project);
            Equal(1, findings.Count, "Coverage fixture must produce exactly one finding.");
            return findings[0];
        }

        private static IEnumerable<MeasurementWorkItemCoverageFinding> Stream(MeasurementWorkItemCoverageFinding finding)
        {
            yield return finding;
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

        private sealed class CountedSequence<T> : ICollection<T>
        {
            private readonly int _advertisedCount;
            private readonly IReadOnlyList<T> _items;

            internal CountedSequence(int advertisedCount, IReadOnlyList<T> items)
            {
                _advertisedCount = advertisedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count => _advertisedCount;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Count; i++)
                    yield return _items[i];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => false;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
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