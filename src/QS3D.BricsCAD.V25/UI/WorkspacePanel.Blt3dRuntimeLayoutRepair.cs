using System;
using System.Windows;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Repairs the owner-reference WorkspacePanel after BricsCAD has finished applying its own
    /// palette/workspace layout. Several legacy presentation passes intentionally share Loaded;
    /// host docking can still resize/reparent the palette after their first ApplicationIdle pass.
    /// Reapplying the authoritative reference layout for two bounded settle ticks keeps the real
    /// model tree + Family/Properties surface visible without introducing a fake model viewport.
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
            return true;
        }

        private static void OnBlt3dRuntimeLayoutLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || !Blt3dRuntimeLayoutRepairRegistered)
                return;

            panel.StartBlt3dRuntimeLayoutRepair();
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
            // follow-up passes cover that host settle window and then stop permanently, so a user
            // manually closing/resizing a palette afterwards remains respected.
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

            // This is the same authoritative pass used by the owner-reference Loaded handler:
            // Menu/model tree at left, Family list above Properties beside it. Columns 3/4 remain
            // retired because the centre region belongs to BricsCAD's native modelspace viewport.
            ApplyReferencePaletteLayout();
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
