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
            GeometryEvidenceAdapterParity();
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
            Equal(first.EvidenceId, second.EvidenceId, "contribution deterministic id");
            Equal(graphA.EvidenceId, graphB.EvidenceId, "graph deterministic id");
            Equal(
                string.Join("|", graphA.Contributions.Select(item => item.EvidenceId)),
                string.Join("|", graphB.Contributions.Select(item => item.EvidenceId)),
                "deterministic contribution order");

            var indexed = QuantityEvidenceSelector.ForFace("W-001", 4);
            var stableKey = QuantityEvidenceSelector.ForFaceKey("W-001", "SOLID-01/FACE-04");
            Equal(QuantityEvidenceSelectorKind.Face, stableKey.Kind, "stable face selector kind");
            Equal("SOLID-01/FACE-04", stableKey.FaceKey, "stable face selector key");
            True(!string.Equals(indexed.CanonicalKey, stableKey.CanonicalKey, StringComparison.Ordinal), "indexed and keyed face selectors remain distinct");
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

        private static void GeometryEvidenceAdapterParity()
        {
            var faces = new[]
            {
                Face("SOLID-01/FACE-01", 0.300d),
                Face("SOLID-01/FACE-02", 0.300d),
                Face("SOLID-01/FACE-03", 0.300d),
                Face("SOLID-01/FACE-04", 0.300d)
            };
            var geometry = new QuantityGeometryExplanation
            {
                ElementId = "F-001",
                ElementName = "Móng Bè-4",
                GeometryFingerprint = "fp-foundation-001",
                GrossVolume = 0.450d,
                DeductionVolume = 0d,
                NetVolume = 0.450d,
                FormworkFaces = faces
            };
            var evidence = QuantityGeometryEvidenceAdapter.Create(geometry);
            Equal(0.450m, evidence.Concrete.GrossValue, "geometry adapter gross concrete");
            Equal(0.450m, evidence.Concrete.NetValue, "geometry adapter net concrete");
            Equal(1.200m, evidence.Formwork.GrossValue, "geometry adapter gross formwork");
            Equal(1.200m, evidence.Formwork.NetValue, "geometry adapter net formwork");

            var faceRows = evidence.Formwork.Contributions
                .Where(x => x.Operation == QuantityEvidenceOperation.Add && x.Selector.Kind == QuantityEvidenceSelectorKind.Face)
                .OrderBy(x => x.Selector.FaceKey, StringComparer.Ordinal)
                .ToArray();
            Equal(4, faceRows.Length, "geometry adapter face contribution count");
            Equal(4, faceRows.Select(x => x.Selector.FaceKey).Distinct(StringComparer.Ordinal).Count(), "geometry adapter distinct face keys");
            foreach (var row in faceRows) Equal(0.300m, row.Value, "geometry adapter face value");

            var reordered = new QuantityGeometryExplanation
            {
                ElementId = geometry.ElementId,
                ElementName = "Presentation-only renamed",
                GeometryFingerprint = geometry.GeometryFingerprint,
                GrossVolume = geometry.GrossVolume,
                DeductionVolume = geometry.DeductionVolume,
                NetVolume = geometry.NetVolume,
                FormworkFaces = faces.Reverse().ToArray()
            };
            var rebuilt = QuantityGeometryEvidenceAdapter.Create(reordered);
            Equal(evidence.Concrete.EvidenceId, rebuilt.Concrete.EvidenceId, "geometry adapter concrete stable id");
            Equal(evidence.Formwork.EvidenceId, rebuilt.Formwork.EvidenceId, "geometry adapter formwork stable id");

            var deductionGeometry = new QuantityGeometryExplanation
            {
                ElementId = "W-100",
                ElementName = "Wall",
                GeometryFingerprint = "fp-wall-100",
                GrossVolume = 2.500d,
                DeductionVolume = 0.300d,
                NetVolume = 2.200d,
                VolumeDeductions = new[]
                {
                    new QuantityGeometryDeduction
                    {
                        ElementId = "O-200",
                        ElementName = "Opening",
                        Relation = QuantityGeometryRelation.VolumeIntersection,
                        Volume = 0.300d,
                        RegionKey = "W-100|V|O-200"
                    }
                }
            };
            var deductionEvidence = QuantityGeometryEvidenceAdapter.Create(deductionGeometry);
            var cause = deductionEvidence.Concrete.Contributions.Single(x =>
                x.Operation == QuantityEvidenceOperation.Deduct &&
                x.Selector.Kind == QuantityEvidenceSelectorKind.Intersection);
            Equal(-0.300m, cause.Value, "geometry adapter deduction cause value");
            Equal("W-100", cause.Selector.SourceEntityKey, "geometry adapter deduction source selector");
            Equal("O-200", cause.Selector.TargetEntityKey, "geometry adapter deduction target selector");

            var causeExport = QuantityEvidenceExportProjection.Create(deductionEvidence.Concrete)
                .Single(x => x.RecordKind == "Contribution" && x.EvidenceId == cause.EvidenceId);
            Equal("W-100", causeExport.SourceReference, "geometry adapter deduction export source");
            Equal("O-200", causeExport.TargetReference, "geometry adapter deduction export target");
            Equal("Intersection", causeExport.SelectorKind, "geometry adapter deduction export selector");
            Equal(-0.300m, causeExport.Value, "geometry adapter deduction export value");
        }

        private static QuantityFormworkFaceExplanation Face(string faceId, double area)
        {
            return new QuantityFormworkFaceExplanation
            {
                FaceId = faceId,
                FaceType = "Side",
                GrossArea = area,
                DeductionArea = 0d,
                NetArea = area
            };
        }

        private static void InvalidEvidenceFailsClosed()
        {
            Throws<ArgumentException>(() => QuantityEvidenceSelector.ForEntity("   "));
            Throws<ArgumentOutOfRangeException>(() => QuantityEvidenceSelector.ForFace("W-1", -1));
            Throws<ArgumentException>(() => QuantityEvidenceSelector.ForFaceKey("W-1", "   "));
            Throws<ArgumentException>(() => QuantityAdjustment.Create(
                "deduct", "rule", "reason", QuantityEvidenceOperation.Deduct,
                "W-1", "O-1", -0.1m, QuantityEvidenceSelector.ForIntersection("W-2", "O-1", "cut")));
            Throws<ArgumentException>(() => QuantityExplanation.Create(
                "W-1", "Wall", "Volume", "m3", 1m, 0.8m));
            Throws<InvalidOperationException>(() => QuantityGeometryEvidenceAdapter.Create(new QuantityGeometryExplanation
            {
                ElementId = "W-1",
                GrossVolume = 1d,
                NetVolume = 1d,
                GeometryFingerprint = "   "
            }));
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
