using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Reporting;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Export;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using QS3D.Core.Templates;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySummaryWindow : Window
    {
        private static readonly string[] ColumnKeys = { "Floor", "Zone", "Category", "FamilyName", "Count", "GrossConcreteM3", "DeductionM3", "NetConcreteM3", "FormworkM2", "LengthM", "OuterPerimeterM", "InnerPerimeterM", "DoorAreaM2", "SideAreaM2", "BottomAreaM2", "TopAreaM2", "OtherAreaM2", "SourceHandleText" };
        private IReadOnlyList<QuantityReportRow> _rows;
        private readonly Action<QuantityReportRow>? _locate;
        private readonly Func<IReadOnlyList<QuantityReportRow>>? _recalculate;
        private readonly Document _document;
        private bool _detailMode;
        private bool _initialized;
        private bool _applyingFilter;
        private bool _switchingMode;
        // XAML Checked/Unchecked handlers may fire during InitializeComponent.
        // Keep them read-only until LoadColumnPreferences has applied the
        // persisted/default state deliberately.
        private bool _loadingColumnPreferences = true;

        public QuantitySummaryWindow(Document document, IReadOnlyList<QuantityReportRow> rows, Action<QuantityReportRow>? locate = null, Func<IReadOnlyList<QuantityReportRow>>? recalculate = null)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _locate = locate;
            _recalculate = recalculate;
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            ReloadFloors();
            ReloadCategories();
            LoadColumnPreferences();
            ApplyFilter();
            UpdateModePresentation();
            _initialized = true;
        }

        private void ReloadFloors(string? preferred = null) { var floors = new List<string> { "Tất cả" }; floors.AddRange(_rows.Select(x => x.Floor).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)); FloorCombo.ItemsSource = floors; FloorCombo.SelectedItem = preferred != null && floors.Any(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase)) ? floors.First(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase)) : "Tất cả"; }
        private void ReloadCategories(string? preferred = null) { var categories = new List<string> { "Tất cả" }; categories.AddRange(_rows.Select(x => x.Category).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)); CategoryList.ItemsSource = categories; CategoryList.SelectedItem = preferred != null && categories.Any(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase)) ? categories.First(x => string.Equals(x, preferred, StringComparison.OrdinalIgnoreCase)) : "Tất cả"; }
        private void ApplyFilter()
        {
            if (QuantityGrid == null || TotalsText == null) return;
            var query = SearchBox == null ? string.Empty : (SearchBox.Text ?? string.Empty).Trim();
            var category = CategoryList?.SelectedItem as string ?? "Tất cả";
            var floor = FloorCombo?.SelectedItem as string ?? "Tất cả";
            var filtered = _rows.Where(x =>
                (floor == "Tất cả" || string.Equals(x.Floor, floor, StringComparison.OrdinalIgnoreCase)) &&
                (category == "Tất cả" || string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase)) &&
                (query.Length == 0 || x.FamilyName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.ElementName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Floor.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Zone.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.ElementIdText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.SourceHandleText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            _applyingFilter = true;
            try { QuantityGrid.ItemsSource = filtered; }
            finally { _applyingFilter = false; }
            var totals = QuantityReportTotals.FromRows(filtered);
            TotalsText.Text = $"TỔNG: {totals.Count:N0} cấu kiện  •  Bê tông {totals.NetConcreteM3:N3} m³  •  Cốp pha {totals.FormworkM2:N3} m²  •  Dài {totals.LengthM:N3} m  •  DT cửa {totals.DoorAreaM2:N3} m²";
            UpdateExplanation(QuantityGrid.SelectedItem as QuantityReportRow);
        }

        private void LoadColumnPreferences()
        {
            var raw = string.Empty;
            var hasSaved = ProjectContextCoordinator.TryGetReadOnly(_document, out var project) &&
                           project.Metadata.TryGetValue(TemplateProfileStore.VisibleBqColumnsKey, out raw) &&
                           !string.IsNullOrWhiteSpace(raw);
            var visible = hasSaved
                ? new HashSet<string>(raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(ColumnKeys, StringComparer.OrdinalIgnoreCase);

            _loadingColumnPreferences = true;
            try
            {
                foreach (var box in ColumnToggleBoxes())
                {
                    if (!TryColumnIndex(box, out var index)) continue;
                    var show = index < ColumnKeys.Length && visible.Contains(ColumnKeys[index]);
                    box.IsChecked = show;
                    if (index < QuantityGrid.Columns.Count) QuantityGrid.Columns[index].Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            finally { _loadingColumnPreferences = false; }
        }

        private void PersistColumnPreferences()
        {
            if (_loadingColumnPreferences) return;
            EnsureActive("lưu cấu hình cột BQ");
            var visible = new List<string>();
            for (var index = 0; index < QuantityGrid.Columns.Count && index < ColumnKeys.Length; index++)
                if (QuantityGrid.Columns[index].Visibility == Visibility.Visible) visible.Add(ColumnKeys[index]);

            if (!ExistingProjectMutationContext.TryGet(_document, out var project))
                throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Đóng bảng BQ và mở lại trước khi đổi cấu hình cột.");
            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                project.Metadata[TemplateProfileStore.VisibleBqColumnsKey] = string.Join("|", visible);
                project.Touch();
            }
            catch (Exception operationError)
            {
                RestoreOrThrow(project, rollback, operationError);
                throw;
            }
        }

        private IEnumerable<CheckBox> ColumnToggleBoxes() { foreach (var child in EnumerateLogicalChildren(this)) if (child is CheckBox box && box.Tag != null) yield return box; }
        private static IEnumerable<object> EnumerateLogicalChildren(DependencyObject parent) { foreach (var child in LogicalTreeHelper.GetChildren(parent).Cast<object>()) { yield return child; if (child is DependencyObject dependency) foreach (var nested in EnumerateLogicalChildren(dependency)) yield return nested; } }
        private static bool TryColumnIndex(CheckBox box, out int index) => int.TryParse(box.Tag?.ToString(), out index) && index >= 0;

        private void OnSearchChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void OnCategoryChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
        private void OnFloorChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void OnViewModeChanged(object sender, RoutedEventArgs e)
        {
            if (!_initialized || _switchingMode || DetailModeRadio == null) return;
            var nextDetailMode = DetailModeRadio.IsChecked == true;
            if (_detailMode == nextDetailMode) { UpdateModePresentation(); return; }
            var previousMode = _detailMode;
            var previousRows = _rows;
            try
            {
                EnsureCurrentProject("đổi chế độ BQ");
                _detailMode = nextDetailMode;
                RefreshRowsForCurrentMode(true);
                UpdateModePresentation();
            }
            catch (Exception ex)
            {
                _detailMode = previousMode;
                _rows = previousRows;
                _switchingMode = true;
                try
                {
                    SummaryModeRadio.IsChecked = !previousMode;
                    DetailModeRadio.IsChecked = previousMode;
                }
                finally { _switchingMode = false; }
                ReloadFloors();
                ReloadCategories();
                ApplyFilter();
                UpdateModePresentation();
                MessageBox.Show(this, "Không thể đổi chế độ BQ: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnRecalculateClick(object sender, RoutedEventArgs e)
        {
            if (!_detailMode && _recalculate == null) { ApplyFilter(); return; }
            try
            {
                EnsureCurrentProject("tính lại BQ");
                RefreshRowsForCurrentMode(false);
            }
            catch (Exception ex) { MessageBox.Show(this, "Không thể tính lại khối lượng: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void RefreshRowsForCurrentMode(bool requireLiveSummarySource)
        {
            var floor = FloorCombo.SelectedItem as string;
            var category = CategoryList.SelectedItem as string;
            _rows = RecalculateRowsForCurrentMode(requireLiveSummarySource);
            ReloadFloors(floor);
            ReloadCategories(category);
            ApplyFilter();
            LoadColumnPreferences();
        }

        private IReadOnlyList<QuantityReportRow> RecalculateRowsForCurrentMode(bool requireLiveSummarySource)
        {
            return _detailMode ? RecalculateDetailRows() : RecalculateSummaryRows(requireLiveSummarySource);
        }

        private IReadOnlyList<QuantityReportRow> RecalculateSummaryRows(bool requireLiveSource)
        {
            if (_recalculate == null)
            {
                if (requireLiveSource)
                    throw new InvalidOperationException("BQ Locate không có nguồn tính lại read-only để xác nhận row hiện hành. Đóng bảng và mở lại QS3DBQ.");
                return _rows;
            }
            var currentRows = _recalculate() ?? Array.Empty<QuantityReportRow>();
            return currentRows;
        }

        private IReadOnlyList<QuantityReportRow> RecalculateDetailRows()
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject))
                throw new InvalidOperationException("BQ Diễn giải cần một QS3D project hiện hữu; chế độ chi tiết không tạo replacement project khi chỉ đọc.");

            var previewProject = ProjectStateSnapshot.CreateDetachedCopy(currentProject);
            new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(previewProject);
            if (previewProject.Elements.Count > 0) return ProjectQuantityReportBuilder.Detail(previewProject);

            var unit = Cad.CadUnitService.GetDrawingUnit(_document);
            var snapshotRows = SnapshotQuantityAdapter.Build(Cad.EntitySnapshotReader.ReadCurrentSelection(_document), unit);
            foreach (var snapshotRow in snapshotRows) snapshotRow.DrawingFingerprint = previewProject.DrawingFingerprint;
            return snapshotRows;
        }

        private void UpdateModePresentation()
        {
            if (ModeHintText == null || AutoRevealCheck == null || QuantityGrid == null) return;
            ModeHintText.Text = _detailMode
                ? "Diễn giải chi tiết: 1 semantic element / dòng. Click dòng để đối chiếu trực tiếp trên View 3D."
                : "Khối lượng đang được gộp theo Floor / Zone / Category / Family.";
            AutoRevealCheck.IsEnabled = _detailMode;
            if (QuantityGrid.Columns.Count > 3)
                QuantityGrid.Columns[3].Header = _detailMode ? "Tên cấu kiện" : "Tên Family / cấu kiện";
        }

        private void OnColumnVisibilityChanged(object sender, RoutedEventArgs e)
        {
            if (QuantityGrid == null || !(sender is CheckBox box) || box.Tag == null || _loadingColumnPreferences) return;
            try
            {
                EnsureCurrentProject("đổi cấu hình cột BQ");
                if (!TryColumnIndex(box, out var index) || index >= QuantityGrid.Columns.Count) return;
                QuantityGrid.Columns[index].Visibility = box.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                PersistColumnPreferences();
            }
            catch (Exception ex)
            {
                LoadColumnPreferences();
                MessageBox.Show(this, ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnQuantityGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_applyingFilter || QuantityGrid == null) return;
            var row = QuantityGrid.SelectedItem as QuantityReportRow;
            UpdateExplanation(row);
            if (!_initialized || !_detailMode || AutoRevealCheck?.IsChecked != true || row == null || e.AddedItems.Count == 0) return;
            LocateCurrent();
        }

        private void UpdateExplanation(QuantityReportRow? row)
        {
            if (ExplanationTitleText == null || ExplanationConcreteText == null || ExplanationFormworkText == null || ExplanationGeometryText == null || ExplanationProvenanceText == null) return;
            if (row == null)
            {
                ExplanationTitleText.Text = "Chọn một dòng để xem diễn giải";
                ExplanationConcreteText.Text = string.Empty;
                ExplanationFormworkText.Text = string.Empty;
                ExplanationGeometryText.Text = string.Empty;
                ExplanationProvenanceText.Text = string.Empty;
                return;
            }

            var name = string.IsNullOrWhiteSpace(row.ElementName) ? row.FamilyName : row.ElementName;
            ExplanationTitleText.Text = name + " — " + row.Category + " • " + row.FloorZoneText;
            ExplanationConcreteText.Text = $"Bê tông: gộp {row.GrossConcreteM3:0.###} m³ • trừ giao {row.DeductionM3:0.###} m³ • còn {row.NetConcreteM3:0.###} m³";
            ExplanationFormworkText.Text = $"Cốp pha: {row.FormworkM2:0.###} m² • mặt tham chiếu thành {row.SideAreaM2:0.###} • đáy {row.BottomAreaM2:0.###} • đỉnh {row.TopAreaM2:0.###} • khác {row.OtherAreaM2:0.###} m²";
            ExplanationGeometryText.Text = $"Hình học: dài {row.LengthM:0.###} m • chu vi ngoài {row.OuterPerimeterM:0.###} m • chu vi trong {row.InnerPerimeterM:0.###} m • DT cửa {row.DoorAreaM2:0.###} m²";
            var semantic = row.ElementIds.Count == 0 ? "—" : string.Join("; ", row.ElementIds);
            var handles = row.SourceHandles.Count == 0 ? "—" : string.Join("; ", row.SourceHandles);
            ExplanationProvenanceText.Text = "Semantic: " + semantic + "\nCAD Handle: " + handles + (_detailMode ? "\nClick dòng này để reveal trong View 3D." : "\nDouble-click hoặc bấm Định vị để reveal cả nhóm trong View 3D.");
        }

        private void OnLocateClick(object sender, RoutedEventArgs e) => LocateCurrent();
        private void OnQuantityGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_detailMode && AutoRevealCheck?.IsChecked == true) return;
            LocateCurrent();
        }
        private void OnEd2ExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureCurrentProject("mở ED2 Excel");
                _document.SendStringToExecute("QS3DED2 ", true, false, false);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void OnExcelLocateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureCurrentProject("định vị từ Excel");
                _document.SendStringToExecute("QS3DEXCELLOCATE ", true, false, false);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private void LocateCurrent()
        {
            if (!(QuantityGrid.SelectedItem is QuantityReportRow row)) return;
            try
            {
                EnsureCurrentProject("định vị BQ");
                var displayedHandles = CanonicalIds(row.SourceHandles);
                var currentRow = ResolveCurrentRow(row);
                var liveHandles = CanonicalIds(currentRow.SourceHandles);
                if (liveHandles.Length > 0)
                {
                    var selectedCount = Cad.CadHandleService.Select(_document, liveHandles);
                    var expectedCount = displayedHandles.Length > 0 ? displayedHandles.Length : liveHandles.Length;
                    if (selectedCount <= 0)
                    {
                        PaletteCoordinator.SetStatus("BQ Định vị: không còn đối tượng CAD hợp lệ trong " + expectedCount + " handle của dòng này.");
                        return;
                    }

                    PaletteCoordinator.SetStatus(selectedCount < expectedCount
                        ? "BQ Định vị: đã chọn " + selectedCount + "/" + expectedCount + " đối tượng CAD; " + (expectedCount - selectedCount) + " handle đã mất hoặc không còn hợp lệ."
                        : "BQ Định vị: đã chọn " + selectedCount + " đối tượng CAD.");
                    _document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
                    return;
                }

                Cad.CadHandleService.Select(_document, liveHandles);
                if (_locate != null)
                {
                    _locate(currentRow);
                    return;
                }

                PaletteCoordinator.SetStatus("BQ Định vị: dòng này không còn CAD handle hợp lệ để chọn.");
            }
            catch (Exception ex) { MessageBox.Show(this, "Không thể định vị: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }

        private QuantityReportRow ResolveCurrentRow(QuantityReportRow displayedRow)
        {
            var displayedIds = CanonicalIds(displayedRow.ElementIds);
            var displayedHandles = CanonicalIds(displayedRow.SourceHandles);
            if (displayedIds.Length == 0 && displayedHandles.Length == 0)
                throw new InvalidOperationException("Dòng BQ này không có semantic ElementId hoặc CAD handle ổn định để định vị an toàn.");

            if (displayedIds.Length == 0)
                return ResolveSourceHandleRow(displayedRow, displayedHandles);

            var currentRows = _detailMode ? RecalculateDetailRows() : RecalculateSummaryRows(true);
            var matches = currentRows.Where(x => x != null && SameElementIdentity(displayedIds, x)).ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException("Dòng BQ đã cũ hoặc không còn định danh duy nhất trong project hiện hành. Đóng bảng và chạy lại QS3DBQ.");
            if (!SameRow(displayedRow, matches[0]))
                throw new InvalidOperationException("Dòng BQ đã thay đổi kể từ lúc bảng được mở. Đóng bảng và chạy lại QS3DBQ trước khi định vị.");
            return matches[0];
        }

        private QuantityReportRow ResolveSourceHandleRow(QuantityReportRow displayedRow, string[] expectedHandles)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject))
                throw new InvalidOperationException("BQ Định vị cần một QS3D project hiện hữu để xác nhận drawing hiện hành.");

            var unit = Cad.CadUnitService.GetDrawingUnit(_document);
            var snapshots = Cad.EntitySnapshotReader.ReadHandles(_document, expectedHandles);
            if (snapshots.Count == 0)
                throw new InvalidOperationException("Không còn CAD handle nào của dòng BQ tồn tại trong bản vẽ hiện hành.");

            var currentRows = SnapshotQuantityAdapter.Build(snapshots, unit);
            foreach (var current in currentRows) current.DrawingFingerprint = currentProject.DrawingFingerprint;
            var matches = currentRows.Where(x => x != null && SameSourceGroupIdentity(displayedRow, x)).ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException("Các CAD handle của dòng BQ đã đổi loại/layer hoặc không còn tạo thành một nhóm định vị duy nhất. Tính lại BQ trước khi tiếp tục.");

            var currentRow = matches[0];
            var currentHandles = CanonicalIds(currentRow.SourceHandles);
            if (currentHandles.Length == 0 || currentHandles.Any(x => !expectedHandles.Contains(x, StringComparer.OrdinalIgnoreCase)))
                throw new InvalidOperationException("CAD handle của dòng BQ không còn khớp drawing hiện hành.");

            if (currentHandles.Length == expectedHandles.Length && !SameRow(displayedRow, currentRow))
                throw new InvalidOperationException("Dòng BQ đã thay đổi kể từ lúc bảng được mở. Tính lại BQ trước khi định vị.");

            return currentRow;
        }

        private static bool SameElementIdentity(string[] expectedIds, QuantityReportRow candidate)
        {
            var currentIds = CanonicalIds(candidate.ElementIds);
            return expectedIds.SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase);
        }

        private static bool SameSourceGroupIdentity(QuantityReportRow expected, QuantityReportRow candidate)
        {
            return string.Equals(expected.Floor, candidate.Floor, StringComparison.Ordinal) &&
                   string.Equals(expected.Zone, candidate.Zone, StringComparison.Ordinal) &&
                   string.Equals(expected.Category, candidate.Category, StringComparison.Ordinal) &&
                   string.Equals(expected.FamilyId, candidate.FamilyId, StringComparison.Ordinal) &&
                   string.Equals(expected.FamilyName, candidate.FamilyName, StringComparison.Ordinal) &&
                   string.Equals(expected.ElementName, candidate.ElementName, StringComparison.Ordinal) &&
                   string.Equals(expected.Material, candidate.Material, StringComparison.Ordinal) &&
                   string.Equals(expected.Note, candidate.Note, StringComparison.Ordinal) &&
                   string.Equals(expected.DrawingFingerprint, candidate.DrawingFingerprint, StringComparison.Ordinal);
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

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog { Title = "Xuất bảng khối lượng QS3D", Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = _detailMode ? "QS3D-Dien-Giai-Khoi-Luong.xlsx" : "QS3D-Khoi-Luong.xlsx", AddExtension = true, DefaultExt = ".xlsx", OverwritePrompt = true };
                if (dialog.ShowDialog(this) != true) return;

                EnsureCurrentProject("xuất BQ XLSX");
                if (_detailMode || _recalculate != null) RefreshRowsForCurrentMode(false);
                var visibleRows = (QuantityGrid.ItemsSource as IEnumerable<QuantityReportRow>)?.ToList() ?? _rows.ToList();
                if (visibleRows.Count == 0) throw new InvalidOperationException("BQ hiện không có dòng nào để xuất.");
                XlsxQuantityExporter.Export(dialog.FileName, visibleRows);
                MessageBox.Show(this, _detailMode ? "Đã xuất diễn giải chi tiết hiện hành ra Excel." : "Đã tính lại dữ liệu hiện hành và xuất Excel thành công.", "QS3D", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show(this, "Không thể xuất Excel: " + ex.Message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private static void RestoreOrThrow(QS3D.Core.Domain.ProjectState project, ProjectStateSnapshot rollback, Exception operationError)
        {
            try { rollback.Restore(project); }
            catch (Exception restoreError)
            {
                throw new InvalidOperationException(
                    "Lưu cấu hình cột BQ thất bại và rollback project cũng không hoàn tất.",
                    new AggregateException(operationError, restoreError));
            }
        }

        private void EnsureCurrentProject(string operation)
        {
            EnsureActive(operation);
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out _))
                throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Đóng bảng BQ và mở lại trước khi " + operation + ".");
        }

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Bảng BQ này thuộc một DWG khác. Hãy kích hoạt lại đúng bản vẽ trước khi " + operation + ".");
        }
    }
}