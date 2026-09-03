using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Read-only Project Information canvas. Every refresh resolves the current QS3D project from
    /// the document supplied by the native palette coordinator; the panel never creates, mutates,
    /// regenerates, saves, or retains semantic/native document state.
    /// </summary>
    internal sealed class BltProjectSetupPanel : UserControl
    {
        private readonly TextBlock _heading;
        private readonly TextBlock _status;
        private readonly TextBlock _projectId;
        private readonly TextBlock _drawing;
        private readonly TextBlock _fingerprint;
        private readonly TextBlock _activeZone;
        private readonly TextBlock _activeFloor;
        private readonly TextBlock _schema;
        private readonly TextBlock _changeVersion;
        private readonly TextBlock _updatedUtc;
        private readonly TextBlock _counts;

        public BltProjectSetupPanel()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));

            var body = new StackPanel { Margin = new Thickness(28) };
            _heading = new TextBlock
            {
                Text = "Thông tin dự án",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };
            body.Children.Add(_heading);

            _status = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(174, 174, 174)),
                Margin = new Thickness(0, 0, 0, 18)
            };
            body.Children.Add(_status);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.Children.Add(grid);

            _projectId = AddRow(grid, 0, "Project ID");
            _drawing = AddRow(grid, 1, "Bản vẽ");
            _fingerprint = AddRow(grid, 2, "Drawing fingerprint");
            _activeZone = AddRow(grid, 3, "Zone active");
            _activeFloor = AddRow(grid, 4, "Tầng active");
            _schema = AddRow(grid, 5, "Schema");
            _changeVersion = AddRow(grid, 6, "Change version");
            _updatedUtc = AddRow(grid, 7, "Cập nhật UTC");
            _counts = AddRow(grid, 8, "Nội dung project");

            Content = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = body
            };

            ShowUnavailable("Không có bản vẽ QS3D đang active.");
        }

        public void RefreshFromDocument(Document? document)
        {
            if (document == null)
            {
                ShowUnavailable("Không có bản vẽ đang active; Project Information đã xóa dữ liệu cũ.");
                return;
            }

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    ShowUnavailable("Bản vẽ hiện hành chưa có QS3D project. Project Information chỉ đọc và không tự tạo project.");
                    return;
                }

                var activeZone = ResolveActiveZone(project);
                var activeFloor = ResolveActiveFloor(project);
                _heading.Text = project.Name;
                _status.Text = "Chỉ đọc • dữ liệu được resolve lại từ project của bản vẽ active mỗi lần mở/chuyển bản vẽ.";
                _projectId.Text = Display(project.ProjectId);
                _drawing.Text = Display(DrawingLabel(document, project));
                _fingerprint.Text = Display(project.DrawingFingerprint);
                _activeZone.Text = activeZone;
                _activeFloor.Text = activeFloor;
                _schema.Text = project.SchemaVersion.ToString(CultureInfo.InvariantCulture);
                _changeVersion.Text = project.ChangeVersion.ToString(CultureInfo.InvariantCulture);
                _updatedUtc.Text = project.UpdatedUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
                _counts.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Zone {0} • Tầng {1} • Family {2} • Element {3} • Rule KL {4}",
                    project.Zones.Count,
                    project.Floors.Count,
                    project.Families.Count,
                    project.Elements.Count,
                    project.QuantityRules.Count);
            }
            catch (Exception)
            {
                ShowUnavailable("Không thể đọc Project Information an toàn từ bản vẽ hiện hành. Dữ liệu cũ đã được xóa; hãy kiểm tra project/sidecar rồi thử lại.");
            }
        }

        public void ShowUnavailable(string status)
        {
            _heading.Text = "Thông tin dự án";
            _status.Text = string.IsNullOrWhiteSpace(status) ? "Project Information không khả dụng." : status.Trim();
            _projectId.Text = "—";
            _drawing.Text = "—";
            _fingerprint.Text = "—";
            _activeZone.Text = "—";
            _activeFloor.Text = "—";
            _schema.Text = "—";
            _changeVersion.Text = "—";
            _updatedUtc.Text = "—";
            _counts.Text = "—";
        }

        private static TextBlock AddRow(Grid grid, int row, string label)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var labelText = new TextBlock
            {
                Text = label,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(174, 174, 174)),
                Margin = new Thickness(0, 5, 16, 5),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRow(labelText, row);
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);

            var valueText = new TextBlock
            {
                Text = "—",
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 5)
            };
            Grid.SetRow(valueText, row);
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);
            return valueText;
        }

        private static string ResolveActiveZone(ProjectState project)
        {
            if (string.IsNullOrWhiteSpace(project.ActiveZoneId)) return "—";
            var zone = project.FindZone(project.ActiveZoneId);
            return zone == null ? project.ActiveZoneId + " (không resolve)" : zone.Name + " • " + zone.Id;
        }

        private static string ResolveActiveFloor(ProjectState project)
        {
            if (string.IsNullOrWhiteSpace(project.ActiveFloorId)) return "—";
            var floor = project.FindFloor(project.ActiveFloorId);
            return floor == null
                ? project.ActiveFloorId + " (không resolve)"
                : floor.Name + " • " + floor.Id + " • Z=" + floor.ElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m";
        }

        private static string DrawingLabel(Document document, ProjectState project)
        {
            var name = string.Empty;
            try { name = document.Name ?? string.Empty; } catch { }
            if (string.IsNullOrWhiteSpace(name)) name = project.DrawingPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu";
            try
            {
                var fileName = Path.GetFileName(name);
                return string.IsNullOrWhiteSpace(fileName) ? name : fileName;
            }
            catch
            {
                return name;
            }
        }

        private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value!.Trim();
    }
}