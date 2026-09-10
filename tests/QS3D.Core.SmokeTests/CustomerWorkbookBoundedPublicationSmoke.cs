using System;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CustomerWorkbookBoundedPublicationSmoke
    {
        public static void Run()
        {
            RejectsOversizedAcceptedPayloadBeforeCommit();
            Console.WriteLine("PASS customer workbook bounded publication");
        }

        private static void RejectsOversizedAcceptedPayloadBeforeCommit()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-customer-bounded-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var output = Path.Combine(root, "customer.xlsx");
            var sentinel = new byte[] { 0x51, 0x53, 0x33, 0x44 };
            File.WriteAllBytes(output, sentinel);
            try
            {
                var details = new List<QuantityReportRow>();
                var summary = BaseRow("SUMMARY", "ABC", 1200);
                summary.ElementIds.Clear();
                summary.SourceHandles.Clear();
                for (var index = 1; index <= 1200; index++)
                {
                    var id = "E" + index.ToString("D4");
                    var handle = index.ToString("X");
                    var row = BaseRow(id, handle, 1);
                    row.ElementName = new string('X', 30000);
                    row.ElementIds.Clear();
                    row.ElementIds.Add(id);
                    row.SourceHandles.Clear();
                    row.SourceHandles.Add(handle);
                    details.Add(row);
                    summary.ElementIds.Add(id);
                    summary.SourceHandles.Add(handle);
                }

                ExpectThrows<InvalidDataException>(() =>
                    QsCustomerWorkbookExporter.Export(output, details, new[] { summary }));
                var actual = File.ReadAllBytes(output);
                if (actual.Length != sentinel.Length)
                    throw new InvalidOperationException("Oversized customer workbook replaced the existing destination.");
                for (var i = 0; i < sentinel.Length; i++)
                    if (actual[i] != sentinel[i])
                        throw new InvalidOperationException("Oversized customer workbook changed the existing destination.");

                var tempPrefix = Path.GetFileName(output) + ".qs3d-tmp-";
                foreach (var file in Directory.GetFiles(root))
                    if (Path.GetFileName(file).StartsWith(tempPrefix, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Oversized customer workbook left owned temp residue.");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static QuantityReportRow BaseRow(string id, string handle, int count)
        {
            var row = new QuantityReportRow
            {
                Floor = "L01",
                Zone = "A",
                Category = "Beam",
                FamilyId = "F-BEAM",
                FamilyName = "Beam 300x600",
                ElementName = "Beam " + id,
                Material = "Concrete",
                DrawingFingerprint = "DWG-CUSTOMER-BOUND",
                Count = count,
                GrossConcreteM3 = count,
                NetConcreteM3 = count,
                HasGrossConcreteM3Evidence = true,
                HasNetConcreteM3Evidence = true
            };
            row.ElementIds.Add(id);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static void ExpectThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
