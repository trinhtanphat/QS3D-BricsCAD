using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxExporterRowBoundSmoke
    {
        public static void Run()
        {
            VerifyRejectsBeforeInspection<MaterialUsageRow>(MaterialUsageXlsxExporter.Export, "Material XLSX");
            VerifyRejectsBeforeInspection<DoorOpeningScheduleRow>(DoorOpeningXlsxExporter.Export, "Door/opening XLSX");
            VerifyRejectsBeforeInspection<CurtainWallScheduleRow>(CurtainWallXlsxExporter.Export, "Curtain XLSX");
        }

        private static void VerifyRejectsBeforeInspection<T>(Action<string, IReadOnlyList<T>> export, string label)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-xlsx-row-bound-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "out.xlsx");
            try
            {
                try
                {
                    export(path, new OversizedRows<T>());
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    if (!string.Equals(ex.ParamName, "rows", StringComparison.Ordinal))
                        throw new Exception(label + " must reject the oversized row list itself.", ex);
                    if (Directory.Exists(directory))
                        throw new Exception(label + " must reject oversized rows before creating the output directory.");
                    return;
                }
                catch (Exception ex)
                {
                    throw new Exception(label + " must reject oversized rows with ArgumentOutOfRangeException before inspecting any row. Received " + ex.GetType().Name + ".", ex);
                }

                throw new Exception(label + " accepted a data-row count that exceeds one worksheet after reserving the header row.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private sealed class OversizedRows<T> : IReadOnlyList<T>
        {
            public int Count { get { return 1048576; } }

            public T this[int index]
            {
                get { throw new InvalidOperationException("Oversized XLSX rows must be rejected before the exporter indexes the list."); }
            }

            public IEnumerator<T> GetEnumerator()
            {
                throw new InvalidOperationException("Oversized XLSX rows must be rejected before the exporter enumerates the list.");
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
