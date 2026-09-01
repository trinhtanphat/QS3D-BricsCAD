using System;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class QsWorkbookTemplateTracePackageBoundSmoke
    {
        internal static void Run()
        {
            OversizedWorkbookFailsAtPackageAdmission();
        }

        private static void OversizedWorkbookFailsAtPackageAdmission()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-template-trace-bound-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var path = Path.Combine(root, "oversized.xlsx");
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.WriteByte(0x50);
                    stream.WriteByte(0x4B);
                    stream.SetLength(128L * 1024L * 1024L + 1L);
                }

                var definition = new QsWorkbookTemplateDefinition(
                    "QTO",
                    2,
                    new List<QsWorkbookTemplateMapping>
                    {
                        new QsWorkbookTemplateMapping(QsWorkbookTemplateField.DrawingFingerprint, "A"),
                        new QsWorkbookTemplateMapping(QsWorkbookTemplateField.ElementIds, "B"),
                        new QsWorkbookTemplateMapping(QsWorkbookTemplateField.SourceHandles, "C"),
                        new QsWorkbookTemplateMapping(QsWorkbookTemplateField.TraceKey, "D")
                    });

                var rejectedAtAdmission = false;
                try
                {
                    QsWorkbookTemplateTraceReader.Read(path, definition, 2);
                }
                catch (InvalidDataException ex)
                {
                    rejectedAtAdmission = string.Equals(
                        ex.Message,
                        "XLSX template workbook is too large for bounded processing.",
                        StringComparison.Ordinal);
                }

                if (!rejectedAtAdmission)
                    throw new Exception("Oversized template trace workbook must fail at the shared package-size admission before ZIP parsing.");
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }
    }
}
