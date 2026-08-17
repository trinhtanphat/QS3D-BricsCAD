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
            BltBimWorkspaceActivationCoordinator.Stop();
            HomeTabActivationCoordinator.Stop();
            Blt3dShellChromeCoordinator.Reset();
            BltHomeRibbonAugmenter.Reset();
            BltDrawRibbonAugmenter.Reset();
            BltToolRibbonAugmenter.Reset();
            BltToolRibbonIconPolisher.Reset();
            BltToolRibbonCommandBinder.Reset();
            BltRecognitionRibbonAugmenter.Reset();
            BltRecognitionIconPolisher.Reset();
            BltViewRibbonAugmenter.Reset();
            BltViewActionOverrideAugmenter.Reset();
            BltBimRibbonMirrorAugmenter.Reset();
            BltModelingRibbonVisualRefiner.Reset();
            BltModelingRibbonFunctionRefiner.Reset();
            BltModelingRibbonAugmenter.Reset();
            QuantityReferenceRibbonAugmenter.Reset();
            BltTopbarTabContract.Reset();
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
                BltBimWorkspaceActivationCoordinator.Start();
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
            // QS3D-owned Home/Draw/Recognition/View groups are deterministic without replacing native tabs.
            ready = BltHomeRibbonAugmenter.TryInitialize() && ready;
            ready = BltDrawRibbonFailSafe.TryInitialize() && ready;

            // Apply the owner-reference icon language to every visible VẼ/Công cụ button after
            // compact layout refinement. MÔ HÌNH BIM later mirrors these same decorated buttons.
            ready = BltDrawRibbonReferenceIconDecorator.TryInitialize() && ready;

            // TOOL is a dedicated owner-reference topbar. Replace only the old QS3D TOOL
            // fallback panels with the compact Cọc/Móng/Sàn/MCP/AutoCAD composition while
            // leaving the BIM-like workspace below the Ribbon unchanged. Bind the finished
            // visual tree to verified runtime commands before the generic fallback wrapper runs.
            ready = BltToolRibbonAugmenter.TryInitialize() && ready;
            ready = BltToolRibbonCommandBinder.TryInitialize() && ready;

            ready = BltRecognitionRibbonAugmenter.TryInitialize() && ready;
            ready = BltViewRibbonAugmenter.TryInitialize() && ready;
            ready = BltViewActionOverrideAugmenter.TryInitialize() && ready;

            // MODELING is a separate owner-reference surface. Rebuild only QS3D-owned panels
            // into the BLT3D large-action + compact three-row layout; native/third-party
            // Ribbon content remains untouched.
            ready = BltModelingRibbonAugmenter.TryInitialize() && ready;

            // Pin the functional route of all 21 reference buttons before visual finalization.
            // In particular, Theo phương Z uses QS3DMOVEZ so it is genuinely Z-constrained rather
            // than falling back to unrestricted MOVE plus manual coordinate instructions.
            ready = BltModelingRibbonFunctionRefiner.TryInitialize() && ready;

            // Apply the final dark-ribbon artwork only after every MODELING button exists. The
            // visual refiner requires all 21 reference buttons and refuses a text-only fallback,
            // while leaving grouping, command routing and native/third-party content unchanged.
            ready = BltModelingRibbonVisualRefiner.TryInitialize() && ready;

            // MÔ HÌNH BIM keeps the qualified Vẽ/Công cụ/IFC surface. Mirror those staging
            // panels first; then finalize the owner-reference VẼ tab by removing only its
            // staging IFC panel, leaving the blank ribbon tail shown in the reference image.
            ready = BltBimRibbonMirrorAugmenter.TryInitialize() && ready;
            ready = BltDrawRibbonReferenceFinalizer.TryInitialize() && ready;

            // Decorate canonical text-only/fallback buttons only after all richer feature
            // augmenters have supplied their own images. This preserves recognition and
            // owner-reference Draw/Modeling/View artwork while filling genuine gaps.
            ready = RibbonBootstrapIconAugmenter.TryInitialize() && ready;

            // BricsCAD's Ribbon host is not reliable with the raw DrawingImage instances used
            // by the TOOL clean-room vector artwork. Rasterize those exact semantic glyphs to
            // explicit 16/32 px bitmaps after generic decoration so the final visible TOOL
            // surface cannot fall back to blank/missing-image placeholders.
            ready = BltToolRibbonIconPolisher.TryInitialize() && ready;

            // Apply the final clean-room BLT3D-familiar Recognition artwork after generic
            // decoration so all eight compact buttons keep their intended semantic icon,
            // Image/LargeImage pair and active-vs-disabled visual hierarchy.
            ready = BltRecognitionIconPolisher.TryInitialize() && ready;

            // BricsCAD can invoke ICommand without forwarding RibbonButton.CommandParameter.
            // Wrap every QS3D ribbon handler after all augmenters have reconciled so visible
            // buttons keep their captured command for both CanExecute and Execute.
            ready = RibbonCommandParameterFallback.TryInitialize() && ready;

            // Retire obsolete QS3D-owned tabs (notably the old TẠO MỚI authoring tab) only
            // after icon/command reconciliation, then group the canonical QS3D tabs, reconcile
            // BLT3D shell chrome, and finally activate HOME without touching native tab ownership.
            ready = BltTopbarTabContract.TryInitialize() && ready;
            ready = Qs3dRibbonTabGroupCoordinator.TryInitialize() && ready;
            ready = Blt3dShellChromeCoordinator.TryInitialize() && ready;
            ready = HomeTabActivationCoordinator.TryInitialize() && ready;
            return ready;
        }
    }
}