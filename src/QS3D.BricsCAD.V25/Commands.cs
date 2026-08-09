using System;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Reporting;
using QS3D.BricsCAD.V25.Ribbon;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
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
                var snapshots = Cad.EntitySnapshotReader.ReadCurrentSelection(doc);
                var rows = SnapshotQuantityAdapter.Build(snapshots, DrawingUnit.Millimeter);
                var window = new QuantitySummaryWindow(rows);
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
            });
        }

        [CommandMethod("QS3DSAVE", CommandFlags.Modal)]
        public void SaveProject()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DSAVE", () => { var path = ProjectContextCoordinator.Save(doc); PaletteCoordinator.SetStatus("Đã lưu " + path); doc.Editor.WriteMessage("\nQS3D saved: " + path); });
        }

        [CommandMethod("QS3DRELOAD", CommandFlags.Modal)]
        public void ReloadProject()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DRELOAD", () => { ProjectContextCoordinator.Reload(doc); PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Đã nạp lại project từ .qsdb"); });
        }

        [CommandMethod("QS3DREFRESH", CommandFlags.Modal)] public void Refresh() { PaletteCoordinator.RefreshAll(); Write("QS3D đã làm mới Project/Layer/Xref."); }
        [CommandMethod("QS3DTAKEOFF", CommandFlags.UsePickSet)] public void QuickTakeoff() => Capture(ElementCategory.CustomQuantity, "Quick Takeoff");
        [CommandMethod("QS3DWALL", CommandFlags.UsePickSet)] public void CaptureWall() => Capture(ElementCategory.ArchitecturalWall, "Tường KT");
        [CommandMethod("QS3DROOM", CommandFlags.UsePickSet)] public void CaptureRoom() => Capture(ElementCategory.Room, "Phòng");
        [CommandMethod("QS3DOPENING", CommandFlags.UsePickSet)] public void CaptureOpening() => Capture(ElementCategory.WallOpening, "Lỗ Mở Vách");
        [CommandMethod("QS3DDOOR", CommandFlags.UsePickSet)] public void CaptureDoor() => Capture(ElementCategory.Door, "Cửa Đi");

        [CommandMethod("QS3DFINISH", CommandFlags.UsePickSet)]
        public void GenerateFinishes()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DFINISH", () => { var count = SemanticCaptureService.GenerateRoomFinishes(doc); PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Đã tạo " + count + " cấu kiện HT_Phòng."); doc.Editor.WriteMessage("\nQS3D: generated " + count + " room finish element(s)."); });
        }

        [CommandMethod("QS3DHEALTH", CommandFlags.Modal)]
        public void Health()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DHEALTH", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var handles = project.Elements.SelectMany(x => x.SourceHandles).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var live = Cad.CadHandleService.GetLiveHandles(doc, handles);
                var summary = new HealthSummary(new ModelHealthService().Inspect(project, live));
                var text = "Model Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(text);
                doc.Editor.WriteMessage("\nQS3D " + text);
                foreach (var issue in summary.Issues.Take(20)) doc.Editor.WriteMessage("\n - [" + issue.Severity + "] " + issue.Code + " " + issue.ElementId + ": " + issue.Message);
            });
        }

        [CommandMethod("QS3DLOCATE", CommandFlags.Modal)]
        public void Locate()
        {
            var doc = Active(); if (doc == null) return;
            var options = new PromptStringOptions("\nNhập QS3D Element Id: ") { AllowSpaces = false };
            var result = doc.Editor.GetString(options); if (result.Status != PromptStatus.OK) return;
            Guard(doc, "QS3DLOCATE", () =>
            {
                var element = ProjectContextCoordinator.GetOrCreate(doc).FindElement(result.StringResult);
                if (element == null) { doc.Editor.WriteMessage("\nKhông tìm thấy QS3D element."); return; }
                var count = Cad.CadHandleService.Select(doc, element.SourceHandles);
                PaletteCoordinator.SetStatus("Locate " + element.Id + " • " + count + " CAD object");
            });
        }

        [CommandMethod("QS3DRESETUI", CommandFlags.Modal)]
        public void ResetUi() { PaletteCoordinator.Dispose(); PaletteCoordinator.EnsureCreated(); PaletteCoordinator.Show(); RibbonBootstrapper.Reset(); RibbonBootstrapper.TryInitialize(); Write("QS3D UI đã reset."); }
        [CommandMethod("QS3DSAFEMODE", CommandFlags.Modal)] public void SafeMode() { PaletteCoordinator.Dispose(); PaletteCoordinator.EnsureCreated(); PaletteCoordinator.ShowSafeMode(); Write("QS3D Safe Mode đã bật."); }
        [CommandMethod("QS3DABOUT", CommandFlags.Modal)] public void About() => Write("QS3D for BricsCAD V25 — clean-room quantity takeoff / semantic QS workspace.");

        private static void Capture(ElementCategory category, string label)
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3D " + label, () =>
            {
                var count = SemanticCaptureService.Capture(doc, category);
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus(label + ": đã ghi " + count + " cấu kiện.");
                doc.Editor.WriteMessage("\nQS3D " + label + ": " + count + " element(s).");
            });
        }

        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Write(string message) => Active()?.Editor.WriteMessage("\n" + message);
        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (Exception ex) { document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message); PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); }
        }
    }
}
