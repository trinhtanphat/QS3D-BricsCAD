using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Final visual-only presentation pass for the owner-provided BLT3D BIM reference.
    /// Existing production handlers and the native BricsCAD viewport remain authoritative.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool Blt3dPixelParityRegistered = RegisterBlt3dPixelParity();
        private bool _blt3dPixelParityApplied;
        private bool _blt3dPixelParityFooterWired;

        private static bool RegisterBlt3dPixelParity()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBlt3dPixelParityLoaded),
                true);
            return true;
        }

        private static void OnBlt3dPixelParityLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel)) return;
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(panel.ApplyBlt3dPixelParity));
        }

        private void ApplyBlt3dPixelParity()
        {
            if (!Blt3dPixelParityRegistered || _blt3dPixelParityApplied) return;

            CollapseBlt3dWorkspaceHeader();
            TuneBlt3dWorkspaceActions();
            TuneBlt3dModelTreeIcons();
            TuneBlt3dFooter();
            _blt3dPixelParityApplied = true;
        }

        private void CollapseBlt3dWorkspaceHeader()
        {
            var root = WorkspaceContentRoot;
            if (root == null || root.RowDefinitions.Count < 3) return;

            root.RowDefinitions[0].MinHeight = 0;
            root.RowDefinitions[0].Height = new GridLength(0);
            root.RowDefinitions[2].Height = new GridLength(30);

            foreach (var border in root.Children.OfType<Border>().Where(border => Grid.GetRow(border) == 0))
                border.Visibility = Visibility.Collapsed;
        }

        private void TuneBlt3dWorkspaceActions()
        {
            var add = FindButton("+ Add") ?? FindButton("+ Thêm");
            if (add != null)
            {
                add.Content = "+ Add";
                add.Padding = new Thickness(8, 2, 8, 2);
                Blt3dVectorIcon.Apply(add, Blt3dVectorIcon.Add);
            }

            var delete = FindButton("Delete") ?? FindButton("Xóa");
            if (delete != null)
            {
                delete.Content = "Delete";
                delete.Padding = new Thickness(8, 2, 8, 2);
                Blt3dVectorIcon.Apply(delete, Blt3dVectorIcon.Delete);
            }

            var import = FindButton("⚡ Nhập từ chọn") ?? FindButton("Bóc chọn");
            if (import != null)
            {
                import.Content = "Nhập tự động";
                import.Padding = new Thickness(8, 2, 8, 2);
                import.ToolTip = "Nhập tự động các đối tượng CAD đang chọn vào đúng nhóm/Family hiện hành; không quét nền toàn DWG.";
                Blt3dVectorIcon.Apply(import, Blt3dVectorIcon.Bolt);
            }

            var model = FindButton("Mô hình");
            if (model != null) model.Padding = new Thickness(10, 2, 10, 2);

            var quantity = FindButton("BQ");
            if (quantity != null) quantity.Padding = new Thickness(10, 2, 10, 2);

            var health = FindButton("Kiểm tra");
            if (health != null) health.Visibility = Visibility.Collapsed;
        }

        private void TuneBlt3dModelTreeIcons()
        {
            foreach (var item in ModelTree.Items.OfType<TreeViewItem>())
            {
                var header = item.Header as string ?? string.Empty;
                Blt3dVectorIcon.Apply(item, IconForBlt3dModelHeader(header));

                foreach (var child in item.Items.OfType<TreeViewItem>())
                    Blt3dVectorIcon.Apply(child, IconForBlt3dModelHeader(child.Header as string ?? header));
            }
        }

        private static string IconForBlt3dModelHeader(string header)
        {
            if (header.IndexOf("Lưới", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Grid;
            if (header.IndexOf("Phòng", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Room;
            if (header.IndexOf("Dầm", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Beam;
            if (header.IndexOf("Sàn", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Slab;
            if (header.IndexOf("Cột", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Column;
            if (header.IndexOf("Vách", StringComparison.OrdinalIgnoreCase) >= 0 ||
                header.IndexOf("Tường", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Wall;
            if (header.IndexOf("Cửa", StringComparison.OrdinalIgnoreCase) >= 0 ||
                header.IndexOf("Lỗ Mở", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Door;
            if (header.IndexOf("Cầu Thang", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Stair;
            if (header.IndexOf("Móng", StringComparison.OrdinalIgnoreCase) >= 0 ||
                header.IndexOf("Cọc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                header.IndexOf("Đài", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Foundation;
            if (header.IndexOf("Đào", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Earth;
            if (header.IndexOf("thép", StringComparison.OrdinalIgnoreCase) >= 0) return Blt3dVectorIcon.Steel;
            return Blt3dVectorIcon.Other;
        }

        private void TuneBlt3dFooter()
        {
            foreach (var text in FindVisualChildren<TextBlock>(this))
            {
                if (string.Equals(text.Text, "LIVE SEMANTIC", StringComparison.Ordinal) ||
                    (!string.IsNullOrWhiteSpace(text.Text) && text.Text.IndexOf("VIEWPORT BRICSCAD", StringComparison.OrdinalIgnoreCase) >= 0))
                    text.Visibility = Visibility.Collapsed;
            }

            if (!_blt3dPixelParityFooterWired)
            {
                FloorCombo.SelectionChanged += (_, __) => RefreshBlt3dFooterContext();
                ZoneCombo.SelectionChanged += (_, __) => RefreshBlt3dFooterContext();
                DataContextChanged += (_, __) => RefreshBlt3dFooterContext();
                IsVisibleChanged += (_, __) => RefreshBlt3dFooterContext();
                _blt3dPixelParityFooterWired = true;
            }

            RefreshBlt3dFooterContext();
        }

        private void RefreshBlt3dFooterContext()
        {
            var context = FindVisualChildren<TextBlock>(this)
                .FirstOrDefault(text => string.Equals(text.ToolTip as string, "Project / Zone / Floor / Cao độ hiện hành", StringComparison.Ordinal));
            if (context == null) return;

            var floorName = FloorCombo.SelectedItem as string;
            var elevation = "—";
            try
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                if (document != null && ExistingProjectMutationContext.TryGet(document, out var project))
                {
                    var floor = project.FindFloor(project.ActiveFloorId);
                    if (floor != null)
                    {
                        if (!string.IsNullOrWhiteSpace(floor.Name)) floorName = floor.Name.Trim();
                        elevation = floor.ElevationM.ToString("0.000", CultureInfo.InvariantCulture) + " m";
                    }
                }
            }
            catch
            {
                // Footer parity is presentation-only and must never break workspace interaction.
            }

            var normalizedFloorName = string.IsNullOrWhiteSpace(floorName) ? "—" : floorName!.Trim();
            context.Text = "Tầng " + normalizedFloorName + "    Cao độ " + elevation;
            context.TextAlignment = TextAlignment.Left;
            context.Margin = new Thickness(10, 0, 10, 0);
        }
    }
}
