using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class DuplicateDetectionSmoke
    {
        public static void Run()
        {
            ExactGeometryIsSingleDeterministicPair();
            NearGeometryRequiresToleranceAndClassificationMatch();
            SemanticIdentityUsesSourceProvenance();
            ExactAndSemanticEvidenceCanCoexist();
            DifferentElementsAreRejected();
            InputOrderingDoesNotChangePairKey();
            DuplicateElementIdentityFailsClosed();
            InvalidToleranceFailsClosed();
            KnownCountTraversalMismatchFailsClosed();
            StreamingElementBoundFailsClosed();
            ResultBoundFailsClosed();
        }

        private static void ExactGeometryIsSingleDeterministicPair()
        {
            var service = new DuplicateDetectionService();
            var result = service.Detect(new[]
            {
                Element("B", "Structure", "Column", Box(0d, 0d, 0d, 1d, 1d, 3d)),
                Element("A", "Structure", "Column", Box(0d, 0d, 0d, 1d, 1d, 3d))
            });

            if (result.Pairs.Count != 1)
                throw new Exception("Exact duplicate geometry must produce exactly one pair, not mirrored results.");
            var pair = result.Pairs[0];
            if (!pair.IsExactGeometry || pair.IsNearGeometry || pair.IsSemanticIdentity)
                throw new Exception("Exact duplicate geometry must be classified as ExactGeometry only without provenance evidence.");
            Equal("A|B", pair.PairKey, "Exact duplicate pair key must be deterministic.");
            if (result.Summary.PairCount != 1 || result.Summary.ExactGeometryCount != 1 || result.Summary.NearGeometryCount != 0)
                throw new Exception("Exact duplicate summary counts are incorrect.");
        }

        private static void NearGeometryRequiresToleranceAndClassificationMatch()
        {
            var service = new DuplicateDetectionService();
            var options = new DuplicateDetectionOptions { CoordinateToleranceM = 0.002d };
            var near = service.Detect(new[]
            {
                Element("A", "Structure", "Wall", Box(0d, 0d, 0d, 5d, 0.2d, 3d)),
                Element("B", "Structure", "Wall", Box(0.001d, 0d, 0d, 5.001d, 0.2d, 3d))
            }, options);

            if (near.Pairs.Count != 1 || !near.Pairs[0].IsNearGeometry || near.Pairs[0].IsExactGeometry)
                throw new Exception("Same-classification geometry within tolerance must be reported as a near duplicate.");

            var outsideTolerance = service.Detect(new[]
            {
                Element("A", "Structure", "Wall", Box(0d, 0d, 0d, 5d, 0.2d, 3d)),
                Element("B", "Structure", "Wall", Box(0.003d, 0d, 0d, 5.003d, 0.2d, 3d))
            }, options);
            if (outsideTolerance.Pairs.Count != 0)
                throw new Exception("Geometry outside the configured near-duplicate tolerance must be rejected.");

            var differentCategory = service.Detect(new[]
            {
                Element("A", "Structure", "Wall", Box(0d, 0d, 0d, 5d, 0.2d, 3d)),
                Element("B", "Structure", "Beam", Box(0.001d, 0d, 0d, 5.001d, 0.2d, 3d))
            }, options);
            if (differentCategory.Pairs.Count != 0)
                throw new Exception("Near geometry across logical categories must not be treated as duplicate by default.");
        }

        private static void SemanticIdentityUsesSourceProvenance()
        {
            var service = new DuplicateDetectionService();
            var result = service.Detect(new[]
            {
                new DuplicateCandidate(Element("A", "Architecture", "Door", Box(0d, 0d, 0d, 1d, 0.2d, 2d)), " SRC-DOOR-42 "),
                new DuplicateCandidate(Element("B", "Architecture", "Door", Box(10d, 0d, 0d, 11d, 0.2d, 2d)), "src-door-42")
            });

            if (result.Pairs.Count != 1 || !result.Pairs[0].IsSemanticIdentity || result.Pairs[0].IsExactGeometry || result.Pairs[0].IsNearGeometry)
                throw new Exception("Shared normalized source identity must produce a semantic duplicate even when geometry differs.");
            if (result.Summary.SemanticIdentityCount != 1)
                throw new Exception("Semantic duplicate summary count is incorrect.");
        }

        private static void ExactAndSemanticEvidenceCanCoexist()
        {
            var service = new DuplicateDetectionService();
            var box = Box(1d, 2d, 3d, 4d, 5d, 6d);
            var result = service.Detect(new[]
            {
                new DuplicateCandidate(Element("A", "Structure", "Column", box), "SOURCE-1"),
                new DuplicateCandidate(Element("B", "Structure", "Column", box), "SOURCE-1")
            });

            if (result.Pairs.Count != 1 || !result.Pairs[0].IsExactGeometry || !result.Pairs[0].IsSemanticIdentity)
                throw new Exception("A duplicate pair must retain both exact-geometry and semantic evidence when both are true.");
            if (result.Summary.ExactGeometryCount != 1 || result.Summary.SemanticIdentityCount != 1)
                throw new Exception("Combined duplicate evidence must be reflected in summary counts.");
        }

        private static void DifferentElementsAreRejected()
        {
            var service = new DuplicateDetectionService();
            var result = service.Detect(new[]
            {
                Element("A", "Structure", "Foundation", Box(0d, 0d, 0d, 2d, 2d, 0.5d)),
                Element("B", "Structure", "Foundation", Box(20d, 0d, 0d, 22d, 2d, 0.5d))
            });
            if (result.Pairs.Any())
                throw new Exception("Distinct geometry without shared provenance must not be reported as duplicate.");
        }

        private static void InputOrderingDoesNotChangePairKey()
        {
            var service = new DuplicateDetectionService();
            var box = Box(0d, 0d, 0d, 1d, 1d, 1d);
            var first = service.Detect(new[]
            {
                Element("Z-2", "Structure", "Column", box),
                Element("A-1", "Structure", "Column", box)
            });
            var second = service.Detect(new[]
            {
                Element("A-1", "Structure", "Column", box),
                Element("Z-2", "Structure", "Column", box)
            });

            Equal(first.Pairs[0].PairKey, second.Pairs[0].PairKey, "Duplicate pair identity must not depend on enumeration order.");
            Equal("A-1|Z-2", first.Pairs[0].PairKey, "Duplicate pair key canonical ordering is incorrect.");
        }

        private static void DuplicateElementIdentityFailsClosed()
        {
            var service = new DuplicateDetectionService();
            Expect<ArgumentException>(() => service.Detect(new[]
            {
                Element("E-1", "Structure", "Column", Box(0d, 0d, 0d, 1d, 1d, 1d)),
                Element("e-1", "Structure", "Column", Box(0d, 0d, 0d, 1d, 1d, 1d))
            }), "Case-variant duplicate element identities must fail closed before pair evaluation.");
        }

        private static void InvalidToleranceFailsClosed()
        {
            var service = new DuplicateDetectionService();
            var input = new[] { Element("A", "Structure", "Column", Box(0d, 0d, 0d, 1d, 1d, 1d)) };
            Expect<ArgumentOutOfRangeException>(() => service.Detect(input, new DuplicateDetectionOptions { CoordinateToleranceM = -0.001d }), "Negative tolerance must fail closed.");
            Expect<ArgumentOutOfRangeException>(() => service.Detect(input, new DuplicateDetectionOptions { CoordinateToleranceM = double.NaN }), "NaN tolerance must fail closed.");
            Expect<ArgumentOutOfRangeException>(() => service.Detect(input, new DuplicateDetectionOptions { CoordinateToleranceM = double.PositiveInfinity }), "Infinite tolerance must fail closed.");
        }

        private static void KnownCountTraversalMismatchFailsClosed()
        {
            var service = new DuplicateDetectionService();
            var only = new DuplicateCandidate(Element("A", "Structure", "Column", Box(0d, 0d, 0d, 1d, 1d, 1d)));
            Expect<InvalidOperationException>(() => service.Detect(new MisreportedCandidateCollection(2, only)), "Known Count must agree with observed traversal cardinality.");
        }

        private static void StreamingElementBoundFailsClosed()
        {
            var service = new DuplicateDetectionService();
            Expect<InvalidOperationException>(() => service.Detect(StreamCandidates(501, false)), "Streaming input above the 500-element bound must fail rather than truncate.");
        }

        private static void ResultBoundFailsClosed()
        {
            var service = new DuplicateDetectionService();
            Expect<InvalidOperationException>(() => service.Detect(StreamCandidates(142, true)), "More than 10,000 duplicate pairs must fail rather than truncate.");
        }

        private static IEnumerable<DuplicateCandidate> StreamCandidates(int count, bool identicalGeometry)
        {
            for (var index = 0; index < count; index++)
            {
                var x = identicalGeometry ? 0d : index * 10d;
                yield return new DuplicateCandidate(Element("E-" + index, "Structure", "Column", Box(x, 0d, 0d, x + 1d, 1d, 1d)));
            }
        }

        private sealed class MisreportedCandidateCollection : ICollection<DuplicateCandidate>
        {
            private readonly DuplicateCandidate _candidate;

            public MisreportedCandidateCollection(int count, DuplicateCandidate candidate)
            {
                Count = count;
                _candidate = candidate;
            }

            public int Count { get; }
            public bool IsReadOnly => true;
            public IEnumerator<DuplicateCandidate> GetEnumerator()
            {
                yield return _candidate;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(DuplicateCandidate item) => ReferenceEquals(item, _candidate);
            public void CopyTo(DuplicateCandidate[] array, int arrayIndex) => array[arrayIndex] = _candidate;
            public void Add(DuplicateCandidate item) => throw new NotSupportedException();
            public bool Remove(DuplicateCandidate item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
        }

        private static CoordinationElement Element(string id, string discipline, string category, AxisAlignedBox bounds)
        {
            return new CoordinationElement(id, discipline, category, "Default", "Model", bounds);
        }

        private static AxisAlignedBox Box(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            return new AxisAlignedBox(minX, minY, minZ, maxX, maxY, maxZ);
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected '" + expected + "', actual '" + actual + "'.");
        }

        private static void Expect<TException>(Action action, string message) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new Exception(message);
        }
    }
}
