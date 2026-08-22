using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationSeparatedOverlapOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OppositeExtremeSeparatedBoxesDoNotOverflowWithoutClearance();
            TouchingExtremeIntervalsRemainNonClashing();
            OrdinaryOverlapRemainsHardClash();
            ActualExtremeGapStillFailsClosedWhenClearanceRequiresIt();
            OrdinaryClearanceRemainsDeterministic();
            DuplicateIdsRemainCaseInsensitiveRejected();
            NullElementRetainsIndexDiagnostic();
            SameDisciplineFilteringRemainsStable();
        }

        private static void OppositeExtremeSeparatedBoxesDoNotOverflowWithoutClearance()
        {
            var service = new ClashDetectionService();
            var results = service.Detect(new[]
            {
                Element("A", new AxisAlignedBox(-double.MaxValue, 0d, 0d, -double.MaxValue, 1d, 1d), "Architecture"),
                Element("B", new AxisAlignedBox(double.MaxValue, 0d, 0d, double.MaxValue, 1d, 1d), "MEP")
            });

            if (results.Count != 0)
                throw new InvalidOperationException("Widely separated finite boxes must not produce a clash when clearance is disabled.");
        }

        private static void TouchingExtremeIntervalsRemainNonClashing()
        {
            var service = new ClashDetectionService();
            var results = service.Detect(new[]
            {
                Element("A", new AxisAlignedBox(-double.MaxValue, 0d, 0d, 0d, 1d, 1d), "Architecture"),
                Element("B", new AxisAlignedBox(0d, 0d, 0d, double.MaxValue, 1d, 1d), "MEP")
            });

            if (results.Count != 0)
                throw new InvalidOperationException("Boxes that only touch on one axis must remain non-clashing without clearance.");
        }

        private static void OrdinaryOverlapRemainsHardClash()
        {
            var service = new ClashDetectionService();
            var results = service.Detect(new[]
            {
                Element("B", new AxisAlignedBox(1d, 2d, 3d, 4d, 5d, 6d), "MEP"),
                Element("A", new AxisAlignedBox(0d, 0d, 0d, 2d, 3d, 4d), "Architecture")
            });

            if (results.Count != 1 || results[0].Kind != ClashKind.Hard)
                throw new InvalidOperationException("Ordinary positive overlap must remain a hard clash.");
            if (Math.Abs(results[0].OverlapXM - 1d) > 1e-12 ||
                Math.Abs(results[0].OverlapYM - 1d) > 1e-12 ||
                Math.Abs(results[0].OverlapZM - 1d) > 1e-12)
                throw new InvalidOperationException("Ordinary overlap extents changed unexpectedly.");
            AssertOrdered(results[0], "A", "B");
        }

        private static void ActualExtremeGapStillFailsClosedWhenClearanceRequiresIt()
        {
            var service = new ClashDetectionService();
            try
            {
                service.Detect(new[]
                {
                    Element("A", new AxisAlignedBox(-double.MaxValue, 0d, 0d, -double.MaxValue, 1d, 1d), "Architecture"),
                    Element("B", new AxisAlignedBox(double.MaxValue, 0d, 0d, double.MaxValue, 1d, 1d), "MEP")
                }, clearanceM: 1d);
                throw new InvalidOperationException("An unrepresentable positive gap must fail closed when clearance evaluation requires it.");
            }
            catch (OverflowException ex)
            {
                if (!ex.Message.Contains("Coordination gap extent", StringComparison.Ordinal))
                    throw new InvalidOperationException("Extreme positive-gap overflow diagnostic changed unexpectedly.", ex);
            }
        }

        private static void OrdinaryClearanceRemainsDeterministic()
        {
            var service = new ClashDetectionService();
            var results = service.Detect(new[]
            {
                Element("B", new AxisAlignedBox(1.5d, 0d, 0d, 2.5d, 1d, 1d), "MEP"),
                Element("A", new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d), "Architecture")
            }, clearanceM: 0.5d);

            if (results.Count != 1 || results[0].Kind != ClashKind.Clearance)
                throw new InvalidOperationException("Ordinary separated boxes at the threshold must remain a clearance clash.");
            if (Math.Abs(results[0].SeparationM - 0.5d) > 1e-12 ||
                results[0].OverlapXM != 0d ||
                Math.Abs(results[0].OverlapYM - 1d) > 1e-12 ||
                Math.Abs(results[0].OverlapZM - 1d) > 1e-12)
                throw new InvalidOperationException("Ordinary clearance separation/overlap semantics changed unexpectedly.");
            AssertOrdered(results[0], "A", "B");
        }

        private static void DuplicateIdsRemainCaseInsensitiveRejected()
        {
            var service = new ClashDetectionService();
            try
            {
                service.Detect(new[]
                {
                    Element("A", new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d), "Architecture"),
                    Element("a", new AxisAlignedBox(2d, 0d, 0d, 3d, 1d, 1d), "MEP")
                });
                throw new InvalidOperationException("Case-insensitive duplicate ids must remain rejected.");
            }
            catch (ArgumentException ex)
            {
                if (!ex.Message.Contains("Duplicate coordination element id", StringComparison.Ordinal))
                    throw new InvalidOperationException("Duplicate-id diagnostic changed unexpectedly.", ex);
            }
        }

        private static void NullElementRetainsIndexDiagnostic()
        {
            var service = new ClashDetectionService();
            try
            {
                service.Detect(new CoordinationElement[]
                {
                    Element("A", new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d), "Architecture"),
                    null!
                });
                throw new InvalidOperationException("Null coordination elements must remain rejected.");
            }
            catch (ArgumentException ex)
            {
                if (!ex.Message.Contains("index 1", StringComparison.Ordinal))
                    throw new InvalidOperationException("Null-element index diagnostic changed unexpectedly.", ex);
            }
        }

        private static void SameDisciplineFilteringRemainsStable()
        {
            var service = new ClashDetectionService();
            var elements = new[]
            {
                Element("A", new AxisAlignedBox(0d, 0d, 0d, 2d, 2d, 2d), "Architecture"),
                Element("B", new AxisAlignedBox(1d, 1d, 1d, 3d, 3d, 3d), "architecture")
            };

            if (service.Detect(elements).Count != 0)
                throw new InvalidOperationException("Same-discipline clashes must remain filtered by default.");

            var included = service.Detect(elements, includeSameDiscipline: true);
            if (included.Count != 1 || included[0].Kind != ClashKind.Hard)
                throw new InvalidOperationException("Explicit same-discipline inclusion must remain supported.");
        }

        private static void AssertOrdered(ClashResult result, string expectedLeft, string expectedRight)
        {
            if (!string.Equals(result.LeftElementId, expectedLeft, StringComparison.Ordinal) ||
                !string.Equals(result.RightElementId, expectedRight, StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination result ordering changed unexpectedly.");
        }

        private static CoordinationElement Element(string id, AxisAlignedBox bounds, string discipline)
        {
            return new CoordinationElement(id, discipline, "Generic", "System", "R1", bounds);
        }
    }
}
