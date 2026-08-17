using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private Grid? _dedicatedPropertiesPaletteHost;
        private Border? _dedicatedPropertiesRegion;
        private Grid? _dedicatedPropertiesOwnerGrid;
        private bool _dedicatedPropertiesPaletteActive;

        internal bool IsDedicatedPropertiesPaletteActive => _dedicatedPropertiesPaletteActive;

        /// <summary>
        /// Creates a stable host for the native QS3D Properties PaletteSet without permanently
        /// removing the real editor from Workspace. ShowWorkspace keeps the historical embedded
        /// editor; BIM activation moves that same visual into this host.
        /// </summary>
        internal FrameworkElement CreatePropertiesPaletteVisual()
        {
            if (_dedicatedPropertiesPaletteHost != null)
                return _dedicatedPropertiesPaletteHost;

            _dedicatedPropertiesRegion = FindPropertiesRegion();
            _dedicatedPropertiesOwnerGrid = PropertiesParentOf(_dedicatedPropertiesRegion) as Grid
                ?? throw new InvalidOperationException("QS3D Properties region is not hosted by the expected Workspace grid.");

            BindingOperations.SetBinding(
                _dedicatedPropertiesRegion,
                FrameworkElement.DataContextProperty,
                new Binding(nameof(DataContext))
                {
                    Source = this,
                    Mode = BindingMode.OneWay
                });

            _dedicatedPropertiesPaletteHost = new Grid
            {
                MinWidth = 0,
                MinHeight = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            return _dedicatedPropertiesPaletteHost;
        }

        internal void SetDedicatedPropertiesPaletteActive(bool active)
        {
            var host = (Grid)CreatePropertiesPaletteVisual();
            var region = _dedicatedPropertiesRegion
                ?? throw new InvalidOperationException("QS3D Properties region is unavailable.");
            var ownerGrid = _dedicatedPropertiesOwnerGrid
                ?? throw new InvalidOperationException("QS3D Properties owner grid is unavailable.");

            if (active)
            {
                if (ReferenceEquals(PropertiesParentOf(region), ownerGrid))
                    ownerGrid.Children.Remove(region);
                if (!host.Children.Contains(region))
                    host.Children.Add(region);

                Grid.SetRow(region, 0);
                Grid.SetColumn(region, 0);
                Grid.SetRowSpan(region, 1);
                Grid.SetColumnSpan(region, 1);
                region.HorizontalAlignment = HorizontalAlignment.Stretch;
                region.VerticalAlignment = VerticalAlignment.Stretch;
                region.Visibility = Visibility.Visible;
                CollapseEmbeddedPropertiesSlot(ownerGrid);
            }
            else
            {
                if (ReferenceEquals(PropertiesParentOf(region), host))
                    host.Children.Remove(region);
                if (!ownerGrid.Children.Contains(region))
                    ownerGrid.Children.Add(region);

                Grid.SetRow(region, 2);
                Grid.SetColumn(region, 0);
                Grid.SetRowSpan(region, 1);
                Grid.SetColumnSpan(region, 1);
                region.HorizontalAlignment = HorizontalAlignment.Stretch;
                region.VerticalAlignment = VerticalAlignment.Stretch;
                region.Visibility = Visibility.Visible;
                RestoreEmbeddedPropertiesSlot(ownerGrid);
            }

            _dedicatedPropertiesPaletteActive = active;
            Dispatcher.BeginInvoke(
                DispatcherPriority.SystemIdle,
                new Action(ApplyBlt3dFiveZoneRuntimeLayout));
        }

        private Border FindPropertiesRegion()
        {
            DependencyObject? current = PropertyList;
            while (current != null && !ReferenceEquals(current, this))
            {
                current = PropertiesParentOf(current);
                if (current is Border border && Grid.GetRow(border) == 2 && PropertiesParentOf(border) is Grid)
                    return border;
            }

            throw new InvalidOperationException("QS3D Properties editor region could not be located in WorkspacePanel.");
        }

        private static DependencyObject? PropertiesParentOf(DependencyObject child)
        {
            if (child is FrameworkElement element && element.Parent != null)
                return element.Parent;
            return VisualTreeHelper.GetParent(child);
        }

        private static void CollapseEmbeddedPropertiesSlot(Grid familyGrid)
        {
            if (familyGrid.RowDefinitions.Count >= 3)
            {
                familyGrid.RowDefinitions[0].MinHeight = 100;
                familyGrid.RowDefinitions[0].MaxHeight = double.PositiveInfinity;
                familyGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                familyGrid.RowDefinitions[1].MinHeight = 0;
                familyGrid.RowDefinitions[1].MaxHeight = 0;
                familyGrid.RowDefinitions[1].Height = new GridLength(0);
                familyGrid.RowDefinitions[2].MinHeight = 0;
                familyGrid.RowDefinitions[2].MaxHeight = 0;
                familyGrid.RowDefinitions[2].Height = new GridLength(0);
            }

            foreach (var splitter in familyGrid.Children.OfType<GridSplitter>()
                         .Where(child => Grid.GetRow(child) == 1))
                splitter.Visibility = Visibility.Collapsed;
        }

        private static void RestoreEmbeddedPropertiesSlot(Grid familyGrid)
        {
            if (familyGrid.RowDefinitions.Count >= 3)
            {
                familyGrid.RowDefinitions[0].MinHeight = 75;
                familyGrid.RowDefinitions[0].MaxHeight = double.PositiveInfinity;
                familyGrid.RowDefinitions[0].Height = new GridLength(42, GridUnitType.Star);
                familyGrid.RowDefinitions[1].MinHeight = 4;
                familyGrid.RowDefinitions[1].MaxHeight = 4;
                familyGrid.RowDefinitions[1].Height = new GridLength(4);
                familyGrid.RowDefinitions[2].MinHeight = 120;
                familyGrid.RowDefinitions[2].MaxHeight = double.PositiveInfinity;
                familyGrid.RowDefinitions[2].Height = new GridLength(58, GridUnitType.Star);
            }

            foreach (var splitter in familyGrid.Children.OfType<GridSplitter>()
                         .Where(child => Grid.GetRow(child) == 1))
                splitter.Visibility = Visibility.Visible;
        }
    }
}
