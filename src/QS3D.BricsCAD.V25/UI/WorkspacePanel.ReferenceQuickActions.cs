using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Clean-room ergonomic bridge inspired by compact BricsCAD modeling sidebars.
    /// The panel already owns Floor/Zone, category tree, Family mutation and Quick Draw flows;
    /// this partial only makes those authoritative actions reachable beside the model tree.
    /// </summary>
    public partial class WorkspacePanel
    {
        internal static readonly bool ReferenceQuickActionsRegistrationReady = RegisterReferenceQuickActions();

        private bool _referenceQuickActionsApplied;
        private ComboBox? _referenceDrawModeCombo;

        private static bool RegisterReferenceQuickActions()
        {
            try
            {
                EventManager.RegisterClassHandler(
                    typeof(WorkspacePanel),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler(OnReferenceQuickActionsLoaded),
                    true);
                return true;
            }
            catch
            {
                // Presentation-only enhancement: never poison WorkspacePanel type initialization.
                return false;
            }
        }

        private static void OnReferenceQuickActionsLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                panel.ApplyReferenceQuickActions();
        }

        private void ApplyReferenceQuickActions()
        {
            if (_referenceQuickActionsApplied || ModelTree == null)
                return;

            if (!(ModelTree.Parent is DockPanel modelDock))
                return;

            if (modelDock.Children.OfType<FrameworkElement>().Any(element =>
                    string.Equals(element.Tag as string, "QS3D_REFERENCE_QUICK_ACTIONS", StringComparison.Ordinal)))
            {
                _referenceQuickActionsApplied = true;
                return;
            }

            var band = new Border
            {
                Tag = "QS3D_REFERENCE_QUICK_ACTIONS",
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(5)
            };
            var bandStyle = TryFindResource("WorkspaceToolbarBand") as Style;
            if (bandStyle != null)
                band.Style = bandStyle;

            var content = new StackPanel();
            band.Child = content;

            var title = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 0, 0, 4) };
            title.Children.Add(new TextBlock
            {
                Text = "VẼ / NHẬP MÔ HÌNH",
                FontWeight = FontWeights.SemiBold,
                FontSize = 10.5,
                VerticalAlignment = VerticalAlignment.Center
            });
            content.Children.Add(title);

            var drawRow = new DockPanel { LastChildFill = true };
            var drawButton = CreateReferenceQuickButton(
                "Vẽ",
                "Chạy chế độ vẽ đang chọn trên Family / Type hiện hành.",
                OnReferenceDrawClick,
                accent: true);
            drawButton.MinWidth = 42;
            drawButton.Margin = new Thickness(5, 0, 0, 0);
            DockPanel.SetDock(drawButton, Dock.Right);
            drawRow.Children.Add(drawButton);

            _referenceDrawModeCombo = new ComboBox
            {
                MinHeight = 24,
                Padding = new Thickness(4, 0, 4, 0),
                ToolTip = "Chọn workflow vẽ hiện có của QS3D; không tạo engine hình học song song."
            };
            _referenceDrawModeCombo.Items.Add(CreateReferenceDrawMode("Vẽ nhanh", "quick"));
            _referenceDrawModeCombo.Items.Add(CreateReferenceDrawMode("Tùy chỉnh", "advanced"));
            _referenceDrawModeCombo.Items.Add(CreateReferenceDrawMode("Đường", "line"));
            _referenceDrawModeCombo.Items.Add(CreateReferenceDrawMode("Chữ nhật", "rectangle"));
            _referenceDrawModeCombo.Items.Add(CreateReferenceDrawMode("Hình tròn", "circle"));
            _referenceDrawModeCombo.SelectedIndex = 0;
            drawRow.Children.Add(_referenceDrawModeCombo);
            content.Children.Add(drawRow);

            var actionGrid = new Grid { Margin = new Thickness(0, 5, 0, 0) };
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });

            var add = CreateReferenceQuickButton(
                "Thêm",
                "Thêm / nhân bản Family theo handler chuẩn của Workspace.",
                OnReferenceAddClick);
            Grid.SetColumn(add, 0);
            actionGrid.Children.Add(add);

            var delete = CreateReferenceQuickButton(
                "Xóa",
                "Xóa Family chưa được sử dụng; handler chuẩn sẽ chặn Family đang có đối tượng tham chiếu.",
                OnReferenceDeleteClick);
            delete.Margin = new Thickness(3, 0, 0, 0);
            Grid.SetColumn(delete, 1);
            actionGrid.Children.Add(delete);

            var capture = CreateReferenceQuickButton(
                "Bóc chọn",
                "Chọn Category rồi bóc đối tượng CAD đang chọn. QS3D không tự đoán layer/category như một Auto Import giả.",
                OnReferenceCaptureClick);
            capture.Margin = new Thickness(3, 0, 0, 0);
            Grid.SetColumn(capture, 2);
            actionGrid.Children.Add(capture);

            content.Children.Add(actionGrid);

            var hint = new TextBlock
            {
                Text = "Dùng Tầng + Category đang chọn",
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = "Tầng làm việc và cây mô hình bên dưới là scope chuẩn của project QS3D."
            };
            var captionStyle = TryFindResource("Caption") as Style;
            if (captionStyle != null)
                hint.Style = captionStyle;
            content.Children.Add(hint);

            DockPanel.SetDock(band, Dock.Top);
            var modelTreeIndex = modelDock.Children.IndexOf(ModelTree);
            if (modelTreeIndex < 0)
                modelDock.Children.Add(band);
            else
                modelDock.Children.Insert(modelTreeIndex, band);

            _referenceQuickActionsApplied = true;
        }

        private static ComboBoxItem CreateReferenceDrawMode(string label, string mode)
        {
            return new ComboBoxItem { Content = label, Tag = mode };
        }

        private Button CreateReferenceQuickButton(
            string label,
            string toolTip,
            RoutedEventHandler handler,
            bool accent = false)
        {
            var button = new Button
            {
                Content = label,
                ToolTip = toolTip,
                MinHeight = 24,
                Padding = new Thickness(4, 1, 4, 1),
                FontSize = 10.5
            };
            var style = TryFindResource(accent ? "AccentButton" : "DenseButton") as Style;
            if (style != null)
                button.Style = style;
            button.Click += handler;
            return button;
        }

        private void OnReferenceDrawClick(object sender, RoutedEventArgs e)
        {
            var mode = (_referenceDrawModeCombo?.SelectedItem as ComboBoxItem)?.Tag as string ?? "quick";
            switch (mode)
            {
                case "advanced":
                    ExecuteWorkspaceDraw(advanced: true);
                    break;
                case "line":
                    ExecuteWorkspaceBasicDraw("QS3DDRAWLINE", "Đường");
                    break;
                case "rectangle":
                    ExecuteWorkspaceBasicDraw("QS3DDRAWRECT", "Chữ nhật");
                    break;
                case "circle":
                    ExecuteWorkspaceBasicDraw("QS3DDRAWCIRCLE", "Hình tròn");
                    break;
                default:
                    ExecuteWorkspaceDraw(advanced: false);
                    break;
            }
        }

        private void OnReferenceAddClick(object sender, RoutedEventArgs e) => OnAddClick(sender, e);
        private void OnReferenceDeleteClick(object sender, RoutedEventArgs e) => OnDeleteClick(sender, e);
        private void OnReferenceCaptureClick(object sender, RoutedEventArgs e) => OnCaptureSelectedClick(sender, e);
    }
}
