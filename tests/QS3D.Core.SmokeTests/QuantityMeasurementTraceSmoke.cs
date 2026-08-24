using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityMeasurementTraceSmoke
    {
        internal static void Run()
        {
            var face = new QuantityFormworkFaceExplanation
            {
                FaceId = "SOLID-01/FACE-03",
                FaceType = "Side",
                GrossArea = 0.300d,
                DeductionArea = 0d,
                NetArea = 0.300d,
                MeasurementKind = "brep-rectangle-extents-v1",
                MeasurementLength = 1.50d,
                MeasurementHeight = 0.20d
            };
            var geometry = new QuantityGeometryExplanation
            {
                ElementId = "F-001",
                ElementName = "Móng Bè-4",
                GeometryFingerprint = "fp-measurement-trace",
                GrossVolume = 0.450d,
                DeductionVolume = 0d,
                NetVolume = 0.450d,
                FormworkFaces = new[] { face }
            };

            geometry.Validate(new QuantityGeometryTolerances());
            var bundle = QuantityGeometryEvidenceAdapter.Create(geometry);
            var contribution = bundle.Formwork.Contributions.Single(x =>
                x.Selector.Kind == QuantityEvidenceSelectorKind.Face &&
                string.Equals(x.Selector.FaceKey, face.FaceId, StringComparison.Ordinal));

            Equal("BREP validated face length × height", contribution.Formula, "measurement formula");
            Equal(2, contribution.Operands.Count, "measurement operand count");
            Equal(1.50m, contribution.Operands.Single(x => x.Key == "length").Value, "measurement length");
            Equal(0.20m, contribution.Operands.Single(x => x.Key == "height").Value, "measurement height");
            Equal("m", contribution.Operands.Single(x => x.Key == "length").Unit, "measurement length unit");
            Equal("m", contribution.Operands.Single(x => x.Key == "height").Unit, "measurement height unit");

            var export = QuantityEvidenceExportProjection.Create(bundle.Formwork)
                .Single(x => x.RecordKind == "Contribution" && x.EvidenceId == contribution.EvidenceId);
            Contains(export.Operands, "length=1.5 m", "projection length operand");
            Contains(export.Operands, "height=0.2 m", "projection height operand");

            var path = Path.Combine(Path.GetTempPath(), "qs3d-measurement-trace-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                XlsxQuantityEvidenceExporter.Export(path, bundle.Explanations);
                using (var archive = ZipFile.OpenRead(path))
                {
                    var entry = archive.GetEntry("xl/worksheets/sheet1.xml")
                        ?? throw new Exception("measurement trace workbook is missing EVIDENCE worksheet");
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        Contains(xml, "Operands", "xlsx operands header");
                        Contains(xml, "length=1.5 m", "xlsx length operand");
                        Contains(xml, "height=0.2 m", "xlsx height operand");
                        Contains(xml, "BREP validated face length × height", "xlsx measurement formula");
                    }
                }
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }

            var mismatched = new QuantityGeometryExplanation
            {
                ElementId = "F-002",
                GeometryFingerprint = "fp-invalid-measurement-trace",
                GrossVolume = 1d,
                DeductionVolume = 0d,
                NetVolume = 1d,
                FormworkFaces = new[]
                {
                    new QuantityFormworkFaceExplanation
                    {
                        FaceId = "SOLID-01/FACE-01",
                        FaceType = "Side",
                        GrossArea = 0.301d,
                        NetArea = 0.301d,
                        MeasurementKind = "brep-rectangle-extents-v1",
                        MeasurementLength = 1.50d,
                        MeasurementHeight = 0.20d
                    }
                }
            };
            Throws<InvalidOperationException>(() => QuantityGeometryEvidenceAdapter.Create(mismatched));

            var partial = new QuantityGeometryExplanation
            {
                ElementId = "F-003",
                GeometryFingerprint = "fp-partial-measurement-trace",
                GrossVolume = 1d,
                DeductionVolume = 0d,
                NetVolume = 1d,
                FormworkFaces = new[]
                {
                    new QuantityFormworkFaceExplanation
                    {
                        FaceId = "SOLID-01/FACE-01",
                        FaceType = "Side",
                        GrossArea = 0.300d,
                        NetArea = 0.300d,
                        MeasurementKind = "brep-rectangle-extents-v1",
                        MeasurementLength = 1.50d,
                        MeasurementHeight = 0d
                    }
                }
            };
            Throws<InvalidOperationException>(() => QuantityGeometryEvidenceAdapter.Create(partial));

            var unsupported = new QuantityGeometryExplanation
            {
                ElementId = "F-004",
                GeometryFingerprint = "fp-unsupported-measurement-trace",
                GrossVolume = 1d,
                DeductionVolume = 0d,
                NetVolume = 1d,
                FormworkFaces = new[]
                {
                    new QuantityFormworkFaceExplanation
                    {
                        FaceId = "SOLID-01/FACE-02",
                        FaceType = "Side",
                        GrossArea = 0.300d,
                        NetArea = 0.300d,
                        MeasurementKind = "unknown",
                        MeasurementLength = 1.50d,
                        MeasurementHeight = 0.20d
                    }
                }
            };
            Throws<InvalidOperationException>(() => unsupported.Validate(new QuantityGeometryTolerances()));
            Throws<InvalidOperationException>(() => QuantityGeometryEvidenceAdapter.Create(unsupported));

            Console.WriteLine("PASS quantity exact-face measurement trace/evidence/XLSX parity");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception(label + ": expected " + expected + ", got " + actual + ".");
        }

        private static void Contains(string value, string expected, string label)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception(label + ": expected text '" + expected + "'.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
