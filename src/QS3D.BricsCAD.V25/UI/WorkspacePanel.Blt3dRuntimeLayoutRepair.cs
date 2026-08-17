using System;
using System.Windows;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Repairs the owner-approved BLT3D five-zone WorkspacePanel after BricsCAD has finished
    /// applying its own palette/workspace layout. Several legacy presentation passes intentionally
    /// share Loaded; host docking can still resize/reparent the palette after their first idle pass.
    /// Reapplying the final five-zone runtime layout for two bounded settle ticks keeps the real
    /// model tree + dedicated QS3D Family/Properties region visible without a fake viewport.
    /// </summary>
    public partial class WorkspacePanel
    {
        private const int Blt3dRuntimeSettlePasses = 2;
        private static readonly TimeSpan Blt3dRuntimeSettleInterval = TimeSpan.FromMilliseconds(250);
        private static readonly bool Blt3dRuntimeLayoutRepairRegistered = RegisterBlt3dRuntimeLayoutRepair();

        private DispatcherTimer? _blt3dRuntimeLayoutRepairTimer;
        private int _blt3dRuntimeSettlePassesRemaining;
        private bool _blt3dRuntimeLayoutRepairStarted;

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

            panel.StopBlt3dRuntimeLayoutRepairTimer();
            panel._blt3dRuntimeSettlePassesRemaining = 0;
            panel._blt3dRuntimeLayoutRepairStarted = false;
        }

        private void StartBlt3dRuntimeLayoutRepair()
        {
            if (_blt3dRuntimeLayoutRepairStarted)
                return;

            _blt3dRuntimeLayoutRepairStarted = true;
            _blt3dRuntimeSettlePassesRemaining = Blt3dRuntimeSettlePasses;

            // Run once behind all existing Loaded/ContextIdle compatibility passes.
            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(ReassertBlt3dRuntimeLayout));

            // BricsCAD can apply the native dock layout after WPF ApplicationIdle. Two bounded
            // follow-up passes cover that host settle window and then stop permanently for this
            // loaded lifetime, so manual resizing remains respected. A later unload/reload starts
            // a fresh bounded settle window because BricsCAD may have reparented the palette.
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

        private void ReassertBlt3dRuntimeLayout()
        {
            if (!IsLoaded)
                return;

            // The later owner screenshot contract supersedes the old side-by-side reference
            // interpretation: retain the host-owned modelspace in the centre and restore the
            // compact left QS3D rail with Model above the dedicated Family/Properties region.
            ApplyBlt3dFiveZoneRuntimeLayout();
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