using System;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Reporting;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class QuantitySettingsDiagnosticCommands
    {
        private const int DetailLimit = 20;

        [CommandMethod("QS3DQSETTINGSHEALTH", CommandFlags.Modal)]
        public void ShowQuantitySettingsHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var settings = new QuantitySettingsStore().Load();
                var diagnostics = QuantityCalculationMatrixDiagnostics.Analyze(settings);

                WriteLine(document,
                    "Quantity Settings Health: " + diagnostics.ObservedCategoryCodes.Count + " mã cấu kiện • " +
                    diagnostics.ExistingDirectedRuleCount + "/" + diagnostics.ExpectedDirectedRuleCount + " luật có hướng • " +
                    diagnostics.MissingDirectedPairs.Count + " thiếu • " +
                    diagnostics.IntersectionOnlyCategoryCodes.Count + " mã chỉ có trong giao cắt • " +
                    diagnostics.UnreferencedCategoryRuleCodes.Count + " mã chưa được tham chiếu.");

                if (diagnostics.IsCompleteDirectedMatrix)
                    WriteLine(document, "  Ma trận luật giao cắt đầy đủ theo tập mã đang quan sát.");

                WriteCodes(
                    document,
                    "Mã chỉ có trong luật giao cắt",
                    diagnostics.IntersectionOnlyCategoryCodes);
                WriteCodes(
                    document,
                    "Mã CategoryRules chưa được luật giao cắt tham chiếu",
                    diagnostics.UnreferencedCategoryRuleCodes);

                if (diagnostics.MissingDirectedPairs.Count > 0)
                {
                    WriteLine(document, "  Luật có hướng còn thiếu (tối đa " + DetailLimit + "):");
                    foreach (var pair in diagnostics.MissingDirectedPairs.Take(DetailLimit))
                        WriteLine(document, "    " + pair.SourceCode + " -> " + pair.TargetCode);
                    if (diagnostics.MissingDirectedPairs.Count > DetailLimit)
                        WriteLine(document, "    … còn " + (diagnostics.MissingDirectedPairs.Count - DetailLimit) + " cặp thiếu.");
                }
            }
            catch (System.Exception ex)
            {
                WriteLine(document, "QS3DQSETTINGSHEALTH lỗi: " + ex.Message);
            }
        }

        private static void WriteCodes(Document document, string label, System.Collections.Generic.IReadOnlyList<int> codes)
        {
            if (codes.Count == 0) return;
            var visible = codes.Take(DetailLimit).Select(x => x.ToString()).ToArray();
            var suffix = codes.Count > DetailLimit ? " … +" + (codes.Count - DetailLimit) : string.Empty;
            WriteLine(document, "  " + label + ": " + string.Join(", ", visible) + suffix);
        }

        private static void WriteLine(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); }
            catch { }
        }
    }
}
