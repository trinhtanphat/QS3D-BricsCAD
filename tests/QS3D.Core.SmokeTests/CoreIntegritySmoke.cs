using System;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using QS3D.Core.Reporting;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class CoreIntegritySmoke
    {
        public static void Run()
        {
            QsdbDeepValidationPreservesExistingFile();
            RevisionDeepValidationPreservesExistingFile();
            ZeroQuantityAddRemoveIsReported();
            RebarRejectsNonFiniteAndOverflowingValues();
            RebarNotationRejectsEmptyGroups();
            XlsxRejectsInvalidXmlTextBeforeReplace();
        }

        private static void QsdbDeepValidationPreservesExistingFile()
        {
            var directory = TempDirectory("qsdb-deep-validation");
            var path = Path.Combine(directory, "project.qsdb");
            try
            {
                var project = NewProject("Original");
                var store = new QsdbProjectStore();
                store.Save(project, path);

                project.Zones[0].Name = string.Empty;
                Throws<InvalidDataException>(() => store.Save(project, path));
                Equal("Original", store.Load(path).Name);

                project.Zones[0].Name = "Zone";
                project.Metadata[string.Empty] = "invalid-key";
                Throws<InvalidDataException>(() => store.Save(project, path));
                Equal("Original", store.Load(path).Name);
            }
            finally { DeleteDirectory(directory); }
        }

        private static void RevisionDeepValidationPreservesExistingFile()
        {
            var directory = TempDirectory("revision-deep-validation");
            var path = Path.Combine(directory, "baseline.qsrev");
            try
            {
                var store = new RevisionSnapshotStore();
                var original = Snapshot("original", "E1");
                store.Save(original, path);

                var invalid = Snapshot("invalid", "E1");
                invalid.Elements.Add(new RevisionElementSnapshot { ElementId = "e1", Category = "Beam" });
                Throws<InvalidDataException>(() => store.Save(invalid, path));
                Equal("original", store.Load(path).Id);
            }
            finally { DeleteDirectory(directory); }
        }

        private static void ZeroQuantityAddRemoveIsReported()
        {
            var withoutQuantity = Snapshot("without", "E1");
            var withZero = Snapshot("with-zero", "E1");
            withZero.Elements[0].Quantities["ZeroM3"] = 0d;
            var report = new QuantityRevisionReport();

            var added = report.Build(withoutQuantity, withZero).Single();
            Equal("ZeroM3", added.QuantityName);
            Equal("Added", added.Change);
            Near(0d, added.Before);
            Near(0d, added.After);

            var removed = report.Build(withZero, withoutQuantity).Single();
            Equal("ZeroM3", removed.QuantityName);
            Equal("Removed", removed.Change);
        }

        private static void RebarRejectsNonFiniteAndOverflowingValues()
        {
            Throws<ArgumentOutOfRangeException>(() => RebarWeight.KilogramsPerMeter(double.NaN));
            Throws<ArgumentOutOfRangeException>(() => RebarWeight.KilogramsPerMeter(double.PositiveInfinity));
            Throws<OverflowException>(() => RebarWeight.KilogramsPerMeter(double.MaxValue));
            Throws<ArgumentOutOfRangeException>(() => RebarWeight.TotalKilograms(20d, double.NaN));
            Throws<OverflowException>(() => RebarWeight.TotalKilograms(20d, double.MaxValue));

            Throws<OverflowException>(() => RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { Notation = "1D20", CuttingLengthM = double.MaxValue, LapLengthM = double.MaxValue }
            }));
            Throws<OverflowException>(() => RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { Notation = "2D20", CuttingLengthM = double.MaxValue }
            }));
        }

        private static void RebarNotationRejectsEmptyGroups()
        {
            Throws<FormatException>(() => RebarNotationParser.Parse("4D20++2D16"));
            Throws<FormatException>(() => RebarNotationParser.Parse("+4D20"));
            Throws<FormatException>(() => RebarNotationParser.Parse("4D20+"));
            Equal(2, RebarNotationParser.Parse("4D20+2D16").Count);
        }

        private static void XlsxRejectsInvalidXmlTextBeforeReplace()
        {
            var directory = TempDirectory("xlsx-xml-text");
            var quantityPath = Path.Combine(directory, "quantity.xlsx");
            var rebarPath = Path.Combine(directory, "rebar.xlsx");
            try
            {
                File.WriteAllText(quantityPath, "quantity-sentinel");
                Throws<ArgumentException>(() => XlsxQuantityExporter.Export(quantityPath, new[]
                {
                    new QuantityReportRow { Floor = "F", Category = "Beam", FamilyName = "Bad" + (char)1 + "Name", Count = 1 }
                }));
                Equal("quantity-sentinel", File.ReadAllText(quantityPath));

                File.WriteAllText(rebarPath, "rebar-sentinel");
                Throws<ArgumentException>(() => XlsxRebarScheduleExporter.Export(rebarPath, new[]
                {
                    new RebarScheduleRow { ElementId = "E1", BarMark = "Bad" + (char)1 + "Mark", Notation = "1D20", DiameterMm = 20d, Quantity = 1 }
                }));
                Equal("rebar-sentinel", File.ReadAllText(rebarPath));
            }
            finally { DeleteDirectory(directory); }
        }

        private static ProjectState NewProject(string name)
        {
            var project = new ProjectState("integrity", name);
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.ActiveZoneId = "z";
            project.ActiveFloorId = "f";
            return project;
        }

        private static RevisionSnapshot Snapshot(string id, string elementId)
        {
            var snapshot = new RevisionSnapshot { Id = id, CreatedUtc = DateTime.UtcNow };
            snapshot.Elements.Add(new RevisionElementSnapshot { ElementId = elementId, Category = "Beam" });
            return snapshot;
        }

        private static string TempDirectory(string name)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-" + name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void DeleteDirectory(string directory) { try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { } }
        private static void Near(double expected, double actual) { if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
