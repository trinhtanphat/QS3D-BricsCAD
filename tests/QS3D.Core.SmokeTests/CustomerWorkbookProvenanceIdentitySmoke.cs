using System;
using System.IO;
using System.Linq;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CustomerWorkbookProvenanceIdentitySmoke
    {
        internal static void Run()
        {
            RejectsInvalidFingerprintBeforeFilesystemMutation();
            RejectsInvalidElementIdWithoutReplacingDestination();
            RejectsMalformedSurrogateIdentity();
            PreservesValidUnicodeAndCanonicalHandles();
        }

        private static void RejectsInvalidFingerprintBeforeFilesystemMutation()
        {
            var root = NewRoot("fingerprint");
            var output = Path.Combine(root, "nested", "customer.xlsx");
            try
            {
                var detail = Row("DWG\u0001BAD", "E1", "A1");
                var summary = Row("DWG\u0001BAD", "E1", "A1");

                ExpectThrows<InvalidDataException>(
                    () => QsCustomerWorkbookExporter.Export(output, new[] { detail }, new[] { summary }),
                    "Customer workbook must reject XML-invalid drawing fingerprint provenance.");
                Require(!Directory.Exists(root),
                    "Invalid drawing fingerprint provenance must fail before creating the output directory.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void RejectsInvalidElementIdWithoutReplacingDestination()
        {
            var root = NewRoot("element-id");
            Directory.CreateDirectory(root);
            var output = Path.Combine(root, "customer.xlsx");
            var original = Encoding.UTF8.GetBytes("existing-customer-workbook");
            File.WriteAllBytes(output, original);
            try
            {
                var detail = Row("DWG-CUSTOMER", "E\u0002BAD", "A1");
                var summary = Row("DWG-CUSTOMER", "E\u0002BAD", "A1");

                ExpectThrows<InvalidDataException>(
                    () => QsCustomerWorkbookExporter.Export(output, new[] { detail }, new[] { summary }),
                    "Customer workbook must reject XML-invalid Element ID provenance.");
                Require(File.ReadAllBytes(output).SequenceEqual(original),
                    "Invalid Element ID provenance must not replace an existing customer workbook.");
                Require(Directory.GetFiles(root).Length == 1,
                    "Invalid Element ID provenance must not leave a temporary workbook package.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void RejectsMalformedSurrogateIdentity()
        {
            var root = NewRoot("surrogate");
            var output = Path.Combine(root, "customer.xlsx");
            try
            {
                var detail = Row("DWG-CUSTOMER", "E-\uD800", "A1");
                var summary = Row("DWG-CUSTOMER", "E-\uD800", "A1");

                ExpectThrows<InvalidDataException>(
                    () => QsCustomerWorkbookExporter.Export(output, new[] { detail }, new[] { summary }),
                    "Customer workbook must reject malformed-surrogate Element ID provenance.");
                Require(!Directory.Exists(root),
                    "Malformed-surrogate provenance must fail before filesystem mutation.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void PreservesValidUnicodeAndCanonicalHandles()
        {
            var root = NewRoot("unicode");
            Directory.CreateDirectory(root);
            var output = Path.Combine(root, "customer.xlsx");
            const string fingerprint = "DWG-工程-😀";
            const string elementId = "E-測試-😀";
            try
            {
                var detail = Row(fingerprint, elementId, "0x00a1");
                var summary = Row(fingerprint, elementId, "0x00a1");
                QsCustomerWorkbookExporter.Export(output, new[] { detail }, new[] { summary });

                var trace = QsCustomerWorkbookTraceReader.Read(output, QsCustomerWorkbookExporter.DetailSheet, 2);
                Require(trace.DrawingFingerprint == fingerprint,
                    "Valid Unicode drawing fingerprint must round-trip without replacement.");
                Require(trace.ElementIds.Count == 1 && trace.ElementIds[0] == elementId,
                    "Valid Unicode Element ID must round-trip without replacement.");
                Require(trace.Handles.Count == 1 && trace.Handles[0] == "A1",
                    "Customer workbook must preserve canonical CAD Handle behavior.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static QuantityReportRow Row(string fingerprint, string elementId, string handle)
        {
            var row = new QuantityReportRow
            {
                Floor = "L01",
                Zone = "A",
                Category = "Beam",
                FamilyId = "F-BEAM",
                FamilyName = "Beam 300x600",
                DrawingFingerprint = fingerprint,
                Count = 1
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static string NewRoot(string suffix)
        {
            return Path.Combine(Path.GetTempPath(), "qs3d-customer-provenance-" + suffix + "-" + Guid.NewGuid().ToString("N"));
        }

        private static void ExpectThrows<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception(message);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch
            {
            }
        }
    }
}
