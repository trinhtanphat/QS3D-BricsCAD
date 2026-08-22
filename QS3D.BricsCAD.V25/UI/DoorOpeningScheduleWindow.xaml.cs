using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Export;
using QS3D.Core.Reporting;
using QS3D.Core.Services;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class DoorOpeningScheduleWindow : Window
    {
        private readonly Document _document;
        private IReadOnlyList<DoorOpeningScheduleRow> _rows = Array.Empty<DoorOpeningScheduleRow>();

        private sealed class RowView
        {
            public DoorOpeningScheduleRow Source { get; set; } = null!;
            public string Floor => Source.Floor;
            public string Category => Source.Category;
            public string FamilyName => Source.FamilyName;
            public string Material => Source.Material;
            public double WidthM => Source.WidthM;
            public double HeightM => Source.HeightM;
            public double SillHeightM => Source.SillHeightM;
            public double ThicknessM => Source.ThicknessM;
            public int Count => Source.Count;
            public double OpeningAreaM2 => Source.OpeningAreaM2;
            public int HostCount => Source.HostCount;
            public string HostIdsText => string.Join(";", Source.HostIds);
            public string SearchText => string.Join(" ", Floor, Category, FamilyName, Material, HostIdsText, string.Join(" ", Source.ElementIds)).ToLowerInvariant();
        }

        public DoorOpeningScheduleWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            Loaded += (_, __) => RefreshRows();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshRows();
        private void OnSearchChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_rows.Count == 0) throw new InvalidOperationException("Schedule hiện chưa có dòng để xuất.");
                var drawingName = string.IsNullOrWhiteSpace(_document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(_document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất bảng Cửa / Lỗ mở",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-Cua-Lo-Mo.xlsx"
                };
                if (dialog.ShowDialog() != true) return;
                DoorOpeningXlsxExporter.Export(dialog.FileName, _rows);
                SetStatus("Đã xuất " + _rows.Count + " nhóm Cửa/Lỗ → " + dialog.FileName);
            }
            catch (Exception ex) { SetStatus("Xuất Door/Opening XLSX lỗi: " + ex.Message); }
        }

        private void RefreshRows()
        {
            try
            {
                var project = ProjectContextCoordinator.GetOrCreate(_document);
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                _rows = DoorOpeningScheduleBuilder.Build(project);
                Title = "QS3D • Cửa / Lỗ mở • " + DrawingLabel(_document);
                ApplyFilter();
                SetStatus("Đã nạp " + _rows.Count + " nhóm schedule • regen " + regenerated + " cấu kiện dirty.");
            }
            catch (Exception ex)
            {
                _rows = Array.Empty<DoorOpeningScheduleRow>();
                ApplyFilter();
                SetStatus("Đọc Door/Opening Schedule lỗi: " + ex.Message);
            }
        }

        private void ApplyFilter()
        {
            var query = (SearchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            var views = _rows.Select(x => new RowView { Source = x });
            if (query.Length > 0) views = views.Where(x => x.SearchText.Contains(query));
            var visible = views.ToList();
            ScheduleGrid.ItemsSource = visible;
            GroupCountText.Text = visible.Count.ToString(CultureInfo.InvariantCulture);
            ElementCountText.Text = visible.Sum(x => x.Count).ToString(CultureInfo.InvariantCulture);
            AreaText.Text = visible.Sum(x => x.OpeningAreaM2).ToString("0.###", CultureInfo.InvariantCulture) + " m²";
            HostCountText.Text = visible.SelectMany(x => x.Source.HostIds).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString(CultureInfo.InvariantCulture);
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu";
            try { return Path.GetFileName(name); }
            catch { return name; }
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text ?? string.Empty;
            PaletteCoordinator.SetStatus(StatusText.Text);
        }
    }
}
