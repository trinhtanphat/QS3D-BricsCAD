using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WallQuantityWindow : Window
    {
        private readonly Document _document;
        private readonly string _sourceProjectId;
        private ProjectState? _snapshot;
        private IReadOnlyList<QuantityReportRow> _rows = Array.Empty<QuantityReportRow>();
        private IReadOnlyList<WallRowView> _visible = Array.Empty<WallRowView>();
        private bool _suppressFilterEvents;
        private bool _suppressSelectionSync;

        private sealed class FilterOption
        {
            public string Key { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
        }

        private sealed class WallRowView
        {
            public QuantityReportRow Source { get; set; } = null!;
            public int Index { get; set; }
            public string ElementId { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Floor { get; set; } = string.Empty;
            public string CategoryKey { get; set; } = string.Empty;
            public string CategoryLabel { get; set; } = string.Empty;
            public string FamilyName { get; set; } = string.Empty;
            public string Material { get; set; } = string.Empty;
            public string HandleText { get; set; } = string.Empty;
            public double LengthM { get; set; }
            public double? ThicknessMm { get; set; }
            public double? HeightM { get; set; }
            public double GrossConcreteM3 { get; set; }
            public double DeductionM3 { get; set; }
            public double NetConcreteM3 { get; set; }
            public double FormworkM2 { get; set; }
            public string LengthText => LengthM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            public string NetText => NetConcreteM3.ToString("0.###", CultureInfo.InvariantCulture) + " m³";
            public string SearchText => string.Join(" ", ElementId, DisplayName, Floor, CategoryKey, CategoryLabel, FamilyName, Material, HandleText).ToLowerInvariant();
        }

        public WallQuantityWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                throw new InvalidOperationException("Bản vẽ hiện tại chưa có QS3D project để đọc khối lượng Tường.");

            _sourceProjectId = project.ProjectId;
            _suppressFilterEvents = true;
            InitializeComponent();
            _suppressFilterEvents = false;
            DocumentBoundWindowLifetime.Attach(this, _document);
            Loaded += (_, __) => RefreshRows();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshRows();

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (!_suppressFilterEvents) ApplyFilter();
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_suppressFilterEvents) ApplyFilter();
        }

        private void OnWallSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionSync) return;
            var selected = WallList.SelectedItem as WallRowView;
            ShowSelected(selected);
            if (selected == null) return;
            try
            {
                _suppressSelectionSync = true;
                TakeoffGrid.SelectedItem = selected;
                TakeoffGrid.ScrollIntoView(selected);
            }
            finally
            {
                _suppressSelectionSync = false;
            }

            if (e.AddedItems.Count > 0 && AutoRevealCheck?.IsChecked == true)
                LocateSelected(selected, "danh sách Tường");
        }

        private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionSync) return;
            var selected = TakeoffGrid.SelectedItem as WallRowView;
            ShowSelected(selected);
            if (selected == null) return;
            try
            {
                _suppressSelectionSync = true;
                WallList.SelectedItem = selected;
                WallList.ScrollIntoView(selected);
            }
            finally
            {
                _suppressSelectionSync = false;
            }

            if (e.AddedItems.Count > 0 && AutoRevealCheck?.IsChecked == true)
                LocateSelected(selected, "bảng chi tiết Tường");
        }

        private void OnLocateClick(object sender, RoutedEventArgs e)
        {
            LocateSelected(CurrentSelectedView(), "nút Định vị 3D");
        }

        private void OnWallListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AutoRevealCheck?.IsChecked == true) return;
            LocateSelected(WallList.SelectedItem as WallRowView, "double-click danh sách Tường");
        }

        private void OnGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AutoRevealCheck?.IsChecked == true) return;
            LocateSelected(TakeoffGrid.SelectedItem as WallRowView, "double-click bảng chi tiết Tường");
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureCurrentProject("xuất bảng khối lượng Tường");
                var visible = _visible.Select(x => x.Source).ToList();
                if (visible.Count == 0)
                    throw new InvalidOperationException("Bộ lọc hiện tại không có dòng Tường để xuất.");

                var drawingName = string.IsNullOrWhiteSpace(_document.Name)
                    ? "QS3D"
                    : Path.GetFileNameWithoutExtension(_document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất khối lượng Tường",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-Khoi-Luong-Tuong.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                XlsxQuantityExporter.Export(dialog.FileName, visible);
                SetStatus("Đã xuất " + visible.Count + " dòng Tường đang lọc → " + dialog.FileName);
            }
            catch (Exception ex)
            {
                SetStatus("Xuất khối lượng Tường lỗi: " + ex.Message);
            }
        }

        private void RefreshRows()
        {
            try
            {
                var selectedId = CurrentSelectedElementId();
                _rows = BuildCurrentRows(out var snapshot, out var regenerated);
                _snapshot = snapshot;
                Title = "QS3D • Khối lượng Tường • " + DrawingLabel(_document);
                PopulateFilters();
                ApplyFilter(selectedId);
                SetStatus("Đã nạp " + _rows.Count + " Tường • preview regen " + regenerated + " cấu kiện dirty trên snapshot tách rời.");
            }
            catch (Exception ex)
            {
                _snapshot = null;
                _rows = Array.Empty<QuantityReportRow>();
                _visible = Array.Empty<WallRowView>();
                WallList.ItemsSource = _visible;
                TakeoffGrid.ItemsSource = _visible;
                WallCountBadge.Text = "0";
                ShowSelected(null);
                UpdateTotals(_visible);
                SetStatus("Đọc khối lượng Tường lỗi: " + ex.Message);
            }
        }

        private IReadOnlyList<QuantityReportRow> BuildCurrentRows(out ProjectState snapshot, out int regenerated)
        {
            var project = EnsureCurrentProject("đọc khối lượng Tường hiện hành");
            snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
            regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
            return ProjectQuantityReportBuilder.Detail(snapshot).Where(IsWallRow).ToList();
        }

        private void PopulateFilters()
        {
            var previousFloor = FloorFilter.SelectedItem as string;
            var previousCategory = (CategoryFilter.SelectedItem as FilterOption)?.Key ?? string.Empty;

            try
            {
                _suppressFilterEvents = true;
                var floors = new List<string> { "Tất cả tầng" };
                floors.AddRange(_rows.Select(x => x.Floor)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase));
                FloorFilter.ItemsSource = floors;
                FloorFilter.SelectedItem = previousFloor != null && floors.Contains(previousFloor, StringComparer.OrdinalIgnoreCase)
                    ? floors.First(x => string.Equals(x, previousFloor, StringComparison.OrdinalIgnoreCase))
                    : floors[0];

                var options = new List<FilterOption>
                {
                    new FilterOption { Key = string.Empty, Label = "Tất cả loại tường" },
                    new FilterOption { Key = ElementCategory.ArchitecturalWall.ToString(), Label = "Tường kiến trúc" },
                    new FilterOption { Key = ElementCategory.StructuralWall.ToString(), Label = "Vách kết cấu" },
                    new FilterOption { Key = ElementCategory.GlassWall.ToString(), Label = "Tường kính" },
                    new FilterOption { Key = ElementCategory.WallPier.ToString(), Label = "Trụ tường" }
                };
                CategoryFilter.ItemsSource = options;
                CategoryFilter.SelectedItem = options.FirstOrDefault(x => string.Equals(x.Key, previousCategory, StringComparison.OrdinalIgnoreCase)) ?? options[0];
            }
            finally
            {
                _suppressFilterEvents = false;
            }
        }

        private void ApplyFilter(string? preferredElementId = null)
        {
            var query = (SearchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            var floor = FloorFilter.SelectedItem as string ?? "Tất cả tầng";
            var category = (CategoryFilter.SelectedItem as FilterOption)?.Key ?? string.Empty;

            var source = _rows.Where(row =>
                (string.Equals(floor, "Tất cả tầng", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(row.Floor, floor, StringComparison.OrdinalIgnoreCase)) &&
                (category.Length == 0 || string.Equals(row.Category, category, StringComparison.OrdinalIgnoreCase)));

            var views = source.Select((row, index) => CreateView(row, index + 1))
                .Where(view => query.Length == 0 || view.SearchText.Contains(query))
                .ToList();

            _visible = views;
            WallList.ItemsSource = views;
            TakeoffGrid.ItemsSource = views;
            WallCountBadge.Text = views.Count.ToString(CultureInfo.InvariantCulture);
            UpdateTotals(views);

            var selected = !string.IsNullOrWhiteSpace(preferredElementId)
                ? views.FirstOrDefault(x => string.Equals(x.ElementId, preferredElementId, StringComparison.OrdinalIgnoreCase))
                : views.FirstOrDefault();

            try
            {
                _suppressSelectionSync = true;
                WallList.SelectedItem = selected;
                TakeoffGrid.SelectedItem = selected;
            }
            finally
            {
                _suppressSelectionSync = false;
            }
            ShowSelected(selected);
        }

        private WallRowView CreateView(QuantityReportRow row, int index)
        {
            var elementId = row.ElementIds.FirstOrDefault() ?? string.Empty;
            var element = _snapshot?.FindElement(elementId);
            return new WallRowView
            {
                Source = row,
                Index = index,
                ElementId = elementId,
                DisplayName = FirstNonEmpty(row.ElementName, row.FamilyName, elementId, "(không tên)"),
                Floor = FirstNonEmpty(row.Floor, "(chưa gán tầng)"),
                CategoryKey = row.Category ?? string.Empty,
                CategoryLabel = CategoryLabel(row.Category),
                FamilyName = FirstNonEmpty(row.FamilyName, row.FamilyId, "—"),
                Material = FirstNonEmpty(row.Material, "—"),
                HandleText = row.SourceHandleText,
                LengthM = row.LengthM,
                ThicknessMm = ResolveThicknessMm(_snapshot, element),
                HeightM = ResolveHeightM(_snapshot, element),
                GrossConcreteM3 = row.GrossConcreteM3,
                DeductionM3 = row.DeductionM3,
                NetConcreteM3 = row.NetConcreteM3,
                FormworkM2 = row.FormworkM2
            };
        }

        private void ShowSelected(WallRowView? view)
        {
            if (view == null)
            {
                SelectedSubtitleText.Text = "Chọn một dòng ở danh sách hoặc bảng chi tiết";
                SelectedCategoryBadge.Text = "—";
                SelectedNameText.Text = "—";
                SelectedFloorText.Text = "—";
                SelectedIdText.Text = "—";
                SelectedFamilyText.Text = "—";
                SelectedMaterialText.Text = "—";
                SelectedHandleText.Text = "—";
                SelectedLengthText.Text = "—";
                SelectedThicknessText.Text = "—";
                SelectedHeightText.Text = "—";
                SelectedFormworkText.Text = "—";
                SelectedGrossText.Text = "—";
                SelectedNetBreakdownText.Text = "—";
                SelectedNoteText.Text = "Khối lượng lấy từ cùng một pipeline ProjectQuantityReportBuilder.Detail; bật Bám 3D để click dòng và đối chiếu trong View 3D.";
                return;
            }

            SelectedSubtitleText.Text = view.ElementId;
            SelectedCategoryBadge.Text = view.CategoryLabel;
            SelectedNameText.Text = view.DisplayName;
            SelectedFloorText.Text = view.Floor;
            SelectedIdText.Text = view.ElementId;
            SelectedFamilyText.Text = view.FamilyName;
            SelectedMaterialText.Text = view.Material;
            SelectedHandleText.Text = FirstNonEmpty(view.HandleText, "—");
            SelectedLengthText.Text = Format(view.LengthM, " m");
            SelectedThicknessText.Text = FormatNullable(view.ThicknessMm, " mm");
            SelectedHeightText.Text = FormatNullable(view.HeightM, " m");
            SelectedFormworkText.Text = Format(view.FormworkM2, " m²");
            SelectedGrossText.Text = Format(view.GrossConcreteM3, " m³");
            SelectedNetBreakdownText.Text = Format(view.DeductionM3, " m³") + " / " + Format(view.NetConcreteM3, " m³");
            SelectedNoteText.Text = string.IsNullOrWhiteSpace(view.Source.Note)
                ? "Read-only • snapshot tách rời • không ghi project/CAD. Bám 3D luôn revalidate ElementId + Handle hiện hành trước khi chọn/zoom."
                : view.Source.Note + "\nBám 3D: revalidate semantic + Handle hiện hành trước khi chọn/zoom.";
        }

        private void UpdateTotals(IReadOnlyList<WallRowView> rows)
        {
            var count = 0;
            var length = 0d;
            var gross = 0d;
            var deduction = 0d;
            var net = 0d;
            var formwork = 0d;
            foreach (var view in rows)
            {
                count = QuantityReportMath.AddCount(count, view.Source.Count);
                length = QuantityReportMath.Add(length, view.LengthM, "Visible wall length");
                gross = QuantityReportMath.Add(gross, view.GrossConcreteM3, "Visible wall gross concrete");
                deduction = QuantityReportMath.Add(deduction, view.DeductionM3, "Visible wall deduction");
                net = QuantityReportMath.Add(net, view.NetConcreteM3, "Visible wall net concrete");
                formwork = QuantityReportMath.Add(formwork, view.FormworkM2, "Visible wall formwork");
            }
            TotalCountText.Text = count.ToString(CultureInfo.InvariantCulture);
            TotalLengthText.Text = Format(length, " m");
            TotalGrossText.Text = Format(gross, " m³");
            TotalDeductionText.Text = Format(deduction, " m³");
            TotalNetText.Text = Format(net, " m³");
            TotalFormworkText.Text = Format(formwork, " m²");
        }

        private void LocateSelected(WallRowView? displayedView, string trigger)
        {
            if (displayedView == null)
            {
                SetStatus("Định vị Tường: chưa có dòng nào được chọn.");
                return;
            }

            try
            {
                var currentProject = EnsureCurrentProject("định vị Tường trong View 3D");
                var currentRow = ResolveCurrentRow(currentProject, displayedView);
                var elementId = currentRow.ElementIds[0];
                var currentElement = currentProject.FindElement(elementId)
                    ?? throw new InvalidOperationException("Tường đã bị xóa hoặc semantic identity vừa thay đổi. Tính lại trước khi định vị.");
                var handles = Resolve3DLocateHandles(currentProject, currentElement, currentRow);

                var count = QS3D.BricsCAD.V25.Cad.CadHandleService.Select(_document, handles);
                if (count <= 0)
                    throw new InvalidOperationException("Không resolve được CAD object hiện hành từ Handle đã xác thực của Tường.");

                SetStatus("Bám 3D • " + trigger + ": đã chọn " + count + " đối tượng CAD cho " + displayedView.ElementId + ".");
                // EnsureCurrentProject already requires this document to be active. Reactivating it here can
                // clear the implied selection before QS3DZOOMSELECTED consumes it.
                _document.SendStringToExecute("QS3DZOOMSELECTED ", false, false, false);
            }
            catch (Exception ex)
            {
                SetStatus("Định vị Tường lỗi: " + ex.Message);
            }
        }

        private IReadOnlyList<string> Resolve3DLocateHandles(ProjectState currentProject, ProjectElement currentElement, QuantityReportRow currentRow)
        {
            const string generatedSolidHandleKey = "GeneratedSolidHandle";
            if (!currentElement.Properties.TryGetValue(generatedSolidHandleKey, out var rawGeneratedHandle))
            {
                var sourceHandles = SourceHandleResolver.Resolve(currentProject, currentRow.ElementIds);
                if (sourceHandles.Count == 0)
                    throw new InvalidOperationException("Tường chưa có Solid3d generated và cũng không còn CAD Handle nguồn để định vị an toàn.");
                return sourceHandles;
            }

            if (currentElement.IsGeneratedSolidStale())
                throw new InvalidOperationException("Solid3d generated của Tường đang stale; hãy regenerate trước khi Định vị 3D.");

            var normalized = QS3D.BricsCAD.V25.Cad.CadHandleService.NormalizeHexHandle(rawGeneratedHandle);
            if (normalized == null)
                throw new InvalidOperationException("GeneratedSolidHandle của Tường tồn tại nhưng rỗng hoặc không phải handle hex hợp lệ; từ chối fallback sang hình học nguồn.");

            var liveSolidHandles = QS3D.BricsCAD.V25.Cad.CadHandleService.GetLiveSolidHandles(_document, new[] { normalized });
            if (!liveSolidHandles.Contains(normalized))
                throw new InvalidOperationException("GeneratedSolidHandle của Tường không còn resolve tới Solid3d sống; từ chối fallback sang hình học nguồn.");

            var ownedHandles = QS3D.BricsCAD.V25.Cad.GeneratedGeometryService.FindMatchingOwnedHandles(
                _document,
                currentProject.ProjectId,
                currentElement.Id,
                currentElement.Category);
            var ownershipMatches = ownedHandles.Any(handle =>
                string.Equals(
                    QS3D.BricsCAD.V25.Cad.CadHandleService.NormalizeHexHandle(handle),
                    normalized,
                    StringComparison.OrdinalIgnoreCase));
            if (!ownershipMatches)
                throw new InvalidOperationException("GeneratedSolidHandle trỏ tới Solid3d sống nhưng QS3D ownership không khớp project/element/category; từ chối fallback sang hình học nguồn.");

            return new[] { normalized };
        }

        private QuantityReportRow ResolveCurrentRow(ProjectState currentProject, WallRowView displayedView)
        {
            var elementId = (displayedView.ElementId ?? string.Empty).Trim();
            if (elementId.Length == 0)
                throw new InvalidOperationException("Dòng Tường không có semantic ElementId ổn định để định vị.");

            var currentElement = currentProject.FindElement(elementId)
                ?? throw new InvalidOperationException("Tường đã bị xóa hoặc dòng hiển thị đã stale. Tính lại trước khi định vị.");
            if (!IsWallCategory(currentElement.Category))
                throw new InvalidOperationException("Semantic hiện hành của dòng này không còn thuộc nhóm Tường được hỗ trợ.");

            var currentSnapshot = ProjectStateSnapshot.CreateDetachedCopy(currentProject);
            var currentRows = ProjectQuantityReportBuilder.Detail(currentSnapshot, new[] { elementId });
            if (currentRows.Count != 1)
                throw new InvalidOperationException("Không thể xác nhận duy nhất dòng Tường hiện hành cho ElementId " + elementId + ".");

            var currentRow = currentRows[0];
            if (!IsWallRow(currentRow) || currentRow.ElementIds.Count != 1 ||
                !string.Equals(currentRow.ElementIds[0], elementId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Dòng Tường hiện hành không còn khớp semantic identity đã hiển thị.");

            return currentRow;
        }

        private ProjectState EnsureCurrentProject(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Khối lượng Tường trước khi " + operation + ".");
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Đóng cửa sổ Khối lượng Tường và mở lại sau khi nạp project.");
            if (!string.Equals(project.ProjectId, _sourceProjectId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Project của bản vẽ đã thay đổi kể từ khi mở cửa sổ. Đóng Khối lượng Tường và mở lại để tránh đọc dữ liệu stale.");
            return project;
        }

        private static bool IsWallRow(QuantityReportRow row)
        {
            return string.Equals(row.Category, ElementCategory.StructuralWall.ToString(), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(row.Category, ElementCategory.ArchitecturalWall.ToString(), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(row.Category, ElementCategory.GlassWall.ToString(), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(row.Category, ElementCategory.WallPier.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWallCategory(ElementCategory category)
        {
            return category == ElementCategory.StructuralWall ||
                   category == ElementCategory.ArchitecturalWall ||
                   category == ElementCategory.GlassWall ||
                   category == ElementCategory.WallPier;
        }

        private static string CategoryLabel(string? category)
        {
            if (string.Equals(category, ElementCategory.ArchitecturalWall.ToString(), StringComparison.OrdinalIgnoreCase)) return "Tường kiến trúc";
            if (string.Equals(category, ElementCategory.StructuralWall.ToString(), StringComparison.OrdinalIgnoreCase)) return "Vách kết cấu";
            if (string.Equals(category, ElementCategory.GlassWall.ToString(), StringComparison.OrdinalIgnoreCase)) return "Tường kính";
            if (string.Equals(category, ElementCategory.WallPier.ToString(), StringComparison.OrdinalIgnoreCase)) return "Trụ tường";
            return FirstNonEmpty(category, "Tường");
        }
        private static double? ResolveThicknessMm(ProjectState? project, ProjectElement? element)
        {
            if (element == null) return null;
            var mm = ResolveNumber(project, element, "ThicknessMm", "WallThicknessMm");
            if (mm.HasValue) return mm;
            var metres = ResolveNumber(project, element, "ThicknessM", "WallThicknessM");
            if (!metres.HasValue) return null;
            var converted = metres.Value * 1000d;
            return IsFiniteNonNegative(converted) ? converted : (double?)null;
        }

        private static double? ResolveHeightM(ProjectState? project, ProjectElement? element)
        {
            if (element == null) return null;
            if (element.Quantities.TryGetValue("HeightM", out var quantityHeight) && IsFiniteNonNegative(quantityHeight)) return quantityHeight;
            var metres = ResolveNumber(project, element, "HeightM", "WallHeightM");
            if (metres.HasValue) return metres;
            var millimetres = ResolveNumber(project, element, "HeightMm", "WallHeightMm");
            if (!millimetres.HasValue) return null;
            var converted = millimetres.Value / 1000d;
            return IsFiniteNonNegative(converted) ? converted : (double?)null;
        }

        private static double? ResolveNumber(ProjectState? project, ProjectElement element, params string[] keys)
        {
            foreach (var key in keys)
                if (element.Properties.TryGetValue(key, out var instance) && TryInvariantNonNegative(instance, out var value)) return value;
            var family = project?.FindFamily(element.FamilyId);
            if (family == null) return null;
            foreach (var key in keys)
                if (family.Properties.TryGetValue(key, out var inherited) && TryInvariantNonNegative(inherited, out var value)) return value;
            return null;
        }

        private static bool TryInvariantNonNegative(string? text, out double value)
        {
            value = 0d;
            return !string.IsNullOrWhiteSpace(text) &&
                   double.TryParse((text ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   IsFiniteNonNegative(value);
        }

        private static bool IsFiniteNonNegative(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;

        private WallRowView? CurrentSelectedView()
        {
            if (WallList.SelectedItem is WallRowView selected) return selected;
            if (TakeoffGrid.SelectedItem is WallRowView gridSelected) return gridSelected;
            return null;
        }

        private string CurrentSelectedElementId()
        {
            return CurrentSelectedView()?.ElementId ?? string.Empty;
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu";
            try { return Path.GetFileName(name); }
            catch { return name; }
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
                if (!string.IsNullOrWhiteSpace(value)) return (value ?? string.Empty).Trim();
            return string.Empty;
        }

        private static string Format(double value, string suffix) => value.ToString("0.###", CultureInfo.InvariantCulture) + suffix;
        private static string FormatNullable(double? value, string suffix) => value.HasValue ? Format(value.Value, suffix) : "—";

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            try { PaletteCoordinator.SetStatus(StatusText.Text); } catch { }
        }
    }
}