using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class P0QuantityContractRegressionSmoke
    {
        public static void Run()
        {
            P0CategoryEnvelopePreservesQuantityAndProvenance();
            MissingQuantityEvidenceRemainsExplicit();
            InvalidQuantityEvidenceFailsClosed();
        }

        private static void P0CategoryEnvelopePreservesQuantityAndProvenance()
        {
            var project = NewProject("P0-FP");
            AddVolumetric(project, "AW", ElementCategory.ArchitecturalWall, 5d, 3d, 2.5d, 0.5d, 12d, "A1");
            AddVolumetric(project, "BM", ElementCategory.Beam, 4d, 1.2d, 1.0d, 0.2d, 8d, "B1");
            AddVolumetric(project, "CL", ElementCategory.Column, 3d, 0.9d, 0.8d, 0.1d, 7d, "C1");
            AddVolumetric(project, "SL", ElementCategory.Slab, 0d, 2.4d, 2.0d, 0.4d, 20d, "D1");
            AddVolumetric(project, "SW", ElementCategory.StructuralWall, 6d, 4.0d, 3.4d, 0.6d, 15d, "E1");
            AddVolumetric(project, "FD", ElementCategory.Foundation, 0d, 5.0d, 4.5d, 0.5d, 10d, "F1");
            AddOpening(project, "DR", ElementCategory.Door, 1.98d, "D0");
            AddOpening(project, "OP", ElementCategory.WallOpening, 2.25d, "O1");

            var rows = ProjectQuantityReportBuilder.Detail(project);
            if (rows.Count != 8) throw new Exception("P0 quantity detail must retain one row per semantic element.");

            AssertVolumetric(rows, "AW", ElementCategory.ArchitecturalWall, 5d, 3d, 2.5d, 0.5d, 12d, "A1");
            AssertVolumetric(rows, "BM", ElementCategory.Beam, 4d, 1.2d, 1.0d, 0.2d, 8d, "B1");
            AssertVolumetric(rows, "CL", ElementCategory.Column, 3d, 0.9d, 0.8d, 0.1d, 7d, "C1");
            AssertVolumetric(rows, "SL", ElementCategory.Slab, 0d, 2.4d, 2.0d, 0.4d, 20d, "D1");
            AssertVolumetric(rows, "SW", ElementCategory.StructuralWall, 6d, 4.0d, 3.4d, 0.6d, 15d, "E1");
            AssertVolumetric(rows, "FD", ElementCategory.Foundation, 0d, 5.0d, 4.5d, 0.5d, 10d, "F1");
            AssertOpening(rows, "DR", ElementCategory.Door, 1.98d, "D0");
            AssertOpening(rows, "OP", ElementCategory.WallOpening, 2.25d, "O1");
        }

        private static void MissingQuantityEvidenceRemainsExplicit()
        {
            var project = NewProject("MISSING-FP");
            var family = AddFamily(project, "beam-missing", ElementCategory.Beam);
            var beam = new ProjectElement("BM0", ElementCategory.Beam, family.Id, "floor", "zone");
            beam.SourceHandles.Add("B0");
            project.Elements.Add(beam);

            var row = ProjectQuantityReportBuilder.Detail(project).Single();
            if (row.HasGrossConcreteM3Evidence || row.HasNetConcreteM3Evidence || row.HasDeductionM3Evidence ||
                row.HasFormworkM2Evidence || row.HasLengthMEvidence)
                throw new Exception("Missing P0 quantities must remain distinguishable from measured zero values.");
            if (row.DrawingFingerprint != "MISSING-FP" || row.ElementIds.Single() != "BM0" || row.SourceHandles.Single() != "B0")
                throw new Exception("Missing quantity evidence must not erase P0 provenance.");
        }

        private static void InvalidQuantityEvidenceFailsClosed()
        {
            var element = new ProjectElement("BAD", ElementCategory.Foundation, "foundation", "floor", "zone");
            ExpectThrows<ArgumentOutOfRangeException>(() => element.SetQuantity("NetVolumeM3", -0.001d));
            ExpectThrows<ArgumentOutOfRangeException>(() => element.SetQuantity("NetVolumeM3", double.NaN));
            ExpectThrows<ArgumentOutOfRangeException>(() => element.SetQuantity("NetVolumeM3", double.PositiveInfinity));
        }

        private static ProjectState NewProject(string fingerprint)
        {
            var project = new ProjectState("p0-quantity", "P0 quantity contract") { DrawingFingerprint = fingerprint };
            project.Floors.Add(new FloorDefinition("floor", "Tầng P0", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone P0"));
            return project;
        }

        private static ProjectFamily AddFamily(ProjectState project, string id, ElementCategory category)
        {
            var family = new ProjectFamily(id, "Family " + id, category);
            family.Properties["Material"] = category == ElementCategory.Door ? "Timber" : "Concrete";
            project.Families.Add(family);
            return family;
        }

        private static void AddVolumetric(ProjectState project, string id, ElementCategory category, double length, double gross, double net, double deduction, double formwork, string handle)
        {
            var family = AddFamily(project, id.ToLowerInvariant() + "-family", category);
            var element = new ProjectElement(id, category, family.Id, "floor", "zone");
            if (length > 0d) element.SetQuantity("LengthM", length);
            element.SetQuantity("GrossConcreteM3", gross);
            element.SetQuantity("NetConcreteM3", net);
            element.SetQuantity("DeductionM3", deduction);
            element.SetQuantity("FormworkM2", formwork);
            element.SourceHandles.Add(handle);
            project.Elements.Add(element);
        }

        private static void AddOpening(ProjectState project, string id, ElementCategory category, double openingArea, string handle)
        {
            var family = AddFamily(project, id.ToLowerInvariant() + "-family", category);
            var element = new ProjectElement(id, category, family.Id, "floor", "zone");
            element.SetQuantity("OpeningAreaM2", openingArea);
            element.SourceHandles.Add(handle);
            project.Elements.Add(element);
        }

        private static void AssertVolumetric(IReadOnlyList<QuantityReportRow> rows, string id, ElementCategory category, double length, double gross, double net, double deduction, double formwork, string handle)
        {
            var row = rows.Single(x => x.ElementIds.Count == 1 && x.ElementIds[0] == id);
            if (row.Category != category.ToString() || row.Floor != "Tầng P0" || row.Zone != "Zone P0" || string.IsNullOrWhiteSpace(row.FamilyId) || row.Material != "Concrete")
                throw new Exception("P0 semantic dimensions were not preserved for " + id + ".");
            Near(gross, row.GrossConcreteM3, id + "/gross");
            Near(net, row.NetConcreteM3, id + "/net");
            Near(deduction, row.DeductionM3, id + "/deduction");
            Near(formwork, row.FormworkM2, id + "/formwork");
            if (!row.HasGrossConcreteM3Evidence || !row.HasNetConcreteM3Evidence || !row.HasDeductionM3Evidence || !row.HasFormworkM2Evidence)
                throw new Exception("P0 volume/formwork evidence flags were lost for " + id + ".");
            if (length > 0d)
            {
                Near(length, row.LengthM, id + "/length");
                if (!row.HasLengthMEvidence) throw new Exception("P0 length evidence flag was lost for " + id + ".");
            }
            else if (row.HasLengthMEvidence)
                throw new Exception("Unsupported length must remain absent for " + id + ".");
            AssertProvenance(row, id, handle, "P0-FP");
        }

        private static void AssertOpening(IReadOnlyList<QuantityReportRow> rows, string id, ElementCategory category, double openingArea, string handle)
        {
            var row = rows.Single(x => x.ElementIds.Count == 1 && x.ElementIds[0] == id);
            if (row.Category != category.ToString() || row.Floor != "Tầng P0" || row.Zone != "Zone P0" || string.IsNullOrWhiteSpace(row.FamilyId))
                throw new Exception("P0 opening semantic dimensions were not preserved for " + id + ".");
            Near(openingArea, row.DoorAreaM2, id + "/opening-area");
            if (!row.HasDoorAreaM2Evidence)
                throw new Exception("P0 opening area evidence flag was lost for " + id + ".");
            if (row.HasGrossConcreteM3Evidence || row.HasNetConcreteM3Evidence || row.HasLengthMEvidence)
                throw new Exception("P0 opening must not fabricate unrelated quantity evidence for " + id + ".");
            AssertProvenance(row, id, handle, "P0-FP");
        }

        private static void AssertProvenance(QuantityReportRow row, string id, string handle, string fingerprint)
        {
            if (row.Count != 1 || row.ElementIds.Single() != id || row.SourceHandles.Single() != handle || row.DrawingFingerprint != fingerprint)
                throw new Exception("P0 quantity provenance failed for " + id + ".");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new Exception(label + " expected " + expected + " but got " + actual + ".");
        }

        private static void ExpectThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
