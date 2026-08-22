using System;
using Bricscad.ApplicationServices;
using Bricscad.Windows;
using QS3D.BricsCAD.V25.UI;
using DrawingSize = System.Drawing.Size;
using WpfSize = System.Windows.Size;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Owns the Start Center as a native BricsCAD PaletteSet so KHỞI ĐẦU stays inside
    /// the BricsCAD host instead of opening a separate top-level WPF/Windows window.
    /// </summary>
    internal static class StartCenterPaletteCoordinator
    {
        private static readonly Guid StartCenterGuid = new Guid("CA48885E-9C0C-4E86-925E-5FC084FCA22A");
        private static PaletteSet? _palette;
        private static BltStartCenterPanel? _panel;
        private static bool _documentActivatedSubscribed;

        public static bool IsVisible => _palette != null && _palette.Visible;

        public static void Show()
        {
            EnsureCreated();
            SubscribeToDocumentActivation();

            if (_palette != null)
                _palette.Visible = true;

            _panel?.RefreshFromActiveDocument();
        }

        public static void Hide()
        {
            if (_palette != null)
                _palette.Visible = false;
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
                // BricsCAD may already be tearing native UI down during plugin unload.
            }
        }

        private static void EnsureCreated()
        {
            if (_palette != null && _panel != null) return;

            Dispose();
            try
            {
                _panel = new BltStartCenterPanel();
                _palette = new PaletteSet("BLT3D — Khởi đầu", StartCenterGuid)
                {
                    DockEnabled = DockSides.Left | DockSides.Right,
                    Dock = DockSides.Left,
                    Visible = false,
                    KeepFocus = false,
                    MinimumSize = new DrawingSize(720, 480)
                };
                _palette.DeviceIndependentSize = new WpfSize(1040, 680);
                _palette.AddVisual("Khởi đầu", _panel, true);
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
                // Keep the flag true so a later cleanup can retry without duplicate subscriptions.
            }
        }

        private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (_palette == null || !_palette.Visible || _panel == null) return;

            try
            {
                _panel.RefreshFromActiveDocument();
            }
            catch (Exception ex)
            {
                try
                {
                    e.Document?.Editor.WriteMessage("\nQS3DSTART refresh warning: " + ex.Message);
                }
                catch
                {
                    // Optional Start Center diagnostics must never escape document activation.
                }
            }
        }
    }
}
