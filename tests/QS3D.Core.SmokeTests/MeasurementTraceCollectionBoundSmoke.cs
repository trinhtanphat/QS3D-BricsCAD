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
            CountedFactsOversizeFailsBeforeEnumeration();
            NonGenericFactsOversizeFailsBeforeEnumeration();
            StreamingFactsStopAtFirstDisallowedEntry();
            StreamingAdjustmentsStopAtFirstDisallowedEntry();
            StreamingWarningsStopAtFirstDisallowedEntry();
            StreamingAssumptionsStopAtFirstDisallowedEntry();
            ExactFactBoundaryRemainsAccepted();
            ExactAdjustmentBoundaryRemainsAccepted();
            ExactMessageBoundariesRemainAccepted();
            CanonicalOrderingAndValidationRemainStable();
        }

        private static void CountedFactsOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<MeasurementTraceFact>(MaximumEntries + 1);
            var error = Capture<ArgumentException>(() => Trace(source, EmptyAdjustments(), null, null));
            Equal(0, source.GetEnumeratorCalls, "Oversized counted facts must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted facts oversize must report the bound.");
        }

        private static void NonGenericFactsOversizeFailsBeforeEnumeration()
        {
            var source = new NonGenericCountedFacts(MaximumEntries + 1);
            var error = Capture<ArgumentException>(() => Trace(source, EmptyAdjustments(), null, null));
            Equal(0, source.GetEnumeratorCalls, "Oversized non-generic counted facts must fail before enumeration.");
            Contains("at most 10000", error.Message, "Non-generic facts oversize must report the bound.");
        }

        private static void StreamingFactsStopAtFirstDisallowedEntry()
        {
            var source = new StreamingSequence<MeasurementTraceFact>(MaximumEntries + 2, Fact);
            var error = Capture<ArgumentException>(() => Trace(source, EmptyAdjustments(), null, null));
            Equal(MaximumEntries + 1, source.YieldedCount, "Facts ingestion must stop after observing entry 10,001.");
            Contains("at most 10000", error.Message, "Streaming facts oversize must report the bound.");
        }

        private static void StreamingAdjustmentsStopAtFirstDisallowedEntry()
        {
            var source = new StreamingSequence<MeasurementTraceAdjustment>(MaximumEntries + 2, Adjustment);
            var error = Capture<ArgumentException>(() => Trace(EmptyFacts(), source, null, null));
            Equal(MaximumEntries + 1, source.YieldedCount, "Adjustment ingestion must stop after observing entry 10,001.");
            Contains("at most 10000", error.Message, "Streaming adjustment oversize must report the bound.");
        }

        private static void StreamingWarningsStopAtFirstDisallowedEntry()
        {
            var source = new StreamingSequence<string>(MaximumEntries + 2, Message);
            var error = Capture<ArgumentException>(() => Trace(EmptyFacts(), EmptyAdjustments(), source, null));
            Equal(MaximumEntries + 1, source.YieldedCount, "Warning ingestion must stop after observing entry 10,001.");
            Contains("at most 10000", error.Message, "Streaming warning oversize must report the bound.");
        }

        private static void StreamingAssumptionsStopAtFirstDisallowedEntry()
        {
            var source = new StreamingSequence<string>(MaximumEntries + 2, Message);
            var error = Capture<ArgumentException>(() => Trace(EmptyFacts(), EmptyAdjustments(), null, source));
            Equal(MaximumEntries + 1, source.YieldedCount, "Assumption ingestion must stop after observing entry 10,001.");
            Contains("at most 10000", error.Message, "Streaming assumption oversize must report the bound.");
        }

        private static void ExactFactBoundaryRemainsAccepted()
        {
            var facts = new MeasurementTraceFact[MaximumEntries];
            for (var i = 0; i < facts.Length; i++) facts[i] = Fact(i);
            var trace = Trace(facts, EmptyAdjustments(), null, null);
            Equal(MaximumEntries, trace.InputFacts.Count, "MeasurementTrace must accept exactly 10,000 facts.");
            Equal("FACT-00000", trace.InputFacts[0].Name, "Fact ordering changed at exact boundary.");
            Equal("FACT-09999", trace.InputFacts[MaximumEntries - 1].Name, "Fact ordering changed at exact boundary tail.");
        }

        private static void ExactAdjustmentBoundaryRemainsAccepted()
        {
            var adjustments = new MeasurementTraceAdjustment[MaximumEntries];
            for (var i = 0; i < adjustments.Length; i++) adjustments[i] = Adjustment(i);
            var trace = Trace(EmptyFacts(), adjustments, null, null);
            Equal(MaximumEntries, trace.Adjustments.Count, "MeasurementTrace must accept exactly 10,000 adjustments.");
        }

        private static void ExactMessageBoundariesRemainAccepted()
        {
            var warnings = new string[MaximumEntries];
            var assumptions = new string[MaximumEntries];
            for (var i = 0; i < MaximumEntries; i++)
            {
                warnings[i] = "Warning " + i.ToString("D5", CultureInfo.InvariantCulture);
                assumptions[i] = "Assumption " + i.ToString("D5", CultureInfo.InvariantCulture);
            }

            var trace = Trace(EmptyFacts(), EmptyAdjustments(), warnings, assumptions);
            Equal(MaximumEntries, trace.Warnings.Count, "MeasurementTrace must accept exactly 10,000 warnings.");
            Equal(MaximumEntries, trace.Assumptions.Count, "MeasurementTrace must accept exactly 10,000 assumptions.");
        }

        private static void CanonicalOrderingAndValidationRemainStable()
        {
            var trace = Trace(
                new[] { Fact(2), Fact(0), Fact(1) },
                new[] { Adjustment(2), Adjustment(0), Adjustment(1) },
                new[] { "Warning B", "Warning A" },
                new[] { "Assumption B", "Assumption A" });

            Equal("FACT-00000", trace.InputFacts[0].Name, "Fact canonical ordering changed.");
            Equal("SRC-00000", trace.Adjustments[0].SourceIdentity, "Adjustment canonical ordering changed.");
            Equal("Warning A", trace.Warnings[0], "Warning canonical ordering changed.");
            Equal("Assumption A", trace.Assumptions[0], "Assumption canonical ordering changed.");

            Capture<ArgumentException>(() => Trace(new[] { Fact(7), Fact(7) }, EmptyAdjustments(), null, null));
            Capture<ArgumentException>(() => Trace(EmptyFacts(), new[] { Adjustment(7), Adjustment(7) }, null, null));
            Capture<ArgumentException>(() => Trace(EmptyFacts(), EmptyAdjustments(), new[] { "Same", "Same" }, null));
            Capture<ArgumentException>(() => Trace(new MeasurementTraceFact[] { null! }, EmptyAdjustments(), null, null));
        }

        private static MeasurementTrace Trace(
            IEnumerable<MeasurementTraceFact> facts,
            IEnumerable<MeasurementTraceAdjustment> adjustments,
            IEnumerable<string>? warnings,
            IEnumerable<string>? assumptions)
        {
            return new MeasurementTrace(
                "SEM-TRACE",
                "SRC-TRACE",
                "QTY-TRACE",
                facts,
                0d,
                adjustments,
                0d,
                "m",
                "manual",
                warnings,
                assumptions);
        }

        private static MeasurementTraceFact Fact(int index)
        {
            var suffix = index.ToString("D5", CultureInfo.InvariantCulture);
            return new MeasurementTraceFact("FACT-" + suffix, index, "m", "SRC-" + suffix);
        }

        private static MeasurementTraceAdjustment Adjustment(int index)
        {
            var suffix = index.ToString("D5", CultureInfo.InvariantCulture);
            return new MeasurementTraceAdjustment(
                MeasurementTraceAdjustmentKind.Addition,
                0d,
                "m",
                "Adjustment " + suffix,
                "SRC-" + suffix);
        }

        private static string Message(int index)
        {
            return "Message " + index.ToString("D5", CultureInfo.InvariantCulture);
        }

        private static IEnumerable<MeasurementTraceFact> EmptyFacts() => Array.Empty<MeasurementTraceFact>();
        private static IEnumerable<MeasurementTraceAdjustment> EmptyAdjustments() => Array.Empty<MeasurementTraceAdjustment>();

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

        private sealed class CountedNeverEnumerated<T> : IReadOnlyCollection<T>
        {
            internal CountedNeverEnumerated(int count) { Count = count; }
            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }
            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericCountedFacts : ICollection, IEnumerable<MeasurementTraceFact>
        {
            internal NonGenericCountedFacts(int count) { Count = count; }
            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot { get; } = new object();
            internal int GetEnumeratorCalls { get; private set; }
            public void CopyTo(Array array, int index) => throw new InvalidOperationException("Must not copy oversized source.");
            public IEnumerator<MeasurementTraceFact> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingSequence<T> : IEnumerable<T>
        {
            private readonly int _count;
            private readonly Func<int, T> _factory;

            internal StreamingSequence(int count, Func<int, T> factory)
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
