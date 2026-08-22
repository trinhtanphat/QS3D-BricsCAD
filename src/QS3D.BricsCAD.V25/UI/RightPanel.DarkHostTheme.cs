using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host-theme guard for RightPanel. It keeps list selection and
    /// Xref/layer context-menu chrome on the QS3D dark palette without touching handlers
    /// or CAD/project mutation paths.
    /// </summary>
    public partial class RightPanel
    {
        private static readonly bool _rightDarkHostThemeGuardRegistered = RegisterRightDarkHostThemeGuard();
        private bool _rightDarkHostThemeApplied;
        private Style? _rightDarkMenuItemStyle;
        private Style? _rightDarkSeparatorStyle;

        private static bool RegisterRightDarkHostThemeGuard()
        {
            EventManager.RegisterClassHandler(
                typeof(RightPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnRightDarkHostThemeLoaded),
                true);
            return true;
        }

        private static void OnRightDarkHostThemeLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RightPanel panel)
                panel.ApplyRightDarkHostTheme();
        }

        private void ApplyRightDarkHostTheme()
        {
            if (_rightDarkHostThemeApplied)
                return;

            _rightDarkHostThemeApplied = true;

            if (TryFindResource("BgSelectedBrush") is Brush selectionBrush)
            {
                PinRightSelectionResource(SystemColors.HighlightBrushKey, selectionBrush);
                PinRightSelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);
            }

            if (TryFindResource("TextBrush") is Brush selectionTextBrush)
            {
                PinRightSelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);
                PinRightSelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);
            }

            _rightDarkMenuItemStyle = BuildRightDarkMenuItemStyle();
            _rightDarkSeparatorStyle = BuildRightDarkSeparatorStyle();
            ApplyRightDarkContextMenu(DrawingList.ContextMenu);
            ApplyRightDarkContextMenu(LayerList.ContextMenu);
        }

        private void PinRightSelectionResource(object key, Brush brush)
        {
            Resources[key] = brush;
            DrawingList.Resources[key] = brush;
            LayerList.Resources[key] = brush;
        }

        private void ApplyRightDarkContextMenu(ContextMenu? menu)
        {
            if (menu == null)
                return;

            menu.Background = TryFindResource("Bg2Brush") as Brush ?? Brushes.Black;
            menu.Foreground = TryFindResource("TextBrush") as Brush ?? Brushes.White;
            menu.BorderBrush = TryFindResource("BorderStrongBrush") as Brush ?? Brushes.DimGray;
            menu.BorderThickness = new Thickness(1);
            menu.Padding = new Thickness(2);
            menu.HasDropShadow = false;
            menu.SnapsToDevicePixels = true;
            menu.UseLayoutRounding = true;

            menu.Opened -= OnRightDarkContextMenuOpened;
            menu.Opened += OnRightDarkContextMenuOpened;
            ApplyRightDarkMenuItems(menu.Items);
        }

        private void OnRightDarkContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
                ApplyRightDarkMenuItems(menu.Items);
        }

        private void ApplyRightDarkMenuItems(IEnumerable items)
        {
            foreach (var raw in items)
            {
                if (raw is Separator separator)
                {
                    if (_rightDarkSeparatorStyle != null)
                        separator.Style = _rightDarkSeparatorStyle;
                    continue;
                }

                if (!(raw is MenuItem item))
                    continue;

                if (!item.HasItems && _rightDarkMenuItemStyle != null)
                    item.Style = _rightDarkMenuItemStyle;

                if (item.HasItems)
                    ApplyRightDarkMenuItems(item.Items);
            }
        }

        private Style BuildRightDarkMenuItemStyle()
        {
            var text = TryFindResource("TextBrush") as Brush ?? Brushes.White;
            var hover = TryFindResource("BgHoverBrush") as Brush ?? Brushes.DimGray;
            var selected = TryFindResource("BgSelectedBrush") as Brush ?? hover;
            var border = TryFindResource("BorderStrongBrush") as Brush ?? Brushes.DimGray;

            var template = new ControlTemplate(typeof(MenuItem));
            var chrome = new FrameworkElementFactory(typeof(Border), "MenuChrome");
            chrome.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            chrome.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
            chrome.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            chrome.SetBinding(Border.PaddingProperty, new Binding("Padding")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
            chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            var header = new FrameworkElementFactory(typeof(ContentPresenter), "HeaderPresenter");
            header.SetBinding(ContentPresenter.ContentProperty, new Binding("Header")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
            header.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding("HeaderTemplate")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
            header.SetBinding(ContentPresenter.ContentStringFormatProperty, new Binding("HeaderStringFormat")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
            });
            header.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            header.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            chrome.AppendChild(header);
            template.VisualTree = chrome;

            var highlighted = new Trigger
            {
                Property = MenuItem.IsHighlightedProperty,
                Value = true
            };
            highlighted.Setters.Add(new Setter(Border.BackgroundProperty, hover, "MenuChrome"));
            highlighted.Setters.Add(new Setter(Border.BorderBrushProperty, border, "MenuChrome"));
            template.Triggers.Add(highlighted);

            var submenuOpen = new Trigger
            {
                Property = MenuItem.IsSubmenuOpenProperty,
                Value = true
            };
            submenuOpen.Setters.Add(new Setter(Border.BackgroundProperty, selected, "MenuChrome"));
            submenuOpen.Setters.Add(new Setter(Border.BorderBrushProperty, border, "MenuChrome"));
            template.Triggers.Add(submenuOpen);

            var disabled = new Trigger
            {
                Property = UIElement.IsEnabledProperty,
                Value = false
            };
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.55, "MenuChrome"));
            template.Triggers.Add(disabled);

            var style = new Style(typeof(MenuItem));
            style.Setters.Add(new Setter(Control.ForegroundProperty, text));
            style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(9, 5, 14, 5)));
            style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 26.0));
            style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private Style BuildRightDarkSeparatorStyle()
        {
            var line = TryFindResource("BorderBrush") as Brush ?? Brushes.DimGray;
            var template = new ControlTemplate(typeof(Separator));
            var rule = new FrameworkElementFactory(typeof(Border), "SeparatorRule");
            rule.SetValue(Border.BackgroundProperty, line);
            rule.SetValue(FrameworkElement.HeightProperty, 1.0);
            rule.SetValue(FrameworkElement.MarginProperty, new Thickness(7, 3, 7, 3));
            rule.SetValue(UIElement.SnapsToDevicePixelsProperty, true);
            template.VisualTree = rule;

            var style = new Style(typeof(Separator));
            style.Setters.Add(new Setter(Control.BackgroundProperty, line));
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }
    }
}
