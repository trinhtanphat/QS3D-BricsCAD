using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
                SelectedNoteText.Text = "Khối lượng lấy từ cùng một pipeline ProjectQuantityReportBuilder.Detail; cửa sổ này không có công thức tính riêng.";
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
                ? "Read-only • snapshot tách rời • không ghi project/CAD. Trừ giao/mở dùng đúng DeductionM3 của pipeline QS3D."
                : view.Source.Note;
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
                   double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                   IsFiniteNonNegative(value);
        }

        private static bool IsFiniteNonNegative(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;

        private string CurrentSelectedElementId()
        {
            if (WallList.SelectedItem is WallRowView selected) return selected.ElementId;
            if (TakeoffGrid.SelectedItem is WallRowView gridSelected) return gridSelected.ElementId;
            return string.Empty;
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
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
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
