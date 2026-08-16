using System;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Ribbon
{
    internal static class RibbonInitializationCoordinator
    {
        private const int MaxTimedAttempts = 60;
        private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(500);
        private static bool _started;
        private static bool _initialized;
        private static int _timedAttempts;
        private static DispatcherTimer? _retryTimer;

        public static void Start()
        {
            if (_started)
            {
                if (!_initialized) StartTimedRetry();
                return;
            }

            _started = true;
            _initialized = false;
            var documents = Application.DocumentManager;
            try { documents.DocumentCreated += OnDocumentAvailable; } catch { }
            try { documents.DocumentActivated += OnDocumentAvailable; } catch { }

            // NETLOAD runs on BricsCAD's UI thread. Do not synchronously reconcile the
            // large reflective ribbon tree before NETLOAD can return; queue the first
            // attempt through the same bounded retry path used when the host ribbon is
            // not ready yet.
            StartTimedRetry();
        }

        public static void Stop()
        {
            if (!_started) return;
            _started = false;
            _initialized = false;

            var documents = Application.DocumentManager;
            try { documents.DocumentCreated -= OnDocumentAvailable; } catch { }
            try { documents.DocumentActivated -= OnDocumentAvailable; } catch { }
            StopTimedRetry();
            HomeTabActivationCoordinator.Stop();
            Blt3dShellChromeCoordinator.Reset();
            BltHomeRibbonAugmenter.Reset();
            BltDrawRibbonAugmenter.Reset();
            BltRecognitionRibbonAugmenter.Reset();
            RibbonBootstrapIconAugmenter.Reset();
            Qs3dRibbonTabGroupCoordinator.Reset();
        }

        private static void OnDocumentAvailable(object sender, DocumentCollectionEventArgs e)
        {
            // Document creation/activation is also a host UI callback. Keep it passive;
            // the timer runs after the event returns and retries only while needed.
            if (!_initialized) StartTimedRetry();
        }

        private static void StartTimedRetry()
        {
            if (!_started || _initialized || _retryTimer != null) return;

            _timedAttempts = 0;
            var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = RetryInterval
            };
            timer.Tick += OnRetryTick;
            _retryTimer = timer;
            timer.Start();
        }

        private static void StopTimedRetry()
        {
            var timer = _retryTimer;
            _retryTimer = null;
            if (timer == null) return;
            try { timer.Stop(); } catch { }
            try { timer.Tick -= OnRetryTick; } catch { }
        }

        private static void OnRetryTick(object? sender, EventArgs e)
        {
            if (!_started)
            {
                StopTimedRetry();
                return;
            }

            _timedAttempts++;
            if (TryInitializeAll())
            {
                _initialized = true;
                StopTimedRetry();
                return;
            }

            if (_timedAttempts >= MaxTimedAttempts)
                StopTimedRetry();
        }

        private static bool TryInitializeAll()
        {
            if (!RibbonBootstrapper.TryInitialize()) return false;

            var ready = true;
            ready = ReferenceWallRibbonAugmenter.TryInitialize() && ready;
            ready = ProjectRibbonAugmenter.TryInitialize() && ready;
            ready = QuickWorkflowRibbonAugmenter.TryInitialize() && ready;
            ready = RaftFoundationRibbonAugmenter.TryInitialize() && ready;
            ready = QuantityReferenceRibbonAugmenter.TryInitialize() && ready;
            ready = UpdateRibbonAugmenter.TryInitialize() && ready;

            // Reconcile screenshot-familiar presentation after feature augmenters so the
            // QS3D-owned Home/Draw/Recognition groups are deterministic without replacing native tabs.
            ready = BltHomeRibbonAugmenter.TryInitialize() && ready;
            ready = BltDrawRibbonFailSafe.TryInitialize() && ready;
            ready = BltRecognitionRibbonAugmenter.TryInitialize() && ready;

            // The baseline Project/Authoring tabs are still text-only in RibbonBootstrapper.
            // Decorate them after feature augmenters have finished so both baseline and added
            // buttons receive QS3D-generated icons without changing their command routing.
            ready = RibbonBootstrapIconAugmenter.TryInitialize() && ready;

            // BricsCAD can invoke ICommand without forwarding RibbonButton.CommandParameter.
            // Wrap every QS3D ribbon handler after all augmenters have reconciled so visible
            // buttons keep their captured command for both CanExecute and Execute.
            ready = RibbonCommandParameterFallback.TryInitialize() && ready;

            // Keep repository ownership semantics intact: native/third-party tabs stay in the
            // Ribbon collection, QS3D_* tabs are grouped in BLT3D order, and only shell chrome
            // (application/QAT/search) is hidden to match the reference presentation.
            ready = Qs3dRibbonTabGroupCoordinator.TryInitialize() && ready;
            ready = Blt3dShellChromeCoordinator.TryInitialize() && ready;
            ready = HomeTabActivationCoordinator.TryInitialize() && ready;
            return ready;
        }
    }
}
