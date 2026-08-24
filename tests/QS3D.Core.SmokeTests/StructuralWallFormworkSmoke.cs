using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class StructuralWallFormworkSmoke
    {
        internal static void Run()
        {
            AssertBltParityCase();
            AssertOverlappingContactsAreUnionedOnce();
            AssertOpeningFaceDeductionKeepsRevealFormwork();
            AssertAuditQuantitiesPersist();
        }

        private static void AssertBltParityCase()
        {
            const double length = 1.468d;
            const double thickness = 0.20d;
            const double height = 0.80d;
            var contacts = new[]
            {
                new StructuralWallConcreteContactPatch(StructuralWallFormworkFace.StartEnd, 0d, thickness, 0d, height),
                new StructuralWallConcreteContactPatch(StructuralWallFormworkFace.EndEnd, 0d, thickness, 0d, height)
            };

            var result = StructuralWallFormworkCalculator.Calculate(length, thickness, height, contacts);

            ExpectNearly(2.6688d, result.GrossFormworkM2, "BLT parity gross formwork");
            ExpectNearly(0.3200d, result.ConcreteContactDeductionM2, "BLT parity concrete-contact deduction");
            ExpectNearly(2.3488d, result.FormworkM2, "BLT parity net formwork");
        }

        private static void AssertOverlappingContactsAreUnionedOnce()
        {
            var contacts = new[]
            {
                new StructuralWallConcreteContactPatch(StructuralWallFormworkFace.StartEnd, 0d, 0.20d, 0d, 0.50d),
                new StructuralWallConcreteContactPatch(StructuralWallFormworkFace.StartEnd, 0d, 0.20d, 0.25d, 0.80d),
                new StructuralWallConcreteContactPatch(StructuralWallFormworkFace.StartEnd, -1d, 1d, -1d, 1d)
            };

            var result = StructuralWallFormworkCalculator.Calculate(1.468d, 0.20d, 0.80d, contacts);

            ExpectNearly(0.16d, result.ConcreteContactDeductionM2, "overlapping/clipped contacts must union to one end face");
            ExpectNearly(2.5088d, result.FormworkM2, "union deduction must not double-count overlapping neighbours");
        }

        private static void AssertOpeningFaceDeductionKeepsRevealFormwork()
        {
            var openings = new List<StructuralWallFormworkOpening>
            {
                new StructuralWallFormworkOpening(0.80d, 1.00d, includeBottomReveal: true),
                new StructuralWallFormworkOpening(0.90d, 2.00d, includeBottomReveal: false)
            };

            var result = StructuralWallFormworkCalculator.Calculate(5.0d, 0.20d, 3.0d, openings: openings);

            var expectedFaceDeduction = 2d * (0.80d * 1.00d + 0.90d * 2.00d);
            var expectedReveal = 0.20d * ((2d * 1.00d + 2d * 0.80d) + (2d * 2.00d + 0.90d));
            ExpectNearly(expectedFaceDeduction, result.OpeningFaceDeductionM2, "opening broad-face deduction");
            ExpectNearly(expectedReveal, result.OpeningRevealM2, "opening reveal formwork");
            ExpectNearly(result.GrossFormworkM2 - expectedFaceDeduction + expectedReveal, result.FormworkM2, "opening/reveal net adjustment");
        }

        private static void AssertAuditQuantitiesPersist()
        {
            var wall = new ProjectElement("wall-formwork-smoke", ElementCategory.ArchitecturalWall);
            var result = StructuralWallFormworkCalculator.Calculate(
                1.468d,
                0.20d,
                0.80d,
                new[]
                {
                    new StructuralWallConcreteContactPatch(StructuralWallFormworkFace.StartEnd, 0d, 0.20d, 0d, 0.80d),
                    new StructuralWallConcreteContactPatch(StructuralWallFormworkFace.EndEnd, 0d, 0.20d, 0d, 0.80d)
                });

            StructuralWallFormworkQuantityWriter.Persist(wall, result);

            ExpectQuantity(wall, StructuralWallFormworkQuantityWriter.GrossFormworkM2, 2.6688d);
            ExpectQuantity(wall, StructuralWallFormworkQuantityWriter.ConcreteContactDeductionM2, 0.3200d);
            ExpectQuantity(wall, StructuralWallFormworkQuantityWriter.OpeningRevealAdjustmentM2, 0d);
            ExpectQuantity(wall, StructuralWallFormworkQuantityWriter.NetFormworkM2, 2.3488d);
        }

        private static void ExpectQuantity(ProjectElement element, string key, double expected)
        {
            if (!element.Quantities.TryGetValue(key, out var actual))
                throw new InvalidOperationException("Missing structural wall formwork audit quantity: " + key);
            ExpectNearly(expected, actual, key);
        }

        private static void ExpectNearly(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-9d)
                throw new InvalidOperationException(label + " mismatch. Expected " + expected + ", actual " + actual + ".");
        }
    }
}
