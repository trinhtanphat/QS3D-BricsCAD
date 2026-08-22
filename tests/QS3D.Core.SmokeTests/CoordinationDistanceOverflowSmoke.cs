using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationDistanceOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OverflowingFiniteDiagonalFailsClosed();
            LargeRepresentableDiagonalRemainsClassified();
            OrdinaryThreeFourFiveGapRemainsStable();
        }

        private static void OverflowingFiniteDiagonalFailsClosed()
        {
            var service = new ClashDetectionService();
            var elements = Pair(
                "A",
                new AxisAlignedBox(0d, 0d, 0d, 0d, 0d, 0d),
                "B",
                new AxisAlignedBox(1.3e308, 1.3e308, 0d, 1.3e308, 1.3e308, 0d));

            try
            {
                service.Detect(elements, double.MaxValue);
                throw new InvalidOperationException(
                    "Finite coordination gaps whose Euclidean separation overflows must fail closed.");
            }
            catch (OverflowException ex) when (
                ex.Message.IndexOf("separation distance", StringComparison.OrdinalIgnoreCase) >= 0)
            {
            }
        }

        private static void LargeRepresentableDiagonalRemainsClassified()
        {
            var service = new ClashDetectionService();
            var elements = Pair(
                "A",
                new AxisAlignedBox(0d, 0d, 0d, 0d, 0d, 0d),
                "B",
                new AxisAlignedBox(1e308, 1e308, 0d, 1e308, 1e308, 0d));

            var results = service.Detect(elements, double.MaxValue);
            if (results.Count != 1 || results[0].Kind != ClashKind.Clearance)
                throw new InvalidOperationException("A large but representable finite diagonal must remain a clearance result.");
            if (double.IsNaN(results[0].SeparationM) || double.IsInfinity(results[0].SeparationM) ||
                results[0].SeparationM <= 1e308 || results[0].SeparationM >= double.MaxValue)
                throw new InvalidOperationException("Large representable coordination separation must remain finite and scaled correctly.");
        }

        private static void OrdinaryThreeFourFiveGapRemainsStable()
        {
            var service = new ClashDetectionService();
            var elements = Pair(
                "B",
                new AxisAlignedBox(3d, 4d, 0d, 3d, 4d, 0d),
                "A",
                new AxisAlignedBox(0d, 0d, 0d, 0d, 0d, 0d));

            var results = service.Detect(elements, 5d);
            if (results.Count != 1 || results[0].Kind != ClashKind.Clearance)
                throw new InvalidOperationException("Ordinary finite coordination clearance must remain classified.");
            if (Math.Abs(results[0].SeparationM - 5d) > 1e-12)
                throw new InvalidOperationException("Ordinary 3-4-5 coordination distance changed unexpectedly.");
            if (!string.Equals(results[0].LeftElementId, "A", StringComparison.Ordinal) ||
                !string.Equals(results[0].RightElementId, "B", StringComparison.Ordinal))
                throw new InvalidOperationException("Coordination results must preserve deterministic element ordering.");
        }

        private static IReadOnlyList<CoordinationElement> Pair(
            string leftId,
            AxisAlignedBox leftBounds,
            string rightId,
            AxisAlignedBox rightBounds)
        {
            return new[]
            {
                new CoordinationElement(leftId, "Architecture", "Generic", "A", "R1", leftBounds),
                new CoordinationElement(rightId, "MEP", "Generic", "B", "R1", rightBounds)
            };
        }
    }
}
