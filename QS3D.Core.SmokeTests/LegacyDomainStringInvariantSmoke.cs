using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class LegacyDomainStringInvariantSmoke
    {
        internal static void Run()
        {
            FamilyNameSetterPreservesRequiredTrimmedInvariant();
            FamilyMaterialSetterPreservesDefaultAndTrimInvariant();
            ElementFloorSetterPreservesDefaultAndTrimInvariant();
        }

        private static void FamilyNameSetterPreservesRequiredTrimmedInvariant()
        {
            var family = new FamilyDefinition("  Wall Type A  ", ElementCategory.ArchitecturalWall);
            Expect(family.Name == "Wall Type A", "Family constructor must trim accepted names.");

            family.Name = "  Wall Type B  ";
            Expect(family.Name == "Wall Type B", "Family name assignments must be trimmed.");

            ExpectArgumentException(() => family.Name = "   ", "Whitespace-only family names must be rejected.");
            Expect(family.Name == "Wall Type B", "Rejected family names must preserve the previous valid value.");

            ExpectArgumentException(() => family.Name = null!, "Null family names must be rejected.");
            Expect(family.Name == "Wall Type B", "Rejected null family names must preserve the previous valid value.");
        }

        private static void FamilyMaterialSetterPreservesDefaultAndTrimInvariant()
        {
            var family = new FamilyDefinition("Wall", ElementCategory.ArchitecturalWall, "  Concrete  ");
            Expect(family.Material == "Concrete", "Family constructor must trim accepted materials.");

            family.Material = "  Steel  ";
            Expect(family.Material == "Steel", "Family material assignments must be trimmed.");

            family.Material = "   ";
            Expect(family.Material == "Khác", "Blank family material assignments must retain the constructor fallback.");

            family.Material = null!;
            Expect(family.Material == "Khác", "Null family material assignments must retain the constructor fallback.");
        }

        private static void ElementFloorSetterPreservesDefaultAndTrimInvariant()
        {
            var family = new FamilyDefinition("Wall", ElementCategory.ArchitecturalWall);
            var element = new ElementInstance("E-1", family, "  Tầng 2  ");
            Expect(element.Floor == "Tầng 2", "Element constructor must trim accepted floors.");

            element.Floor = "  Tầng 3  ";
            Expect(element.Floor == "Tầng 3", "Element floor assignments must be trimmed.");

            element.Floor = "   ";
            Expect(element.Floor == "Nền 0.00", "Blank floor assignments must retain the constructor fallback.");

            element.Floor = null!;
            Expect(element.Floor == "Nền 0.00", "Null floor assignments must retain the constructor fallback.");
        }

        private static void ExpectArgumentException(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
