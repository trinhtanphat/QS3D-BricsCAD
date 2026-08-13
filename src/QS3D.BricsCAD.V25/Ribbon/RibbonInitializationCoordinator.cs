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
        private static int _timedAttempts;
        private static DispatcherTimer? _retryTimer;

        public static void Start()
        {
            if (_started)
            {
                EnsureInitialized();
                return;
            }

            _started = true;
            var documents = Application.DocumentManager;
            try { documents.DocumentCreated += OnDocumentAvailable; } catch { }
            try { documents.DocumentActivated += OnDocumentAvailable; } catch { }

            if (!TryInitializeAll()) StartTimedRetry();
        }

        public static void Stop()
        {
            if (!_started) return;
            _started = false;

            var documents = Application.DocumentManager;
            try { documents.DocumentCreated -= OnDocumentAvailable; } catch { }
            try { documents.DocumentActivated -= OnDocumentAvailable; } catch { }
            StopTimedRetry();
        }

        private static void OnDocumentAvailable(object sender, DocumentCollectionEventArgs e)
        {
            EnsureInitialized();
        }

        private static void EnsureInitialized()
        {
            if (!_started) return;
            if (TryInitializeAll())
            {
                StopTimedRetry();
                return;
            }

            StartTimedRetry();
        }

        private static void StartTimedRetry()
        {
            if (!_started || _retryTimer != null) return;

            _timedAttempts = 0;
            var timer = new DispatcherTimer
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
            if (TryInitializeAll() || _timedAttempts >= MaxTimedAttempts)
                StopTimedRetry();
        }

        private static bool TryInitializeAll()
        {
            if (!RibbonBootstrapper.TryInitialize()) return false;

            var ready = true;
            ready = ReferenceWallRibbonAugmenter.TryInitialize() && ready;
            ready = ProjectRibbonAugmenter.TryInitialize() && ready;
            ready = QuickWorkflowRibbonAugmenter.TryInitialize() && ready;
            ready = QuantityReferenceRibbonAugmenter.TryInitialize() && ready;
            ready = UpdateRibbonAugmenter.TryInitialize() && ready;
            return ready;
        }
    }
}
