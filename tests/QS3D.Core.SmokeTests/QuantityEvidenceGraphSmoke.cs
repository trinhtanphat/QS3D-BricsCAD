using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityEvidenceGraphSmoke
    {
        public static void Run()
        {
            VolumeAndFormworkParity();
            DeterministicIdentityAndOrdering();
            DeductionProvenance();
            InvalidEvidenceFailsClosed();
            XlsxCarriesGraphValuesAndEvidenceIds();
            Console.WriteLine("PASS quantity evidence graph/export parity");
        }

        private static void VolumeAndFormworkParity()
        {
            var volumeValue = 1.50m * 1.50m * 0.20m;
            var volume = QuantityContribution.Create(
                "foundation.volume",
                "Foundation volume",
                QuantityEvidenceOperation.Add,
                "L × W × H",
                volumeValue,
                QuantityEvidenceSelector.ForEntity("F-001"),
                new[]
                {
                    new QuantityEvidenceOperand("L", 1.50m, "m"),
                    new QuantityEvidenceOperand("W", 1.50m, "m"),
                    new QuantityEvidenceOperand("H", 0.20m, "m")
                });
            var volumeGraph = QuantityExplanation.Create(
                "F-001", "Foundation", "ConcreteVolume", "m3", volumeValue, volumeValue,
                new[] { volume });
            var volumeRows = QuantityEvidenceExportProjection.Create(volumeGraph);
            Equal(0.450m, volumeGraph.NetValue, "volume graph");
            Equal(0.450m, volumeRows[0].NetValue, "volume export");
            Equal(volumeGraph.EvidenceId, volumeRows[0].EvidenceId, "volume evidence id");

            var formworkValue = 4m * 0.300m;
            var formwork = QuantityContribution.Create(
                "beam.formwork.side",
                "Beam side formwork",
                QuantityEvidenceOperation.Add,
                "4 × H",
                formworkValue,
                QuantityEvidenceSelector.ForFace("B-001", 2),
                new[]
                {
                    new QuantityEvidenceOperand("faces", 4m, "count"),
                    new QuantityEvidenceOperand("H", 0.300m, "m")
                });
            var formworkGraph = QuantityExplanation.Create(
                "B-001", "Beam", "FormworkArea", "m2", formworkValue, formworkValue,
                new[] { formwork });
            var formworkRows = QuantityEvidenceExportProjection.Create(formworkGraph);
            Equal(1.200m, formworkGraph.NetValue, "formwork graph");
            Equal(1.200m, formworkRows[0].NetValue, "formwork export");
        }

        private static void DeterministicIdentityAndOrdering()
        {
            var first = QuantityContribution.Create(
                "wall.area",
                "Wall area",
                QuantityEvidenceOperation.Add,
                "L × H",
                15m,
                QuantityEvidenceSelector.ForFace("W-001", 4),
                new[]
                {
                    new QuantityEvidenceOperand("H", 3m, "m"),
                    new QuantityEvidenceOperand("L", 5m, "m")
                });
            var second = QuantityContribution.Create(
                "wall.area",
                "Wall area rebuilt",
                QuantityEvidenceOperation.Add,
                "different presentation text is non-semantic",
                15m,
                QuantityEvidenceSelector.ForFace("W-001", 4),
                new[]
                {
                    new QuantityEvidenceOperand("L", 5m, "m"),
                    new QuantityEvidenceOperand("H", 3m, "m")
                });
            Equal(first.EvidenceId, second.EvidenceId, "contribution deterministic id");

            var other = QuantityContribution.Create(
                "wall.length",
                "Wall length",
                QuantityEvidenceOperation.Add,
                "L",
                5m,
                QuantityEvidenceSelector.ForEntity("W-001"));
            var graphA = QuantityExplanation.Create(
                "W-001", "Wall", "Review", "unit", 15m, 15m,
                new[] { first, other });
            var graphB = QuantityExplanation.Create(
                "W-001", "Wall", "Review", "unit", 15m, 15m,
                new[] { other, second });
            Equal(graphA.EvidenceId, graphB.EvidenceId, "graph deterministic id");
            Equal(
                string.Join("|", graphA.Contributions.Select(item => item.EvidenceId)),
                string.Join("|", graphB.Contributions.Select(item => item.EvidenceId)),
                "deterministic contribution order");
        }

        private static void DeductionProvenance()
        {
            var selector = QuantityEvidenceSelector.ForIntersection("W-100", "O-200", "cut-01");
            var deduction = QuantityAdjustment.Create(
                "opening.deduction",
                "wall-opening-volume-v1",
                "Opening subtracts wall concrete",
                QuantityEvidenceOperation.Deduct,
                "W-100",
                "O-200",
                -0.300m,
                selector);
            var graph = QuantityExplanation.Create(
                "W-100", "Wall", "ConcreteVolume", "m3", 2.500m, 2.200m,
                adjustments: new[] { deduction });
            var row = QuantityEvidenceExportProjection.Create(graph).Single(item => item.RecordKind == "Adjustment");
            Equal("Deduct", row.Operation, "deduction operation");
            Equal("W-100", row.SourceReference, "deduction source");
            Equal("O-200", row.TargetReference, "deduction target");
            Equal("Intersection", row.SelectorKind, "deduction selector kind");
            Equal(-0.300m, row.Value, "deduction signed delta");
            Equal(deduction.EvidenceId, row.EvidenceId, "deduction evidence id");
        }

        private static void InvalidEvidenceFailsClosed()
        {
            Throws<ArgumentException>(() => QuantityEvidenceSelector.ForEntity("   "));
            Throws<ArgumentOutOfRangeException>(() => QuantityEvidenceSelector.ForFace("W-1", -1));
            Throws<ArgumentException>(() => QuantityAdjustment.Create(
                "deduct", "rule", "reason", QuantityEvidenceOperation.Deduct,
                "W-1", "O-1", -0.1m, QuantityEvidenceSelector.ForIntersection("W-2", "O-1", "cut")));
            Throws<ArgumentException>(() => QuantityExplanation.Create(
                "W-1", "Wall", "Volume", "m3", 1m, 0.8m));
        }

        private static void XlsxCarriesGraphValuesAndEvidenceIds()
        {
            var contribution = QuantityContribution.Create(
                "foundation.volume",
                "Foundation volume",
                QuantityEvidenceOperation.Add,
                "L × W × H",
                0.450m,
                QuantityEvidenceSelector.ForEntity("F-001"));
            var graph = QuantityExplanation.Create(
                "F-001", "Foundation", "ConcreteVolume", "m3", 0.450m, 0.450m,
                new[] { contribution });
            var path = Path.Combine(Path.GetTempPath(), "qs3d-evidence-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                XlsxQuantityEvidenceExporter.Export(path, new[] { graph });
                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                    if (entry == null) throw new Exception("Missing quantity evidence worksheet.");
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        True(xml.Contains(graph.EvidenceId), "xlsx summary evidence id");
                        True(xml.Contains(contribution.EvidenceId), "xlsx contribution evidence id");
                        True(xml.Contains("<v>0.45</v>"), "xlsx exact decimal value");
                    }
                }
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception(label + ": expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new Exception(label + ": expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
