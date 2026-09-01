using System;
using System.Collections.Generic;
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
            catch (Exception)
            {
                ReportOperationFailure(document, "QS3DGRID lỗi: không thể hoàn tất Grid capture.");
                return;
            }

            FinalizeUi(document, count, subtype);
        }

        [CommandMethod("QS3DGRIDINTERSECTIONS")]
        public void RefreshAllIntersectionMarkers()
        {
            RefreshIntersectionMarkers(false);
        }

        [CommandMethod("QS3DGRIDINTERSECTIONSSEL", CommandFlags.UsePickSet)]
        public void RefreshSelectedIntersectionMarkers()
        {
            RefreshIntersectionMarkers(true);
        }

        [CommandMethod("QS3DGRIDINTERSECTIONHEALTH")]
        public void InspectIntersectionMarkers()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    TryWriteMessage(document, "\nQS3D Grid intersections: chưa có project sidecar để kiểm tra.");
                    return;
                }
                var issues = Cad.GridIntersectionMarkerService.Inspect(document, project);
                var status = issues.Count == 0
                    ? "Grid intersections: native marker health OK."
                    : "Grid intersections: " + issues.Count + " health issue(s). " + string.Join(" | ", issues.Take(5));
                try { PaletteCoordinator.SetStatus(status); } catch { }
                TryWriteMessage(document, "\nQS3D " + status);
            }
            catch (Exception)
            {
                ReportOperationFailure(document, "QS3DGRIDINTERSECTIONHEALTH lỗi: không thể kiểm tra Grid intersection markers.");
            }
        }

        private static void RefreshIntersectionMarkers(bool selectedOnly)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    TryWriteMessage(document, "\nQS3D Grid intersections: chưa có project sidecar. Capture Grid và lưu project trước.");
                    return;
                }

                IReadOnlyCollection<string>? targets = null;
                if (selectedOnly)
                {
                    var snapshots = Cad.EntitySnapshotReader.ReadCurrentSelection(document);
                    if (snapshots.Count == 0)
                    {
                        TryWriteMessage(document, "\nQS3D Grid intersections: chưa chọn native Grid source.");
                        return;
                    }
                    var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
                    var selected = previewProject.Elements
                        .Where(x => x.Category == ElementCategory.Grid && x.SourceHandles.Any(handles.Contains))
                        .Select(x => x.Id)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    if (selected.Length == 0)
                    {
                        TryWriteMessage(document, "\nQS3D Grid intersections: selection không map tới semantic Grid hiện hành.");
                        return;
                    }
                    targets = selected;
                }

                var project = ProjectContextCoordinator.GetOrCreate(document);
                if (!string.Equals(project.ProjectId, previewProject.ProjectId, StringComparison.Ordinal) ||
                    project.ChangeVersion != previewProject.ChangeVersion)
                    throw new InvalidOperationException("Grid intersection project changed after preview/selection; rerun the command.");

                var count = Cad.GridIntersectionMarkerService.Refresh(document, project, targets);
                var status = "Grid intersections: đã materialize " + count + " pair-owned marker(s)" + (selectedOnly ? " cho selection." : ".");
                try
                {
                    PaletteCoordinator.RefreshProject();
                    PaletteCoordinator.SetStatus(status);
                }
                catch { }
                TryWriteMessage(document, "\nQS3D " + status);
            }
            catch (Exception)
            {
                ReportOperationFailure(
                    document,
                    selectedOnly
                        ? "QS3DGRIDINTERSECTIONSSEL lỗi: không thể refresh Grid intersection markers cho selection."
                        : "QS3DGRIDINTERSECTIONS lỗi: không thể refresh Grid intersection markers.");
            }
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
            var status = label + ": đã capture " + count + " semantic reference(s). Grid hiện là reference/takeoff semantic, không sinh native 3D; chạy QS3DGRIDINTERSECTIONS để materialize giao điểm pair-owned.";
            var uiSyncFailed = false;
            try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }
            try { PaletteCoordinator.SetStatus(status); } catch { uiSyncFailed = true; }
            TryWriteMessage(document, "\nQS3D " + status);
            if (uiSyncFailed)
                TryWriteMessage(document, "\nQS3D Grid: semantic capture đã hoàn tất; một phần UI không thể đồng bộ.");
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
