using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only selected-target drilldown for the existing RightPanel. The cards bind
    /// to DrawingList/LayerList selection state and deliberately introduce no CAD or semantic
    /// mutation path. Registration is independent of the compact-shell partial so startup/lifecycle
    /// ownership remains unchanged.
    /// </summary>
    public partial class RightPanel
    {
        private static readonly bool ReferenceDetailRegistrationReady = RegisterReferenceDetail();
        private bool _referenceDetailApplied;

        private static bool RegisterReferenceDetail()
        {
            EventManager.RegisterClassHandler(
                typeof(RightPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnReferenceDetailLoaded),
                true);
            return true;
        }

        private static void OnReferenceDetailLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RightPanel panel)
                panel.ApplyReferenceDetailPresentation();
        }

        private void ApplyReferenceDetailPresentation()
        {
            if (_referenceDetailApplied)
                return;

            var drawingHost = FindReferenceDetailHost(0);
            var layerHost = FindReferenceDetailHost(2);
            if (drawingHost == null || layerHost == null)
                return;

            drawingHost.Children.Add(CreateDrawingReferenceDetailCard());
            layerHost.Children.Add(CreateLayerReferenceDetailCard());
            _referenceDetailApplied = true;
        }

        private StackPanel? FindReferenceDetailHost(int row)
        {
            if (!(Content is Grid root))
                return null;

            var section = root.Children
                .OfType<Border>()
                .FirstOrDefault(border => Grid.GetRow(border) == row);
            if (!(section?.Child is DockPanel dock))
                return null;

            return dock.Children
                .OfType<StackPanel>()
                .FirstOrDefault(panel => DockPanel.GetDock(panel) == Dock.Top);
        }

        private Border CreateDrawingReferenceDetailCard()
        {
            var card = CreateReferenceDetailCard();
            var content = CreateReferenceDetailGrid();
            card.Child = content;

            content.Children.Add(CreateReferenceDetailCaption("BẢN VẼ / XREF ĐANG CHỌN"));

            var name = CreateReferenceDetailPrimaryText();
            name.SetBinding(TextBlock.TextProperty, SelectedItemBinding(DrawingList, "Name", "Chưa chọn bản vẽ/Xref"));
            name.SetBinding(FrameworkElement.ToolTipProperty, SelectedItemBinding(DrawingList, "Name", "Chưa chọn bản vẽ/Xref"));
            Grid.SetRow(name, 1);
            content.Children.Add(name);

            var meta = CreateReferenceDetailMetaText();
            var metaBinding = new MultiBinding
            {
                StringFormat = "Loại: {0}  •  Khóa: {1}  •  SL: {2}  •  Tỉ lệ: {3}"
            };
            metaBinding.Bindings.Add(SelectedItemBinding(DrawingList, "Kind", "—"));
            metaBinding.Bindings.Add(SelectedItemBinding(DrawingList, "LockState", "—"));
            metaBinding.Bindings.Add(SelectedItemBinding(DrawingList, "InstanceText", "—"));
            metaBinding.Bindings.Add(SelectedItemBinding(DrawingList, "ScaleText", "—"));
            meta.SetBinding(TextBlock.TextProperty, metaBinding);
            Grid.SetRow(meta, 2);
            content.Children.Add(meta);

            var path = CreateReferenceDetailMetaText();
            path.TextTrimming = TextTrimming.CharacterEllipsis;
            path.SetBinding(TextBlock.TextProperty, SelectedItemBinding(DrawingList, "Path", "Chọn một dòng để xem đường dẫn/tham chiếu."));
            path.SetBinding(FrameworkElement.ToolTipProperty, SelectedItemBinding(DrawingList, "Path", "Chọn một dòng để xem đường dẫn/tham chiếu."));
            Grid.SetRow(path, 3);
            content.Children.Add(path);

            return card;
        }

        private Border CreateLayerReferenceDetailCard()
        {
            var card = CreateReferenceDetailCard();
            var content = CreateReferenceDetailGrid();
            card.Child = content;

            content.Children.Add(CreateReferenceDetailCaption("LỚP ĐANG CHỌN"));

            var nameRow = new Grid();
            nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(nameRow, 1);
            content.Children.Add(nameRow);

            var swatch = new Border
            {
                Width = 11,
                Height = 11,
                CornerRadius = new CornerRadius(2),
                BorderThickness = new Thickness(1),
                BorderBrush = ResourceBrush("BorderStrongBrush", Brushes.Gray),
                Margin = new Thickness(0, 1, 6, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            swatch.SetBinding(Border.BackgroundProperty, SelectedItemBinding(LayerList, "ColorBrush", Brushes.Transparent));
            nameRow.Children.Add(swatch);

            var name = CreateReferenceDetailPrimaryText();
            name.SetBinding(TextBlock.TextProperty, SelectedItemBinding(LayerList, "Name", "Chưa chọn lớp"));
            name.SetBinding(FrameworkElement.ToolTipProperty, SelectedItemBinding(LayerList, "Name", "Chưa chọn lớp"));
            Grid.SetColumn(name, 1);
            nameRow.Children.Add(name);

            var meta = CreateReferenceDetailMetaText();
            var metaBinding = new MultiBinding
            {
                StringFormat = "Hiện: {0}  •  Khóa: {1}  •  ACI: {2}"
            };
            metaBinding.Bindings.Add(SelectedItemBinding(LayerList, "IsVisible", false));
            metaBinding.Bindings.Add(SelectedItemBinding(LayerList, "IsLocked", false));
            metaBinding.Bindings.Add(SelectedItemBinding(LayerList, "ColorIndex", "—"));
            meta.SetBinding(TextBlock.TextProperty, metaBinding);
            Grid.SetRow(meta, 2);
            content.Children.Add(meta);

            var hint = CreateReferenceDetailMetaText();
            hint.Text = "Ctrl/Shift chọn nhiều lớp; card hiển thị lớp focus hiện tại.";
            Grid.SetRow(hint, 3);
            content.Children.Add(hint);

            return card;
        }

        private Border CreateReferenceDetailCard()
        {
            var card = new Border
            {
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 0, 5),
                CornerRadius = new CornerRadius(4),
                Background = ResourceBrush("Bg2Brush", Brushes.Transparent),
                BorderBrush = ResourceBrush("BorderBrush", Brushes.Gray),
                BorderThickness = new Thickness(1)
            };

            if (TryFindResource("PremiumCard") is Style premiumCard)
                card.Style = premiumCard;

            return card;
        }

        private static Grid CreateReferenceDetailGrid()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            return grid;
        }

        private TextBlock CreateReferenceDetailCaption(string text) => new TextBlock
        {
            Text = text,
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Foreground = ResourceBrush("LuxuryBrush", ResourceBrush("MutedBrush", Brushes.Gray)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 0, 1)
        };

        private TextBlock CreateReferenceDetailPrimaryText() => new TextBlock
        {
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = ResourceBrush("TextBrush", Brushes.White),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            MinWidth = 0
        };

        private TextBlock CreateReferenceDetailMetaText() => new TextBlock
        {
            FontSize = 9.5,
            Foreground = ResourceBrush("MutedBrush", Brushes.Gray),
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            MinWidth = 0,
            Margin = new Thickness(0, 1, 0, 0)
        };

        private static Binding SelectedItemBinding(ListView list, string property, object fallback)
        {
            return new Binding("SelectedItem." + property)
            {
                Source = list,
                Mode = BindingMode.OneWay,
                FallbackValue = fallback,
                TargetNullValue = fallback
            };
        }

        private Brush ResourceBrush(string key, Brush fallback) =>
            TryFindResource(key) as Brush ?? fallback;
    }
}
