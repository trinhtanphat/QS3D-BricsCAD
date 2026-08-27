using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Responsive presentation shell for the Workspace palette.
    ///
    /// The legacy XAML intentionally remains the authoritative content tree because several
    /// BLT3D runtime-repair passes target its named controls. This partial only removes the
    /// hard horizontal-overflow contract and replaces the footer presentation after those
    /// repair passes have finished, without changing model/selection semantics.
    /// </summary>
    public partial class WorkspacePanel
    {
        private const string ResponsiveFooterTag = "QS3D_RESPONSIVE_BOTTOM_NAV";
        private static readonly bool ResponsiveBottomNavigationRegistered = RegisterResponsiveBottomNavigation();
        private bool _responsiveWorkspaceSizeWired;

        private static bool RegisterResponsiveBottomNavigation()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnResponsiveWorkspaceLoaded),
                true);
            return true;
        }

        private static void OnResponsiveWorkspaceLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !ResponsiveBottomNavigationRegistered)
                return;

            if (!panel._responsiveWorkspaceSizeWired && panel.WorkspaceOverflow != null)
            {
                panel.WorkspaceOverflow.SizeChanged += panel.OnResponsiveWorkspaceSizeChanged;
                panel._responsiveWorkspaceSizeWired = true;
            }

            // BLT3D pixel parity runs at ContextIdle. Apply this shell one priority later so
            // its 42 px navigation row remains the final presentation contract.
            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(panel.ApplyResponsiveWorkspaceShell));
        }

        private void OnResponsiveWorkspaceSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveWorkspaceShell();
        }

        private void ApplyResponsiveWorkspaceShell()
        {
            if (WorkspaceOverflow == null || WorkspaceContentRoot == null)
                return;

            WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            WorkspaceOverflow.PanningMode = PanningMode.None;
            WorkspaceContentRoot.MinWidth = 0d;

            Grid? body = WorkspaceContentRoot.Children
                .OfType<Grid>()
                .FirstOrDefault(candidate =>
                    Grid.GetRow(candidate) == 1 && candidate.ColumnDefinitions.Count >= 5);

            if (body != null)
            {
                // Browser / Family+Properties / Finish+Selection. Splitters stay fixed while
                // all content columns are allowed to shrink with the BricsCAD palette.
                body.ColumnDefinitions[0].MinWidth = 0d;
                body.ColumnDefinitions[0].Width = new GridLength(2.2d, GridUnitType.Star);
                body.ColumnDefinitions[1].MinWidth = 4d;
                body.ColumnDefinitions[1].Width = new GridLength(4d);
                body.ColumnDefinitions[2].MinWidth = 0d;
                body.ColumnDefinitions[2].Width = new GridLength(3.2d, GridUnitType.Star);
                body.ColumnDefinitions[3].MinWidth = 4d;
                body.ColumnDefinitions[3].Width = new GridLength(4d);
                body.ColumnDefinitions[4].MinWidth = 0d;
                body.ColumnDefinitions[4].Width = new GridLength(2.1d, GridUnitType.Star);
            }

            if (WorkspaceContentRoot.RowDefinitions.Count > 2)
                WorkspaceContentRoot.RowDefinitions[2].Height = new GridLength(42d);

            EnsureResponsiveBottomNavigation();
        }

        private void EnsureResponsiveBottomNavigation()
        {
            Border? footer = WorkspaceContentRoot.Children
                .OfType<Border>()
                .FirstOrDefault(candidate => Grid.GetRow(candidate) == 2);
            if (footer == null)
                return;

            Grid? existing = footer.Child as Grid;
            if (existing != null && string.Equals(existing.Tag as string, ResponsiveFooterTag, StringComparison.Ordinal))
                return;

            var navigation = new Grid
            {
                Tag = ResponsiveFooterTag,
                MinWidth = 0d,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            for (int index = 0; index < 5; index++)
                navigation.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star), MinWidth = 0d });

            AddNavigationButton(navigation, 0, CreateNavigationButton("Mô hình", OnResponsiveModelClick, true));
            AddNavigationButton(navigation, 1, CreateNavigationButton("Cấu kiện", OnResponsiveComponentsClick, false));
            AddNavigationButton(navigation, 2, CreateNavigationButton("Hoàn thiện", OnResponsiveFinishClick, false));
            AddNavigationButton(navigation, 3, CreateNavigationButton("Thống kê", OnResponsiveQuantityClick, false));

            Button more = CreateNavigationButton("⋯", OnResponsiveMoreClick, false);
            more.ToolTip = "Thêm tác vụ Workspace";
            more.ContextMenu = CreateResponsiveMoreMenu();
            AddNavigationButton(navigation, 4, more);

            footer.Padding = new Thickness(6d, 4d, 6d, 4d);
            footer.Child = navigation;
        }

        private Button CreateNavigationButton(string caption, RoutedEventHandler handler, bool accent)
        {
            var button = new Button
            {
                Content = caption,
                MinWidth = 0d,
                Margin = new Thickness(2d, 0d, 2d, 0d),
                Padding = new Thickness(7d, 2d, 7d, 2d),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Style? style = TryFindResource(accent ? "AccentButton" : "DenseButton") as Style;
            if (style != null)
                button.Style = style;

            button.Click += handler;
            return button;
        }

        private static void AddNavigationButton(Grid navigation, int column, Button button)
        {
            Grid.SetColumn(button, column);
            navigation.Children.Add(button);
        }

        private ContextMenu CreateResponsiveMoreMenu()
        {
            var menu = new ContextMenu
            {
                Placement = PlacementMode.Top,
                StaysOpen = false,
                MinWidth = 155d
            };

            Brush? background = TryFindResource("Bg1Brush") as Brush;
            Brush? foreground = TryFindResource("TextBrush") as Brush;
            if (background != null)
                menu.Background = background;
            if (foreground != null)
                menu.Foreground = foreground;

            menu.Items.Add(CreateMoreMenuItem("Kiểm tra mô hình", OnResponsiveHealthClick));
            menu.Items.Add(CreateMoreMenuItem("Làm mới", OnResponsiveRefreshClick));
            return menu;
        }

        private static MenuItem CreateMoreMenuItem(string caption, RoutedEventHandler handler)
        {
            var item = new MenuItem { Header = caption, Padding = new Thickness(9d, 5d, 9d, 5d) };
            item.Click += handler;
            return item;
        }

        private void OnResponsiveModelClick(object sender, RoutedEventArgs e)
        {
            OnViewModel3DClick(sender, e);
        }

        private void OnResponsiveComponentsClick(object sender, RoutedEventArgs e)
        {
            FamilyList.Focus();
            SetStatus("Đã chuyển đến danh sách cấu kiện.");
        }

        private void OnResponsiveFinishClick(object sender, RoutedEventArgs e)
        {
            ModelTree.Focus();
            SetStatus("Đã chuyển đến cây mô hình / hoàn thiện.");
        }

        private void OnResponsiveQuantityClick(object sender, RoutedEventArgs e)
        {
            OnQuantityClick(sender, e);
        }

        private void OnResponsiveHealthClick(object sender, RoutedEventArgs e)
        {
            OnHealthClick(sender, e);
        }

        private void OnResponsiveRefreshClick(object sender, RoutedEventArgs e)
        {
            OnRefreshClick(sender, e);
        }

        private void OnResponsiveMoreClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || button.ContextMenu == null)
                return;

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = PlacementMode.Top;
            button.ContextMenu.IsOpen = true;
        }
    }
}
