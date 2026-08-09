using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Reporting;
using QS3D.BricsCAD.V25.Ribbon;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using QS3D.Core.Units;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class Commands
    {
        [CommandMethod("QS3D", CommandFlags.Modal)] public void ShowWorkspace() => PaletteCoordinator.Show();
        [CommandMethod("QS3DHIDE", CommandFlags.Modal)] public void HideWorkspace() => PaletteCoordinator.Hide();
        [CommandMethod("QS3DRIBBON", CommandFlags.Modal)] public void RebuildRibbon() { RibbonBootstrapper.Reset(); Write(RibbonBootstrapper.TryInitialize() ? "QS3D Ribbon đã sẵn sàng." : "Chưa thể gắn QS3D Ribbon; palette vẫn hoạt động."); }

        [CommandMethod("QS3DINSPECT", CommandFlags.UsePickSet)]
        public void InspectSelection()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DINSPECT", () => { var snapshots = Cad.EntitySnapshotReader.ReadCurrentSelection(doc); PaletteCoordinator.SetInspection(snapshots); PaletteCoordinator.Show(); doc.Editor.WriteMessage("\nQS3D: inspected " + snapshots.Count + " object(s)."); });
        }

        [CommandMethod("QS3DBQ", CommandFlags.UsePickSet)]
        public void ShowQuantitySummary()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DBQ", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                IReadOnlyList<QuantityReportRow> rows; Action<QuantityReportRow>? locate = null;
                if (project.Elements.Count > 0)
                {
                    rows = ProjectQuantityReportBuilder.Group(project);
                    locate = row => { var handles = row.ElementIds.SelectMany(id => project.FindElement(id)?.SourceHandles ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); var count = Cad.CadHandleService.Select(doc, handles); PaletteCoordinator.SetStatus("BQ Locate: " + count + " CAD object"); };
                }
                else rows = SnapshotQuantityAdapter.Build(Cad.EntitySnapshotReader.ReadCurrentSelection(doc), DrawingUnit.Millimeter);
                Application.ShowModelessWindow(IntPtr.Zero, new QuantitySummaryWindow(rows, locate), true);
            });
        }

        [CommandMethod("QS3DSAVE", CommandFlags.Modal)] public void SaveProject() { var doc = Active(); if (doc == null) return; Guard(doc, "QS3DSAVE", () => { var path = ProjectContextCoordinator.Save(doc); PaletteCoordinator.SetStatus("Đã lưu " + path); doc.Editor.WriteMessage("\nQS3D saved: " + path); }); }
        [CommandMethod("QS3DRELOAD", CommandFlags.Modal)] public void ReloadProject() { var doc = Active(); if (doc == null) return; Guard(doc, "QS3DRELOAD", () => { ProjectContextCoordinator.Reload(doc); PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Đã nạp lại project từ .qsdb"); }); }
        [CommandMethod("QS3DREFRESH", CommandFlags.Modal)] public void Refresh() { PaletteCoordinator.RefreshAll(); Write("QS3D đã làm mới Project/Layer/Xref."); }
        [CommandMethod("QS3DTAKEOFF", CommandFlags.UsePickSet)] public void QuickTakeoff() => Capture(ElementCategory.CustomQuantity, "Quick Takeoff");

        [CommandMethod("QS3DWALL", CommandFlags.UsePickSet)]
        public void CaptureWall()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3D Tường KT", () =>
            {
                var captured = SemanticCaptureService.Capture(doc, ElementCategory.ArchitecturalWall);
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var solids = Cad.WallSolidBuilder.BuildSelectedLineWalls(doc, project);
                foreach (var wall in project.Elements.Where(x => x.Category == ElementCategory.ArchitecturalWall && x.Dirty != ElementDirtyFlags.None)) { new WallRegenerator().Regenerate(project, wall); wall.MarkClean(ElementDirtyFlags.All); }
                PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Tường KT: " + captured + " semantic • " + solids + " solid 3D từ LINE.");
                doc.Editor.WriteMessage("\nQS3D Tường KT: captured " + captured + ", created " + solids + " line-wall solid(s).");
            });
        }

        [CommandMethod("QS3DROOM", CommandFlags.UsePickSet)] public void CaptureRoom() => Capture(ElementCategory.Room, "Phòng");
        [CommandMethod("QS3DOPENING", CommandFlags.UsePickSet)] public void CaptureOpening() => Capture(ElementCategory.WallOpening, "Lỗ Mở Vách");
        [CommandMethod("QS3DDOOR", CommandFlags.UsePickSet)] public void CaptureDoor() => Capture(ElementCategory.Door, "Cửa Đi");

        [CommandMethod("QS3DLINKHOST", CommandFlags.UsePickSet)]
        public void LinkOpeningHost()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DLINKHOST", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var selectedHandles = new HashSet<string>(Cad.EntitySnapshotReader.ReadCurrentSelection(doc).Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
                var selected = project.Elements.Where(x => x.SourceHandles.Any(selectedHandles.Contains)).ToList();
                var opening = selected.FirstOrDefault(x => x.Category == ElementCategory.WallOpening || x.Category == ElementCategory.Door);
                var wall = selected.FirstOrDefault(x => x.Category == ElementCategory.ArchitecturalWall || x.Category == ElementCategory.GlassWall || x.Category == ElementCategory.WallPier);
                if (opening == null || wall == null) { doc.Editor.WriteMessage("\nChọn đồng thời 1 Tường KT và 1 Cửa/Lỗ Mở đã được QS3D capture, rồi chạy QS3DLINKHOST."); return; }
                new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
                new OpeningRegenerator().Regenerate(project, opening); opening.MarkClean(ElementDirtyFlags.All);
                new WallRegenerator().Regenerate(project, wall); wall.MarkClean(ElementDirtyFlags.All);
                project.Touch(); PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Đã link " + opening.Id + " → " + wall.Id);
            });
        }

        [CommandMethod("QS3DFINISH", CommandFlags.UsePickSet)]
        public void GenerateFinishes()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DFINISH", () => { var count = SemanticCaptureService.GenerateRoomFinishes(doc); PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Đã tạo/cập nhật " + count + " cấu kiện HT_Phòng mới."); doc.Editor.WriteMessage("\nQS3D: generated " + count + " new room finish element(s)."); });
        }

        [CommandMethod("QS3DHEALTH", CommandFlags.Modal)]
        public void Health()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DHEALTH", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc); var handles = project.Elements.SelectMany(x => x.SourceHandles).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(); var live = Cad.CadHandleService.GetLiveHandles(doc, handles); var issues = new ModelHealthService().Inspect(project, live); var summary = new HealthSummary(issues);
                var text = "Model Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin"; PaletteCoordinator.SetStatus(text); doc.Editor.WriteMessage("\nQS3D " + text);
                var window = new ModelHealthWindow(issues, issue => { var element = project.FindElement(issue.ElementId); if (element == null) return; var count = Cad.CadHandleService.Select(doc, element.SourceHandles); PaletteCoordinator.SetStatus("Health Locate " + element.Id + " • " + count + " CAD object"); }); Application.ShowModelessWindow(IntPtr.Zero, window, true);
            });
        }

        [CommandMethod("QS3DLOCATE", CommandFlags.Modal)]
        public void Locate()
        {
            var doc = Active(); if (doc == null) return; var options = new PromptStringOptions("\nNhập QS3D Element Id: ") { AllowSpaces = false }; var result = doc.Editor.GetString(options); if (result.Status != PromptStatus.OK) return;
            Guard(doc, "QS3DLOCATE", () => { var element = ProjectContextCoordinator.GetOrCreate(doc).FindElement(result.StringResult); if (element == null) { doc.Editor.WriteMessage("\nKhông tìm thấy QS3D element."); return; } var count = Cad.CadHandleService.Select(doc, element.SourceHandles); PaletteCoordinator.SetStatus("Locate " + element.Id + " • " + count + " CAD object"); });
        }

        [CommandMethod("QS3DRESETUI", CommandFlags.Modal)] public void ResetUi() { PaletteCoordinator.Dispose(); PaletteCoordinator.EnsureCreated(); PaletteCoordinator.Show(); RibbonBootstrapper.Reset(); RibbonBootstrapper.TryInitialize(); Write("QS3D UI đã reset."); }
        [CommandMethod("QS3DSAFEMODE", CommandFlags.Modal)] public void SafeMode() { PaletteCoordinator.Dispose(); PaletteCoordinator.EnsureCreated(); PaletteCoordinator.ShowSafeMode(); Write("QS3D Safe Mode đã bật."); }
        [CommandMethod("QS3DABOUT", CommandFlags.Modal)] public void About() => Write("QS3D for BricsCAD V25 — clean-room quantity takeoff / semantic QS workspace.");

        private static void Capture(ElementCategory category, string label)
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3D " + label, () => { var count = SemanticCaptureService.Capture(doc, category); PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus(label + ": đã ghi " + count + " cấu kiện."); doc.Editor.WriteMessage("\nQS3D " + label + ": " + count + " element(s)."); });
        }
        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Write(string message) => Active()?.Editor.WriteMessage("\n" + message);
        private static void Guard(Document document, string operation, Action action) { try { action(); } catch (Exception ex) { document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message); PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); } }
    }
}
