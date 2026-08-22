using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class DuplicateClusterPlanningSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ConnectedPairsBecomeOneStableCluster();
            ExplicitRepresentativeProducesSafeDryRun();
            AmbiguousRepresentativeFailsClosed();
            ConflictingQuantityOwnershipFailsClosed();
            StaleAndProtectedEvidenceFailClosed();
            MissingEvidenceFailsClosed();
        }

        private static void ConnectedPairsBecomeOneStableCluster()
        {
            var detector = new DuplicateDetectionService();
            var options = new DuplicateDetectionOptions { CoordinateToleranceM = 0.001d };
            var forward = detector.Detect(new[]
            {
                Element("A", Box(0d, 0d, 0d, 1d, 1d, 1d)),
                Element("B", Box(0.0009d, 0d, 0d, 1.0009d, 1d, 1d)),
                Element("C", Box(0.0018d, 0d, 0d, 1.0018d, 1d, 1d))
            }, options);
            var reverse = detector.Detect(new[]
            {
                Element("C", Box(0.0018d, 0d, 0d, 1.0018d, 1d, 1d)),
                Element("B", Box(0.0009d, 0d, 0d, 1.0009d, 1d, 1d)),
                Element("A", Box(0d, 0d, 0d, 1d, 1d, 1d))
            }, options);

            if (forward.Pairs.Count != 2)
                throw new Exception("Expected chain fixture to produce exactly A-B and B-C duplicate pairs.");

            var service = new DuplicateClusterService();
            var first = service.Build(forward);
            var second = service.Build(reverse);
            if (first.Count != 1 || second.Count != 1)
                throw new Exception("Connected duplicate pairs must collapse to one cluster.");
            Equal("A,B,C", string.Join(",", first[0].ElementIds), "Cluster member order must be deterministic.");
            Equal(first[0].ClusterId, second[0].ClusterId, "Cluster identity must not depend on input enumeration order.");
            if (first[0].Pairs.Count != 2 || !first[0].HasNearGeometry)
                throw new Exception("Cluster must retain all pair evidence from the connected component.");
        }

        private static void ExplicitRepresentativeProducesSafeDryRun()
        {
            var cluster = ExactCluster("KEEP", "DROP");
            var plan = new DuplicateRemediationPlanner().Plan(
                cluster,
                new[]
                {
                    Evidence("KEEP", "SEM-42", "QTY-42"),
                    Evidence("DROP", "sem-42", "qty-42")
                },
                "KEEP");

            if (!plan.CanApply || !plan.IsDryRun)
                throw new Exception("Consistent ownership plus explicit representative must produce an applicable dry-run preview.");
            Equal("KEEP", plan.RepresentativeElementId, "Explicit representative was not preserved.");
            Equal("DROP", string.Join(",", plan.RemovableElementIds), "Dry-run removable set is incorrect.");
            Equal(cluster.ClusterId, plan.ClusterId, "Remediation plan lost cluster identity.");
        }

        private static void AmbiguousRepresentativeFailsClosed()
        {
            var cluster = ExactCluster("A", "B");
            var plan = new DuplicateRemediationPlanner().Plan(
                cluster,
                new[]
                {
                    Evidence("A", "SEM-1", "QTY-1"),
                    Evidence("B", "SEM-1", "QTY-1")
                });

            Blocked(plan, DuplicateRemediationBlockedReason.AmbiguousRepresentative);
        }

        private static void ConflictingQuantityOwnershipFailsClosed()
        {
            var cluster = ExactCluster("A", "B");
            var plan = new DuplicateRemediationPlanner().Plan(
                cluster,
                new[]
                {
                    Evidence("A", "SEM-1", "QTY-A"),
                    Evidence("B", "SEM-1", "QTY-B")
                },
                "A");

            Blocked(plan, DuplicateRemediationBlockedReason.ConflictingQuantityOwnership);
        }

        private static void StaleAndProtectedEvidenceFailClosed()
        {
            var cluster = ExactCluster("A", "B");
            var stale = new DuplicateRemediationPlanner().Plan(
                cluster,
                new[]
                {
                    Evidence("A", "SEM-1", "QTY-1", isStale: true),
                    Evidence("B", "SEM-1", "QTY-1")
                },
                "A");
            Blocked(stale, DuplicateRemediationBlockedReason.StaleEvidence);

            var protectedRemoval = new DuplicateRemediationPlanner().Plan(
                cluster,
                new[]
                {
                    Evidence("A", "SEM-1", "QTY-1"),
                    Evidence("B", "SEM-1", "QTY-1", isProtectedFromRemoval: true)
                },
                "A");
            Blocked(protectedRemoval, DuplicateRemediationBlockedReason.ProtectedRemoval);
        }

        private static void MissingEvidenceFailsClosed()
        {
            var cluster = ExactCluster("A", "B");
            var plan = new DuplicateRemediationPlanner().Plan(
                cluster,
                new[] { Evidence("A", "SEM-1", "QTY-1") },
                "A");
            Blocked(plan, DuplicateRemediationBlockedReason.MissingElementEvidence);
        }

        private static DuplicateCluster ExactCluster(string leftId, string rightId)
        {
            var box = Box(0d, 0d, 0d, 1d, 1d, 1d);
            var detected = new DuplicateDetectionService().Detect(new[]
            {
                Element(leftId, box),
                Element(rightId, box)
            });
            var clusters = new DuplicateClusterService().Build(detected);
            if (clusters.Count != 1) throw new Exception("Expected exact duplicate fixture to create one cluster.");
            return clusters[0];
        }

        private static DuplicateRemediationEvidence Evidence(
            string elementId,
            string semanticOwnerId,
            string quantityOwnerId,
            bool isStale = false,
            bool isAmbiguous = false,
            bool isProtectedFromRemoval = false)
        {
            return new DuplicateRemediationEvidence(
                elementId,
                semanticOwnerId,
                quantityOwnerId,
                isStale,
                isAmbiguous,
                isProtectedFromRemoval);
        }

        private static CoordinationElement Element(string id, AxisAlignedBox bounds)
        {
            return new CoordinationElement(id, "Structure", "Column", "Default", "Model", bounds);
        }

        private static AxisAlignedBox Box(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            return new AxisAlignedBox(minX, minY, minZ, maxX, maxY, maxZ);
        }

        private static void Blocked(DuplicateRemediationPlan plan, DuplicateRemediationBlockedReason reason)
        {
            if (plan.CanApply)
                throw new Exception("Expected remediation plan to fail closed for " + reason + ".");
            if (!plan.BlockedReasons.Contains(reason))
                throw new Exception("Expected blocked reason " + reason + ", got: " + string.Join(",", plan.BlockedReasons) + ".");
            if (plan.RepresentativeElementId.Length != 0 || plan.RemovableElementIds.Count != 0)
                throw new Exception("Blocked remediation plans must not expose an actionable representative/removal set.");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected '" + expected + "', actual '" + actual + "'.");
        }
    }
}
