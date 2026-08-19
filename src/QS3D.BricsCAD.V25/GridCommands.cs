using System;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class GridCommands
    {
        [CommandMethod("QS3DGRID", CommandFlags.UsePickSet)]
        public void CaptureGrid()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            int count;
            var subtype = ResolveActiveGridSubtype(document);
            try
            {
                var selectedIds = Cad.CadSelectionGuard.AcquireCurrentSelection(document);
                if (selectedIds.Length == 0)
                {
                    TryWriteMessage(document, "\nQS3D Grid: chưa chọn LINE/ARC trục tham chiếu.");
                    return;
                }

                var snapshots = Cad.EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                {
                    TryWriteMessage(document, "\nQS3D Grid: selection hiện tại không đọc được LINE/ARC trục tham chiếu.");
                    return;
                }

                var invalid = snapshots
                    .Where(x => !IsSupportedGridSource(x.EntityType) ||
                                !MatchesSubtype(x.EntityType, subtype) ||
                                !x.LengthDrawingUnits.HasValue ||
                                double.IsNaN(x.LengthDrawingUnits.Value) ||
                                double.IsInfinity(x.LengthDrawingUnits.Value) ||
                                !(x.LengthDrawingUnits.Value > 0d))
                    .ToArray();
                if (invalid.Length > 0)
                {
                    var kinds = string.Join(", ", invalid.Select(x => x.EntityType).Distinct(StringComparer.OrdinalIgnoreCase));
                    var expected = subtype == "Lưới Cong"
                        ? "ARC"
                        : subtype == "Lưới Thẳng" ? "LINE" : "LINE/ARC";
                    TryWriteMessage(document, "\nQS3D Grid " + (subtype.Length > 0 ? "(" + subtype + ") " : string.Empty) +
                                              ": chỉ nhận " + expected + " có chiều dài hữu hạn dương. Selection không hợp lệ: " + kinds + ".");
                    return;
                }

                count = SemanticCaptureService.Capture(document, ElementCategory.Grid);
            }
            catch (Exception ex)
            {
                ReportOperationFailure(document, "QS3DGRID lỗi: " + ex.Message);
                return;
            }

            FinalizeUi(document, count, subtype);
        }

        private static string ResolveActiveGridSubtype(Document document)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project)) return string.Empty;
            var family = ProjectFamilyActivationService.GetActive(project);
            if (family == null || family.Category != ElementCategory.Grid) return string.Empty;
            if (FamilyNameHasSubtype(family.Name, "Lưới Cong")) return "Lưới Cong";
            if (FamilyNameHasSubtype(family.Name, "Lưới Thẳng")) return "Lưới Thẳng";
            return string.Empty;
        }

        private static bool FamilyNameHasSubtype(string familyName, string subtype)
        {
            var name = (familyName ?? string.Empty).Trim();
            var prefix = (subtype ?? string.Empty).Trim();
            if (string.Equals(name, prefix, StringComparison.OrdinalIgnoreCase)) return true;
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || name.Length <= prefix.Length) return false;
            var separator = name[prefix.Length];
            return separator == '-' || separator == '_' || char.IsWhiteSpace(separator);
        }

        private static bool MatchesSubtype(string entityType, string subtype)
        {
            if (string.Equals(subtype, "Lưới Cong", StringComparison.OrdinalIgnoreCase))
                return string.Equals(entityType, "Arc", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(subtype, "Lưới Thẳng", StringComparison.OrdinalIgnoreCase))
                return string.Equals(entityType, "Line", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        private static void FinalizeUi(Document document, int count, string subtype)
        {
            var label = subtype.Length > 0 ? subtype : "Grid/Trục";
            var status = label + ": đã capture " + count + " semantic reference(s). Grid hiện là reference/takeoff semantic, không sinh native 3D.";
            try
            {
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                TryWriteMessage(document, "\nQS3D " + status + " UI sync warning: " + ex.Message);
            }
        }

        private static void ReportOperationFailure(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            TryWriteMessage(document, "\n" + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }

        private static bool IsSupportedGridSource(string entityType) =>
            string.Equals(entityType, "Line", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entityType, "Arc", StringComparison.OrdinalIgnoreCase);
    }
}
