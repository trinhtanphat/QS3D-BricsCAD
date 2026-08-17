using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private FrameworkElement? _dedicatedPropertiesPaletteVisual;

        /// <summary>
        /// Moves the existing QS3D Properties editor into a dedicated palette host without cloning
        /// its ViewModel or edit handlers. The binding follows WorkspacePanel.DataContext so project
        /// reloads and selection changes continue to update the detached editor deterministically.
        /// </summary>
        internal FrameworkElement DetachPropertiesPaletteVisual()
        {
            if (_dedicatedPropertiesPaletteVisual != null)
                return _dedicatedPropertiesPaletteVisual;

            var propertiesRegion = FindPropertiesRegion();
            var ownerGrid = PropertiesParentOf(propertiesRegion) as Grid;
            if (ownerGrid == null)
                throw new InvalidOperationException("QS3D Properties region is not hosted by the expected Workspace grid.");

            ownerGrid.Children.Remove(propertiesRegion);
            CollapseEmbeddedPropertiesSlot(ownerGrid);

            Grid.SetRow(propertiesRegion, 0);
            Grid.SetColumn(propertiesRegion, 0);
            Grid.SetRowSpan(propertiesRegion, 1);
            Grid.SetColumnSpan(propertiesRegion, 1);
            propertiesRegion.HorizontalAlignment = HorizontalAlignment.Stretch;
            propertiesRegion.VerticalAlignment = VerticalAlignment.Stretch;

            BindingOperations.SetBinding(
                propertiesRegion,
                FrameworkElement.DataContextProperty,
                new Binding(nameof(DataContext))
                {
                    Source = this,
                    Mode = BindingMode.OneWay
                });

            var host = new Grid
            {
                MinWidth = 0,
                MinHeight = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            host.Children.Add(propertiesRegion);
            _dedicatedPropertiesPaletteVisual = host;
            return host;
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
                familyGrid.RowDefinitions[0].MinHeight = 120;
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
            {
                splitter.Visibility = Visibility.Collapsed;
            }
        }
    }
}
