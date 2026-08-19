using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Interoperability;

namespace QS3D.Core.SmokeTests
{
    internal static class InteroperabilityElementRecordNestedCollectionBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OversizedKnownPropertyCountFailsBeforeEnumeration();
            ChangingKnownProvenanceCountFailsClosed();
            StreamingProvenanceOverflowFailsClosed();
            StableBoundedInputsPreserveCanonicalBehavior();
        }

        private static void OversizedKnownPropertyCountFailsBeforeEnumeration()
        {
            var properties = new OversizedKnownCollection<InteroperabilityPropertyFact>(
                InteroperabilityElementRecord.MaxNestedItems + 1);

            var error = Capture<InvalidOperationException>(() => Create(properties: properties));
            Require(error.Message.IndexOf("properties", StringComparison.Ordinal) >= 0,
                "Oversized interoperability properties did not identify the bounded collection.");
            Require(error.Message.IndexOf("10000", StringComparison.Ordinal) >= 0,
                "Oversized interoperability properties did not report the nested collection bound.");
            Require(!properties.EnumeratorAccessed,
                "Oversized known interoperability properties were enumerated before Count rejection.");
        }

        private static void ChangingKnownProvenanceCountFailsClosed()
        {
            var tokens = new ChangingCountCollection<string>(
                new[] { "source-a" },
                initialCount: 1,
                changedCount: 2);

            var error = Capture<InvalidOperationException>(() => Create(provenanceTokens: tokens));
            Require(error.Message.IndexOf("Count", StringComparison.Ordinal) >= 0,
                "Changing interoperability provenance Count did not report the Count contract.");
            Require(tokens.CountReadCount >= 2,
                "Interoperability provenance Count was not re-read after bounded snapshot materialization.");
            Require(tokens.EnumerationCount == 1,
                "Interoperability provenance snapshot did not enumerate exactly the originally supplied item sequence.");
        }

        private static void StreamingProvenanceOverflowFailsClosed()
        {
            var tokens = new StreamingTokens(InteroperabilityElementRecord.MaxNestedItems + 1);
            var error = Capture<InvalidOperationException>(() => Create(provenanceTokens: tokens));
            Require(error.Message.IndexOf("10000", StringComparison.Ordinal) >= 0,
                "Streaming interoperability provenance overflow did not report the nested collection bound.");
            Require(tokens.YieldCount == InteroperabilityElementRecord.MaxNestedItems + 1,
                "Streaming interoperability provenance did not fail exactly at the first item beyond the bound.");
        }

        private static void StableBoundedInputsPreserveCanonicalBehavior()
        {
            var arrayRecord = Create(provenanceTokens: new[] { "beta", "alpha", "beta" });
            var listRecord = Create(provenanceTokens: new List<string> { "beta", "alpha", "beta" });

            Require(arrayRecord.ProvenanceTokens.SequenceEqual(listRecord.ProvenanceTokens, StringComparer.Ordinal),
                "Bounded interoperability provenance collection type changed canonical output.");
            Require(arrayRecord.ProvenanceTokens.SequenceEqual(new[] { "alpha", "beta" }, StringComparer.Ordinal),
                "Bounded interoperability provenance tokens no longer preserve sorted distinct canonicalization.");
        }

        private static InteroperabilityElementRecord Create(
            IEnumerable<InteroperabilityPropertyFact>? properties = null,
            IEnumerable<string>? provenanceTokens = null)
        {
            var provenance = new InteroperabilitySourceProvenance(
                InteroperabilitySourceSystem.NeutralSnapshot,
                InteroperabilityTransport.NeutralSnapshot,
                "doc-1",
                null,
                "schema-1",
                "batch-1");
            var identity = InteroperabilityElementIdentity.ForExternalAuthoring(provenance, "element-1");
            return new InteroperabilityElementRecord(
                identity,
                properties,
                classifications: null,
                quantities: null,
                provenanceTokens: provenanceTokens,
                diagnostics: null);
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class OversizedKnownCollection<T> : IReadOnlyCollection<T>
        {
            private readonly int _count;

            internal OversizedKnownCollection(int count)
            {
                _count = count;
            }

            public bool EnumeratorAccessed { get; private set; }
            public int Count => _count;

            public IEnumerator<T> GetEnumerator()
            {
                EnumeratorAccessed = true;
                throw new InvalidOperationException("Oversized known collection must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ChangingCountCollection<T> : IReadOnlyCollection<T>
        {
            private readonly IReadOnlyList<T> _items;
            private readonly int _initialCount;
            private readonly int _changedCount;

            internal ChangingCountCollection(IReadOnlyList<T> items, int initialCount, int changedCount)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _initialCount = initialCount;
                _changedCount = changedCount;
            }

            public int CountReadCount { get; private set; }
            public int EnumerationCount { get; private set; }

            public int Count
            {
                get
                {
                    CountReadCount++;
                    return CountReadCount == 1 ? _initialCount : _changedCount;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var item in _items)
                {
                    EnumerationCount++;
                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingTokens : IEnumerable<string>
        {
            private readonly int _count;

            internal StreamingTokens(int count)
            {
                _count = count;
            }

            public int YieldCount { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                for (var index = 0; index < _count; index++)
                {
                    YieldCount++;
                    yield return "token";
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
