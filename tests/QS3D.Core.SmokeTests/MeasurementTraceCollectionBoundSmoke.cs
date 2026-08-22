using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceCollectionBoundSmoke
    {
        private const int MaximumEntries = 10000;

        internal static void Run()
        {
            GenericCountedFactsOverflowFailsBeforeEnumeration();
            ReadOnlyCountedAdjustmentsOverflowFailsBeforeEnumeration();
            NonGenericCountedMessagesOverflowFailsBeforeEnumeration();
            StreamingFactsOverflowStopsAtFirstDisallowedEntry();
            StreamingAdjustmentsOverflowStopsAtFirstDisallowedEntry();
            StreamingMessagesOverflowStopsAtFirstDisallowedEntry();
            ExactBoundaryRemainsAccepted();
            CanonicalValidationRemainsStable();
        }

        private static void GenericCountedFactsOverflowFailsBeforeEnumeration()
        {
            var source = new GenericCountedNeverEnumerated<MeasurementTraceFact>(MaximumEntries + 1);
            var error = Capture<ArgumentException>(() => Trace(inputFacts: source));

            Equal(0, source.GetEnumeratorCalls, "Oversized ICollection facts must fail before enumeration.");
            Contains("at most 10000", error.Message, "Facts oversize failure must report the collection bound.");
        }

        private static void ReadOnlyCountedAdjustmentsOverflowFailsBeforeEnumeration()
        {
            var source = new ReadOnlyCountedNeverEnumerated<MeasurementTraceAdjustment>(MaximumEntries + 1);
            var error = Capture<ArgumentException>(() => Trace(adjustments: source));

            Equal(0, source.GetEnumeratorCalls, "Oversized IReadOnlyCollection adjustments must fail before enumeration.");
            Contains("at most 10000", error.Message, "Adjustment oversize failure must report the collection bound.");
        }

        private static void NonGenericCountedMessagesOverflowFailsBeforeEnumeration()
        {
            var source = new NonGenericCountedNeverEnumerated(MaximumEntries + 1);
            var error = Capture<ArgumentException>(() => Trace(warnings: source));

            Equal(0, source.GetEnumeratorCalls, "Oversized non-generic ICollection messages must fail before enumeration.");
            Contains("at most 10000", error.Message, "Message oversize failure must report the collection bound.");
        }

        private static void StreamingFactsOverflowStopsAtFirstDisallowedEntry()
        {
            var source = new StreamingSource<MeasurementTraceFact>(MaximumEntries + 2, Fact);
            var error = Capture<ArgumentException>(() => Trace(inputFacts: source));

            Equal(MaximumEntries + 1, source.YieldedCount, "Facts ingestion must stop after observing entry 10,001.");
            Contains("at most 10000", error.Message, "Streaming facts oversize failure must report the collection bound.");
        }

        private static void StreamingAdjustmentsOverflowStopsAtFirstDisallowedEntry()
        {
            var source = new StreamingSource<MeasurementTraceAdjustment>(MaximumEntries + 2, Adjustment);
            var error = Capture<ArgumentException>(() => Trace(adjustments: source));

            Equal(MaximumEntries + 1, source.YieldedCount, "Adjustment ingestion must stop after observing entry 10,001.");
            Contains("at most 10000", error.Message, "Streaming adjustment oversize failure must report the collection bound.");
        }

        private static void StreamingMessagesOverflowStopsAtFirstDisallowedEntry()
        {
            var source = new StreamingSource<string>(MaximumEntries + 2, Message);
            var error = Capture<ArgumentException>(() => Trace(assumptions: source));

            Equal(MaximumEntries + 1, source.YieldedCount, "Message ingestion must stop after observing entry 10,001.");
            Contains("at most 10000", error.Message, "Streaming message oversize failure must report the collection bound.");
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var facts = new MeasurementTraceFact[MaximumEntries];
            var adjustments = new MeasurementTraceAdjustment[MaximumEntries];
            var warnings = new string[MaximumEntries];
            var assumptions = new string[MaximumEntries];
            for (var i = 0; i < MaximumEntries; i++)
            {
                facts[i] = Fact(i);
                adjustments[i] = Adjustment(i);
                warnings[i] = "warning-" + Suffix(i);
                assumptions[i] = "assumption-" + Suffix(i);
            }

            var trace = Trace(facts, adjustments, warnings, assumptions);
            Equal(MaximumEntries, trace.InputFacts.Count, "Measurement trace must accept exactly 10,000 facts.");
            Equal(MaximumEntries, trace.Adjustments.Count, "Measurement trace must accept exactly 10,000 adjustments.");
            Equal(MaximumEntries, trace.Warnings.Count, "Measurement trace must accept exactly 10,000 warnings.");
            Equal(MaximumEntries, trace.Assumptions.Count, "Measurement trace must accept exactly 10,000 assumptions.");
        }

        private static void CanonicalValidationRemainsStable()
        {
            var trace = Trace(
                new[] { Fact(2), Fact(0), Fact(1) },
                new[] { Adjustment(2), Adjustment(0), Adjustment(1) },
                new[] { "warning-b", "warning-a" },
                new[] { "assumption-b", "assumption-a" });

            Equal("fact-00000", trace.InputFacts[0].Name, "Fact canonical ordering changed.");
            Equal("adj-src-00000", trace.Adjustments[0].SourceIdentity, "Adjustment canonical ordering changed.");
            Equal("warning-a", trace.Warnings[0], "Warning canonical ordering changed.");
            Equal("assumption-a", trace.Assumptions[0], "Assumption canonical ordering changed.");

            Capture<ArgumentException>(() => Trace(inputFacts: new[] { Fact(7), Fact(7) }));
            Capture<ArgumentException>(() => Trace(adjustments: new[] { Adjustment(7), Adjustment(7) }));
            Capture<ArgumentException>(() => Trace(warnings: new[] { "duplicate", "duplicate" }));
            Capture<ArgumentException>(() => Trace(inputFacts: new MeasurementTraceFact[] { null! }));
        }

        private static MeasurementTrace Trace(
            IEnumerable<MeasurementTraceFact>? inputFacts = null,
            IEnumerable<MeasurementTraceAdjustment>? adjustments = null,
            IEnumerable<string>? warnings = null,
            IEnumerable<string>? assumptions = null)
        {
            return new MeasurementTrace(
                "SEM-TRACE-BOUND",
                "SRC-TRACE-BOUND",
                "QTY-TRACE-BOUND",
                inputFacts ?? Array.Empty<MeasurementTraceFact>(),
                1d,
                adjustments ?? Array.Empty<MeasurementTraceAdjustment>(),
                1d,
                "m",
                "none",
                warnings,
                assumptions);
        }

        private static MeasurementTraceFact Fact(int index)
        {
            var suffix = Suffix(index);
            return new MeasurementTraceFact("fact-" + suffix, index, "m", "fact-src-" + suffix);
        }

        private static MeasurementTraceAdjustment Adjustment(int index)
        {
            var suffix = Suffix(index);
            return new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Addition,
                0d,
                "m",
                "adjustment-" + suffix,
                "adj-src-" + suffix);
        }

        private static string Message(int index) => "message-" + Suffix(index);

        private static string Suffix(int index) => index.ToString("D5", CultureInfo.InvariantCulture);

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
            internal GenericCountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            public bool IsReadOnly => true;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
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
            internal ReadOnlyCountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericCountedNeverEnumerated : ICollection, IEnumerable<string>
        {
            internal NonGenericCountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot { get; } = new object();
            internal int GetEnumeratorCalls { get; private set; }

            public void CopyTo(Array array, int index) => throw new NotSupportedException();

            public IEnumerator<string> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingSource<T> : IEnumerable<T>
        {
            private readonly int _count;
            private readonly Func<int, T> _factory;

            internal StreamingSource(int count, Func<int, T> factory)
            {
                _count = count;
                _factory = factory;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return _factory(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class MeasurementTraceCollectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasurementTraceCollectionBoundSmoke.Run();
        }
    }
}
