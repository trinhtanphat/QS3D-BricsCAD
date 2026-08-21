using System;
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
                new DuplicateCandidate(Element("A", "Architecture", "Door", Box(0d, 0d, 0d, 1d, 0.2d, 2d)), "SRC-DOOR-42"),
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
    }
}
