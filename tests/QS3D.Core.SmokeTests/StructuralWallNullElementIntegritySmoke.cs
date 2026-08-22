using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class StructuralWallNullElementIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsNullSemanticElementBeforeWallQuantityMutation();
            PreservesValidLinkedOpeningDeduction();
        }

        private static void RejectsNullSemanticElementBeforeWallQuantityMutation()
        {
            var project = new ProjectState("P-STRUCT-NULL", "Structural wall null integrity");
            var wall = Wall();
            wall.Quantities["Sentinel"] = 17d;
            project.Elements.Add(wall);
            project.Elements.Add(null!);
            var quantityCount = wall.Quantities.Count;

            try
            {
                new StructuralRegenerator().Regenerate(project, wall);
            }
            catch (InvalidOperationException ex)
            {
                if (!string.Equals(
                        ex.Message,
                        "Structural wall quantity cannot inspect a project containing a null semantic element.",
                        StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected structural-wall null-element error.", ex);

                if (wall.Quantities.Count != quantityCount ||
                    !wall.Quantities.TryGetValue("Sentinel", out var sentinel) || sentinel != 17d)
                    throw new InvalidOperationException("Null-element rejection partially mutated structural-wall quantities.");
                return;
            }

            throw new InvalidOperationException("Expected structural-wall regeneration to reject a null semantic element.");
        }

        private static void PreservesValidLinkedOpeningDeduction()
        {
            var project = new ProjectState("P-STRUCT-VALID", "Structural wall valid linked opening");
            var wall = Wall();
            var opening = new ProjectElement("O1", ElementCategory.WallOpening);
            opening.Properties["HostWallId"] = "w1";
            opening.Quantities["OpeningAreaM2"] = 2d;
            project.Elements.Add(wall);
            project.Elements.Add(opening);

            new StructuralRegenerator().Regenerate(project, wall);

            Require(wall, "GrossWallAreaM2", 30d);
            Require(wall, "OpeningAreaM2", 2d);
            Require(wall, "NetWallAreaM2", 28d);
            Require(wall, "DeductionM3", 0.4d);
        }

        private static ProjectElement Wall()
        {
            var wall = new ProjectElement("W1", ElementCategory.StructuralWall);
            wall.Properties["LengthM"] = "10";
            wall.Properties["HeightM"] = "3";
            wall.Properties["ThicknessM"] = "0.2";
            return wall;
        }

        private static void Require(ProjectElement element, string key, double expected)
        {
            if (!element.Quantities.TryGetValue(key, out var actual) || Math.Abs(actual - expected) > 1e-12)
                throw new InvalidOperationException("Unexpected structural-wall quantity: " + key + ".");
        }
    }
}
