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
            if (!string.Equals(results[0].LeftElementId, "A", StringComparison.Ordinal) ||
                !string.Equals(results[0].RightElementId, "B", StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination result ordering changed unexpectedly.");
        }

        private static CoordinationElement Element(string id, AxisAlignedBox bounds, string discipline)
        {
            return new CoordinationElement(id, discipline, "Generic", "System", "R1", bounds);
        }
    }
}
