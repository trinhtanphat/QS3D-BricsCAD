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
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RoomFinishScheduleWindow : Window
    {
        private readonly Document _document;
        private IReadOnlyList<RoomFinishScheduleRow> _rows = Array.Empty<RoomFinishScheduleRow>();

        private sealed class RowView
        {
            public RoomFinishScheduleRow Source { get; set; } = null!;
            public string Floor => Source.Floor;
            public string Room => Source.Room;
            public string Category => Source.Category;
            public string FamilyName => Source.FamilyName;
            public string Material => Source.Material;
            public string UnitHint => Source.UnitHint;
            public int Count => Source.Count;
            public double PrimaryQuantity => Source.PrimaryQuantity;
            public double LengthM => Source.LengthM;
            public double AreaM2 => Source.AreaM2;
            public string RoomIdsText => string.Join(";", Source.RoomIds);
            public string SearchText => string.Join(" ", Floor, Room, Category, FamilyName, Material, RoomIdsText, string.Join(" ", Source.ElementIds)).ToLowerInvariant();
        }

        private sealed class CompensatedVisibleTotal
        {
            private double _sum;
            private double _compensation;

            internal void Add(double value, string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var incoming = QuantityReportMath.NonNegative(value, label);

                var result = _sum + incoming;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Room Finish visible total overflowed: " + label + ".");

                var correction = Math.Abs(_sum) >= Math.Abs(incoming)
                    ? (_sum - result) + incoming
                    : (incoming - result) + _sum;
                var nextCompensation = _compensation + correction;
                if (double.IsNaN(nextCompensation) || double.IsInfinity(nextCompensation))
                    throw new OverflowException("Room Finish visible total compensation overflowed: " + label + ".");

                _sum = result == 0d ? 0d : result;
                _compensation = nextCompensation == 0d ? 0d : nextCompensation;
            }

            internal double Value(string label)
            {
                QuantityReportMath.Finite(_sum, label + "/sum");
                QuantityReportMath.Finite(_compensation, label + "/compensation");
                var result = _sum + _compensation;
                if (double.IsNaN(result) || double.IsInfinity(result))
                    throw new OverflowException("Room Finish visible total overflowed: " + label + ".");
                if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))
                    throw new OverflowException("Room Finish visible total lost a non-zero compensation at floating-point precision: " + label + ".");
                if (_sum != 0d && result == _compensation)
                    throw new OverflowException("Room Finish visible total lost a non-zero accumulated value at floating-point precision: " + label + ".");
                return result == 0d ? 0d : result;
            }

            private static bool IsStrictlyBelowHalfUlp(double current, double compensation)
            {
                if (current <= 0d || compensation == 0d) return false;
                var currentBits = BitConverter.DoubleToInt64Bits(current);
                var adjacentBits = compensation > 0d ? currentBits + 1L : currentBits - 1L;
                var adjacent = BitConverter.Int64BitsToDouble(adjacentBits);
                var spacing = Math.Abs(adjacent - current);
                return Math.Abs(compensation) < spacing / 2d;
            }
        }

        public RoomFinishScheduleWindow(Document document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Loaded += (_, __) => RefreshRows();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshRows();
        private void OnSearchChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("xuất HT_Phòng XLSX");
                var drawingName = string.IsNullOrWhiteSpace(_document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(_document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "Xuất bảng hoàn thiện phòng",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-HT-Phong.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                var current = BuildCurrentRows(out var regenerated);
                if (current.Count == 0) throw new InvalidOperationException("Schedule hiện chưa có dòng để xuất.");
                _rows = current;
                ApplyFilter();
                RoomFinishXlsxExporter.Export(dialog.FileName, current);
                SetStatus("Đã làm mới (preview regen " + regenerated + ") và xuất " + current.Count + " nhóm HT_Phòng → " + dialog.FileName);
            }
            catch (Exception ex) { SetStatus("Xuất HT_Phòng XLSX lỗi: " + ex.Message); }
        }

        private void RefreshRows()
        {
            try
            {
                _rows = BuildCurrentRows(out var regenerated);
                Title = "QS3D • HT_Phòng • " + DrawingLabel(_document);
                ApplyFilter();
                SetStatus("Đã nạp " + _rows.Count + " nhóm HT_Phòng • preview regen " + regenerated + " cấu kiện dirty.");
            }
            catch (Exception ex)
            {
                _rows = Array.Empty<RoomFinishScheduleRow>();
                ApplyFilter();
                SetStatus("Đọc HT_Phòng Schedule lỗi: " + ex.Message);
            }
        }

        private IReadOnlyList<RoomFinishScheduleRow> BuildCurrentRows(out int regenerated)
        {
            EnsureActive("đọc HT_Phòng Schedule hiện hành");
            if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng. Đóng HT_Phòng Schedule và mở lại sau khi nạp project.");
            var snapshot = ProjectStateSnapshot.CreateDetachedCopy(project);
            regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(snapshot);
            return RoomFinishScheduleBuilder.Build(snapshot);
        }

        private void ApplyFilter()
        {
            var query = (SearchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            var views = _rows.Select(x => new RowView { Source = x });
            if (query.Length > 0) views = views.Where(x => x.SearchText.Contains(query));
            var visible = views.ToList();
            ScheduleGrid.ItemsSource = visible;

            var elementCount = 0;
            var length = new CompensatedVisibleTotal();
            var area = new CompensatedVisibleTotal();
            foreach (var row in visible)
            {
                elementCount = QuantityReportMath.AddCount(elementCount, row.Count);
                length.Add(row.LengthM, "HT_Phòng visible length");
                area.Add(row.AreaM2, "HT_Phòng visible area");
            }
            var totalLengthM = length.Value("HT_Phòng visible length");
            var totalAreaM2 = area.Value("HT_Phòng visible area");

            GroupCountText.Text = visible.Count.ToString(CultureInfo.InvariantCulture);
            ElementCountText.Text = elementCount.ToString(CultureInfo.InvariantCulture);
            LengthText.Text = totalLengthM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            AreaText.Text = totalAreaM2.ToString("0.###", CultureInfo.InvariantCulture) + " m²";
        }

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở HT_Phòng Schedule trước khi " + operation + ".");
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
            try { PaletteCoordinator.SetStatus(StatusText.Text); } catch { }
        }
    }
}