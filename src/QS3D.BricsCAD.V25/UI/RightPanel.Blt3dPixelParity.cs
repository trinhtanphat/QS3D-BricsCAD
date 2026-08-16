using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Owner-reference visual pass for the docked drawing/layer manager. Existing Xref/layer
    /// handlers stay authoritative; this only removes extra chrome, fixes ordering and adds glyphs.
    /// </summary>
    public partial class RightPanel
    {
        private static readonly bool Blt3dRightPixelParityRegistered = RegisterBlt3dRightPixelParity();
        private bool _blt3dRightPixelParityApplied;
        private bool _blt3dDrawingColumnsWired;

        private static bool RegisterBlt3dRightPixelParity()
        {
            EventManager.RegisterClassHandler(
                typeof(RightPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBlt3dRightPixelParityLoaded),
                true);
            return true;
        }

        private static void OnBlt3dRightPixelParityLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is RightPanel panel)) return;
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(panel.ApplyBlt3dRightPixelParity));
        }

        private void ApplyBlt3dRightPixelParity()
        {
            if (!Blt3dRightPixelParityRegistered || _blt3dRightPixelParityApplied) return;

            TuneBlt3dRightRows();
            TuneBlt3dRightHeaders();
            TuneBlt3dDrawingActions();
            TuneBlt3dDrawingColumns();
            TuneBlt3dLayerSurface();
            _blt3dRightPixelParityApplied = true;
        }

        private void TuneBlt3dRightRows()
        {
            if (!(Content is Grid root) || root.RowDefinitions.Count < 4) return;

            root.RowDefinitions[0].MinHeight = 150;
            root.RowDefinitions[0].Height = new GridLength(282);
            root.RowDefinitions[1].Height = new GridLength(4);
            root.RowDefinitions[3].MinHeight = 0;
            root.RowDefinitions[3].Height = new GridLength(0);

            foreach (var border in root.Children.OfType<Border>().Where(border => Grid.GetRow(border) == 3))
                border.Visibility = Visibility.Collapsed;
        }

        private void TuneBlt3dRightHeaders()
        {
            foreach (var text in FindRightVisualChildren<TextBlock>(this))
            {
                if (string.Equals(text.Text, "Xref / Drawing", StringComparison.Ordinal) ||
                    string.Equals(text.Text, "Hiện / Ẩn / Khóa / Màu native", StringComparison.Ordinal) ||
                    string.Equals(text.Text, "Tìm lớp", StringComparison.Ordinal))
                    text.Visibility = Visibility.Collapsed;

                if (!string.IsNullOrWhiteSpace(text.Text) &&
                    text.Text.StartsWith("Tick đầu = hiện/ẩn", StringComparison.Ordinal))
                {
                    text.Text = "Bỏ tick = ẩn. Click chọn lớp, Ctrl+click thêm/bớt, Shift+click chọn dải — rồi tick 1 lớp là áp cho CẢ CỤM.";
                    text.Margin = new Thickness(0, 7, 0, 5);
                }
            }

            CollapseHeaderBadges(DrawingHeaderGrid);
            CollapseHeaderBadges(LayerHeaderGrid);

            var refresh = FindRightButton("Làm mới");
            if (refresh != null)
            {
                refresh.Content = "Làm mới";
                refresh.Padding = new Thickness(5, 2, 5, 2);
                refresh.ToolTip = "Làm mới danh sách lớp";
                Blt3dVectorIcon.Apply(refresh, Blt3dVectorIcon.Reload);
            }
        }

        private static void CollapseHeaderBadges(Grid header)
        {
            if (header == null) return;
            foreach (var stack in header.Children.OfType<StackPanel>())
                foreach (var badge in stack.Children.OfType<Border>())
                    badge.Visibility = Visibility.Collapsed;
        }

        private void TuneBlt3dDrawingActions()
        {
            foreach (var button in FindRightVisualChildren<Button>(this))
            {
                var label = button.Content as string;
                if (string.Equals(label, "Khóa", StringComparison.Ordinal) ||
                    string.Equals(label, "Mở khóa", StringComparison.Ordinal))
                {
                    button.Visibility = Visibility.Collapsed;
                    continue;
                }

                if (string.Equals(label, "+ Thêm", StringComparison.Ordinal))
                    Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Add);
                else if (string.Equals(label, "Nạp", StringComparison.Ordinal) ||
                         string.Equals(label, "Nạp lại", StringComparison.Ordinal))
                    Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Reload);
                else if (string.Equals(label, "Di chuyển", StringComparison.Ordinal))
                    Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Move);
                else if (string.Equals(label, "Xóa", StringComparison.Ordinal) ||
                         string.Equals(label, "Gỡ Xref", StringComparison.Ordinal))
                    Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Delete);
                else if (string.Equals(label, "Khoanh vùng", StringComparison.Ordinal))
                    Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Zoom);
                else if (string.Equals(label, "Hiện", StringComparison.Ordinal))
                    Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Eye);
                else if (string.Equals(label, "Ẩn", StringComparison.Ordinal))
                    Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.EyeOff);
                else if (string.Equals(label, "Đảo", StringComparison.Ordinal) ||
                         string.Equals(label, "Đảo chọn", StringComparison.Ordinal))
                    Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Invert);
                else if (string.Equals(label, "Bỏ chọn", StringComparison.Ordinal))
                    Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Clear);
            }

            var delete = FindRightButton("Xóa") ?? FindRightButton("Gỡ Xref");
            var zoom = FindRightButton("Khoanh vùng");
            if (delete != null && zoom != null && ReferenceEquals(delete.Parent, zoom.Parent) && delete.Parent is Panel parent)
            {
                var zoomIndex = parent.Children.IndexOf(zoom);
                var deleteIndex = parent.Children.IndexOf(delete);
                if (zoomIndex >= 0 && deleteIndex > zoomIndex)
                {
                    parent.Children.Remove(delete);
                    parent.Children.Insert(zoomIndex, delete);
                }
            }
        }

        private void TuneBlt3dDrawingColumns()
        {
            if (!(DrawingList.View is GridView gridView) || gridView.Columns.Count < 4) return;

            void ApplyColumns()
            {
                var width = DrawingList.ActualWidth > 0 ? DrawingList.ActualWidth : Math.Max(260d, ActualWidth - 16d);
                var lockWidth = 48d;
                var scaleWidth = 64d;
                var chrome = 24d;

                gridView.Columns[0].Header = "Tên";
                gridView.Columns[0].Width = Math.Max(105d, width - lockWidth - scaleWidth - chrome);
                gridView.Columns[1].Header = "Khóa";
                gridView.Columns[1].Width = lockWidth;
                gridView.Columns[2].Header = string.Empty;
                gridView.Columns[2].Width = 0d;
                gridView.Columns[3].Header = "Tỉ lệ";
                gridView.Columns[3].Width = scaleWidth;
            }

            if (!_blt3dDrawingColumnsWired)
            {
                DrawingList.SizeChanged += (_, __) => ApplyColumns();
                _blt3dDrawingColumnsWired = true;
            }
            ApplyColumns();
        }

        private void TuneBlt3dLayerSurface()
        {
            LayerSearchBox.Margin = new Thickness(0, 0, 0, 6);

            foreach (var button in FindRightVisualChildren<Button>(this))
            {
                if (!string.Equals(button.Content as string, "Đảo chọn", StringComparison.Ordinal)) continue;
                button.Content = "Đảo";
                Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Invert);
            }
        }
    }
}
