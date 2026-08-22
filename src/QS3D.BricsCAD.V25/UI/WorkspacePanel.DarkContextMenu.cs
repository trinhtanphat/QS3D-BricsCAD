using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only dark chrome for the existing Workspace context menus.
    /// It styles the menus already created by WorkspacePanel.xaml.cs and never
    /// creates commands, changes menu handlers, or mutates CAD/project state.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool _darkContextMenuClassHandlerRegistered = RegisterDarkContextMenuClassHandler();

        private bool _darkContextMenuPresentationApplied;
        private Style? _darkContextMenuStyle;
        private Style? _darkMenuItemStyle;
        private Style? _darkSeparatorStyle;

        private static bool RegisterDarkContextMenuClassHandler()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnDarkContextMenuLoaded),
                true);
            return true;
        }

        private static void OnDarkContextMenuLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel)
                panel.ApplyDarkContextMenuPresentation();
        }

        private void ApplyDarkContextMenuPresentation()
        {
            if (_darkContextMenuPresentationApplied)
                return;

            _darkContextMenuPresentationApplied = true;
            _darkContextMenuStyle = BuildDarkContextMenuStyle();
            _darkMenuItemStyle = BuildDarkMenuItemStyle();
            _darkSeparatorStyle = BuildDarkSeparatorStyle();

            ApplyDarkContextMenu(FamilyList.ContextMenu);
            ApplyDarkContextMenu(InspectionList.ContextMenu);
        }

        private void ApplyDarkContextMenu(ContextMenu? menu)
        {
            if (menu == null || _darkContextMenuStyle == null)
                return;

            menu.Style = _darkContextMenuStyle;
            menu.HasDropShadow = false;
            menu.SnapsToDevicePixels = true;
            menu.UseLayoutRounding = true;

            menu.Opened -= OnDarkContextMenuOpened;
            menu.Opened += OnDarkContextMenuOpened;
            ApplyDarkMenuItems(menu.Items);
        }

        private void OnDarkContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
                ApplyDarkMenuItems(menu.Items);
        }

        private void ApplyDarkMenuItems(IEnumerable items)
        {
            foreach (var raw in items)
            {
                if (raw is Separator separator)
                {
                    if (_darkSeparatorStyle != null)
                        separator.Style = _darkSeparatorStyle;
                    continue;
                }

                if (!(raw is MenuItem item))
                    continue;

                // Current Workspace menus are leaf command items. Keep any future submenu
                // header on its native functional template rather than accidentally removing
                // PART_Popup behavior; its children still receive the dark leaf presentation.
                if (!item.HasItems && _darkMenuItemStyle != null)
                    item.Style = _darkMenuItemStyle;

                if (item.HasItems)
                    ApplyDarkMenuItems(item.Items);
            }
        }

        private Style BuildDarkContextMenuStyle()
        {
            var surface = TryFindResource("BgRaisedBrush") as Brush
                ?? TryFindResource("Bg2Brush") as Brush
                ?? Brushes.Black;
            var text = TryFindResource("TextBrush") as Brush ?? Brushes.White;
            var borderBrush = TryFindResource("BorderStrongBrush") as Brush ?? Brushes.DimGray;

            var template = new ControlTemplate(typeof(ContextMenu));
            var chrome = new FrameworkElementFactory(typeof(Border), "PopupChrome");
            chrome.SetValue(Border.BackgroundProperty, surface);
            chrome.SetValue(Border.BorderBrushProperty, borderBrush);
            chrome.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            chrome.SetValue(Border.PaddingProperty, new Thickness(2));
            chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            var presenter = new FrameworkElementFactory(typeof(ItemsPresenter), "ItemsHost");
            chrome.AppendChild(presenter);
            template.VisualTree = chrome;

            var style = new Style(typeof(ContextMenu));
            style.Setters.Add(new Setter(Control.BackgroundProperty, surface));
            style.Setters.Add(new Setter(Control.ForegroundProperty, text));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, borderBrush));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(2)));
            style.Setters.Add(new Setter(ContextMenu.HasDropShadowProperty, false));
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private Style BuildDarkMenuItemStyle()
        {
            var text = TryFindResource("TextBrush") as Brush ?? Brushes.White;
            var hover = TryFindResource("BgHoverBrush") as Brush ?? Brushes.DimGray;
            var selected = TryFindResource("BgSelectedBrush") as Brush ?? hover;
            var borderBrush = TryFindResource("BorderStrongBrush") as Brush ?? Brushes.DimGray;

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
            header.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            chrome.AppendChild(header);
            template.VisualTree = chrome;

            var highlighted = new Trigger
            {
                Property = MenuItem.IsHighlightedProperty,
                Value = true
            };
            highlighted.Setters.Add(new Setter(Border.BackgroundProperty, hover, "MenuChrome"));
            highlighted.Setters.Add(new Setter(Border.BorderBrushProperty, borderBrush, "MenuChrome"));
            template.Triggers.Add(highlighted);

            var submenuOpen = new Trigger
            {
                Property = MenuItem.IsSubmenuOpenProperty,
                Value = true
            };
            submenuOpen.Setters.Add(new Setter(Border.BackgroundProperty, selected, "MenuChrome"));
            submenuOpen.Setters.Add(new Setter(Border.BorderBrushProperty, borderBrush, "MenuChrome"));
            template.Triggers.Add(submenuOpen);

            var disabledTrigger = new Trigger
            {
                Property = UIElement.IsEnabledProperty,
                Value = false
            };
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.55, "MenuChrome"));
            template.Triggers.Add(disabledTrigger);

            var style = new Style(typeof(MenuItem));
            style.Setters.Add(new Setter(Control.ForegroundProperty, text));
            style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(9, 5, 14, 5)));
            style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 26.0));
            style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
            style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
            style.Setters.Add(new Setter(Control.TemplateProperty, template));

            return style;
        }

        private Style BuildDarkSeparatorStyle()
        {
            var line = TryFindResource("BorderBrush") as Brush ?? Brushes.DimGray;

            var template = new ControlTemplate(typeof(Separator));
            var rule = new FrameworkElementFactory(typeof(Border), "SeparatorRule");
            rule.SetValue(Border.BackgroundProperty, line);
            rule.SetValue(FrameworkElement.HeightProperty, 1.0);
            rule.SetValue(FrameworkElement.MarginProperty, new Thickness(7, 3));
            rule.SetValue(UIElement.SnapsToDevicePixelsProperty, true);
            template.VisualTree = rule;

            var style = new Style(typeof(Separator));
            style.Setters.Add(new Setter(Control.BackgroundProperty, line));
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }
    }
}
