using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Reporting;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel : UserControl
    {
        private readonly QuantityInsightViewModel _viewModel = new QuantityInsightViewModel();
        private IReadOnlyList<EntitySnapshot> _selectionSnapshots = Array.Empty<EntitySnapshot>();
        private Dictionary<QuantityInsightItemViewModel, QuantityReportRow> _rowSnapshots = new Dictionary<QuantityInsightItemViewModel, QuantityReportRow>();
        private Document? _boundDocument;
        private string _boundProjectId = string.Empty;
        private string _boundDrawingFingerprint = string.Empty;
        private bool _selectionGeometryFallback;

        public QuantityInsightPanel()
        {
            InitializeComponent();
            DataContext = _viewModel;
            Loaded += OnLoaded;
        }

        public void RefreshQuantityInsights()
        {
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                ClearQuantityInsights("Không có bản vẽ đang hoạt động.");
                return;
            }

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
            {
                if (_selectionSnapshots.Count > 0)
                {
                    ShowSelectionGeometryFallback(document);
                    return;
                }

                ClearQuantityInsights("Chưa có QS3D project hiện hành. Chọn đối tượng CAD để xem hình học read-only hoặc mở/tạo project để xem khối lượng semantic.");
                return;
            }

            try
            {
                var rows = BuildPreviewRows(document, project, out var regenerated);
                var rowSnapshots = new Dictionary<QuantityInsightItemViewModel, QuantityReportRow>();
                var floors = rows
                    .GroupBy(x => DisplayFloor(x.Floor), StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x.Key, StringComparer.CurrentCultureIgnoreCase)
                    .Select(group => new QuantityInsightFloorViewModel(
                        group.Key,
                        group
                            .OrderBy(x => x.Category, StringComparer.CurrentCultureIgnoreCase)
                            .ThenBy(x => x.FamilyName, StringComparer.CurrentCultureIgnoreCase)
                            .ThenBy(x => x.ElementName, StringComparer.CurrentCultureIgnoreCase)
                            .Select(row => ToInsightItem(row, rowSnapshots))))
                    .ToList();

                var totals = QuantityReportTotals.FromRows(rows);
                _viewModel.Replace(floors, totals, rows.Count);
                _rowSnapshots = rowSnapshots;
                _boundDocument = document;
                _boundProjectId = project.ProjectId;
                _boundDrawingFingerprint = project.DrawingFingerprint ?? string.Empty;
                _selectionGeometryFallback = false;
                EmptyHint.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                QuantityTree.Visibility = rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                _viewModel.Status = rows.Count == 0
                    ? "Project hiện chưa có dòng khối lượng. Preview-regenerate " + regenerated.ToString("N0") + " lượt trên snapshot tách rời."
                    : "Read-only • preview-regenerate " + regenerated.ToString("N0") + " lượt • click dòng cấu kiện để reveal trong View 3D.";
                ApplySelectionHighlights(project, false);
            }
            catch (Exception ex)
            {
                ClearQuantityInsights("Không đọc được khối lượng: " + ex.Message);
            }
        }

        public void SetInspectionReadOnly(IReadOnlyList<EntitySnapshot> snapshots, ProjectState? project)
        {
            _selectionSnapshots = snapshots ?? Array.Empty<EntitySnapshot>();
            var activeDocument = BcadApplication.DocumentManager.MdiActiveDocument;

            if (project == null)
            {
                if (activeDocument == null || _selectionSnapshots.Count == 0)
                {
                    ClearQuantityInsights("Không có selection CAD hợp lệ để diễn giải read-only.");
                    return;
                }

                ShowSelectionGeometryFallback(activeDocument);
                return;
            }

            _selectionGeometryFallback = false;
            if (_boundDocument == null || !ReferenceEquals(activeDocument, _boundDocument) || !SameProjectIdentity(project))
            {
                RefreshQuantityInsights();
                if (_boundDocument == null || !ReferenceEquals(activeDocument, _boundDocument) || !SameProjectIdentity(project))
                {
                    ClearSelectionHighlights();
                    return;
                }
            }

            try
            {
                ApplySelectionHighlights(project, true);
            }
            catch
            {
                ClearSelectionHighlights();
            }
        }

        public void ClearQuantityInsights(string status)
        {
            _selectionSnapshots = Array.Empty<EntitySnapshot>();
            _rowSnapshots.Clear();
            _boundDocument = null;
            _boundProjectId = string.Empty;
            _boundDrawingFingerprint = string.Empty;
            _selectionGeometryFallback = false;
            _viewModel.Clear(status ?? string.Empty);
            EmptyHint.Visibility = Visibility.Visible;
            QuantityTree.Visibility = Visibility.Collapsed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            RefreshQuantityInsights();
        }

        private static IReadOnlyList<QuantityReportRow> BuildPreviewRows(Document document, ProjectState project, out int regenerated)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var previewProject = ProjectStateSnapshot.CreateDetachedCopy(project);
            regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(previewProject);

            var rules = new QuantityCalculationRuleSet(new QuantitySettingsStore().Load());
            foreach (var beam in previewProject.Elements.Where(x => x.Category == ElementCategory.Beam))
                BeamFormworkQuantityPolicy.ApplyAnalyticFallback(beam, rules);

            var beamIds = project.Elements
                .Where(x => x.Category == ElementCategory.Beam)
                .Select(x => x.Id)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (beamIds.Length > 0)
            {
                var geometryProject = PrepareQuantityGeometrySnapshot(document, project, beamIds, out _);
                if (geometryProject != null)
                {
                    foreach (var beamId in beamIds)
                    {
                        try
                        {
                            var geometry = QuantityGeometryExplanationService.Build(document, geometryProject, beamId);
                            geometry = BeamFormworkQuantityPolicy.Apply(document, geometryProject, beamId, geometry, rules);
                            var previewBeam = previewProject.FindElement(beamId);
                            if (previewBeam != null) BeamFormworkQuantityPolicy.ApplyExactQuantity(previewBeam, geometry);
                        }
                        catch (Exception ex) when (!(ex is OutOfMemoryException) && !(ex is StackOverflowException) && !(ex is AccessViolationException))
                        {
                            // Keep the rule-aware analytic fallback for this Beam. Exact BREP is
                            // an enhancement of the detached preview, never a reason to restore
                            // the old hard-coded Side+Bottom value or fail the whole report.
                        }
                    }
                }
            }

            return ProjectQuantityReportBuilder.Detail(previewProject);
        }

        private static QuantityInsightItemViewModel ToInsightItem(
            QuantityReportRow row,
            IDictionary<QuantityInsightItemViewModel, QuantityReportRow> rowSnapshots)
        {
            var item = new QuantityInsightItemViewModel(
                DisplayFloor(row.Floor),
                row.Category,
                row.FamilyName,
                row.ElementName,
                row.Count,
                FormatSummary(row),
                row.ElementIds
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray());
            rowSnapshots.Add(item, row);
            return item;
        }

        private static string DisplayFloor(string? floor) =>
            string.IsNullOrWhiteSpace(floor) ? "Chưa gán tầng" : (floor ?? string.Empty).Trim();

        private static string FormatSummary(QuantityReportRow row)
        {
            var parts = new List<string>(3);
            if (Math.Abs(row.NetConcreteM3) > 1e-9) parts.Add(row.NetConcreteM3.ToString("0.###") + " m³");
            if (Math.Abs(row.FormworkM2) > 1e-9) parts.Add(row.FormworkM2.ToString("0.###") + " m²");
            if (Math.Abs(row.LengthM) > 1e-9 && parts.Count < 2) parts.Add(row.LengthM.ToString("0.###") + " m");
            return parts.Count > 0 ? string.Join(" • ", parts) : row.Count.ToString("N0") + " cấu kiện";
        }

        private void ShowSelectionGeometryFallback(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            var snapshots = _selectionSnapshots
                .Where(x => x != null)
                .OrderBy(x => x.Handle, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var items = snapshots.Select(ToSelectionGeometryItem).ToArray();
            var floors = items.Length == 0
                ? Array.Empty<QuantityInsightFloorViewModel>()
                : new[] { new QuantityInsightFloorViewModel("Selection CAD • read-only", items) };

            _rowSnapshots.Clear();
            _boundDocument = document;
            _boundProjectId = string.Empty;
            _boundDrawingFingerprint = string.Empty;
            _selectionGeometryFallback = true;

            _viewModel.ReplaceSelectionGeometry(
                floors,
                items.Length,
                snapshots.Length,
                "Selection CAD read-only • chưa có QS3D project • số đo native hiển thị theo drawing units (DU/DU²/DU³); tổng quan semantic của dự án không khả dụng.");

            foreach (var item in items) item.SetSelectionMatch(true);
            EmptyHint.Visibility = items.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            QuantityTree.Visibility = items.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private static QuantityInsightItemViewModel ToSelectionGeometryItem(EntitySnapshot snapshot)
        {
            var layer = string.IsNullOrWhiteSpace(snapshot.Layer) ? "Layer chưa xác định" : snapshot.Layer.Trim();
            return new QuantityInsightItemViewModel(
                "Selection CAD • read-only",
                snapshot.EntityType,
                layer,
                "#" + snapshot.Handle,
                1,
                FormatSelectionGeometrySummary(snapshot),
                new[] { snapshot.Handle });
        }

        private static string FormatSelectionGeometrySummary(EntitySnapshot snapshot)
        {
            var parts = new List<string>(4);
            if (snapshot.VolumeDrawingUnitsCubed.HasValue)
                parts.Add("V " + FormatDrawingMetric(snapshot.VolumeDrawingUnitsCubed.Value, "DU³"));
            if (snapshot.SurfaceAreaDrawingUnitsSquared.HasValue)
                parts.Add("S " + FormatDrawingMetric(snapshot.SurfaceAreaDrawingUnitsSquared.Value, "DU²"));
            if (snapshot.AreaDrawingUnitsSquared.HasValue)
                parts.Add("A " + FormatDrawingMetric(snapshot.AreaDrawingUnitsSquared.Value, "DU²"));
            if (snapshot.LengthDrawingUnits.HasValue)
                parts.Add("L " + FormatDrawingMetric(snapshot.LengthDrawingUnits.Value, "DU"));
            return parts.Count > 0 ? string.Join(" • ", parts) : "Không có số đo length/area/volume native";
        }

        private static string FormatDrawingMetric(double value, string unit) =>
            value.ToString("0.###", CultureInfo.CurrentCulture) + " " + unit;

        private void ApplySelectionHighlights(ProjectState project, bool updateStatus)
        {
            if (!SameProjectIdentity(project))
            {
                ClearSelectionHighlights();
                return;
            }

            var selectedHandles = new HashSet<string>(
                _selectionSnapshots
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Handle))
                    .Select(x => x.Handle.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var matchCount = 0;
            foreach (var item in _viewModel.AllItems())
            {
                if (!_rowSnapshots.TryGetValue(item, out var row))
                {
                    item.SetSelectionMatch(false);
                    continue;
                }

                var handles = row.ElementIds.Count == 0
                    ? Array.Empty<string>()
                    : SourceHandleResolver.Resolve(project, row.ElementIds).ToArray();
                var matched = selectedHandles.Count > 0 && handles.Any(selectedHandles.Contains);
                item.SetSelectionMatch(matched);
                if (matched) matchCount++;
            }

            if (!updateStatus) return;
            if (selectedHandles.Count == 0)
                _viewModel.Status = "CAD selection đã bỏ chọn • khối lượng vẫn ở chế độ read-only.";
            else if (matchCount > 0)
                _viewModel.Status = "Đã highlight " + matchCount.ToString("N0") + " dòng khối lượng liên quan selection CAD.";
            else
                _viewModel.Status = "Selection CAD hiện tại chưa ánh xạ tới dòng khối lượng semantic nào.";
        }

        private void ClearSelectionHighlights()
        {
            foreach (var item in _viewModel.AllItems()) item.SetSelectionMatch(false);
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshQuantityInsights();

        private void OnRegenerateClick(object sender, RoutedEventArgs e) =>
            DispatchExistingCommand("QS3DREGEN ", "Đã gửi lệnh QS3DREGEN; bảng sẽ làm mới sau khi regenerate hoàn tất.");

        private void OnOpenBqClick(object sender, RoutedEventArgs e) =>
            DispatchExistingCommand("QS3DBQ ", "Đã mở workflow BQ đầy đủ.");

        private void OnLocateClick(object sender, RoutedEventArgs e) => LocateSelected();

        private void OnQuantityTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (AutoRevealCheck?.IsChecked != true) return;
            if (!(e.NewValue is QuantityInsightItemViewModel)) return;
            LocateSelected();
        }

        private void OnQuantityTreeDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AutoRevealCheck?.IsChecked == true) return;
            if (QuantityTree.SelectedItem is QuantityInsightItemViewModel) LocateSelected();
        }

        private void LocateSelected()
        {
            if (!(QuantityTree.SelectedItem is QuantityInsightItemViewModel item))
            {
                _viewModel.Status = "Chọn một dòng cấu kiện trong cây trước khi định vị.";
                return;
            }

            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                _viewModel.Status = "Không có bản vẽ đang hoạt động để định vị.";
                return;
            }
            if (_boundDocument == null || !ReferenceEquals(document, _boundDocument))
            {
                _viewModel.Status = "Dòng khối lượng này thuộc DWG khác hoặc bảng đã cũ; hãy bấm Làm mới trước khi định vị.";
                return;
            }

            if (_selectionGeometryFallback)
            {
                LocateSelectionGeometry(document, item);
                return;
            }

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
            {
                _viewModel.Status = "QS3D project hiện hành không còn khả dụng; hãy làm mới bảng.";
                return;
            }
            if (!SameProjectIdentity(project))
            {
                _viewModel.Status = "QS3D project đã thay đổi kể từ lần làm mới bảng; từ chối định vị dữ liệu cũ.";
                return;
            }

            try
            {
                var currentRow = ResolveCurrentRow(item, document, project);
                var handles = SourceHandleResolver.Resolve(project, currentRow.ElementIds);
                if (handles.Count == 0)
                {
                    Cad.CadHandleService.ClearSelection(document);
                    _viewModel.Status = "Dòng này chưa có semantic handle hiện hành để định vị trong CAD.";
                    return;
                }

                var count = Cad.CadHandleService.Select(document, handles);
                if (count > 0)
                {
                    if (!global::QS3D.BricsCAD.V25.ViewportCommands.TryZoomSelection(document))
                    {
                        _viewModel.Status = "Định vị: đã chọn " + count.ToString("N0") + " đối tượng CAD nhưng chưa thể zoom vùng chọn hiện hành.";
                        return;
                    }

                    _viewModel.Status = "Định vị: đã chọn/zoom " + count.ToString("N0") + " đối tượng CAD.";
                    return;
                }

                Cad.CadHandleService.ClearSelection(document);
                _viewModel.Status = "Định vị: không còn đối tượng CAD hợp lệ để chọn.";
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Không thể định vị: " + ex.Message;
            }
        }

        private void LocateSelectionGeometry(Document document, QuantityInsightItemViewModel item)
        {
            try
            {
                var handles = item.ElementIds
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (handles.Length != 1)
                {
                    _viewModel.Status = "Dòng geometry read-only không còn một Handle duy nhất; hãy chọn lại đối tượng.";
                    return;
                }

                var live = Cad.EntitySnapshotReader.ReadHandles(document, handles);
                if (live.Count != 1 ||
                    !string.Equals(live[0].EntityType, item.Category, StringComparison.Ordinal) ||
                    !string.Equals(live[0].Layer ?? string.Empty, item.FamilyName ?? string.Empty, StringComparison.Ordinal))
                {
                    Cad.CadHandleService.ClearSelection(document);
                    _viewModel.Status = "Geometry đã thay đổi/xóa kể từ lúc diễn giải; hãy chọn lại đối tượng để lấy snapshot mới.";
                    return;
                }

                var count = Cad.CadHandleService.Select(document, handles);
                if (count != 1)
                {
                    Cad.CadHandleService.ClearSelection(document);
                    _viewModel.Status = "Không còn geometry CAD hợp lệ để định vị.";
                    return;
                }

                if (!global::QS3D.BricsCAD.V25.ViewportCommands.TryZoomSelection(document))
                {
                    _viewModel.Status = "Geometry read-only: đã chọn đối tượng CAD nhưng chưa thể zoom vùng chọn hiện hành.";
                    return;
                }

                _viewModel.Status = "Geometry read-only: đã chọn/zoom Handle " + handles[0] + ".";
            }
            catch (Exception ex)
            {
                _viewModel.Status = "Không thể định vị geometry read-only: " + ex.Message;
            }
        }

        private QuantityReportRow ResolveCurrentRow(QuantityInsightItemViewModel item, Document document, ProjectState project)
        {
            if (!_rowSnapshots.TryGetValue(item, out var displayedRow))
                throw new InvalidOperationException("Dòng khối lượng không còn thuộc snapshot hiện hành của panel. Hãy bấm Làm mới.");

            var displayedIds = CanonicalIds(displayedRow.ElementIds);
            if (displayedIds.Length == 0)
                throw new InvalidOperationException("Dòng khối lượng không có semantic ElementId ổn định để định vị an toàn.");

            var currentRows = BuildPreviewRows(document, project, out _);
            var matches = currentRows.Where(x => x != null && SameElementIdentity(displayedIds, x)).ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException("Dòng khối lượng đã cũ hoặc không còn định danh duy nhất trong project hiện hành. Hãy bấm Làm mới.");
            if (!SameRow(displayedRow, matches[0]))
                throw new InvalidOperationException("Khối lượng hoặc provenance của dòng đã thay đổi kể từ lần làm mới. Hãy bấm Làm mới trước khi định vị.");
            return matches[0];
        }

        private bool SameProjectIdentity(ProjectState project)
        {
            return string.Equals(project.ProjectId, _boundProjectId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(project.DrawingFingerprint ?? string.Empty, _boundDrawingFingerprint, StringComparison.Ordinal);
        }

        private static bool SameElementIdentity(string[] expectedIds, QuantityReportRow candidate)
        {
            var currentIds = CanonicalIds(candidate.ElementIds);
            return expectedIds.SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase);
        }

        private static string[] CanonicalIds(IEnumerable<string> values)
        {
            return (values ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool SameRow(QuantityReportRow left, QuantityReportRow right)
        {
            return string.Equals(left.Floor, right.Floor, StringComparison.Ordinal) &&
                   string.Equals(left.Zone, right.Zone, StringComparison.Ordinal) &&
                   string.Equals(left.Category, right.Category, StringComparison.Ordinal) &&
                   string.Equals(left.FamilyId, right.FamilyId, StringComparison.Ordinal) &&
                   string.Equals(left.FamilyName, right.FamilyName, StringComparison.Ordinal) &&
                   string.Equals(left.ElementName, right.ElementName, StringComparison.Ordinal) &&
                   string.Equals(left.Material, right.Material, StringComparison.Ordinal) &&
                   string.Equals(left.Note, right.Note, StringComparison.Ordinal) &&
                   string.Equals(left.DrawingFingerprint, right.DrawingFingerprint, StringComparison.Ordinal) &&
                   left.Count == right.Count &&
                   left.GrossConcreteM3.Equals(right.GrossConcreteM3) &&
                   left.DeductionM3.Equals(right.DeductionM3) &&
                   left.NetConcreteM3.Equals(right.NetConcreteM3) &&
                   left.FormworkM2.Equals(right.FormworkM2) &&
                   left.LengthM.Equals(right.LengthM) &&
                   left.OuterPerimeterM.Equals(right.OuterPerimeterM) &&
                   left.InnerPerimeterM.Equals(right.InnerPerimeterM) &&
                   left.DoorAreaM2.Equals(right.DoorAreaM2) &&
                   left.SideAreaM2.Equals(right.SideAreaM2) &&
                   left.BottomAreaM2.Equals(right.BottomAreaM2) &&
                   left.TopAreaM2.Equals(right.TopAreaM2) &&
                   left.OtherAreaM2.Equals(right.OtherAreaM2) &&
                   Nullable.Equals(left.DensityKgM3, right.DensityKgM3) &&
                   Nullable.Equals(left.MassKg, right.MassKg) &&
                   CanonicalIds(left.ElementIds).SequenceEqual(CanonicalIds(right.ElementIds), StringComparer.OrdinalIgnoreCase) &&
                   CanonicalIds(left.SourceHandles).SequenceEqual(CanonicalIds(right.SourceHandles), StringComparer.OrdinalIgnoreCase);
        }

        private void DispatchExistingCommand(string command, string status)
        {
            var document = BcadApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                _viewModel.Status = "Không có bản vẽ đang hoạt động.";
                return;
            }

            document.SendStringToExecute(command, true, false, false);
            _viewModel.Status = status;
        }
    }
}
