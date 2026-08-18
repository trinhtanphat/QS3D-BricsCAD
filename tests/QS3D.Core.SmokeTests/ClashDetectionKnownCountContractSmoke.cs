using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class ClashDetectionKnownCountContractSmoke
    {
        private const int MaximumElements = 500;

        public static void Run()
        {
            ConflictingKnownCountsFailBeforeEnumeration();
            NonGenericCountConflictFailsBeforeEnumeration();
            NegativeKnownCountFailsBeforeEnumeration();
            CapacityViolationPrecedesCountConflict();
            ExactBoundRemainsAccepted();
            ConsistentKnownCountsPreserveDeterministicClassification();
            UnderEnumeratingKnownCountFailsAfterTraversal();
            OverEnumeratingKnownCountFailsAfterTraversal();
            DishonestKnownCountStillStopsAtStreamingBoundary();
            LostOrthogonalGapFailsClosed();
            OneAxisClearanceBoundaryRemainsAccepted();
            OrdinaryMultiAxisDistanceRemainsAccepted();
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new MultiCountCollection(
                new[] { Element("A", "Architecture") },
                genericCount: 1,
                readOnlyCount: 2,
                nonGenericCount: 1,
                throwOnEnumeration: true);

            ExpectInvalidOperation(
                () => new ClashDetectionService().Detect(source),
                "conflicting known element counts",
                "Clash detection must reject conflicting generic/read-only counts before enumeration.");
            if (source.EnumerationRequested)
                throw new Exception("Conflicting known clash counts must fail before caller enumeration.");
        }

        private static void NonGenericCountConflictFailsBeforeEnumeration()
        {
            var source = new MultiCountCollection(
                new[] { Element("A", "Architecture") },
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 2,
                throwOnEnumeration: true);

            ExpectInvalidOperation(
                () => new ClashDetectionService().Detect(source),
                "conflicting known element counts",
                "Clash detection must include non-generic ICollection count evidence.");
            if (source.EnumerationRequested)
                throw new Exception("Non-generic clash count conflict must fail before caller enumeration.");
        }

        private static void NegativeKnownCountFailsBeforeEnumeration()
        {
            var source = new MultiCountCollection(
                new[] { Element("A", "Architecture") },
                genericCount: -1,
                readOnlyCount: -1,
                nonGenericCount: -1,
                throwOnEnumeration: true);

            ExpectInvalidOperation(
                () => new ClashDetectionService().Detect(source),
                "invalid negative element count",
                "Negative known clash count must fail closed before enumeration.");
            if (source.EnumerationRequested)
                throw new Exception("Negative known clash count must fail before caller enumeration.");
        }

        private static void CapacityViolationPrecedesCountConflict()
        {
            var source = new MultiCountCollection(
                new[] { Element("A", "Architecture") },
                genericCount: 1,
                readOnlyCount: MaximumElements + 1,
                nonGenericCount: 2,
                throwOnEnumeration: true);

            ExpectInvalidOperation(
                () => new ClashDetectionService().Detect(source),
                "at most 500 elements",
                "Known clash capacity violation must retain precedence over count conflicts.");
            if (source.EnumerationRequested)
                throw new Exception("Known clash capacity violation must fail before caller enumeration.");
        }

        private static void ExactBoundRemainsAccepted()
        {
            var items = new CoordinationElement[MaximumElements];
            for (var index = 0; index < items.Length; index++)
                items[index] = Element("BOUND-" + index, "Architecture");

            var results = new ClashDetectionService().Detect(items);
            if (results.Count != 0)
                throw new Exception("Exactly 500 same-discipline elements must remain accepted without producing cross-discipline clashes.");
        }

        private static void ConsistentKnownCountsPreserveDeterministicClassification()
        {
            var source = new MultiCountCollection(
                new[]
                {
                    Element("B", "Structure"),
                    Element("A", "Architecture")
                },
                genericCount: 2,
                readOnlyCount: 2,
                nonGenericCount: 2,
                throwOnEnumeration: false);
            var service = new ClashDetectionService();

            var first = service.Detect(source);
            var second = service.Detect(source);

            AssertSingleHardClash(first, "A", "B");
            AssertSingleHardClash(second, "A", "B");
            if (source.EnumerationRequestCount != 2)
                throw new Exception("Consistent known-count clash input must enumerate exactly once per Detect call.");
        }

        private static void UnderEnumeratingKnownCountFailsAfterTraversal()
        {
            var source = new MultiCountCollection(
                new[] { Element("A", "Architecture") },
                genericCount: 2,
                readOnlyCount: 2,
                nonGenericCount: 2,
                throwOnEnumeration: false);

            ExpectInvalidOperation(
                () => new ClashDetectionService().Detect(source),
                "did not match its known element count",
                "Clash detection must fail closed when traversal yields fewer elements than the validated known Count.");
            if (!source.EnumerationRequested || source.EnumerationRequestCount != 1)
                throw new Exception("Under-enumeration mismatch must be detected after exactly one traversal.");
        }

        private static void OverEnumeratingKnownCountFailsAfterTraversal()
        {
            var source = new MultiCountCollection(
                new[]
                {
                    Element("A", "Architecture"),
                    Element("B", "Structure")
                },
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 1,
                throwOnEnumeration: false);

            ExpectInvalidOperation(
                () => new ClashDetectionService().Detect(source),
                "did not match its known element count",
                "Clash detection must fail closed when traversal yields more in-bound elements than the validated known Count.");
            if (!source.EnumerationRequested || source.EnumerationRequestCount != 1)
                throw new Exception("Over-enumeration mismatch must be detected after exactly one traversal.");
        }

        private static void DishonestKnownCountStillStopsAtStreamingBoundary()
        {
            var source = new DishonestReadOnlyCollection(MaximumElements + 1, reportedCount: 1);
            ExpectInvalidOperation(
                () => new ClashDetectionService().Detect(source),
                "at most 500 elements",
                "A dishonest known Count must not bypass the streamed clash-input boundary.");
            if (source.MoveNextCalls != MaximumElements + 1)
                throw new Exception("Clash detection must stop immediately after observing element 501 and never request element 502.");
        }

        private static void LostOrthogonalGapFailsClosed()
        {
            var elements = new[]
            {
                Element(
                    "A",
                    "Architecture",
                    new AxisAlignedBox(-1d, -1d, -1d, 0d, 0d, 0d)),
                Element(
                    "B",
                    "Structure",
                    new AxisAlignedBox(1d, 1e-200d, -1d, 2d, 1d, 0d))
            };

            ExpectInvalidOperation(
                () => new ClashDetectionService().Detect(elements, clearanceM: 1d),
                "lost a non-zero orthogonal gap",
                "Clash distance must fail closed when a positive orthogonal gap disappears from the representable norm.");
        }

        private static void OneAxisClearanceBoundaryRemainsAccepted()
        {
            var elements = new[]
            {
                Element(
                    "A",
                    "Architecture",
                    new AxisAlignedBox(-1d, -1d, -1d, 0d, 0d, 0d)),
                Element(
                    "B",
                    "Structure",
                    new AxisAlignedBox(1d, -1d, -1d, 2d, 0d, 0d))
            };

            var results = new ClashDetectionService().Detect(elements, clearanceM: 1d);
            AssertSingleClearance(results, "A", "B", 1d);
        }

        private static void OrdinaryMultiAxisDistanceRemainsAccepted()
        {
            var elements = new[]
            {
                Element(
                    "A",
                    "Architecture",
                    new AxisAlignedBox(-1d, -1d, -1d, 0d, 0d, 0d)),
                Element(
                    "B",
                    "Structure",
                    new AxisAlignedBox(3d, 4d, -1d, 4d, 5d, 0d))
            };

            var results = new ClashDetectionService().Detect(elements, clearanceM: 5d);
            AssertSingleClearance(results, "A", "B", 5d);
        }

        private static CoordinationElement Element(string id, string discipline)
        {
            return Element(
                id,
                discipline,
                new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d));
        }

        private static CoordinationElement Element(string id, string discipline, AxisAlignedBox bounds)
        {
            return new CoordinationElement(
                id,
                discipline,
                "Generic",
                "System",
                "Region",
                bounds);
        }

        private static void AssertSingleHardClash(IReadOnlyList<ClashResult> results, string leftId, string rightId)
        {
            if (results.Count != 1 ||
                results[0].Kind != ClashKind.Hard ||
                !string.Equals(results[0].LeftElementId, leftId, StringComparison.Ordinal) ||
                !string.Equals(results[0].RightElementId, rightId, StringComparison.Ordinal))
            {
                throw new Exception("Valid multi-count input must preserve deterministic hard-clash ordering/classification.");
            }
        }

        private static void AssertSingleClearance(
            IReadOnlyList<ClashResult> results,
            string leftId,
            string rightId,
            double expectedSeparation)
        {
            if (results.Count != 1 ||
                results[0].Kind != ClashKind.Clearance ||
                !string.Equals(results[0].LeftElementId, leftId, StringComparison.Ordinal) ||
                !string.Equals(results[0].RightElementId, rightId, StringComparison.Ordinal) ||
                results[0].SeparationM != expectedSeparation)
            {
                throw new Exception("Valid clash distance input must preserve deterministic clearance classification and separation.");
            }
        }

        private static void ExpectInvalidOperation(Action action, string messageFragment, string failureMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(messageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception(failureMessage + " Actual diagnostic: " + ex.Message);
                return;
            }

            throw new Exception(failureMessage);
        }

        private sealed class MultiCountCollection : ICollection<CoordinationElement>, IReadOnlyCollection<CoordinationElement>, ICollection
        {
            private readonly CoordinationElement[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            public MultiCountCollection(
                CoordinationElement[] items,
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

            public bool EnumerationRequested { get; private set; }
            public int EnumerationRequestCount { get; private set; }
            int ICollection<CoordinationElement>.Count => _genericCount;
            int IReadOnlyCollection<CoordinationElement>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<CoordinationElement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<CoordinationElement> GetEnumerator()
            {
                EnumerationRequested = true;
                EnumerationRequestCount++;
                if (_throwOnEnumeration)
                    throw new Exception("Enumerator must not be requested for invalid known-count input.");
                return ((IEnumerable<CoordinationElement>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<CoordinationElement>.Add(CoordinationElement item) => throw new NotSupportedException();
            void ICollection<CoordinationElement>.Clear() => throw new NotSupportedException();
            bool ICollection<CoordinationElement>.Contains(CoordinationElement item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<CoordinationElement>.CopyTo(CoordinationElement[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<CoordinationElement>.Remove(CoordinationElement item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }

        private sealed class DishonestReadOnlyCollection : IReadOnlyCollection<CoordinationElement>
        {
            private readonly int _actualCount;
            private readonly int _reportedCount;

            public DishonestReadOnlyCollection(int actualCount, int reportedCount)
            {
                _actualCount = actualCount;
                _reportedCount = reportedCount;
            }

            public int Count => _reportedCount;
            public int MoveNextCalls { get; private set; }

            public IEnumerator<CoordinationElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<CoordinationElement>
            {
                private readonly DishonestReadOnlyCollection _owner;
                private int _index = -1;

                public Enumerator(DishonestReadOnlyCollection owner)
                {
                    _owner = owner;
                }

                public CoordinationElement Current { get; private set; } = null!;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index >= _owner._actualCount)
                        return false;
                    Current = Element("STREAM-" + _index, "Architecture");
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
