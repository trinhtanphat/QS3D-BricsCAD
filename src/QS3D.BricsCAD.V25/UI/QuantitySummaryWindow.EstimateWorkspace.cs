using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Clean-room estimating workspace navigation inspired by common quantity-surveying layouts.
    /// This strip does not introduce a second calculation engine: every action delegates to an
    /// existing authoritative QS3D command surface.
    /// </summary>
    public partial class QuantitySummaryWindow
    {
        private static bool QuantityEstimateWorkspaceRegistered { get; } = RegisterQuantityEstimateWorkspace();
        private bool _quantityEstimateWorkspaceApplied;

        private static bool RegisterQuantityEstimateWorkspace()
        {
            EventManager.RegisterClassHandler(
                typeof(QuantitySummaryWindow),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnQuantityEstimateWorkspaceLoaded),
                true);
            return true;
        }

        private static void OnQuantityEstimateWorkspaceLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is QuantitySummaryWindow window)
                window.ApplyQuantityEstimateWorkspace();
        }

        private void ApplyQuantityEstimateWorkspace()
        {
            if (_quantityEstimateWorkspaceApplied)
                return;

            if (!(Content is Grid root) || root.RowDefinitions.Count < 4)
                return;

            _quantityEstimateWorkspaceApplied = true;

            // Insert the workspace strip between the filter bar and the quantity-review body.
            // Shift existing body/status rows rather than changing their internal layout/bindings.
            foreach (var child in root.Children.Cast<UIElement>().ToArray())
            {
                var row = Grid.GetRow(child);
                if (row >= 2)
                    Grid.SetRow(child, row + 1);
            }
            root.RowDefinitions.Insert(2, new RowDefinition { Height = GridLength.Auto });

            var shell = new Border
            {
                Margin = new Thickness(12, 8, 12, 0),
                Padding = new Thickness(10, 7, 10, 7),
                CornerRadius = new CornerRadius(4)
            };
            var cardStyle = TryFindResource("Card") as Style;
            if (cardStyle != null)
                shell.Style = cardStyle;

            var dock = new DockPanel { LastChildFill = true };
            shell.Child = dock;

            var context = new StackPanel
            {
                Width = 230,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(context, Dock.Left);
            context.Children.Add(new TextBlock
            {
                Text = "WORKSPACE DỰ TOÁN",
                FontWeight = FontWeights.SemiBold,
                FontSize = 11
            });
            var contextHint = new TextBlock
            {
                Text = "Hồ sơ → khối lượng → bảng CAD",
                Margin = new Thickness(0, 2, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = "Điều hướng các bề mặt QS3D hiện có; không tạo engine đơn giá/nhân công/máy song song."
            };
            var captionStyle = TryFindResource("Caption") as Style;
            if (captionStyle != null)
                contextHint.Style = captionStyle;
            context.Children.Add(contextHint);
            dock.Children.Add(context);

            var scroller = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalAlignment = VerticalAlignment.Center
            };
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            scroller.Content = actions;
            dock.Children.Add(scroller);

            actions.Children.Add(CreateEstimateWorkspaceButton(
                "Hồ sơ / Hạng mục",
                "QS3DPROJECTTOOLS",
                "Mở Project Tools: tầng, vật liệu, template, module và health của hồ sơ QS3D."));

            var current = new Button
            {
                Content = "Khối lượng",
                IsEnabled = false,
                Margin = new Thickness(6, 0, 0, 0),
                ToolTip = "Đang ở Bảng Tổng Hợp Khối Lượng."
            };
            var accentStyle = TryFindResource("AccentButton") as Style;
            if (accentStyle != null)
                current.Style = accentStyle;
            actions.Children.Add(current);

            actions.Children.Add(CreateEstimateWorkspaceButton(
                "Vật liệu",
                "QS3DMATERIALS",
                "Mở danh mục vật liệu QS3D built-in/custom và áp theo semantic selection."));
            actions.Children.Add(CreateEstimateWorkspaceButton(
                "BQ → CAD",
                "QS3DBQTABLE",
                "Tạo/cập nhật native BQ Table từ project state hiện hữu; không tính lại bằng UI phụ."));
            actions.Children.Add(CreateEstimateWorkspaceButton(
                "VL → CAD",
                "QS3DMATERIALTABLE",
                "Tạo/cập nhật native Material Usage Table từ project state hiện hữu."));
            actions.Children.Add(CreateEstimateWorkspaceButton(
                "Schedule Hub",
                "QS3DSCHEDULES",
                "Mở Schedule Hub cho BQ, vật liệu, curtain, cửa/lỗ và cốt thép."));

            Grid.SetRow(shell, 2);
            root.Children.Add(shell);
        }

        private Button CreateEstimateWorkspaceButton(string label, string command, string toolTip)
        {
            var button = new Button
            {
                Content = label,
                Margin = new Thickness(6, 0, 0, 0),
                ToolTip = toolTip
            };
            var style = TryFindResource("DenseButton") as Style;
            if (style != null)
                button.Style = style;
            button.Click += (_, __) => QueueEstimateWorkspaceCommand(command, label);
            return button;
        }

        private void QueueEstimateWorkspaceCommand(string command, string label)
        {
            try
            {
                var active = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (active == null || !ReferenceEquals(active, _document))
                    throw new InvalidOperationException("Hãy kích hoạt lại bản vẽ đang mở BQ trước khi chuyển workspace.");

                PaletteCoordinator.SetStatus("Workspace dự toán → " + label + " • " + command + ".");
                _document.SendStringToExecute(command + " ", true, false, false);
            }
            catch
            {
                const string message = "Không thể mở workspace dự toán này. Hãy thử lại hoặc đóng bảng BQ và mở lại.";
                try { PaletteCoordinator.SetStatus(message); } catch { }
                MessageBox.Show(this, message, "QS3D", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
