using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Repairs the owner-approved BLT3D five-zone WorkspacePanel after BricsCAD has finished
    /// applying its own palette/workspace layout. Several legacy presentation passes intentionally
    /// share Loaded; host docking can still resize/reparent the palette after their first idle pass.
    /// The bounded startup settle remains, while loaded-lifetime viewport/layout guards repair only
    /// a measurably blank/collapsed client after a later BricsCAD show/reparent/resize/layout event.
    /// </summary>
    public partial class WorkspacePanel
    {
        private const int Blt3dRuntimeSettlePasses = 2;
        private const int Blt3dRuntimeRecoveryRetryPasses = 3;
        private const double Blt3dUsableViewportFloor = 32d;
        private static readonly TimeSpan Blt3dRuntimeSettleInterval = TimeSpan.FromMilliseconds(250);
        private static readonly bool Blt3dRuntimeLayoutRepairRegistered = RegisterBlt3dRuntimeLayoutRepair();

        private DispatcherTimer? _blt3dRuntimeLayoutRepairTimer;
        private int _blt3dRuntimeSettlePassesRemaining;
        private bool _blt3dRuntimeLayoutRepairStarted;
        private bool _blt3dRuntimeViewportEventsWired;
        private bool _blt3dRuntimeViewportRecoveryQueued;
        private bool _blt3dRuntimeViewportRecoveryApplying;
        private int _blt3dRuntimeViewportRecoveryRetriesRemaining;

        private static bool RegisterBlt3dRuntimeLayoutRepair()
        {
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnBlt3dRuntimeLayoutLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnBlt3dRuntimeLayoutUnloaded),
                true);
            return true;
        }

        private static void OnBlt3dRuntimeLayoutLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !Blt3dRuntimeLayoutRepairRegistered)
                return;

            panel.StartBlt3dRuntimeLayoutRepair();
        }

        private static void OnBlt3dRuntimeLayoutUnloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel))
                return;

            panel.UnwireBlt3dRuntimeViewportRecovery();
            panel.StopBlt3dRuntimeLayoutRepairTimer();
            panel._blt3dRuntimeSettlePassesRemaining = 0;
            panel._blt3dRuntimeViewportRecoveryQueued = false;
            panel._blt3dRuntimeViewportRecoveryApplying = false;
            panel._blt3dRuntimeViewportRecoveryRetriesRemaining = 0;
            panel._blt3dRuntimeLayoutRepairStarted = false;
        }

        private void StartBlt3dRuntimeLayoutRepair()
        {
            if (_blt3dRuntimeLayoutRepairStarted)
                return;

            _blt3dRuntimeLayoutRepairStarted = true;
            _blt3dRuntimeSettlePassesRemaining = Blt3dRuntimeSettlePasses;
            _blt3dRuntimeViewportRecoveryRetriesRemaining = Blt3dRuntimeRecoveryRetryPasses;
            WireBlt3dRuntimeViewportRecovery();

            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(ReassertBlt3dRuntimeLayout));

            var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = Blt3dRuntimeSettleInterval
            };
            timer.Tick += OnBlt3dRuntimeLayoutRepairTick;
            _blt3dRuntimeLayoutRepairTimer = timer;
            timer.Start();
        }

        private void OnBlt3dRuntimeLayoutRepairTick(object? sender, EventArgs e)
        {
            if (!IsLoaded || _blt3dRuntimeSettlePassesRemaining <= 0)
            {
                StopBlt3dRuntimeLayoutRepairTimer();
                return;
            }

            ReassertBlt3dRuntimeLayout();
            _blt3dRuntimeSettlePassesRemaining--;
            if (_blt3dRuntimeSettlePassesRemaining <= 0)
                StopBlt3dRuntimeLayoutRepairTimer();
        }

        private void WireBlt3dRuntimeViewportRecovery()
        {
            if (_blt3dRuntimeViewportEventsWired)
                return;

            WorkspaceOverflow.SizeChanged += OnBlt3dRuntimeViewportSizeChanged;
            WorkspaceOverflow.IsVisibleChanged += OnBlt3dRuntimeViewportVisibilityChanged;
            WorkspaceOverflow.LayoutUpdated += OnBlt3dRuntimeViewportLayoutUpdated;
            _blt3dRuntimeViewportEventsWired = true;
        }

        private void UnwireBlt3dRuntimeViewportRecovery()
        {
            if (!_blt3dRuntimeViewportEventsWired)
                return;

            try { WorkspaceOverflow.SizeChanged -= OnBlt3dRuntimeViewportSizeChanged; } catch { }
            try { WorkspaceOverflow.IsVisibleChanged -= OnBlt3dRuntimeViewportVisibilityChanged; } catch { }
            try { WorkspaceOverflow.LayoutUpdated -= OnBlt3dRuntimeViewportLayoutUpdated; } catch { }
            _blt3dRuntimeViewportEventsWired = false;
        }

        private void OnBlt3dRuntimeViewportSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width <= Blt3dUsableViewportFloor ||
                e.NewSize.Height <= Blt3dUsableViewportFloor)
                return;

            _blt3dRuntimeViewportRecoveryRetriesRemaining = Blt3dRuntimeRecoveryRetryPasses;
            QueueBlt3dRuntimeViewportRecovery();
        }

        private void OnBlt3dRuntimeViewportVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsLoaded && WorkspaceOverflow.IsVisible)
            {
                _blt3dRuntimeViewportRecoveryRetriesRemaining = Blt3dRuntimeRecoveryRetryPasses;
                QueueBlt3dRuntimeViewportRecovery();
            }
        }

        private void OnBlt3dRuntimeViewportLayoutUpdated(object? sender, EventArgs e)
        {
            if (_blt3dRuntimeViewportRecoveryApplying ||
                !IsLoaded ||
                !WorkspaceOverflow.IsVisible ||
                !HasUsableBlt3dRuntimeViewport())
                return;

            if (NeedsBlt3dRuntimeViewportRecovery())
            {
                QueueBlt3dRuntimeViewportRecovery();
                return;
            }

            _blt3dRuntimeViewportRecoveryRetriesRemaining = Blt3dRuntimeRecoveryRetryPasses;
        }

        private void QueueBlt3dRuntimeViewportRecovery()
        {
            if (_blt3dRuntimeViewportRecoveryQueued ||
                _blt3dRuntimeViewportRecoveryApplying ||
                !IsLoaded ||
                _blt3dRuntimeViewportRecoveryRetriesRemaining <= 0)
                return;

            _blt3dRuntimeViewportRecoveryQueued = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() =>
                {
                    _blt3dRuntimeViewportRecoveryQueued = false;
                    if (!IsLoaded || !WorkspaceOverflow.IsVisible || !HasUsableBlt3dRuntimeViewport())
                        return;

                    if (!NeedsBlt3dRuntimeViewportRecovery())
                    {
                        _blt3dRuntimeViewportRecoveryRetriesRemaining = Blt3dRuntimeRecoveryRetryPasses;
                        return;
                    }

                    Grid? preservedWorkspace;
                    GridLength preservedModelWidth;
                    GridLength preservedFamilyWidth;
                    var preserveSplitterGeometry = TryCaptureBlt3dRuntimeSplitterGeometry(
                        out preservedWorkspace,
                        out preservedModelWidth,
                        out preservedFamilyWidth);

                    _blt3dRuntimeViewportRecoveryRetriesRemaining--;
                    _blt3dRuntimeViewportRecoveryApplying = true;
                    try
                    {
                        ReassertBlt3dRuntimeLayout();
                        if (preserveSplitterGeometry)
                        {
                            RestoreBlt3dRuntimeSplitterGeometry(
                                preservedWorkspace,
                                preservedModelWidth,
                                preservedFamilyWidth);
                        }
                        InvalidateBlt3dRuntimeLayout();
                    }
                    finally
                    {
                        _blt3dRuntimeViewportRecoveryApplying = false;
                    }
                }));
        }

        private bool TryCaptureBlt3dRuntimeSplitterGeometry(
            out Grid? workspace,
            out GridLength modelWidth,
            out GridLength familyWidth)
        {
            workspace = null;
            modelWidth = new GridLength(0);
            familyWidth = new GridLength(0);

            var root = WorkspaceContentRoot;
            if (root == null)
                return false;

            foreach (UIElement child in root.Children)
            {
                if (child is Grid candidate &&
                    Grid.GetRow(candidate) == 1 &&
                    candidate.ColumnDefinitions.Count == 5)
                {
                    workspace = candidate;
                    break;
                }
            }

            if (workspace == null)
                return false;

            var columns = workspace.ColumnDefinitions;
            var currentModelWidth = columns[0].Width;
            var currentFamilyWidth = columns[2].Width;
            if (!IsPreservableBlt3dRuntimeGridLength(currentModelWidth) ||
                !IsPreservableBlt3dRuntimeGridLength(currentFamilyWidth))
                return false;

            modelWidth = currentModelWidth;
            familyWidth = currentFamilyWidth;
            return true;
        }

        private static bool IsPreservableBlt3dRuntimeGridLength(GridLength width)
        {
            return !width.IsAuto &&
                   width.Value > 0d &&
                   !double.IsNaN(width.Value) &&
                   !double.IsInfinity(width.Value);
        }

        private void RestoreBlt3dRuntimeSplitterGeometry(
            Grid? workspace,
            GridLength modelWidth,
            GridLength familyWidth)
        {
            if (workspace == null || workspace.ColumnDefinitions.Count != 5)
                return;

            var columns = workspace.ColumnDefinitions;
            columns[0].Width = modelWidth;
            columns[2].Width = familyWidth;
        }

        private bool HasUsableBlt3dRuntimeViewport()
        {
            return WorkspaceOverflow.ActualWidth > Blt3dUsableViewportFloor &&
                   WorkspaceOverflow.ActualHeight > Blt3dUsableViewportFloor;
        }

        private bool NeedsBlt3dRuntimeViewportRecovery()
        {
            var root = WorkspaceContentRoot;
            if (root == null)
                return false;

            if (BindingOperations.IsDataBound(root, FrameworkElement.WidthProperty) ||
                root.Visibility != Visibility.Visible ||
                root.Opacity <= 0d ||
                root.HorizontalAlignment != HorizontalAlignment.Stretch ||
                root.VerticalAlignment != VerticalAlignment.Stretch ||
                root.ActualWidth <= 1d ||
                root.ActualHeight <= 1d)
                return true;

            Grid? workspace = null;
            foreach (UIElement child in root.Children)
            {
                if (child is Grid candidate &&
                    Grid.GetRow(candidate) == 1 &&
                    candidate.ColumnDefinitions.Count == 5)
                {
                    workspace = candidate;
                    break;
                }
            }

            if (workspace == null)
                return false;

            if (workspace.Visibility != Visibility.Visible ||
                workspace.Opacity <= 0d ||
                workspace.ActualWidth <= 1d ||
                workspace.ActualHeight <= 1d)
                return true;

            foreach (UIElement child in workspace.Children)
            {
                if (child.Visibility == Visibility.Visible &&
                    child is FrameworkElement element &&
                    element.ActualWidth > 1d &&
                    element.ActualHeight > 1d)
                    return false;
            }

            return true;
        }

        private void ReassertBlt3dRuntimeLayout()
        {
            if (!IsLoaded)
                return;

            WorkspaceOverflow.VerticalContentAlignment = VerticalAlignment.Stretch;
            WorkspaceContentRoot.VerticalAlignment = VerticalAlignment.Stretch;
            ApplyBlt3dFiveZoneRuntimeLayout();
        }

        private void InvalidateBlt3dRuntimeLayout()
        {
            WorkspaceContentRoot.InvalidateMeasure();
            WorkspaceContentRoot.InvalidateArrange();
            WorkspaceOverflow.InvalidateMeasure();
            WorkspaceOverflow.InvalidateArrange();
            InvalidateMeasure();
            InvalidateArrange();
        }

        private void StopBlt3dRuntimeLayoutRepairTimer()
        {
            var timer = _blt3dRuntimeLayoutRepairTimer;
            _blt3dRuntimeLayoutRepairTimer = null;
            if (timer == null)
                return;

            try { timer.Stop(); } catch { }
            try { timer.Tick -= OnBlt3dRuntimeLayoutRepairTick; } catch { }
        }
    }
}
