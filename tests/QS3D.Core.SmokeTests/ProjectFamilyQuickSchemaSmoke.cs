using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFamilyQuickSchemaSmoke
    {
        public static void Run()
        {
            SchemasMatchQsForms();
            MillimeterConversionIsCanonical();
            SuggestedNamesMatchQsConventions();
            AutoIdentityMatchingIsDeterministic();
            AutoIdentityIncludesSizeDefiningHeight();
        }

        private static void SchemasMatchQsForms()
        {
            var beam = ProjectFamilyQuickSchemaService.GetSchema(ElementCategory.Beam);
            Sequence(new[] { "WidthM", "HeightM", "BottomOffsetM" }, beam.FormKeys, "Beam form keys mismatch.");
            Sequence(new[] { "WidthM", "HeightM" }, beam.IdentityKeys, "Beam identity keys mismatch.");
            Near(0.3d, beam.DefaultsM["WidthM"], 1e-12, "Beam default width mismatch.");
            Near(0.5d, beam.DefaultsM["HeightM"], 1e-12, "Beam default height mismatch.");
            Equal("Bê tông", beam.DefaultMaterial, "Beam default material mismatch.");

            var column = ProjectFamilyQuickSchemaService.GetSchema(ElementCategory.Column);
            Sequence(new[] { "WidthM", "DepthM", "HeightM", "BottomOffsetM" }, column.FormKeys, "Column form keys mismatch.");
            Sequence(new[] { "WidthM", "DepthM", "HeightM" }, column.IdentityKeys, "Column identity keys mismatch.");

            var wall = ProjectFamilyQuickSchemaService.GetSchema(ElementCategory.ArchitecturalWall);
            Sequence(new[] { "ThicknessM", "HeightM", "BottomOffsetM" }, wall.FormKeys, "Wall form keys mismatch.");
            Sequence(new[] { "ThicknessM", "HeightM" }, wall.IdentityKeys, "Wall identity keys mismatch.");
            Equal("Gạch", wall.DefaultMaterial, "Wall default material mismatch.");

            var slab = ProjectFamilyQuickSchemaService.GetSchema(ElementCategory.Slab);
            Sequence(new[] { "ThicknessM", "BottomOffsetM" }, slab.FormKeys, "Slab form keys mismatch.");

            var foundation = ProjectFamilyQuickSchemaService.GetSchema(ElementCategory.Foundation);
            Sequence(new[] { "ThicknessM", "BottomOffsetM" }, foundation.FormKeys, "Foundation form keys mismatch.");
        }

        private static void MillimeterConversionIsCanonical()
        {
            var vi = CultureInfo.GetCultureInfo("vi-VN");
            Near(0.3d, ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Bề rộng", "300", vi, true), 1e-12, "300 mm must persist as 0.3 m.");
            Near(3.6d, ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Chiều cao", "3600", vi, true), 1e-12, "3600 mm must persist as 3.6 m.");
            Near(-0.05d, ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Offset đáy", "-50", vi, false), 1e-12, "Negative bottom offset conversion mismatch.");
            Equal("300", ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters("WidthM", "0.300", vi), "0.300 m must display as 300 mm.");
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Bề dày", "0", vi, true));
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters(
                "Bề dày",
                double.Epsilon.ToString("R", CultureInfo.InvariantCulture),
                vi,
                true));
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters(
                "WidthM",
                double.MaxValue.ToString("R", CultureInfo.InvariantCulture),
                vi));
        }

        private static void SuggestedNamesMatchQsConventions()
        {
            Equal("D300x500", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Beam, Values(("WidthM", "0.3"), ("HeightM", "0.5"))), "Beam suggested name mismatch.");
            Equal("C400x400", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Column, Values(("WidthM", "0.4"), ("DepthM", "0.4"))), "Column suggested name mismatch.");
            Equal("T200", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.ArchitecturalWall, Values(("ThicknessM", "0.2"))), "Wall suggested name mismatch.");
            Equal("S120", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Slab, Values(("ThicknessM", "0.12"))), "Slab suggested name mismatch.");
            Equal("Móng BTCT H500", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Foundation, Values(("ThicknessM", "0.5"))), "Foundation suggested name mismatch.");
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.SuggestName(
                ElementCategory.Beam,
                Values(("WidthM", double.MaxValue.ToString("R", CultureInfo.InvariantCulture)), ("HeightM", "0.5"))));
        }

        private static void AutoIdentityMatchingIsDeterministic()
        {
            var project = new ProjectState("P-QUICK-SCHEMA", "Quick schema smoke");
            var concrete = new ProjectFamily("beam-concrete", "D300x500", ElementCategory.Beam);
            concrete.Properties["WidthM"] = "0.300";
            concrete.Properties["HeightM"] = "0.500";
            concrete.Properties["BottomOffsetM"] = "0";
            concrete.Properties["Material"] = "Bê tông";
            project.Families.Add(concrete);

            var steel = new ProjectFamily("beam-steel", "D300x500 thép", ElementCategory.Beam);
            steel.Properties["WidthM"] = "0.3";
            steel.Properties["HeightM"] = "0.5";
            steel.Properties["Material"] = "Thép";
            project.Families.Add(steel);

            var values = Values(("WidthM", "0.3"), ("HeightM", "0.5"), ("BottomOffsetM", "0"));
            var matches = ProjectFamilyQuickSchemaService.FindIdentityMatches(project, ElementCategory.Beam, values, "Bê tông");
            Equal(1, matches.Count, "Material-aware Auto Family matching should resolve exactly one Beam.");
            Equal("beam-concrete", matches[0].Id, "Auto Family matched the wrong Beam.");

            Equal("D300x500 2", ProjectFamilyQuickSchemaService.MakeUniqueName(project, ElementCategory.Beam, "D300x500"), "Collision-safe suggested name mismatch.");
        }

        private static void AutoIdentityIncludesSizeDefiningHeight()
        {
            var project = new ProjectState("P-QUICK-HEIGHT", "Quick schema height identity smoke");

            var column = new ProjectFamily("column-3600", "C400x400", ElementCategory.Column);
            column.Properties["WidthM"] = "0.4";
            column.Properties["DepthM"] = "0.4";
            column.Properties["HeightM"] = "3.6";
            column.Properties["BottomOffsetM"] = "0";
            column.Properties["Material"] = "Bê tông";
            project.Families.Add(column);

            var columnDifferentHeight = Values(
                ("WidthM", "0.4"),
                ("DepthM", "0.4"),
                ("HeightM", "3.0"),
                ("BottomOffsetM", "0"));
            Equal(
                0,
                ProjectFamilyQuickSchemaService.FindIdentityMatches(project, ElementCategory.Column, columnDifferentHeight, "Bê tông").Count,
                "Auto Family must not reuse a Column with a different height and then mutate inherited instances.");

            var wall = new ProjectFamily("wall-3600", "T200", ElementCategory.ArchitecturalWall);
            wall.Properties["ThicknessM"] = "0.2";
            wall.Properties["HeightM"] = "3.6";
            wall.Properties["BottomOffsetM"] = "0";
            wall.Properties["Material"] = "Gạch";
            project.Families.Add(wall);

            var wallDifferentHeight = Values(
                ("ThicknessM", "0.2"),
                ("HeightM", "3.0"),
                ("BottomOffsetM", "0"));
            Equal(
                0,
                ProjectFamilyQuickSchemaService.FindIdentityMatches(project, ElementCategory.ArchitecturalWall, wallDifferentHeight, "Gạch").Count,
                "Auto Family must not reuse a Wall with a different height and then mutate inherited instances.");
        }

        private static IReadOnlyDictionary<string, string> Values(params (string Key, string Value)[] items)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items) result[item.Key] = item.Value;
            return result;
        }

        private static void Sequence(IEnumerable<string> expected, IEnumerable<string> actual, string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.OrdinalIgnoreCase)) throw new Exception(message);
        }

        private static void Near(double expected, double actual, double tolerance, string message)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
