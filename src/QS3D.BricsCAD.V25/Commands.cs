using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Reporting;
using QS3D.BricsCAD.V25.Ribbon;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Model;
using QS3D.Core.Rebar;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
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
            Guard(doc, "QS3DINSPECT", () =>
            {
                var snapshots = Cad.EntitySnapshotReader.ReadCurrentSelection(doc);
                PaletteCoordinator.SetInspection(snapshots);
                PaletteCoordinator.Show();
                doc.Editor.WriteMessage("\nQS3D: inspected " + snapshots.Count + " object(s).");
            });
        }

        [CommandMethod("QS3DBQ", CommandFlags.UsePickSet)]
        public void ShowQuantitySummary()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DBQ", () =>
            {
                Func<IReadOnlyList<QuantityReportRow>> recalculate = () =>
                {
                    var project = ProjectContextCoordinator.GetOrCreate(doc);
                    var regenerated = RegenerateProject(project);
                    if (regenerated > 0) PaletteCoordinator.SetStatus("BQ: đã regenerate " + regenerated + " lượt cấu kiện trước khi tổng hợp.");
                    if (project.Elements.Count > 0) return ProjectQuantityReportBuilder.Group(project);
                    var unit = Cad.CadUnitService.GetDrawingUnit(doc);
                    if (Cad.CadUnitService.IsAssumedMillimeter(doc)) PaletteCoordinator.SetStatus("BQ: INSUNITS chưa hỗ trợ/không xác định, tạm dùng millimeter.");
                    var snapshotRows = SnapshotQuantityAdapter.Build(Cad.EntitySnapshotReader.ReadCurrentSelection(doc), unit);
                    foreach (var snapshotRow in snapshotRows) snapshotRow.DrawingFingerprint = project.DrawingFingerprint;
                    return snapshotRows;
                };

                Action<QuantityReportRow> locate = row =>
                {
                    var project = ProjectContextCoordinator.GetOrCreate(doc);
                    var handles = SourceHandleResolver.Resolve(project, row.ElementIds);
                    if (handles.Count == 0) { PaletteCoordinator.SetStatus("BQ Định vị: dòng này chưa có semantic handle để chọn trong CAD."); return; }
                    var count = Cad.CadHandleService.Select(doc, handles);
                    PaletteCoordinator.SetStatus("BQ Định vị: " + count + " đối tượng CAD");
                    if (count > 0) doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                };

                var rows = recalculate();
                Application.ShowModelessWindow(IntPtr.Zero, new QuantitySummaryWindow(doc, rows, locate, recalculate), true);
            });
        }

        [CommandMethod("QS3DED2", CommandFlags.UsePickSet)]
        public void ExportEd2Workflow()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DED2", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                if (project.Elements.Count == 0)
                    throw new InvalidOperationException("ED2 chưa có semantic element để xuất. Chạy QS3DB4D/capture trước.");

                var implied = Cad.EntitySnapshotReader.ReadImpliedSelection(doc);
                var defaultScope = implied.Count > 0 ? "Selection" : "All";
                var scopePrompt = doc.Editor.GetKeywords(
                    "\nPhạm vi ED2 [Selection/Floor/Zone/All] <" + defaultScope + ">: ",
                    "Selection Floor Zone All");
                if (scopePrompt.Status != PromptStatus.OK && scopePrompt.Status != PromptStatus.None) return;
                var scope = scopePrompt.Status == PromptStatus.None ? defaultScope : scopePrompt.StringResult;

                IReadOnlyList<string>? elementIds = null;
                if (string.Equals(scope, "Selection", StringComparison.OrdinalIgnoreCase))
                {
                    var snapshots = implied.Count > 0 ? implied : Cad.EntitySnapshotReader.ReadCurrentSelection(doc);
                    elementIds = ResolveEd2Selection(project, snapshots);
                }
                else if (string.Equals(scope, "Floor", StringComparison.OrdinalIgnoreCase))
                {
                    var floor = project.FindFloor(project.ActiveFloorId) ?? throw new InvalidOperationException("ED2 Floor cần một Floor/Level active hợp lệ.");
                    elementIds = project.Elements
                        .Where(x => string.Equals(x.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.Id)
                        .ToList();
                }
                else if (string.Equals(scope, "Zone", StringComparison.OrdinalIgnoreCase))
                {
                    var zone = project.FindZone(project.ActiveZoneId) ?? throw new InvalidOperationException("ED2 Zone cần một Zone active hợp lệ.");
                    elementIds = project.Elements
                        .Where(x => string.Equals(x.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.Id)
                        .ToList();
                }
                else if (!string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("ED2 scope không được hỗ trợ: " + scope + ".");
                }

                var regenerated = RegenerateProject(project);
                var details = elementIds == null
                    ? ProjectQuantityReportBuilder.Detail(project)
                    : ProjectQuantityReportBuilder.Detail(project, elementIds);
                var summary = elementIds == null
                    ? ProjectQuantityReportBuilder.Group(project)
                    : ProjectQuantityReportBuilder.Group(project, elementIds);
                if (details.Count == 0)
                    throw new InvalidOperationException("ED2 scope " + scope + " không có cấu kiện hợp lệ để xuất.");

                var drawingName = string.IsNullOrWhiteSpace(doc.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(doc.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "ED2 • Xuất CHI_TIET / TONG_HOP",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-ED2.xlsx"
                };
                if (dialog.ShowDialog() != true) return;
                XlsxQuantityExporter.ExportEd2(dialog.FileName, details, summary);

                var status = "ED2 " + scope + ": " + details.Count + " CHI_TIET • " + summary.Count +
                             " TONG_HOP • regenerate " + regenerated + " • " + dialog.FileName;
                PaletteCoordinator.SetStatus(status);
                doc.Editor.WriteMessage("\nQS3D " + status +
                    "\nDùng QS3DEXCELLOCATE với số dòng trong sheet CHI_TIET để định vị ngược theo Handle.");
            });
        }

        [CommandMethod("QS3DBBS", CommandFlags.Modal)]
        public void ExportBbs()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DBBS", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                RegenerateProject(project);
                var rows = ProjectRebarScheduleBuilder.Build(project);
                if (rows.Count == 0) { doc.Editor.WriteMessage("\nQS3D BBS: chưa có cấu kiện nào khai báo RebarNotation."); return; }
                var totalWeight = 0d;
                foreach (var row in rows)
                {
                    if (row == null) throw new InvalidOperationException("BBS không được chứa dòng null.");
                    totalWeight = QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS command total weight");
                }
                var drawingName = string.IsNullOrWhiteSpace(doc.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(doc.Name);
                var dialog = new SaveFileDialog { Title = "Xuất Bar Bending Schedule", Filter = "Excel Workbook (*.xlsx)|*.xlsx", DefaultExt = ".xlsx", AddExtension = true, OverwritePrompt = true, FileName = drawingName + "-BBS.xlsx" };
                if (dialog.ShowDialog() != true) return;
                XlsxRebarScheduleExporter.Export(dialog.FileName, rows);
                var status = "BBS: " + rows.Count + " bar mark • " + totalWeight.ToString("0.###") + " kg • " + dialog.FileName;
                PaletteCoordinator.SetStatus(status); doc.Editor.WriteMessage("\nQS3D " + status);
            });
        }

        [CommandMethod("QS3DREGEN", CommandFlags.Modal)]
        public void Regenerate()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DREGEN", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var count = RegenerateProject(project);
                PaletteCoordinator.RefreshProject();
                var message = count == 0 ? "QS3D: không có cấu kiện dirty cần regenerate." : "QS3D: đã regenerate " + count + " lượt cấu kiện.";
                PaletteCoordinator.SetStatus(message); doc.Editor.WriteMessage("\n" + message);
            });
        }

        [CommandMethod("QS3DSAVE", CommandFlags.Modal)] public void SaveProject() { var doc = Active(); if (doc == null) return; Guard(doc, "QS3DSAVE", () => { var path = ProjectContextCoordinator.Save(doc); PaletteCoordinator.SetStatus("Đã lưu " + path); doc.Editor.WriteMessage("\nQS3D saved: " + path); }); }
        [CommandMethod("QS3DRELOAD", CommandFlags.Modal)] public void ReloadProject() { var doc = Active(); if (doc == null) return; Guard(doc, "QS3DRELOAD", () => { ProjectContextCoordinator.Reload(doc); PaletteCoordinator.RefreshProject(); PaletteCoordinator.SetStatus("Đã nạp lại project từ .qsdb"); }); }
        [CommandMethod("QS3DREFRESH", CommandFlags.Modal)]
        public void Refresh()
        {
            var doc = Active(); if (doc == null) { PaletteCoordinator.RefreshAll(); return; }
            Guard(doc, "QS3DREFRESH", () =>
            {
                var count = RegenerateProject(ProjectContextCoordinator.GetOrCreate(doc));
                PaletteCoordinator.RefreshAll();
                doc.Editor.WriteMessage("\nQS3D đã làm mới Project/Layer/Xref" + (count > 0 ? " và regenerate " + count + " lượt cấu kiện." : "."));
            });
        }

        [CommandMethod("QS3DTAKEOFF", CommandFlags.UsePickSet)] public void QuickTakeoff() => Capture(ElementCategory.CustomQuantity, "Quick Takeoff");

        [CommandMethod("QS3DWALL", CommandFlags.UsePickSet)]
        public void CaptureWall()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3D Tường KT", () =>
            {
                // BLT-style compatibility workflow is deliberately two-step:
                // capture the reference first, let the user review/edit Family/Instance parameters,
                // then commit/rebuild native Solid3d explicitly with QS3DBUILD3D.
                // Direct Draw remains the one-shot source -> semantic -> native 3D authoring path.
                var captured = SemanticCaptureService.Capture(doc, ElementCategory.ArchitecturalWall);
                PaletteCoordinator.RefreshProject();
                var status = captured > 0
                    ? "Tường KT: đã capture " + captured + " semantic. Chỉnh Family/Instance (bề dày, chiều cao, offset) rồi chạy QS3DBUILD3D."
                    : "Tường KT: chưa capture được semantic nào; chọn LINE/open POLYLINE tham chiếu rồi chạy lại.";
                PaletteCoordinator.SetStatus(status);
                doc.Editor.WriteMessage("\nQS3D " + status);
            });
        }

        [CommandMethod("QS3DROOM", CommandFlags.UsePickSet)] public void CaptureRoom() => Capture(ElementCategory.Room, "Phòng");
        [CommandMethod("QS3DOPENING", CommandFlags.UsePickSet)] public void CaptureOpening() => Capture(ElementCategory.WallOpening, "Lỗ Mở Vách");
        [CommandMethod("QS3DDOOR", CommandFlags.UsePickSet)] public void CaptureDoor() => Capture(ElementCategory.Door, "Cửa Đi");
        [CommandMethod("QS3DBEAM", CommandFlags.UsePickSet)] public void CaptureBeam() => Capture(ElementCategory.Beam, "Dầm");
        [CommandMethod("QS3DSLAB", CommandFlags.UsePickSet)] public void CaptureSlab() => Capture(ElementCategory.Slab, "Sàn");
        [CommandMethod("QS3DCOLUMN", CommandFlags.UsePickSet)] public void CaptureColumn() => Capture(ElementCategory.Column, "Cột");
        [CommandMethod("QS3DSTRUCTWALL", CommandFlags.UsePickSet)] public void CaptureStructuralWall() => Capture(ElementCategory.StructuralWall, "Vách BTCT");
        [CommandMethod("QS3DFOUNDATION", CommandFlags.UsePickSet)] public void CaptureFoundation() => Capture(ElementCategory.Foundation, "Móng");
        [CommandMethod("QS3DSTAIR", CommandFlags.UsePickSet)] public void CaptureStair() => Capture(ElementCategory.Stair, "Cầu thang");
        [CommandMethod("QS3DRAILING", CommandFlags.UsePickSet)] public void CaptureRailing() => Capture(ElementCategory.Railing, "Lan can");
        [CommandMethod("QS3DEARTHWORK", CommandFlags.UsePickSet)] public void CaptureEarthwork() => Capture(ElementCategory.Earthwork, "Đào đất");

        [CommandMethod("QS3DLINKHOST", CommandFlags.UsePickSet)]
        public void LinkOpeningHost()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DLINKHOST", () =>
            {
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var selectedHandles = new HashSet<string>(
                    Cad.EntitySnapshotReader.ReadCurrentSelection(doc).Select(x => x.Handle),
                    StringComparer.OrdinalIgnoreCase);
                var selected = project.Elements
                    .Where(x => SemanticReferenceHandles.MatchesSelection(x, selectedHandles))
                    .ToList();
                var openings = selected
                    .Where(x => x.Category == ElementCategory.WallOpening || x.Category == ElementCategory.Door)
                    .ToList();
                var hosts = selected
                    .Where(x => x.Category == ElementCategory.ArchitecturalWall ||
                                x.Category == ElementCategory.GlassWall ||
                                x.Category == ElementCategory.WallPier ||
                                x.Category == ElementCategory.StructuralWall)
                    .ToList();

                if (openings.Count != 1 || hosts.Count != 1)
                {
                    var selectionStatus = "QS3DLINKHOST: cần đúng 1 Cửa/Lỗ Mở và đúng 1 tường/vách host; nhận " +
                                          openings.Count + " opening, " + hosts.Count + " host.";
                    PaletteCoordinator.SetStatus(selectionStatus);
                    doc.Editor.WriteMessage("\nQS3D " + selectionStatus);
                    return;
                }

                var opening = openings[0];
                var wall = hosts[0];
                var rollback = QS3D.Core.Persistence.ProjectStateSnapshot.Capture(project);
                var regenerated = 0;
                try
                {
                    new HostLinkService().LinkOpening(project, opening.Id, wall.Id);
                    regenerated = RegenerateProject(project);
                    if (!opening.Properties.TryGetValue("HostWallId", out var persistedHostId) ||
                        !string.Equals(persistedHostId, wall.Id, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("QS3DLINKHOST không lưu đúng HostWallId cho opening " + opening.Id + ".");
                    project.Touch();
                }
                catch (System.Exception operationError)
                {
                    try { rollback.Restore(project); }
                    catch (System.Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "QS3DLINKHOST thất bại và rollback project cũng không hoàn tất.",
                            new AggregateException(operationError, restoreError));
                    }
                    throw;
                }

                try
                {
                    PaletteCoordinator.RefreshProject();
                    doc.Editor.Regen();
                    var status = "Đã link " + opening.Id + " → " + wall.Id + " • regenerate " + regenerated + ".";
                    PaletteCoordinator.SetStatus(status);
                    doc.Editor.WriteMessage("\nQS3D " + status);
                }
                catch (System.Exception uiError)
                {
                    try { doc.Editor.WriteMessage("\nQS3D link host đã commit; UI sync warning: " + uiError.Message); }
                    catch { }
                }
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
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var sourceHandles = project.Elements
                    .SelectMany(x => x.SourceHandles)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var generatedHandles = GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project);
                var liveSources = Cad.CadHandleService.GetLiveHandles(doc, sourceHandles);
                var liveGeneratedSolids = Cad.CadHandleService.GetLiveSolidHandles(doc, generatedHandles);
                var issues = new ComprehensiveModelHealthService().Inspect(project, liveSources, liveGeneratedSolids).ToList();
                issues.AddRange(Cad.GeneratedSolidRuntimeHealthService.Inspect(doc, project));
                var summary = new HealthSummary(issues);
                var text = "Model Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(text); doc.Editor.WriteMessage("\nQS3D " + text);
                var window = new ModelHealthWindow(doc, issues, issue =>
                {
                    var element = project.FindElement(issue.ElementId); if (element == null) return;
                    var generatedTarget = ComprehensiveModelHealthService.TargetsGeneratedOutput(issue);
                    IEnumerable<string> locateHandles = SemanticReferenceHandles.Get(element);
                    if (generatedTarget)
                    {
                        var generated = GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element)
                            .Select(x => x.Key)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray();
                        if (generated.Length > 0) locateHandles = generated;
                    }
                    var count = Cad.CadHandleService.Select(doc, locateHandles);
                    var usedSourceFallback = false;
                    if (count == 0 && generatedTarget)
                    {
                        count = Cad.CadHandleService.Select(doc, SourceHandleResolver.Resolve(project, new[] { element.Id }));
                        usedSourceFallback = count > 0;
                    }
                    PaletteCoordinator.SetStatus("Health Định vị " + element.Id + " • " + count + " đối tượng CAD" + (usedSourceFallback ? " • nguồn semantic" : string.Empty));
                    if (count > 0) doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                });
                Application.ShowModelessWindow(IntPtr.Zero, window, true);
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
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                var element = project.FindElement(result.StringResult);
                if (element == null) { doc.Editor.WriteMessage("\nKhông tìm thấy QS3D element."); return; }
                var count = Cad.CadHandleService.Select(doc, SourceHandleResolver.Resolve(project, new[] { element.Id }));
                if (count > 0)
                {
                    PaletteCoordinator.SetStatus("Locate " + element.Id + " • " + count + " CAD object");
                    doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                    return;
                }
                var status = "Locate " + element.Id + ": không tìm thấy CAD object còn sống; giữ nguyên selection hiện tại.";
                PaletteCoordinator.SetStatus(status);
                doc.Editor.WriteMessage("\nQS3D " + status);
            });
        }

        [CommandMethod("QS3DEXCELLOCATE", CommandFlags.Modal)]
        public void LocateFromExcel()
        {
            var doc = Active(); if (doc == null) return;
            Guard(doc, "QS3DEXCELLOCATE", () =>
            {
                var dialog = new OpenFileDialog { Title = "Chọn bảng Excel QS3D/BLT để định vị", Filter = "Excel Workbook (*.xlsx)|*.xlsx", CheckFileExists = true, Multiselect = false };
                if (dialog.ShowDialog() != true) return;
                var prompt = new PromptIntegerOptions("\nNhập số dòng Excel cần định vị: ") { AllowNone = false, LowerLimit = 1, UseDefaultValue = true, DefaultValue = 2 };
                var row = doc.Editor.GetInteger(prompt); if (row.Status != PromptStatus.OK) return;
                var lookup = XlsxHandleReader.ReadHandleLookup(dialog.FileName, row.Value);
                var project = ProjectContextCoordinator.GetOrCreate(doc);
                if (!string.IsNullOrWhiteSpace(lookup.DrawingFingerprint) &&
                    !string.Equals(lookup.DrawingFingerprint, project.DrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Excel drawing fingerprint does not match the active DWG. Workbook=" + lookup.DrawingFingerprint +
                        ", current=" + project.DrawingFingerprint + ".");
                if (string.IsNullOrWhiteSpace(lookup.DrawingFingerprint))
                {
                    if (!lookup.UsesLegacyDecimalHandles)
                        throw new InvalidOperationException("Only a legacy BLT $decimal Handle row may omit the DWG fingerprint.");
                    var warning = "\nLegacy BLT row has no DWG fingerprint. Type YES to locate these Handles in the active drawing: ";
                    var confirmation = doc.Editor.GetString(new PromptStringOptions(warning) { AllowSpaces = false });
                    if (confirmation.Status != PromptStatus.OK || !string.Equals(confirmation.StringResult?.Trim(), "YES", StringComparison.OrdinalIgnoreCase))
                    {
                        doc.Editor.WriteMessage("\nQS3D Excel Locate cancelled; no CAD selection was changed.");
                        return;
                    }
                }

                IReadOnlyList<string> handles = lookup.Handles;
                if (lookup.ElementIds.Count > 0)
                {
                    foreach (var elementId in lookup.ElementIds)
                        if (project.FindElement(elementId) == null)
                            throw new InvalidOperationException("Excel references unknown QS3D Element ID: " + elementId + ".");
                    var projectHandles = SourceHandleResolver.Resolve(project, lookup.ElementIds)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var excelHandles = lookup.Handles
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (!excelHandles.SequenceEqual(projectHandles, StringComparer.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Excel Element ID ↔ CAD Handle provenance does not match the active QS3D project.");
                    handles = projectHandles;
                }
                if (handles.Count == 0) { doc.Editor.WriteMessage("\nQS3D: dòng Excel không có Element ID/CAD Handle hợp lệ."); return; }

                var resolved = Cad.CadHandleService.Resolve(doc, handles);
                if (resolved.Count != handles.Count)
                    throw new InvalidOperationException(
                        "Excel Locate resolved only " + resolved.Count + "/" + handles.Count +
                        " Handle(s). Selection was not changed; repair stale/missing CAD provenance first.");
                doc.Editor.SetImpliedSelection(resolved.ToArray());
                var count = resolved.Count;
                PaletteCoordinator.SetStatus("Excel dòng " + row.Value + ": " + handles.Count + " Handle • " + count + " đối tượng CAD");
                doc.Editor.WriteMessage("\nQS3D Excel Locate: resolved " + count + "/" + handles.Count + " handle(s).");
                if (count > 0) doc.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
            });
        }

        private static IReadOnlyList<string> ResolveEd2Selection(ProjectState project, IReadOnlyList<EntitySnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
                throw new InvalidOperationException("ED2 Selection cần ít nhất một CAD object đã được QS3D theo dõi.");

            var handles = new HashSet<string>(
                snapshots.Select(x => (x.Handle ?? string.Empty).Trim()).Where(x => x.Length > 0),
                StringComparer.OrdinalIgnoreCase);
            var elements = project.Elements
                .Where(x => SemanticReferenceHandles.MatchesSelection(x, handles))
                .ToList();
            if (elements.Count == 0)
                throw new InvalidOperationException("ED2 Selection không khớp semantic element nào; chạy QS3DB4D/capture trước.");

            var aliases = new HashSet<string>(
                elements.SelectMany(SemanticReferenceHandles.GetSelectionAliases),
                StringComparer.OrdinalIgnoreCase);
            var untracked = handles
                .Where(x => !aliases.Contains(x))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (untracked.Count > 0)
                throw new InvalidOperationException(
                    "ED2 Selection trộn CAD object chưa thuộc semantic scope: " + string.Join(", ", untracked) + ".");

            return elements
                .Select(x => x.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        [CommandMethod("QS3DRESETUI", CommandFlags.Modal)] public void ResetUi() { PaletteCoordinator.Dispose(); PaletteCoordinator.EnsureCreated(); PaletteCoordinator.Show(); RibbonBootstrapper.Reset(); RibbonBootstrapper.TryInitialize(); Write("QS3D UI đã reset."); }
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

        private static int RegenerateProject(ProjectState project) => new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
        private static Document? Active() => Application.DocumentManager.MdiActiveDocument;
        private static void Write(string message) => Active()?.Editor.WriteMessage("\n" + message);
        private static void Guard(Document document, string operation, Action action) { try { action(); } catch (System.Exception ex) { document.Editor.WriteMessage("\n" + operation + " error: " + ex.Message); PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); } }
    }
}
