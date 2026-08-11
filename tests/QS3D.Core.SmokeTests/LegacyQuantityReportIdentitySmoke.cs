using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class LegacyQuantityReportIdentitySmoke
    {
        public static void Run()
        {
            var family = new FamilyDefinition("Legacy wall", ElementCategory.ArchitecturalWall, "Concrete");
            var first = new ElementInstance("Legacy-A", family, "Floor") { LengthM = 2d, GrossConcreteM3 = 1d };
            first.SourceHandles.Add("AA");
            var sameIdentityDifferentCase = new ElementInstance("legacy-a", family, "Floor") { LengthM = 3d, GrossConcreteM3 = 2d };
            sameIdentityDifferentCase.SourceHandles.Add("BB");

            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { first, first }));
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { first, sameIdentityDifferentCase }));

            var second = new ElementInstance("Legacy-B", family, "Floor") { LengthM = 3d, GrossConcreteM3 = 2d };
            second.SourceHandles.Add(" aa ");
            second.SourceHandles.Add(" ");
            second.SourceHandles.Add("Bb");
            var valid = QuantityReportBuilder.Group(new[] { first, second }).Single();
            if (valid.Count != 2 || Math.Abs(valid.LengthM - 5d) > 1e-12 || Math.Abs(valid.GrossConcreteM3 - 3d) > 1e-12)
                throw new Exception("Legacy quantity grouping must remain unchanged for distinct element identities.");
            if (valid.SourceHandles.Count != 2 || valid.SourceHandles[0] != "AA" || valid.SourceHandles[1] != "Bb")
                throw new Exception("Legacy quantity source handles must be trimmed and case-insensitively deduplicated in first-seen order.");

            ExpectArgumentException(
                () => QuantityReportBuilder.Group(new ElementInstance[] { first, null!, second }),
                "elements",
                "index: 1");

            var totals = QuantityReportTotals.FromRows(new[] { valid });
            if (totals.Count != 2 || Math.Abs(totals.LengthM - 5d) > 1e-12 || Math.Abs(totals.GrossConcreteM3 - 3d) > 1e-12)
                throw new Exception("Legacy quantity totals must remain unchanged for valid rows.");

            ExpectArgumentException(
                () => QuantityReportTotals.FromRows(new QuantityReportRow[] { valid, null! }),
                "rows",
                "index: 1");

            var negativeLength = new ElementInstance("Legacy-Negative-Length", family, "Floor") { LengthM = -1d };
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { negativeLength }));

            var negativeNet = new ElementInstance("Legacy-Negative-Net", family, "Floor")
            {
                GrossConcreteM3 = 1d,
                DeductionM3 = 2d
            };
            ExpectThrows<InvalidOperationException>(() => QuantityReportBuilder.Group(new[] { negativeNet }));

            var negativeTotalRow = new QuantityReportRow { Count = 1, LengthM = -0.5d };
            ExpectThrows<InvalidOperationException>(() => QuantityReportTotals.FromRows(new[] { negativeTotalRow }));

            var project = new ProjectState("negative-report-project", "Negative report project");
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone"));
            project.Families.Add(new ProjectFamily("slab", "Slab", ElementCategory.Slab));
            var projectElement = new ProjectElement("P1", ElementCategory.Slab, "slab", "floor", "zone");
            projectElement.Quantities["LengthM"] = -1d;
            project.Elements.Add(projectElement);
            ExpectThrows<InvalidOperationException>(() => ProjectQuantityReportBuilder.Group(project));
        }

        private static void ExpectArgumentException(Action action, string paramName, string messagePart)
        {
            try { action(); }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, paramName, StringComparison.Ordinal) ||
                    ex.Message.IndexOf(messagePart, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Expected ArgumentException for '" + paramName + "' containing '" + messagePart + "', got: " + ex.Message);
                return;
            }
            throw new Exception("Expected ArgumentException.");
        }

        private static void ExpectThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
