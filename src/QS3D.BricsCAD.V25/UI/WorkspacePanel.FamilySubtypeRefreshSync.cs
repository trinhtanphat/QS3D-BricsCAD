using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Keeps the visible Family list aligned with the preserved Foundation subtype after a
    /// same-document Workspace reload. RefreshProject intentionally preserves authoring state for
    /// the active DWG, but WorkspaceViewModel.Load rebuilds Families before the generic category
    /// filter is applied. Re-apply the subtype view filter only after that load stack has unwound.
    /// </summary>
    public partial class WorkspacePanel
    {
        private static readonly bool FamilySubtypeRefreshSyncRegistered =
            RegisterFamilySubtypeRefreshSync();

        private bool _familySubtypeRefreshSyncAttached;
        private bool _familySubtypeRefreshQueued;
        private INotifyCollectionChanged? _familySubtypeRefreshFamilies;

        private static bool RegisterFamilySubtypeRefreshSync()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnFamilySubtypeRefreshSyncLoaded),
                true);
            return true;
        }

        private static void OnFamilySubtypeRefreshSyncLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !FamilySubtypeRefreshSyncRegistered)
                return;

            panel.EnsureFamilySubtypeRefreshSync();
        }

        private void EnsureFamilySubtypeRefreshSync()
        {
            if (_familySubtypeRefreshSyncAttached &&
                ReferenceEquals(_familySubtypeRefreshFamilies, _viewModel.Families))
                return;

            if (_familySubtypeRefreshFamilies != null)
                _familySubtypeRefreshFamilies.CollectionChanged -= OnFamilySubtypeRefreshFamiliesChanged;

            _familySubtypeRefreshFamilies = _viewModel.Families;
            _familySubtypeRefreshFamilies.CollectionChanged += OnFamilySubtypeRefreshFamiliesChanged;

            if (_familySubtypeRefreshSyncAttached)
                return;

            DataContextChanged += OnFamilySubtypeRefreshDataContextChanged;
            _familySubtypeRefreshSyncAttached = true;
        }

        private void OnFamilySubtypeRefreshDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            EnsureFamilySubtypeRefreshSync();
        }

        private void OnFamilySubtypeRefreshFamiliesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_familySubtypeFilter))
                return;

            QueueFamilySubtypeRefresh();
        }

        private void QueueFamilySubtypeRefresh()
        {
            if (_familySubtypeRefreshQueued)
                return;

            _familySubtypeRefreshQueued = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    _familySubtypeRefreshQueued = false;
                    if (_loadingContext)
                    {
                        QueueFamilySubtypeRefresh();
                        return;
                    }

                    // A document switch clears the subtype before the new project is loaded. Only
                    // same-document refreshes retain a subtype and therefore reach this view-only
                    // repair path.
                    if (string.IsNullOrWhiteSpace(_familySubtypeFilter))
                        return;

                    ApplyFamilySubtypeFilter();
                }));
        }
    }
}
