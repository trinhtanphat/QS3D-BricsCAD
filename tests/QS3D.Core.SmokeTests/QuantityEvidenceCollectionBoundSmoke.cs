using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityEvidenceCollectionBoundSmoke
    {
        private const int MaximumItems = 10000;

        internal static void Run()
        {
            CountedOperandOversizeFailsBeforeEnumeration();
            ConflictingOperandCountsFailBeforeEnumeration();
            StreamingOperandOversizeStopsAtFirstDisallowedItem();
            CountedContributionOversizeFailsBeforeEnumeration();
            StreamingAdjustmentOversizeStopsAtFirstDisallowedItem();
            NullItemsRemainFailClosed();
            ExactBoundariesPreserveDeterministicEvidence();
        }

        private static void CountedOperandOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<QuantityEvidenceOperand>(MaximumItems + 1);
            var error = Capture<InvalidOperationException>(() => Contribution("COUNTED", source));
            Equal(0, source.GetEnumeratorCalls, "Known oversized operands must fail before enumeration.");
            Contains("at most 10000", error.Message, "Known operand overflow must report the bound.");
        }

        private static void ConflictingOperandCountsFailBeforeEnumeration()
        {
            var source = new ConflictingCountNeverEnumerated<QuantityEvidenceOperand>(1, 2, 1);
            var error = Capture<InvalidOperationException>(() => Contribution("CONFLICT", source));
            Equal(1, source.GenericCountReads, "Generic Count must be read exactly once.");
            Equal(1, source.ReadOnlyCountReads, "Read-only Count must be read exactly once.");
            Equal(1, source.NonGenericCountReads, "Non-generic Count must be read exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Conflicting Count contracts must fail before enumeration.");
            Contains("conflicting known counts", error.Message, "Conflicting Count failure must be explicit.");
        }

        private static void StreamingOperandOversizeStopsAtFirstDisallowedItem()
        {
            var source = new StreamingSequence<QuantityEvidenceOperand>(
                MaximumItems + 2,
                index => Operand(index));
            var error = Capture<InvalidOperationException>(() => Contribution("STREAM", source));
            Equal(MaximumItems + 1, source.YieldedCount, "Streaming operands must stop immediately at item 10,001.");
            Contains("at most 10000", error.Message, "Streaming operand overflow must report the bound.");
        }

        private static void CountedContributionOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<QuantityContribution>(MaximumItems + 1);
            var error = Capture<InvalidOperationException>(() => Explanation(source, Array.Empty<QuantityAdjustment>()));
            Equal(0, source.GetEnumeratorCalls, "Known oversized contributions must fail before enumeration.");
            Contains("at most 10000", error.Message, "Known contribution overflow must report the bound.");
        }

        private static void StreamingAdjustmentOversizeStopsAtFirstDisallowedItem()
        {
            var adjustment = Adjustment();
            var source = new StreamingSequence<QuantityAdjustment>(MaximumItems + 2, _ => adjustment);
            var error = Capture<InvalidOperationException>(() => Explanation(Array.Empty<QuantityContribution>(), source));
            Equal(MaximumItems + 1, source.YieldedCount, "Streaming adjustments must stop immediately at item 10,001.");
            Contains("at most 10000", error.Message, "Streaming adjustment overflow must report the bound.");
        }

        private static void NullItemsRemainFailClosed()
        {
            var operands = new QuantityEvidenceOperand[] { Operand(0), null! };
            Capture<ArgumentException>(() => Contribution("NULL-OPERAND", operands));

            var contributions = new QuantityContribution[] { Contribution("GOOD", Array.Empty<QuantityEvidenceOperand>()), null! };
            Capture<ArgumentException>(() => Explanation(contributions, Array.Empty<QuantityAdjustment>()));

            var adjustments = new QuantityAdjustment[] { Adjustment(), null! };
            Capture<ArgumentException>(() => Explanation(Array.Empty<QuantityContribution>(), adjustments));
        }

        private static void ExactBoundariesPreserveDeterministicEvidence()
        {
            var operands = new QuantityEvidenceOperand[MaximumItems];
            for (var i = 0; i < operands.Length; i++) operands[i] = Operand(MaximumItems - 1 - i);

            var contribution = Contribution("BOUNDARY", operands);
            Equal(MaximumItems, contribution.Operands.Count, "Exactly 10,000 operands must remain supported.");
            Equal("OP-00000", contribution.Operands[0].Key, "Operand deterministic ordering changed at the lower boundary.");
            Equal("OP-09999", contribution.Operands[MaximumItems - 1].Key, "Operand deterministic ordering changed at the upper boundary.");

            var contributions = new QuantityContribution[MaximumItems];
            var adjustments = new QuantityAdjustment[MaximumItems];
            var adjustment = Adjustment();
            for (var i = 0; i < MaximumItems; i++)
            {
                contributions[i] = contribution;
                adjustments[i] = adjustment;
            }

            var first = Explanation(contributions, adjustments);
            var second = Explanation(contributions, adjustments);
            Equal(MaximumItems, first.Contributions.Count, "Exactly 10,000 contributions must remain supported.");
            Equal(MaximumItems, first.Adjustments.Count, "Exactly 10,000 adjustments must remain supported.");
            Equal(first.EvidenceId, second.EvidenceId, "Bounded snapshotting changed deterministic evidence identity.");
            Equal(0m, first.GrossValue, "Boundary evidence gross arithmetic changed.");
            Equal(0m, first.NetValue, "Boundary evidence net arithmetic changed.");
        }

        private static QuantityEvidenceOperand Operand(int index)
        {
            return new QuantityEvidenceOperand("OP-" + index.ToString("D5", CultureInfo.InvariantCulture), index, "m");
        }

        private static QuantityContribution Contribution(string key, IEnumerable<QuantityEvidenceOperand> operands)
        {
            return QuantityContribution.Create(
                key,
                "Contribution " + key,
                QuantityEvidenceOperation.Add,
                "gross",
                0m,
                QuantityEvidenceSelector.ForEntity("ENTITY-1"),
                operands);
        }

        private static QuantityAdjustment Adjustment()
        {
            return QuantityAdjustment.Create(
                "ADJ",
                "RULE",
                "No-op control",
                QuantityEvidenceOperation.Deduct,
                "SOURCE-1",
                "TARGET-1",
                0m,
                QuantityEvidenceSelector.ForIntersection("SOURCE-1", "TARGET-1", "I-1"));
        }

        private static QuantityExplanation Explanation(
            IEnumerable<QuantityContribution> contributions,
            IEnumerable<QuantityAdjustment> adjustments)
        {
            return QuantityExplanation.Create(
                "SUBJECT-1",
                "Beam",
                "Volume",
                "m3",
                0m,
                0m,
                contributions,
                adjustments);
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new Exception(message + " Actual='" + actual + "'.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private sealed class CountedNeverEnumerated<T> : ICollection<T>
        {
            private readonly int _count;
            internal CountedNeverEnumerated(int count) { _count = count; }
            internal int GetEnumeratorCalls { get; private set; }
            public int Count => _count;
            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() { GetEnumeratorCalls++; throw new Exception("Enumeration must not start."); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class ConflictingCountNeverEnumerated<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            internal ConflictingCountNeverEnumerated(int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }
            internal int GenericCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }
            int ICollection<T>.Count { get { GenericCountReads++; return _genericCount; } }
            int IReadOnlyCollection<T>.Count { get { ReadOnlyCountReads++; return _readOnlyCount; } }
            int ICollection.Count { get { NonGenericCountReads++; return _nonGenericCount; } }
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            public IEnumerator<T> GetEnumerator() { GetEnumeratorCalls++; throw new Exception("Enumeration must not start."); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => false;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class StreamingSequence<T> : IEnumerable<T>
        {
            private readonly int _count;
            private readonly Func<int, T> _factory;
            internal StreamingSequence(int count, Func<int, T> factory) { _count = count; _factory = factory; }
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
}
