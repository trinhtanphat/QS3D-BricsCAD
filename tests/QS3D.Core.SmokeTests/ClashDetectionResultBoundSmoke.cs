using System;
using System.Collections.Generic;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class ClashDetectionResultBoundSmoke
    {
        internal static void Run()
        {
            AcceptsExactHardResultBoundary();
            RejectsHardResultBoundaryPlusOne();
            AcceptsExactClearanceResultBoundary();
            RejectsClearanceResultBoundaryPlusOne();
        }

        private static void AcceptsExactHardResultBoundary()
        {
            var service = new ClashDetectionService();
            var elements = BuildTwoDisciplineSet(100, 100, clearance: false);
            AddIsolatedTail(elements);
            var results = service.Detect(elements);
            if (results.Count != 10000)
                throw new InvalidOperationException("Exact hard-clash result boundary should remain accepted when later pairs do not produce results.");
            if (results[0].Kind != ClashKind.Hard || results[9999].Kind != ClashKind.Hard)
                throw new InvalidOperationException("Exact hard-clash boundary should contain only hard clashes.");
        }

        private static void RejectsHardResultBoundaryPlusOne()
        {
            ThrowsResultLimit(
                () => new ClashDetectionService().Detect(BuildTwoDisciplineSet(101, 100, clearance: false)),
                "hard-clash");
        }

        private static void AcceptsExactClearanceResultBoundary()
        {
            var service = new ClashDetectionService();
            var elements = BuildTwoDisciplineSet(100, 100, clearance: true);
            AddIsolatedTail(elements);
            var results = service.Detect(elements, clearanceM: 0.5d);
            if (results.Count != 10000)
                throw new InvalidOperationException("Exact clearance result boundary should remain accepted when later pairs do not produce results.");
            if (results[0].Kind != ClashKind.Clearance || results[9999].Kind != ClashKind.Clearance)
                throw new InvalidOperationException("Exact clearance boundary should contain only clearance clashes.");
            if (Math.Abs(results[0].SeparationM - 0.5d) > 1e-12)
                throw new InvalidOperationException("Clearance boundary should preserve the computed separation.");
        }

        private static void RejectsClearanceResultBoundaryPlusOne()
        {
            ThrowsResultLimit(
                () => new ClashDetectionService().Detect(
                    BuildTwoDisciplineSet(101, 100, clearance: true),
                    clearanceM: 0.5d),
                "clearance");
        }

        private static List<CoordinationElement> BuildTwoDisciplineSet(int leftCount, int rightCount, bool clearance)
        {
            var elements = new List<CoordinationElement>(leftCount + rightCount);
            var leftBounds = new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d);
            var rightBounds = clearance
                ? new AxisAlignedBox(1.5d, 0d, 0d, 2.5d, 1d, 1d)
                : new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d);

            for (var i = 0; i < leftCount; i++)
            {
                elements.Add(new CoordinationElement(
                    "A-" + i.ToString("D3"),
                    "Architecture",
                    "Generic",
                    "A",
                    "R",
                    leftBounds));
            }
            for (var i = 0; i < rightCount; i++)
            {
                elements.Add(new CoordinationElement(
                    "B-" + i.ToString("D3"),
                    "Structure",
                    "Generic",
                    "B",
                    "R",
                    rightBounds));
            }
            return elements;
        }

        private static void AddIsolatedTail(List<CoordinationElement> elements)
        {
            elements.Add(new CoordinationElement(
                "ZZZ-NO-CLASH",
                "MEP",
                "Generic",
                "C",
                "R",
                new AxisAlignedBox(100d, 100d, 100d, 101d, 101d, 101d)));
        }

        private static void ThrowsResultLimit(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex) when (
                ex.Message.IndexOf("at most 10000 results", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            throw new InvalidOperationException(
                "Expected " + scenario + " result boundary+1 to fail before materializing another clash result.");
        }
    }
}