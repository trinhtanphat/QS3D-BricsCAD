using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using QS3D.BricsCAD.V25.UI.ViewModels;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// BricsCAD PaletteSet interaction fallback for the two Workspace scope ComboBoxes.
    ///
    /// The shared premium ComboBox template owns its dark chrome and uses a transparent
    /// template toggle. Some PaletteSet host/input paths can consume the mouse press before
    /// that template toggle changes IsDropDownOpen. Keep the normal ComboBox semantics, but
    /// explicitly open these two non-editable scope selectors on their first preview press.
    /// Item clicks are left untouched because the fallback only runs while the popup is closed.
    ///
    /// This partial also owns the presentation-only empty state for Zone/Floor. Project data
    /// remains unchanged: the Vietnamese sentinel strings live only in the Workspace view-model
    /// collections while those collections have no real entries, and are removed automatically
    /// as soon as project data is rebuilt.
    /// </summary>
    public partial class WorkspacePanel
    {
        private const string EmptyZoneOption = "Không có Zone";
        private const string EmptyFloorOption = "Không có Tầng";

        private static readonly bool WorkspaceScopeDropdownHostInteractionRegistered =
            RegisterWorkspaceScopeDropdownHostInteraction();

        private bool _workspaceScopeDropdownHostInteractionWired;
        private bool _workspaceScopeEmptyStateWired;
        private bool _workspaceScopeEmptyStateRefreshQueued;
        private bool _workspaceZoneEmptyStateInjected;
        private bool _workspaceFloorEmptyStateInjected;
        private WorkspaceViewModel? _workspaceScopeEmptyStateViewModel;

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
            panel.WireWorkspaceScopeEmptyState();
        }

        private void WireWorkspaceScopeDropdownHostInteraction()
        {
            if (_workspaceScopeDropdownHostInteractionWired)
                return;

            _workspaceScopeDropdownHostInteractionWired = true;
            WireWorkspaceScopeCombo(ZoneCombo);
            WireWorkspaceScopeCombo(FloorCombo);
        }

        private void WireWorkspaceScopeEmptyState()
        {
            if (!_workspaceScopeEmptyStateWired)
            {
                _workspaceScopeEmptyStateWired = true;
                DataContextChanged += OnWorkspaceScopeEmptyStateDataContextChanged;
                ZoneCombo.SelectionChanged += OnWorkspaceScopeZoneSelectionChanged;
            }

            AttachWorkspaceScopeEmptyStateViewModel(DataContext as WorkspaceViewModel);
            QueueWorkspaceScopeEmptyStateRefresh();
        }

        private void OnWorkspaceScopeEmptyStateDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachWorkspaceScopeEmptyStateViewModel(e.NewValue as WorkspaceViewModel);
            QueueWorkspaceScopeEmptyStateRefresh();
        }

        private void OnWorkspaceScopeZoneSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Zone changes can coincide with a project/scope rebuild. Re-normalize both selectors
            // after binding/selection settles so Floor never remains as a meaningless blank row.
            QueueWorkspaceScopeEmptyStateRefresh();
        }

        private void AttachWorkspaceScopeEmptyStateViewModel(WorkspaceViewModel? viewModel)
        {
            if (ReferenceEquals(_workspaceScopeEmptyStateViewModel, viewModel))
                return;

            if (_workspaceScopeEmptyStateViewModel != null)
            {
                _workspaceScopeEmptyStateViewModel.Zones.CollectionChanged -= OnWorkspaceScopeCollectionChanged;
                _workspaceScopeEmptyStateViewModel.Floors.CollectionChanged -= OnWorkspaceScopeCollectionChanged;
            }

            _workspaceScopeEmptyStateViewModel = viewModel;
            _workspaceZoneEmptyStateInjected = false;
            _workspaceFloorEmptyStateInjected = false;

            if (_workspaceScopeEmptyStateViewModel != null)
            {
                _workspaceScopeEmptyStateViewModel.Zones.CollectionChanged += OnWorkspaceScopeCollectionChanged;
                _workspaceScopeEmptyStateViewModel.Floors.CollectionChanged += OnWorkspaceScopeCollectionChanged;
            }
        }

        private void OnWorkspaceScopeCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // ObservableCollection.Clear() reports Reset. Forget our presentation sentinel at
            // that boundary so a real project item with the same text can never be mistaken for
            // an injected empty-state row during the subsequent rebuild.
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (_workspaceScopeEmptyStateViewModel != null && ReferenceEquals(sender, _workspaceScopeEmptyStateViewModel.Zones))
                    _workspaceZoneEmptyStateInjected = false;
                if (_workspaceScopeEmptyStateViewModel != null && ReferenceEquals(sender, _workspaceScopeEmptyStateViewModel.Floors))
                    _workspaceFloorEmptyStateInjected = false;
            }

            QueueWorkspaceScopeEmptyStateRefresh();
        }

        private void QueueWorkspaceScopeEmptyStateRefresh()
        {
            if (_workspaceScopeEmptyStateRefreshQueued)
                return;

            _workspaceScopeEmptyStateRefreshQueued = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.DataBind,
                new Action(ApplyWorkspaceScopeEmptyState));
        }

        private void ApplyWorkspaceScopeEmptyState()
        {
            _workspaceScopeEmptyStateRefreshQueued = false;
            var viewModel = DataContext as WorkspaceViewModel;
            if (viewModel == null)
                return;

            if (!ReferenceEquals(_workspaceScopeEmptyStateViewModel, viewModel))
                AttachWorkspaceScopeEmptyStateViewModel(viewModel);

            // Suppress the normal mutation handlers while the presentation sentinel and selected
            // index are synchronized. Empty-state display must never attempt a project mutation or
            // overwrite the more useful no-project/status message from ClearProject().
            var wasLoadingContext = _loadingContext;
            _loadingContext = true;
            try
            {
                NormalizeWorkspaceScopeCollection(
                    viewModel.Zones,
                    EmptyZoneOption,
                    ref _workspaceZoneEmptyStateInjected);
                NormalizeWorkspaceScopeCollection(
                    viewModel.Floors,
                    EmptyFloorOption,
                    ref _workspaceFloorEmptyStateInjected);

                EnsureWorkspaceScopeSelection(ZoneCombo, viewModel.ActiveZoneIndex());
                EnsureWorkspaceScopeSelection(FloorCombo, viewModel.ActiveFloorIndex());
            }
            finally
            {
                _loadingContext = wasLoadingContext;
            }
        }

        private static void NormalizeWorkspaceScopeCollection(
            ObservableCollection<string> items,
            string emptyLabel,
            ref bool injected)
        {
            if (items.Count == 0)
            {
                items.Add(emptyLabel);
                injected = true;
                return;
            }

            if (!injected)
                return;

            var injectedIndex = items.IndexOf(emptyLabel);
            if (injectedIndex < 0)
            {
                injected = false;
                return;
            }

            if (items.Count > 1)
            {
                items.RemoveAt(injectedIndex);
                injected = false;
            }
        }

        private static void EnsureWorkspaceScopeSelection(ComboBox combo, int preferredIndex)
        {
            if (combo.Items.Count == 0)
            {
                combo.SelectedIndex = -1;
                return;
            }

            var index = preferredIndex >= 0 && preferredIndex < combo.Items.Count
                ? preferredIndex
                : 0;
            if (combo.SelectedIndex != index)
                combo.SelectedIndex = index;
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
