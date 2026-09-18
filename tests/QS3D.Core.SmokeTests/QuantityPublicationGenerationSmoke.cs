using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityPublicationGenerationSmoke
    {
        internal static void Run()
        {
            RejectsMixedStandardGeneration();
            RejectsMixedEd2Generation();
        }

        private static void RejectsMixedStandardGeneration()
        {
            var path = Temp("standard");
            try
            {
                Throws<InvalidDataException>(() => XlsxQuantityExporter.Export(path, new[]
                {
                    Row("E1", "1", "P:1:D"), Row("E2", "2", "P:2:D")
                }));
                if (File.Exists(path)) throw new InvalidOperationException("Mixed-generation standard export published partial output.");
            }
            finally { TryDelete(path); }
        }

        private static void RejectsMixedEd2Generation()
        {
            var path = Temp("ed2");
            var detail = new[] { Row("E1", "1", "P:1:D") };
            var summary = Row("E1", "1", "P:2:D");
            try
            {
                Throws<InvalidDataException>(() => XlsxQuantityExporter.ExportEd2(path, detail, new[] { summary }));
                if (File.Exists(path)) throw new InvalidOperationException("Mixed-generation ED2 export published partial output.");
            }
            finally { TryDelete(path); }
        }
        private static QuantityReportRow Row(string id, string handle, string generation)
        {
            var row = new QuantityReportRow
            {
                Floor = "F", Zone = "Z", Category = "Beam", FamilyId = "FAM", FamilyName = "Family",
                ElementName = "Element", Material = "Concrete", DrawingFingerprint = "D", GenerationId = generation,
                Count = 1, GrossConcreteM3 = 1d, NetConcreteM3 = 1d, FormworkM2 = 1d, LengthM = 1d
            };
            row.ElementIds.Add(id); row.SourceHandles.Add(handle); return row;
        }

        private static string Temp(string suffix) => Path.Combine(Path.GetTempPath(), "qs3d-generation-" + suffix + "-" + Guid.NewGuid().ToString("N") + ".xlsx");
        private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }

    internal static class QuantityPublicationGenerationRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityPublicationGenerationSmoke.Run();
    }
}
