using System;
using Bricscad.ApplicationServices;
using Bricscad.Windows;
using QS3D.BricsCAD.V25.UI;
using DrawingSize = System.Drawing.Size;
using WpfSize = System.Windows.Size;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Owns the read-only Project Information surface hosted modelessly inside BricsCAD.
    /// The palette never owns project mutation; it refreshes from the active document whenever
    /// it is shown or BricsCAD activates another drawing.
    /// </summary>
    internal static class ProjectSetupPaletteCoordinator
    {
        private static readonly Guid ProjectSetupGuid =
            new Guid("D9F85CA8-837A-4C40-A60B-3A89B7E1477B");

        private static PaletteSet? _palette;
        private static BltProjectSetupPanel? _panel;
        private static bool _documentActivatedSubscribed;

        public static bool IsVisible => _palette != null && _palette.Visible;

        public static void ShowProjectInformation()
        {
            EnsureCreated();

            // Project Information owns the large embedded canvas while visible. Release the
            // other QS3D docked surfaces first instead of stacking palettes over the CAD viewport.
            StartCenterPaletteCoordinator.Hide();
            PaletteCoordinator.Hide();

            var palette = _palette;
            var panel = _panel;
            if (palette == null || panel == null) return;

            var wasVisible = palette.Visible;
            var wasSubscribed = _documentActivatedSubscribed;
            try
            {
                SubscribeToDocumentActivation();
                panel.RefreshFromDocument(Application.DocumentManager.MdiActiveDocument);
                palette.Visible = true;
            }
            catch (Exception)
            {
                panel.ShowUnavailable("Project Information không thể mở an toàn; dữ liệu cũ đã được xóa.");
                if (!wasVisible)
                {
                    try { palette.Visible = false; } catch { }
                }
                if (!wasSubscribed) UnsubscribeFromDocumentActivation();
                throw;
            }
        }

        public static void Hide()
        {
            var palette = _palette;
            if (palette != null)
            {
                try { palette.Visible = false; }
                finally { UnsubscribeFromDocumentActivation(); }
                return;
            }

            UnsubscribeFromDocumentActivation();
        }

        public static void Dispose()
        {
            UnsubscribeFromDocumentActivation();

            var palette = _palette;
            _palette = null;
            _panel = null;
            if (palette == null) return;

            try { palette.Dispose(); }
            catch
            {
                // BricsCAD may already be tearing down native UI during plugin unload.
            }
        }

        private static void EnsureCreated()
        {
            if (_palette != null && _panel != null) return;

            Dispose();
            try
            {
                _panel = new BltProjectSetupPanel();
                _palette = new PaletteSet("QS3D — Thông tin dự án", ProjectSetupGuid)
                {
                    DockEnabled = DockSides.Left | DockSides.Right,
                    Dock = DockSides.Left,
                    Visible = false,
                    KeepFocus = false,
                    MinimumSize = new DrawingSize(720, 480)
                };
                _palette.DeviceIndependentSize = new WpfSize(1040, 680);
                _palette.AddVisual("Thông tin dự án", _panel, true);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static void SubscribeToDocumentActivation()
        {
            if (_documentActivatedSubscribed) return;
            Application.DocumentManager.DocumentActivated += OnDocumentActivated;
            _documentActivatedSubscribed = true;
        }

        private static void UnsubscribeFromDocumentActivation()
        {
            if (!_documentActivatedSubscribed) return;
            try
            {
                Application.DocumentManager.DocumentActivated -= OnDocumentActivated;
                _documentActivatedSubscribed = false;
            }
            catch
            {
                // Keep the flag true so a later Hide/Dispose can retry without stacking a second hook.
            }
        }

        private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            var panel = _panel;
            if (_palette == null || !_palette.Visible || panel == null) return;

            try
            {
                panel.RefreshFromDocument(e.Document ?? Application.DocumentManager.MdiActiveDocument);
            }
            catch (Exception)
            {
                // Document activation must remain fail-soft. Never retain project information from
                // the previously active drawing after a refresh failure.
                panel.ShowUnavailable("Project Information không thể đọc bản vẽ vừa kích hoạt; dữ liệu cũ đã được xóa.");
            }
        }
    }
}