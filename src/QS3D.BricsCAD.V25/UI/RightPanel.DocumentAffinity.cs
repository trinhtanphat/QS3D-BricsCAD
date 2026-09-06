using System;
using System.Windows;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Owns the host-lifecycle fence for the shared Drawing/Layer palette.
    ///
    /// DocumentLifecycleCoordinator deliberately defers the heavier project/UI reconcile to
    /// ApplicationIdle. That is correct for startup responsiveness, but this palette contains
    /// actionable CAD names. Clear those rows synchronously when MDI ownership changes so a row
    /// published by DWG A can never remain actionable while DWG B is active.
    /// </summary>
    public partial class RightPanel
    {
        private static readonly bool DocumentAffinityRegistrationReady = RegisterRightPanelDocumentAffinity();
        private bool _documentAffinityAttached;

        private static bool RegisterRightPanelDocumentAffinity()
        {
            EventManager.RegisterClassHandler(
                typeof(RightPanel),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnRightPanelAffinityLoaded),
                true);
            EventManager.RegisterClassHandler(
                typeof(RightPanel),
                FrameworkElement.UnloadedEvent,
                new RoutedEventHandler(OnRightPanelAffinityUnloaded),
                true);
            return true;
        }

        private static void OnRightPanelAffinityLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is RightPanel panel)) return;
            panel.AttachRightPanelDocumentAffinity();

            // Loaded can recur after a PaletteSet hide/show cycle while one or more MDI switches
            // happened with the visual detached. Never re-expose rows from that detached interval.
            panel.InvalidateRightPanelDocumentState();
            panel.Refresh();
        }

        private static void OnRightPanelAffinityUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is RightPanel panel)
                panel.DetachRightPanelDocumentAffinity();
        }

        private void AttachRightPanelDocumentAffinity()
        {
            if (_documentAffinityAttached) return;
            Application.DocumentManager.DocumentActivated += OnRightPanelDocumentActivated;
            try
            {
                Application.DocumentManager.DocumentToBeDestroyed += OnRightPanelDocumentToBeDestroyed;
                _documentAffinityAttached = true;
            }
            catch
            {
                try { Application.DocumentManager.DocumentActivated -= OnRightPanelDocumentActivated; }
                catch { }
                throw;
            }
        }

        private void DetachRightPanelDocumentAffinity()
        {
            if (!_documentAffinityAttached) return;
            _documentAffinityAttached = false;
            try { Application.DocumentManager.DocumentActivated -= OnRightPanelDocumentActivated; }
            catch { }
            try { Application.DocumentManager.DocumentToBeDestroyed -= OnRightPanelDocumentToBeDestroyed; }
            catch { }
        }

        private void OnRightPanelDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            // Synchronous by design: do not queue this behind the lifecycle ApplicationIdle
            // reconcile or stale rows from the previous active DWG become actionable in the gap.
            InvalidateRightPanelDocumentState();
        }

        private void OnRightPanelDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if (ReferenceEquals(Application.DocumentManager.MdiActiveDocument, e.Document))
                InvalidateRightPanelDocumentState();
        }

        private void InvalidateRightPanelDocumentState()
        {
            var previousRefreshingDrawings = _refreshingDrawings;
            var previousRefreshingLayers = _refreshingLayers;
            _refreshingDrawings = true;
            _refreshingLayers = true;
            try
            {
                _viewModel.Drawings.Clear();
                DrawingList?.UnselectAll();
                _viewModel.Layers.Clear();
                LayerList?.UnselectAll();
                ApplyLayerFilter();
                _viewModel.Status = "Đang đồng bộ bảng Bản vẽ & Lớp với DWG đang active.";
            }
            finally
            {
                _refreshingLayers = previousRefreshingLayers;
                _refreshingDrawings = previousRefreshingDrawings;
            }
        }
    }
}
