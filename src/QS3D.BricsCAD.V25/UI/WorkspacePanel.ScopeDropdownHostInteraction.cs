using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using QS3D.BricsCAD.V25.Services;
using BricscadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Keeps the two Workspace scope selectors readable and truthful inside the BricsCAD
    /// PaletteSet host. Placeholder text is rendered as UI chrome only; the bound Zone/Floor
    /// catalogs remain real project data and are never padded with display-only rows.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool WorkspaceScopeDropdownHostInteractionRegistered =
            RegisterWorkspaceScopeDropdownHostInteraction();

        private bool _workspaceScopeDropdownHostInteractionWired;
        private TextBlock? _zoneEmptyPlaceholder;
        private TextBlock? _zoneUnselectedPlaceholder;
        private TextBlock? _floorEmptyPlaceholder;
        private TextBlock? _floorUnselectedPlaceholder;

        private static bool RegisterWorkspaceScopeDropdownHostInteraction()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnWorkspaceScopeDropdownHostInteractionLoaded),
                true);
            return true;
        }

        private static void OnWorkspaceScopeDropdownHostInteractionLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !WorkspaceScopeDropdownHostInteractionRegistered)
                return;

            panel.WireWorkspaceScopeDropdownHostInteraction();
        }

        private void WireWorkspaceScopeDropdownHostInteraction()
        {
            if (_workspaceScopeDropdownHostInteractionWired)
                return;

            _workspaceScopeDropdownHostInteractionWired = true;
            WireWorkspaceScopeCombo(ZoneCombo);
            WireWorkspaceScopeCombo(FloorCombo);

            WrapWorkspaceScopeCombo(
                ZoneCombo,
                "Không có Zone",
                "Chưa chọn Zone",
                out _zoneEmptyPlaceholder,
                out _zoneUnselectedPlaceholder);
            WrapWorkspaceScopeCombo(
                FloorCombo,
                "Không có Tầng",
                "Chưa chọn Tầng",
                out _floorEmptyPlaceholder,
                out _floorUnselectedPlaceholder);

            ZoneCombo.SelectionChanged += OnWorkspaceZoneSelectionChanged;
            FloorCombo.SelectionChanged += OnWorkspaceFloorSelectionChanged;
            ZoneCombo.ItemContainerGenerator.ItemsChanged += OnWorkspaceScopeItemsChanged;
            FloorCombo.ItemContainerGenerator.ItemsChanged += OnWorkspaceScopeItemsChanged;

            NormalizeWorkspaceProgrammaticScopeSelection(ZoneCombo, isZone: true);
            NormalizeWorkspaceProgrammaticScopeSelection(FloorCombo, isZone: false);
            UpdateWorkspaceScopePlaceholders();
        }

        private void OnWorkspaceZoneSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NormalizeWorkspaceProgrammaticScopeSelection(ZoneCombo, isZone: true);
            UpdateWorkspaceScopePlaceholders();
        }

        private void OnWorkspaceFloorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NormalizeWorkspaceProgrammaticScopeSelection(FloorCombo, isZone: false);
            UpdateWorkspaceScopePlaceholders();
        }

        private void OnWorkspaceScopeItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            UpdateWorkspaceScopePlaceholders();
        }

        private void NormalizeWorkspaceProgrammaticScopeSelection(ComboBox combo, bool isZone)
        {
            // RefreshProject/ClearProject own selection synchronization while this flag is set.
            // Correct only those programmatic transitions; a user's real click must continue to
            // flow to OnZoneChanged/OnFloorChanged and become the active project scope.
            if (!_loadingContext)
                return;

            var expectedIndex = ResolveWorkspaceActiveScopeIndex(combo, isZone);
            if (combo.SelectedIndex != expectedIndex)
                combo.SelectedIndex = expectedIndex;
        }

        private int ResolveWorkspaceActiveScopeIndex(ComboBox combo, bool isZone)
        {
            if (!combo.HasItems)
                return -1;

            var doc = BricscadApplication.DocumentManager.MdiActiveDocument;
            if (doc == null || !ProjectContextCoordinator.TryGetReadOnly(doc, out var project))
                return -1;

            try
            {
                if (isZone)
                {
                    if (string.IsNullOrWhiteSpace(project.ActiveZoneId))
                        return -1;
                    var zone = project.FindZone(project.ActiveZoneId);
                    return zone == null ? -1 : _viewModel.Zones.IndexOf(zone.Name);
                }

                if (string.IsNullOrWhiteSpace(project.ActiveFloorId))
                    return -1;
                var floor = project.FindFloor(project.ActiveFloorId);
                return floor == null ? -1 : _viewModel.Floors.IndexOf(floor.Name);
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
        }

        private void WrapWorkspaceScopeCombo(
            ComboBox combo,
            string emptyText,
            string unselectedText,
            out TextBlock emptyPlaceholder,
            out TextBlock unselectedPlaceholder)
        {
            emptyPlaceholder = CreateWorkspaceScopePlaceholder(emptyText);
            unselectedPlaceholder = CreateWorkspaceScopePlaceholder(unselectedText);

            if (!(combo.Parent is Panel parent))
                return;

            var index = parent.Children.IndexOf(combo);
            if (index < 0)
                return;

            parent.Children.RemoveAt(index);
            var host = new Grid();
            host.Children.Add(combo);
            host.Children.Add(emptyPlaceholder);
            host.Children.Add(unselectedPlaceholder);
            parent.Children.Insert(index, host);
        }

        private TextBlock CreateWorkspaceScopePlaceholder(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = TryFindResource("SubtleTextBrush") as Brush ?? Brushes.Gray,
                Margin = new Thickness(8, 0, 28, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Visibility = Visibility.Collapsed
            };
        }

        private void UpdateWorkspaceScopePlaceholders()
        {
            UpdateWorkspaceScopePlaceholder(
                ZoneCombo,
                _zoneEmptyPlaceholder,
                _zoneUnselectedPlaceholder);
            UpdateWorkspaceScopePlaceholder(
                FloorCombo,
                _floorEmptyPlaceholder,
                _floorUnselectedPlaceholder);
        }

        private static void UpdateWorkspaceScopePlaceholder(
            ComboBox combo,
            TextBlock? emptyPlaceholder,
            TextBlock? unselectedPlaceholder)
        {
            var hasItems = combo.HasItems;
            if (emptyPlaceholder != null)
                emptyPlaceholder.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
            if (unselectedPlaceholder != null)
                unselectedPlaceholder.Visibility = hasItems && combo.SelectedIndex < 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private static void WireWorkspaceScopeCombo(ComboBox combo)
        {
            combo.PreviewMouseLeftButtonDown += OnWorkspaceScopeComboPreviewMouseLeftButtonDown;
        }

        private static void OnWorkspaceScopeComboPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ComboBox combo) || !combo.IsEnabled || combo.IsDropDownOpen || !combo.HasItems)
                return;

            combo.Focus();
            combo.IsDropDownOpen = true;

            // Prevent the same press from reaching the custom template ToggleButton and
            // immediately toggling the popup closed again inside the host.
            e.Handled = true;
        }
    }
}
