using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CustomerWorkbookGroupedHandleBindingRegressionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-smoke-customer-group-handles-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var details = new[]
                {
                    Row("E1", "A1", 1d),
                    Row("E2", "B2", 2d)
                };

                var validPath = Path.Combine(root, "valid.xlsx");
                QsCustomerWorkbookExporter.Export(
                    validPath,
                    details,
                    new[] { Row("E1", "A1", 1d), Row("E2", "B2", 2d) });
                Require(File.Exists(validPath), "Valid per-group Handle provenance must remain exportable.");

                var first = QsCustomerWorkbookTraceReader.Read(validPath, QsCustomerWorkbookExporter.DgklSheet, 2);
                Require(first.ElementIds.Count == 1 && first.ElementIds[0] == "E1" &&
                        first.Handles.Count == 1 && first.Handles[0] == "A1",
                    "First grouped row must preserve the Handle set of its semantic element.");
                var second = QsCustomerWorkbookTraceReader.Read(validPath, QsCustomerWorkbookExporter.DgklSheet, 3);
                Require(second.ElementIds.Count == 1 && second.ElementIds[0] == "E2" &&
                        second.Handles.Count == 1 && second.Handles[0] == "B2",
                    "Second grouped row must preserve the Handle set of its semantic element.");

                var swappedPath = Path.Combine(root, "swapped.xlsx");
                Expect<InvalidDataException>(
                    () => QsCustomerWorkbookExporter.Export(
                        swappedPath,
                        details,
                        new[] { Row("E1", "B2", 1d), Row("E2", "A1", 2d) }),
                    "Globally-correct but row-swapped grouped CAD Handle provenance must fail closed.");
                Require(!File.Exists(swappedPath), "Rejected grouped provenance must not commit a workbook.");
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static QuantityReportRow Row(string elementId, string handle, double gross)
        {
            var row = new QuantityReportRow
            {
                Floor = "L01",
                Zone = "A",
                Category = "Beam",
                FamilyId = "F-BEAM",
                FamilyName = "Beam 300x600",
                ElementName = "Beam " + elementId,
                Material = "Concrete",
                DrawingFingerprint = "DWG-CUSTOMER-GROUP-HANDLES",
                Count = 1,
                GrossConcreteM3 = gross,
                NetConcreteM3 = gross,
                HasGrossConcreteM3Evidence = true,
                HasNetConcreteM3Evidence = true,
                HasDeductionM3Evidence = false,
                HasFormworkM2Evidence = false,
                HasLengthMEvidence = false,
                HasOuterPerimeterMEvidence = false,
                HasInnerPerimeterMEvidence = false,
                HasDoorAreaM2Evidence = false,
                HasSideAreaM2Evidence = false,
                HasBottomAreaM2Evidence = false,
                HasTopAreaM2Evidence = false,
                HasOtherAreaM2Evidence = false
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Expect<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(message);
        }
    }
}