using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class Ed2NumericParitySmoke
    {
        public static void Run()
        {
            CanonicalNumericParityPublishes();
            NumericDriftPreservesExistingDestination();
            NullDensityAndMassRulesRemainExplicit();
            SummaryHandleSwapsFailClosed();
        }

        private static void SummaryHandleSwapsFailClosed()
        {
            var directory = TempDirectory("ed2-summary-handle-swap");
            try
            {
                var first = Detail("S1", "D1", 1d, 2400d, 2400d);
                var second = Detail("S2", "D2", 2d, 2400d, 4800d);
                var firstSummary = Aggregate(first);
                var secondSummary = Aggregate(second);
                firstSummary.SourceHandles.Clear();
                firstSummary.SourceHandles.Add("D2");
                secondSummary.SourceHandles.Clear();
                secondSummary.SourceHandles.Add("D1");
                Throws<InvalidDataException>(() => XlsxQuantityExporter.ExportEd2(
                    Path.Combine(directory, "swapped-summary-handles.xlsx"),
                    new[] { first, second },
                    new[] { firstSummary, secondSummary }));
            }
            finally { DeleteDirectory(directory); }
        }

        private static void CanonicalNumericParityPublishes()
        {
            var directory = TempDirectory("ed2-numeric-parity-pass");
            try
            {
                var details = CanonicalDetails();
                var summary = CanonicalSummary();
                var path = Path.Combine(directory, "canonical.xlsx");
                XlsxQuantityExporter.ExportEd2(path, details, new[] { summary });
                if (!File.Exists(path)) throw new Exception("Canonical ED2 numeric parity did not publish a workbook.");
            }
            finally { DeleteDirectory(directory); }
        }

        private static void NumericDriftPreservesExistingDestination()
        {
            var directory = TempDirectory("ed2-numeric-parity-refusal");
            var sentinel = Encoding.UTF8.GetBytes("existing ED2 destination");
            var mutations = new Action<QuantityReportRow>[]
            {
                row => row.Count++,
                row => row.GrossConcreteM3 += 0.5d,
                row => row.FormworkM2 += 0.5d,
                row => row.LengthM += 0.5d,
                row => row.DensityKgM3 = 2500d,
                row => row.MassKg = row.MassKg.GetValueOrDefault() + 1d,
            };

            try
            {
                for (var i = 0; i < mutations.Length; i++)
                {
                    var path = Path.Combine(directory, "existing-" + i + ".xlsx");
                    File.WriteAllBytes(path, sentinel);
                    var summary = CanonicalSummary();
                    mutations[i](summary);

                    Throws<InvalidDataException>(() =>
                        XlsxQuantityExporter.ExportEd2(path, CanonicalDetails(), new[] { summary }));
                    if (!File.ReadAllBytes(path).SequenceEqual(sentinel))
                        throw new Exception("Rejected ED2 numeric drift changed the existing destination.");
                    if (Directory.EnumerateFiles(directory, Path.GetFileName(path) + ".*.tmp").Any())
                        throw new Exception("Rejected ED2 numeric drift left a temporary publication file.");
                }
            }
            finally { DeleteDirectory(directory); }
        }

        private static void NullDensityAndMassRulesRemainExplicit()
        {
            var directory = TempDirectory("ed2-null-density-mass");
            try
            {
                var explicitFirst = Detail("X1", "C1", 1d, null, 10d);
                var explicitSecond = Detail("X2", "C2", 2d, null, 20d);
                var explicitSummary = Aggregate(explicitFirst, explicitSecond);
                if (explicitSummary.DensityKgM3.HasValue || explicitSummary.MassKg != 30d)
                    throw new Exception("Explicit mass must remain available when density is null.");
                XlsxQuantityExporter.ExportEd2(
                    Path.Combine(directory, "null-density-explicit-mass.xlsx"),
                    new[] { explicitFirst, explicitSecond },
                    new[] { explicitSummary });

                var first = Detail("N1", "B1", 1d, null, 10d);
                var second = Detail("N2", "B2", 2d, null, null);
                var summary = Aggregate(first, second);
                if (summary.DensityKgM3.HasValue || summary.MassKg.HasValue)
                    throw new Exception("Mixed null ED2 density/mass fixture is invalid.");

                XlsxQuantityExporter.ExportEd2(
                    Path.Combine(directory, "nulls.xlsx"),
                    new[] { first, second },
                    new[] { summary });

                var inventedDensity = Clone(summary);
                inventedDensity.DensityKgM3 = 2400d;
                Throws<InvalidDataException>(() => XlsxQuantityExporter.ExportEd2(
                    Path.Combine(directory, "invented-density.xlsx"),
                    new[] { first, second },
                    new[] { inventedDensity }));

                var inventedMass = Clone(summary);
                inventedMass.MassKg = 10d;
                Throws<InvalidDataException>(() => XlsxQuantityExporter.ExportEd2(
                    Path.Combine(directory, "invented-mass.xlsx"),
                    new[] { first, second },
                    new[] { inventedMass }));
            }
            finally { DeleteDirectory(directory); }
        }

        private static QuantityReportRow[] CanonicalDetails() => new[]
        {
            Detail("E1", "A1", 1d, 2400d, 2400d),
            Detail("E2", "A2", 2d, 2400d, 4800d),
        };

        private static QuantityReportRow CanonicalSummary() => Aggregate(CanonicalDetails());

        private static QuantityReportRow Detail(string elementId, string handle, double scale, double? density, double? mass)
        {
            var row = new QuantityReportRow
            {
                Floor = "Tầng 1",
                Zone = "Zone A",
                Category = "Beam",
                FamilyId = "BEAM-FAMILY",
                FamilyName = "Dầm chính",
                ElementName = "Dầm " + elementId,
                Material = "Bê tông",
                DrawingFingerprint = "DWG-ED2-PARITY",
                Count = 1,
                GrossConcreteM3 = scale,
                DeductionM3 = scale / 8d,
                NetConcreteM3 = scale * 0.875d,
                FormworkM2 = scale * 4d,
                LengthM = scale * 2d,
                OuterPerimeterM = scale * 3d,
                InnerPerimeterM = scale,
                DoorAreaM2 = scale / 4d,
                SideAreaM2 = scale * 5d,
                BottomAreaM2 = scale * 6d,
                TopAreaM2 = scale * 7d,
                OtherAreaM2 = scale * 8d,
                DensityKgM3 = density,
                MassKg = mass,
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static QuantityReportRow Aggregate(params QuantityReportRow[] details)
        {
            var first = details[0];
            var row = new QuantityReportRow
            {
                Floor = first.Floor,
                Zone = first.Zone,
                Category = first.Category,
                FamilyId = first.FamilyId,
                FamilyName = first.FamilyName,
                ElementName = first.FamilyName,
                Material = first.Material,
                DrawingFingerprint = first.DrawingFingerprint,
                Count = details.Length,
                GrossConcreteM3 = details.Sum(x => x.GrossConcreteM3),
                DeductionM3 = details.Sum(x => x.DeductionM3),
                NetConcreteM3 = details.Sum(x => x.NetConcreteM3),
                FormworkM2 = details.Sum(x => x.FormworkM2),
                LengthM = details.Sum(x => x.LengthM),
                OuterPerimeterM = details.Sum(x => x.OuterPerimeterM),
                InnerPerimeterM = details.Sum(x => x.InnerPerimeterM),
                DoorAreaM2 = details.Sum(x => x.DoorAreaM2),
                SideAreaM2 = details.Sum(x => x.SideAreaM2),
                BottomAreaM2 = details.Sum(x => x.BottomAreaM2),
                TopAreaM2 = details.Sum(x => x.TopAreaM2),
                OtherAreaM2 = details.Sum(x => x.OtherAreaM2),
                DensityKgM3 = first.DensityKgM3,
                MassKg = details.All(x => x.MassKg.HasValue) ? details.Sum(x => x.MassKg!.Value) : null,
            };
            foreach (var detail in details)
            {
                foreach (var id in detail.ElementIds) row.ElementIds.Add(id);
                foreach (var handle in detail.SourceHandles) row.SourceHandles.Add(handle);
            }
            return row;
        }

        private static QuantityReportRow Clone(QuantityReportRow source)
        {
            var clone = new QuantityReportRow
            {
                Floor = source.Floor,
                Zone = source.Zone,
                Category = source.Category,
                FamilyId = source.FamilyId,
                FamilyName = source.FamilyName,
                ElementName = source.ElementName,
                Material = source.Material,
                Note = source.Note,
                DrawingFingerprint = source.DrawingFingerprint,
                Count = source.Count,
                GrossConcreteM3 = source.GrossConcreteM3,
                DeductionM3 = source.DeductionM3,
                NetConcreteM3 = source.NetConcreteM3,
                FormworkM2 = source.FormworkM2,
                LengthM = source.LengthM,
                OuterPerimeterM = source.OuterPerimeterM,
                InnerPerimeterM = source.InnerPerimeterM,
                DoorAreaM2 = source.DoorAreaM2,
                SideAreaM2 = source.SideAreaM2,
                BottomAreaM2 = source.BottomAreaM2,
                TopAreaM2 = source.TopAreaM2,
                OtherAreaM2 = source.OtherAreaM2,
                DensityKgM3 = source.DensityKgM3,
                MassKg = source.MassKg,
            };
            foreach (var id in source.ElementIds) clone.ElementIds.Add(id);
            foreach (var handle in source.SourceHandles) clone.SourceHandles.Add(handle);
            return clone;
        }

        private static string TempDirectory(string name)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-smoke-" + name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch { }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
