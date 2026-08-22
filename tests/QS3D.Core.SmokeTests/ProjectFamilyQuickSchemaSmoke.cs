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
            EveryFamilyCategoryHasAnIntentionalSchema();
            MillimeterConversionIsCanonical();
            SuggestedNamesMatchQsConventions();
            AutoIdentityMatchingIsDeterministic();
            AutoIdentityIncludesSizeDefiningHeight();
        }

        private static void SchemasMatchQsForms()
        {
            AssertSchema(ElementCategory.FloorFinish, new[] { "ThicknessM", "BottomOffsetM" }, new[] { "ThicknessM" });
            AssertSchema(ElementCategory.Waterproofing, new[] { "ThicknessM", "BottomOffsetM" }, new[] { "ThicknessM" });
            AssertSchema(ElementCategory.Skirting, new[] { "HeightM", "ThicknessM", "BottomOffsetM" }, new[] { "HeightM", "ThicknessM" });
            AssertSchema(ElementCategory.WallFinish, new[] { "ThicknessM", "HeightM", "BottomOffsetM" }, new[] { "ThicknessM", "HeightM" });
            AssertSchema(ElementCategory.CeilingFinish, new[] { "ThicknessM", "BottomOffsetM" }, new[] { "ThicknessM" });
            AssertSchema(ElementCategory.Railing, new[] { "HeightM", "WidthM", "BottomOffsetM" }, new[] { "HeightM", "WidthM" });
            AssertSchema(ElementCategory.WallOpening, new[] { "WidthM", "HeightM", "BottomOffsetM" }, new[] { "WidthM", "HeightM" });
            AssertSchema(ElementCategory.Beam, new[] { "WidthM", "HeightM", "BottomOffsetM" }, new[] { "WidthM", "HeightM" });
            AssertSchema(ElementCategory.Column, new[] { "WidthM", "DepthM", "HeightM", "BottomOffsetM" }, new[] { "WidthM", "DepthM", "HeightM" });
            AssertSchema(ElementCategory.ArchitecturalWall, new[] { "ThicknessM", "HeightM", "BottomOffsetM" }, new[] { "ThicknessM", "HeightM" });
            AssertSchema(ElementCategory.StructuralWall, new[] { "ThicknessM", "HeightM", "BottomOffsetM" }, new[] { "ThicknessM", "HeightM" });
            AssertSchema(ElementCategory.WallPier, new[] { "ThicknessM", "HeightM", "BottomOffsetM" }, new[] { "ThicknessM", "HeightM" });
            AssertSchema(ElementCategory.GlassWall, new[] { "ThicknessM", "HeightM", "BottomOffsetM" }, new[] { "ThicknessM", "HeightM" });
            AssertSchema(ElementCategory.Slab, new[] { "ThicknessM", "BottomOffsetM" }, new[] { "ThicknessM" });
            AssertSchema(ElementCategory.Door, new[] { "WidthM", "HeightM", "BottomOffsetM" }, new[] { "WidthM", "HeightM" });
            AssertSchema(ElementCategory.Stair, new[] { "WidthM", "HeightM", "DepthM", "BottomOffsetM" }, new[] { "WidthM", "HeightM", "DepthM" });
            AssertSchema(ElementCategory.Foundation, new[] { "ThicknessM", "BottomOffsetM" }, new[] { "ThicknessM" });
            AssertSchema(ElementCategory.Earthwork, new[] { "LengthM", "WidthM", "DepthM", "BottomOffsetM" }, new[] { "LengthM", "WidthM", "DepthM" });
            AssertSchema(ElementCategory.CustomQuantity, new[] { "LengthM", "WidthM", "HeightM" }, new[] { "LengthM", "WidthM", "HeightM" });

            var beam = ProjectFamilyQuickSchemaService.GetSchema(ElementCategory.Beam);
            Near(0.3d, beam.DefaultsM["WidthM"], 1e-12, "Beam default width mismatch.");
            Near(0.5d, beam.DefaultsM["HeightM"], 1e-12, "Beam default height mismatch.");
            Equal("Bê tông", beam.DefaultMaterial, "Beam default material mismatch.");

            var wall = ProjectFamilyQuickSchemaService.GetSchema(ElementCategory.ArchitecturalWall);
            Equal("Gạch", wall.DefaultMaterial, "Wall default material mismatch.");

            var door = ProjectFamilyQuickSchemaService.GetSchema(ElementCategory.Door);
            Near(0.9d, door.DefaultsM["WidthM"], 1e-12, "Door default width mismatch.");
            Near(2.2d, door.DefaultsM["HeightM"], 1e-12, "Door default height mismatch.");

            var earthwork = ProjectFamilyQuickSchemaService.GetSchema(ElementCategory.Earthwork);
            Near(0.5d, earthwork.DefaultsM["DepthM"], 1e-12, "Earthwork default depth mismatch.");
        }

        private static void EveryFamilyCategoryHasAnIntentionalSchema()
        {
            foreach (ElementCategory category in Enum.GetValues(typeof(ElementCategory)))
            {
                var schema = ProjectFamilyQuickSchemaService.GetSchema(category);
                if (category == ElementCategory.Grid || category == ElementCategory.Room)
                {
                    if (schema.SupportsQuickForm)
                        throw new Exception(category + " has a dedicated Workspace workflow and must not be routed through the shared quick form.");
                    continue;
                }

                if (!schema.SupportsQuickForm)
                    throw new Exception(category + " silently fell back to the empty generic quick schema.");
                Equal(category, schema.Category, category + " quick schema category mismatch.");

                foreach (var key in schema.FormKeys)
                {
                    if (!schema.DefaultsM.ContainsKey(key))
                        throw new Exception(category + " quick schema does not seed visible field " + key + ".");
                }
                foreach (var key in schema.IdentityKeys)
                {
                    if (!schema.Contains(key))
                        throw new Exception(category + " identity field " + key + " is not present in its own form schema.");
                }
            }
        }

        private static void MillimeterConversionIsCanonical()
        {
            var vi = CultureInfo.GetCultureInfo("vi-VN");
            Near(0.3d, ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Bề rộng", "300", vi, true), 1e-12, "300 mm must persist as 0.3 m.");
            Near(3.6d, ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Chiều cao", "3600", vi, true), 1e-12, "3600 mm must persist as 3.6 m.");
            Near(-0.05d, ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Offset đáy", "-50", vi, false), 1e-12, "Negative bottom offset conversion mismatch.");
            Near(0d, ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Offset đáy", "0", vi, false), 0d, "Explicit zero offset must remain valid.");
            Equal("300", ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters("WidthM", "0.300", vi), "0.300 m must display as 300 mm.");
            Equal("0", ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters("BottomOffsetM", "0", vi), "Explicit zero must continue to format as 0 mm.");

            var overflowMeters = double.MaxValue.ToString("R", CultureInfo.InvariantCulture);
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters("WidthM", overflowMeters, vi));
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters("OffsetM", "-" + overflowMeters, vi));
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters("WidthM", "0.0000004", vi));
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters("BottomOffsetM", "-0.0000004", vi));

            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Bề dày", "0", vi, true));
            var epsilonMm = double.Epsilon.ToString("R", CultureInfo.InvariantCulture);
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters(
                "Bề dày",
                epsilonMm,
                vi,
                true));
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters(
                "Offset đáy",
                epsilonMm,
                vi,
                false));
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters(
                "Offset đáy",
                "-" + epsilonMm,
                vi,
                false));
        }

        private static void SuggestedNamesMatchQsConventions()
        {
            Equal("HTS20", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.FloorFinish, Values(("ThicknessM", "0.02"))), "Floor finish suggested name mismatch.");
            Equal("ChanTuong100x15", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Skirting, Values(("HeightM", "0.1"), ("ThicknessM", "0.015"))), "Skirting suggested name mismatch.");
            Equal("LanCanH1100x50", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Railing, Values(("HeightM", "1.1"), ("WidthM", "0.05"))), "Railing suggested name mismatch.");
            Equal("LoTuong1000x2100", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.WallOpening, Values(("WidthM", "1"), ("HeightM", "2.1"))), "Wall opening suggested name mismatch.");
            Equal("D300x500", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Beam, Values(("WidthM", "0.3"), ("HeightM", "0.5"))), "Beam suggested name mismatch.");
            Equal("C400x400", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Column, Values(("WidthM", "0.4"), ("DepthM", "0.4"))), "Column suggested name mismatch.");
            Equal("T200", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.ArchitecturalWall, Values(("ThicknessM", "0.2"))), "Wall suggested name mismatch.");
            Equal("S120", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Slab, Values(("ThicknessM", "0.12"))), "Slab suggested name mismatch.");
            Equal("Cua900x2200", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Door, Values(("WidthM", "0.9"), ("HeightM", "2.2"))), "Door suggested name mismatch.");
            Equal("CauThang1200xH3600xD300", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Stair, Values(("WidthM", "1.2"), ("HeightM", "3.6"), ("DepthM", "0.3"))), "Stair suggested name mismatch.");
            Equal("Móng BTCT H500", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Foundation, Values(("ThicknessM", "0.5"))), "Foundation suggested name mismatch.");
            Equal("DaoDap1000x1000x500", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.Earthwork, Values(("LengthM", "1"), ("WidthM", "1"), ("DepthM", "0.5"))), "Earthwork suggested name mismatch.");
            Equal("Khac1000x1000x1000", ProjectFamilyQuickSchemaService.SuggestName(ElementCategory.CustomQuantity, Values(("LengthM", "1"), ("WidthM", "1"), ("HeightM", "1"))), "Custom component suggested name mismatch.");

            var overflowMeters = double.MaxValue.ToString("R", CultureInfo.InvariantCulture);
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.SuggestName(
                ElementCategory.Beam,
                Values(("WidthM", overflowMeters), ("HeightM", "0.5"))));
            Throws<InvalidOperationException>(() => ProjectFamilyQuickSchemaService.SuggestName(
                ElementCategory.Beam,
                Values(("WidthM", "0.0000004"), ("HeightM", "0.5"))));
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

        private static void AssertSchema(ElementCategory category, IEnumerable<string> formKeys, IEnumerable<string> identityKeys)
        {
            var schema = ProjectFamilyQuickSchemaService.GetSchema(category);
            if (!schema.SupportsQuickForm) throw new Exception(category + " should expose a category-specific quick form.");
            Equal(category, schema.Category, category + " schema category mismatch.");
            Sequence(formKeys, schema.FormKeys, category + " form keys mismatch.");
            Sequence(identityKeys, schema.IdentityKeys, category + " identity keys mismatch.");
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