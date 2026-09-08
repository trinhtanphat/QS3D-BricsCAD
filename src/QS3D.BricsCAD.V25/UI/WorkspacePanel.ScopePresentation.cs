using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Presentation-only host adapter for workspace scope selectors and the runtime Project Browser tabs.
    /// No sentinel Zone/Floor values enter the view model, Core model, persistence, or CAD mutation paths.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool WorkspaceScopePresentationHandlersRegistered = RegisterWorkspaceScopePresentationHandlers();

        private bool _workspaceScopePresentationAttached;
        private bool _workspaceScopePresentationRefreshQueued;
        private INotifyCollectionChanged? _workspaceZoneCollection;
        private INotifyCollectionChanged? _workspaceFloorCollection;
        private Style? _workspaceBrowserTabStyle;

        private static bool RegisterWorkspaceScopePresentationHandlers()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnWorkspaceScopePresentationLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnWorkspaceScopePresentationUnloaded),
                true);
            return true;
        }

        private static void OnWorkspaceScopePresentationLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel) panel.AttachWorkspaceScopePresentation();
        }

        private static void OnWorkspaceScopePresentationUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is WorkspacePanel panel) panel.DetachWorkspaceScopePresentation();
        }

        private void AttachWorkspaceScopePresentation()
        {
            if (!WorkspaceScopePresentationHandlersRegistered)
                throw new InvalidOperationException("Workspace scope presentation handlers were not registered.");

            if (!_workspaceScopePresentationAttached)
            {
                _workspaceScopePresentationAttached = true;
                DataContextChanged += OnWorkspaceScopePresentationDataContextChanged;
            }

            QueueWorkspaceScopePresentationRefresh();
        }

        private void DetachWorkspaceScopePresentation()
        {
            if (!_workspaceScopePresentationAttached) return;
            _workspaceScopePresentationAttached = false;
            _workspaceScopePresentationRefreshQueued = false;
            DataContextChanged -= OnWorkspaceScopePresentationDataContextChanged;
            RebindWorkspaceScopeCollection(ref _workspaceZoneCollection, null);
            RebindWorkspaceScopeCollection(ref _workspaceFloorCollection, null);
        }

        private void OnWorkspaceScopePresentationDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            QueueWorkspaceScopePresentationRefresh();
        }

        private void OnWorkspaceScopeCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            QueueWorkspaceScopePresentationRefresh();
        }

        private void QueueWorkspaceScopePresentationRefresh()
        {
            if (!_workspaceScopePresentationAttached || !IsLoaded || _workspaceScopePresentationRefreshQueued) return;
            _workspaceScopePresentationRefreshQueued = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() =>
                {
                    _workspaceScopePresentationRefreshQueued = false;
                    if (!_workspaceScopePresentationAttached || !IsLoaded) return;
                    ApplyWorkspaceScopePresentation();
                }));
        }

        private void ApplyWorkspaceScopePresentation()
        {
            RebindWorkspaceScopeCollection(
                ref _workspaceZoneCollection,
                ZoneCombo.ItemsSource as INotifyCollectionChanged);
            RebindWorkspaceScopeCollection(
                ref _workspaceFloorCollection,
                FloorCombo.ItemsSource as INotifyCollectionChanged);

            ApplyWorkspaceScopeComboState(ZoneCombo, "Không có Zone");
            ApplyWorkspaceScopeComboState(FloorCombo, "Không có Tầng");
            ApplyWorkspaceBrowserTabStyle();
        }

        private void RebindWorkspaceScopeCollection(
            ref INotifyCollectionChanged? current,
            INotifyCollectionChanged? next)
        {
            if (ReferenceEquals(current, next)) return;
            if (current != null) current.CollectionChanged -= OnWorkspaceScopeCollectionChanged;
            current = next;
            if (current != null) current.CollectionChanged += OnWorkspaceScopeCollectionChanged;
        }

        private static void ApplyWorkspaceScopeComboState(ComboBox combo, string emptyText)
        {
            if (combo.Items.Count == 0)
            {
                // Theme.xaml already exposes an editable-text path. Reuse it only as a visible
                // no-data presenter; hit testing/tab focus stay disabled so this is not a fake option.
                combo.IsEditable = true;
                combo.IsReadOnly = true;
                combo.Text = emptyText;
                combo.IsHitTestVisible = false;
                combo.IsTabStop = false;
                combo.Opacity = 0.78d;
                combo.ToolTip = emptyText;
                return;
            }

            combo.IsEditable = false;
            combo.IsReadOnly = false;
            combo.IsHitTestVisible = true;
            combo.IsTabStop = true;
            combo.Opacity = 1d;
            combo.ClearValue(ComboBox.TextProperty);
            combo.ClearValue(FrameworkElement.ToolTipProperty);
        }

        private void ApplyWorkspaceBrowserTabStyle()
        {
            // ProjectBrowser builds these tabs at runtime. The dispatcher hop from Loaded guarantees
            // the surface exists regardless of class-handler registration order across partial files.
            if (_browserTabs == null) return;
            var style = _workspaceBrowserTabStyle ?? (_workspaceBrowserTabStyle = BuildWorkspaceBrowserTabItemStyle());
            _browserTabs.ItemContainerStyle = style;
            foreach (var rawItem in _browserTabs.Items)
            {
                if (rawItem is TabItem item) item.Style = style;
            }
        }

        private Style BuildWorkspaceBrowserTabItemStyle()
        {
            var textBrush = TryFindResource("TextBrush") as Brush ?? Brushes.White;
            var disabledTextBrush = TryFindResource("DisabledTextBrush") as Brush ?? Brushes.Gray;
            var idleBrush = TryFindResource("Bg2Brush") as Brush ?? Brushes.DimGray;
            var selectedBrush = TryFindResource("Bg1Brush") as Brush ?? Brushes.Black;
            var hoverBrush = TryFindResource("BgHoverBrush") as Brush ?? Brushes.DimGray;
            var borderBrush = TryFindResource("BorderStrongBrush") as Brush ?? Brushes.Gray;
            var focusBrush = TryFindResource("BorderFocusBrush") as Brush ?? Brushes.LightBlue;
            var accentBrush = TryFindResource("AccentBrush") as Brush ?? Brushes.DodgerBlue;

            var style = new Style(typeof(TabItem));
            style.Setters.Add(new Setter(Control.ForegroundProperty, textBrush));
            style.Setters.Add(new Setter(Control.BackgroundProperty, idleBrush));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, borderBrush));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 3, 8, 3)));
            style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 2, 0)));
            style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 24d));
            style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));

            var template = new ControlTemplate(typeof(TabItem));
            var chrome = new FrameworkElementFactory(typeof(Border)) { Name = "Chrome" };
            chrome.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            chrome.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            chrome.SetValue(Border.BorderThicknessProperty, new Thickness(1, 1, 1, 0));
            chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(4, 4, 0, 0));
            chrome.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            chrome.SetValue(FrameworkElement.SnapsToDevicePixelsProperty, true);

            var header = new FrameworkElementFactory(typeof(ContentPresenter));
            header.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            header.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            header.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            chrome.AppendChild(header);
            template.VisualTree = chrome;

            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(TargetSetter(Border.BackgroundProperty, hoverBrush, "Chrome"));
            hover.Setters.Add(TargetSetter(Border.BorderBrushProperty, focusBrush, "Chrome"));
            template.Triggers.Add(hover);

            var selected = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
            selected.Setters.Add(new Setter(Control.ForegroundProperty, textBrush));
            selected.Setters.Add(TargetSetter(Border.BackgroundProperty, selectedBrush, "Chrome"));
            selected.Setters.Add(TargetSetter(Border.BorderBrushProperty, accentBrush, "Chrome"));
            template.Triggers.Add(selected);

            var keyboardFocus = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
            keyboardFocus.Setters.Add(TargetSetter(Border.BorderBrushProperty, focusBrush, "Chrome"));
            template.Triggers.Add(keyboardFocus);

            var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(Control.ForegroundProperty, disabledTextBrush));
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.75d));
            disabled.Setters.Add(TargetSetter(Border.BackgroundProperty, selectedBrush, "Chrome"));
            disabled.Setters.Add(TargetSetter(Border.BorderBrushProperty, borderBrush, "Chrome"));
            template.Triggers.Add(disabled);

            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private static Setter TargetSetter(DependencyProperty property, object value, string targetName)
        {
            return new Setter(property, value) { TargetName = targetName };
        }
    }
}
