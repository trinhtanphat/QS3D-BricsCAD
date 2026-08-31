using System;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.Ribbon
{
    internal static class RibbonInitializationCoordinator
    {
        [Flags]
        private enum HostSubscription : byte
        {
            None = 0,
            DocumentCreated = 1,
            DocumentActivated = 2,
            All = DocumentCreated | DocumentActivated
        }

        private const int MaxTimedAttempts = 60;
        private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(500);
        private static bool _started;
        private static bool _initialized;
        private static bool _stopping;
        private static bool _cleanupPending;
        private static int _timedAttempts;
        private static DispatcherTimer? _retryTimer;
        private static HostSubscription _hostSubscriptions;

        public static void Start()
        {
            if (_stopping) return;

            if (!_started)
            {
                _started = true;
                _initialized = false;
                _cleanupPending = true;
            }

            // Host event acquisition is retryable and transactional per attempt. If an add
            // fails, newly acquired handlers from that attempt are rolled back. Any rollback
            // failure keeps its ownership bit so the next retry never double-subscribes it.
            TryEnsureHostSubscriptions();

            // NETLOAD runs on BricsCAD's UI thread. Do not synchronously reconcile the
            // large reflective ribbon tree before NETLOAD can return; queue initialization
            // and any missing host subscription retry through the bounded idle timer.
            if (!_initialized || _hostSubscriptions != HostSubscription.All)
                StartTimedRetry();
        }

        public static void Stop()
        {
            if (_stopping) return;
            if (!_started && !_cleanupPending && _hostSubscriptions == HostSubscription.None && _retryTimer == null)
                return;

            _stopping = true;
            _started = false;
            _initialized = false;
            var cleanupComplete = true;

            try
            {
                DocumentCollection? documents = null;
                try { documents = Application.DocumentManager; }
                catch { cleanupComplete = false; }

                if (documents != null)
                {
                    if (!TryDetachHostSubscription(
                            HostSubscription.DocumentCreated,
                            () => documents.DocumentCreated -= OnDocumentAvailable))
                        cleanupComplete = false;
                    if (!TryDetachHostSubscription(
                            HostSubscription.DocumentActivated,
                            () => documents.DocumentActivated -= OnDocumentAvailable))
                        cleanupComplete = false;
                }
                else if (_hostSubscriptions != HostSubscription.None)
                {
                    cleanupComplete = false;
                }

                if (!TryStopTimedRetry()) cleanupComplete = false;

                // Every downstream coordinator owns independent external/UI state. Teardown is
                // deliberately fail-soft so one faulty cleanup cannot strand later publishers,
                // command bindings, images, tabs, or workspace activation across NETLOAD reload.
                if (!TryCleanup(() => { BltBimWorkspaceActivationCoordinator.Stop(); })) cleanupComplete = false;
                if (!TryCleanup(() => { HomeTabActivationCoordinator.Stop(); })) cleanupComplete = false;
                if (!TryCleanup(() => { Blt3dShellChromeCoordinator.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltHomeRibbonAugmenter.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltDrawRibbonAugmenter.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltToolRibbonAugmenter.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltToolRibbonCommandBinder.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { McpRibbonCommandOverride.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltToolRibbonIconPolisher.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltRecognitionRibbonAugmenter.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltRecognitionIconPolisher.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltViewRibbonAugmenter.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltViewActionOverrideAugmenter.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltBimRibbonMirrorAugmenter.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltModelingRibbonVisualRefiner.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltModelingRibbonFunctionRefiner.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltModelingRibbonAugmenter.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { QuantityReferenceRibbonAugmenter.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { BltTopbarTabContract.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { RibbonBootstrapIconAugmenter.Reset(); })) cleanupComplete = false;
                if (!TryCleanup(() => { Qs3dRibbonTabGroupCoordinator.Reset(); })) cleanupComplete = false;
            }
            finally
            {
                _cleanupPending = !cleanupComplete
                    || _hostSubscriptions != HostSubscription.None
                    || _retryTimer != null;
                _stopping = false;
            }
        }

        private static bool TryEnsureHostSubscriptions()
        {
            if (!_started) return false;
            if (_hostSubscriptions == HostSubscription.All) return true;

            DocumentCollection documents;
            try { documents = Application.DocumentManager; }
            catch { return false; }

            var acquiredThisAttempt = HostSubscription.None;
            try
            {
                if ((_hostSubscriptions & HostSubscription.DocumentCreated) == 0)
                {
                    documents.DocumentCreated += OnDocumentAvailable;
                    _hostSubscriptions |= HostSubscription.DocumentCreated;
                    acquiredThisAttempt |= HostSubscription.DocumentCreated;
                }

                if ((_hostSubscriptions & HostSubscription.DocumentActivated) == 0)
                {
                    documents.DocumentActivated += OnDocumentAvailable;
                    _hostSubscriptions |= HostSubscription.DocumentActivated;
                    acquiredThisAttempt |= HostSubscription.DocumentActivated;
                }

                return _hostSubscriptions == HostSubscription.All;
            }
            catch
            {
                RollbackHostSubscriptions(documents, acquiredThisAttempt);
                return false;
            }
        }

        private static void RollbackHostSubscriptions(
            DocumentCollection documents,
            HostSubscription acquiredThisAttempt)
        {
            if ((acquiredThisAttempt & HostSubscription.DocumentActivated) != 0)
                TryDetachHostSubscription(
                    HostSubscription.DocumentActivated,
                    () => documents.DocumentActivated -= OnDocumentAvailable);
            if ((acquiredThisAttempt & HostSubscription.DocumentCreated) != 0)
                TryDetachHostSubscription(
                    HostSubscription.DocumentCreated,
                    () => documents.DocumentCreated -= OnDocumentAvailable);
        }

        private static bool TryDetachHostSubscription(HostSubscription subscription, Action detach)
        {
            if ((_hostSubscriptions & subscription) == 0) return true;
            try
            {
                detach();
                _hostSubscriptions &= ~subscription;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCleanup(Action cleanup)
        {
            try
            {
                cleanup();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void OnDocumentAvailable(object sender, DocumentCollectionEventArgs e)
        {
            if (!_started || _stopping) return;

            // Document creation/activation is also a host UI callback. Keep it passive;
            // the timer runs after the event returns and retries only while needed.
            if (!_initialized || _hostSubscriptions != HostSubscription.All)
                StartTimedRetry();
        }

        private static void StartTimedRetry()
        {
            if (!_started || (_initialized && _hostSubscriptions == HostSubscription.All) || _retryTimer != null)
                return;

            _timedAttempts = 0;
            var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = RetryInterval
            };
            timer.Tick += OnRetryTick;
            _retryTimer = timer;
            timer.Start();
        }

        // Preserve the long-standing NETLOAD source contract used by generic startup guards and
        // ordinary retry call sites. Stop() uses the status-aware helper below so teardown can
        // retain timer ownership when either native stop or event detach fails.
        private static void StopTimedRetry()
        {
            TryStopTimedRetry();
        }

        private static bool TryStopTimedRetry()
        {
            var timer = _retryTimer;
            if (timer == null) return true;

            var stopped = true;
            var detached = true;
            try { timer.Stop(); } catch { stopped = false; }
            try { timer.Tick -= OnRetryTick; } catch { detached = false; }

            if (stopped && detached)
                _retryTimer = null;
            return stopped && detached;
        }

        private static void OnRetryTick(object? sender, EventArgs e)
        {
            if (!_started)
            {
                StopTimedRetry();
                return;
            }

            _timedAttempts++;
            if (!TryEnsureHostSubscriptions())
            {
                if (_timedAttempts >= MaxTimedAttempts)
                    StopTimedRetry();
                return;
            }

            if (_initialized)
            {
                StopTimedRetry();
                return;
            }

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
            ready = McpRibbonCommandOverride.TryInitialize() && ready;

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

            // Apply the final clean-room BLT3D-familiar TOOL artwork after generic decoration
            // so BricsCAD receives deterministic 16px/32px frozen bitmap sources.
            ready = BltToolRibbonIconPolisher.TryInitialize() && ready;

            // Apply the final clean-room BLT3D-familiar Recognition artwork after generic
            // decoration so all eight compact buttons keep their intended semantic icon,
            // Image/LargeImage pair and active-vs-disabled visual hierarchy.
            ready = BltRecognitionIconPolisher.TryInitialize() && ready;

            // BricsCAD's Ribbon consumes exact-size frozen bitmaps more reliably than raw
            // DrawingImage sources. Rasterize only after semantic Recognition artwork is final.
            ready = BltRecognitionBitmapFinalizer.TryInitialize() && ready;

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
