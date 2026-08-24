using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class WallFormworkContactSmoke
    {
        internal static void Run()
        {
            BltParityDeductsConcreteContactAndPersistsAudit();
            PartialUnionResolvedContactIsDeductedOnce();
        }

        private static void BltParityDeductsConcreteContactAndPersistsAudit()
        {
            var project = new ProjectState("wall-formwork-blt", "Wall formwork BLT regression");
            var wall = new ProjectElement(
                "W-BLT",
                ElementCategory.StructuralWall,
                string.Empty,
                string.Empty,
                string.Empty);
            wall.Properties["LengthM"] = "1.468";
            wall.Properties["ThicknessM"] = "0.20";
            wall.Properties["HeightM"] = "0.80";
            wall.Properties["ConcreteContactAreaM2"] = "0.3200";
            project.Elements.Add(wall);

            new StructuralRegenerator().Regenerate(project, wall);

            Near(2.6688d, wall.Quantities["GrossFormworkM2"], "BLT gross wall formwork");
            Near(0.3200d, wall.Quantities["ConcreteContactDeductionM2"], "BLT concrete-contact deduction");
            Near(0d, wall.Quantities["OpeningRevealFormworkAdjustmentM2"], "BLT opening/reveal adjustment");
            Near(2.3488d, wall.Quantities["FormworkM2"], "BLT net wall formwork");
        }

        private static void PartialUnionResolvedContactIsDeductedOnce()
        {
            var project = new ProjectState("wall-formwork-partial", "Wall formwork partial contact regression");
            var wall = new ProjectElement(
                "W-PARTIAL",
                ElementCategory.StructuralWall,
                string.Empty,
                string.Empty,
                string.Empty);
            wall.Properties["LengthM"] = "2";
            wall.Properties["ThicknessM"] = "0.20";
            wall.Properties["HeightM"] = "1";
            // The host supplies actual union-resolved contact area. Overlapping neighbours
            // therefore contribute only their geometric union, never a sum of overlaps.
            wall.Properties["ConcreteContactAreaM2"] = "0.15";
            project.Elements.Add(wall);

            new StructuralRegenerator().Regenerate(project, wall);

            Near(4.4d, wall.Quantities["GrossFormworkM2"], "partial-contact gross wall formwork");
            Near(0.15d, wall.Quantities["ConcreteContactDeductionM2"], "partial union contact deduction");
            Near(4.25d, wall.Quantities["FormworkM2"], "partial-contact net wall formwork");
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new Exception(
                    "Wall formwork contact regression: " + message +
                    ". Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
