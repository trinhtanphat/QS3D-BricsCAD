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
            NoContactKeepsGrossVerticalFormwork();
            PartialUnionResolvedContactIsDeductedOnce();
            ContactDeductionClampsAtAvailableFormwork();
            DoorAtFloorOmitsBottomReveal();
            SillOpeningIncludesBottomReveal();
            OpeningThenConcreteContactKeepsAuditOrder();
            LinkedOpeningRevealAdjustmentIsPersisted();
        }

        private static void BltParityDeductsConcreteContactAndPersistsAudit()
        {
            var project = new ProjectState("wall-formwork-blt", "Wall formwork BLT regression");
            var wall = NewWall("W-BLT", "1.468", "0.20", "0.80");
            wall.Properties["ConcreteContactAreaM2"] = "0.3200";
            project.Elements.Add(wall);

            new StructuralRegenerator().Regenerate(project, wall);

            Near(2.6688d, wall.Quantities["GrossFormworkM2"], "BLT gross wall formwork");
            Near(0.3200d, wall.Quantities["ConcreteContactDeductionM2"], "BLT concrete-contact deduction");
            Near(0d, wall.Quantities["OpeningRevealFormworkAdjustmentM2"], "BLT opening/reveal adjustment");
            Near(2.3488d, wall.Quantities["FormworkM2"], "BLT net wall formwork");
        }

        private static void NoContactKeepsGrossVerticalFormwork()
        {
            var project = new ProjectState("wall-formwork-free", "Wall formwork free-end regression");
            var wall = NewWall("W-FREE", "2", "0.20", "1");
            project.Elements.Add(wall);

            new StructuralRegenerator().Regenerate(project, wall);

            // Rule 1 gross = two broad faces + two exposed end faces. Top/bottom are excluded.
            Near(4.4d, wall.Quantities["GrossFormworkM2"], "free wall gross vertical formwork");
            Near(0d, wall.Quantities["ConcreteContactDeductionM2"], "free wall contact deduction");
            Near(4.4d, wall.Quantities["FormworkM2"], "free wall net formwork");
        }

        private static void PartialUnionResolvedContactIsDeductedOnce()
        {
            var project = new ProjectState("wall-formwork-partial", "Wall formwork partial contact regression");
            var wall = NewWall("W-PARTIAL", "2", "0.20", "1");
            // The host supplies actual union-resolved contact area. Overlapping neighbours
            // therefore contribute only their geometric union, never a sum of overlaps.
            wall.Properties["ConcreteContactAreaM2"] = "0.15";
            project.Elements.Add(wall);

            new StructuralRegenerator().Regenerate(project, wall);

            Near(4.4d, wall.Quantities["GrossFormworkM2"], "partial-contact gross wall formwork");
            Near(0.15d, wall.Quantities["ConcreteContactDeductionM2"], "partial union contact deduction");
            Near(4.25d, wall.Quantities["FormworkM2"], "partial-contact net wall formwork");
        }

        private static void ContactDeductionClampsAtAvailableFormwork()
        {
            var project = new ProjectState("wall-formwork-clamp", "Wall formwork contact clamp regression");
            var wall = NewWall("W-CLAMP", "2", "0.20", "1");
            wall.Properties["ConcreteContactAreaM2"] = "99";
            project.Elements.Add(wall);

            new StructuralRegenerator().Regenerate(project, wall);

            Near(4.4d, wall.Quantities["GrossFormworkM2"], "contact clamp gross wall formwork");
            Near(4.4d, wall.Quantities["ConcreteContactDeductionM2"], "contact clamp bounded deduction");
            Near(0d, wall.Quantities["FormworkM2"], "contact clamp non-negative net formwork");
        }

        private static void DoorAtFloorOmitsBottomReveal()
        {
            var project = new ProjectState("wall-formwork-door", "Door reveal regression");
            var wall = NewWall("W-DOOR", "5", "0.20", "3");
            project.Elements.Add(wall);

            var door = new ProjectElement("D-FLOOR", ElementCategory.Door);
            door.Properties["WidthM"] = "1";
            door.Properties["HeightM"] = "2";
            project.Elements.Add(door);
            new HostLinkService().LinkOpening(project, door.Id, wall.Id);
            new OpeningRegenerator().Regenerate(project, door);

            new StructuralRegenerator().Regenerate(project, wall);

            // Door at floor: gross - 2*W*H + (2*H + W)*T = 31.2 - 4 + 1 = 28.2 m2.
            Near(31.2d, wall.Quantities["GrossFormworkM2"], "door gross wall formwork");
            Near(3.0d, wall.Quantities["OpeningRevealFormworkAdjustmentM2"], "door opening/reveal adjustment");
            Near(28.2d, wall.Quantities["FormworkM2"], "door omits bottom reveal");
        }

        private static void SillOpeningIncludesBottomReveal()
        {
            var project = new ProjectState("wall-formwork-window", "Sill opening reveal regression");
            var wall = NewWall("W-WINDOW", "5", "0.20", "3");
            project.Elements.Add(wall);

            var opening = new ProjectElement("O-SILL", ElementCategory.WallOpening);
            opening.Properties["WidthM"] = "1";
            opening.Properties["HeightM"] = "1";
            opening.Properties["SillOffsetMm"] = "500";
            project.Elements.Add(opening);
            new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
            new OpeningRegenerator().Regenerate(project, opening);

            new StructuralRegenerator().Regenerate(project, wall);

            // Sill/window opening: gross - 2*W*H + 2*(W + H)*T = 31.2 - 2 + 0.8 = 30.0 m2.
            Near(31.2d, wall.Quantities["GrossFormworkM2"], "sill opening gross wall formwork");
            Near(1.2d, wall.Quantities["OpeningRevealFormworkAdjustmentM2"], "sill opening/reveal adjustment");
            Near(30.0d, wall.Quantities["FormworkM2"], "sill opening includes bottom reveal");
        }

        private static void OpeningThenConcreteContactKeepsAuditOrder()
        {
            var project = new ProjectState("wall-formwork-opening-contact", "Opening plus concrete contact regression");
            var wall = NewWall("W-OPEN-CONTACT", "5", "0.20", "3");
            wall.Properties["ConcreteContactAreaM2"] = "0.4";
            project.Elements.Add(wall);

            var opening = new ProjectElement("O-OPEN-CONTACT", ElementCategory.WallOpening);
            opening.Properties["WidthM"] = "1";
            opening.Properties["HeightM"] = "1";
            opening.Properties["SillOffsetMm"] = "500";
            project.Elements.Add(opening);
            new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
            new OpeningRegenerator().Regenerate(project, opening);

            new StructuralRegenerator().Regenerate(project, wall);

            Near(31.2d, wall.Quantities["GrossFormworkM2"], "opening/contact gross formwork");
            Near(1.2d, wall.Quantities["OpeningRevealFormworkAdjustmentM2"], "opening/contact reveal adjustment");
            Near(0.4d, wall.Quantities["ConcreteContactDeductionM2"], "opening/contact concrete deduction");
            Near(29.6d, wall.Quantities["FormworkM2"], "opening/contact net formwork");
        }

        private static void LinkedOpeningRevealAdjustmentIsPersisted()
        {
            var project = new ProjectState("wall-formwork-opening", "Wall formwork opening audit regression");
            var wall = NewWall("W-OPEN", "5", "0.20", "3");
            project.Elements.Add(wall);

            var opening = new ProjectElement("O-OPEN", ElementCategory.WallOpening);
            opening.Properties["WidthM"] = "0.9";
            opening.Properties["HeightM"] = "2.2";
            project.Elements.Add(opening);
            new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
            new OpeningRegenerator().Regenerate(project, opening);

            new StructuralRegenerator().Regenerate(project, wall);

            Near(31.2d, wall.Quantities["GrossFormworkM2"], "opening gross wall formwork");
            Near(0d, wall.Quantities["ConcreteContactDeductionM2"], "opening concrete-contact deduction");
            Near(2.90d, wall.Quantities["OpeningRevealFormworkAdjustmentM2"], "opening/reveal adjustment audit");
            Near(28.30d, wall.Quantities["FormworkM2"], "opening net wall formwork");
        }

        private static ProjectElement NewWall(string id, string lengthM, string thicknessM, string heightM)
        {
            var wall = new ProjectElement(
                id,
                ElementCategory.StructuralWall,
                string.Empty,
                string.Empty,
                string.Empty);
            wall.Properties["LengthM"] = lengthM;
            wall.Properties["ThicknessM"] = thicknessM;
            wall.Properties["HeightM"] = heightM;
            return wall;
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