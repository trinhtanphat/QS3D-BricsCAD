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

            var palette = _palette;
            var panel = _panel;
            if (palette == null || panel == null) return;

            var wasVisible = palette.Visible;
            var wasSubscribed = _documentActivatedSubscribed;

            try
            {
                SubscribeToDocumentActivation();
                palette.Visible = true;
                panel.RefreshFromDocument(Application.DocumentManager.MdiActiveDocument);
            }
            catch
            {
                if (!wasVisible)
                {
                    try { palette.Visible = false; }
                    catch
                    {
                        // Native visibility rollback is best-effort. Event ownership is still
                        // restored below so a failed Show cannot strand a hidden callback root.
                    }
                }

                if (!wasSubscribed)
                    UnsubscribeFromDocumentActivation();

                throw;
            }
        }

        public static void Hide()
        {
            var palette = _palette;
            if (palette != null)
            {
                try
                {
                    palette.Visible = false;
                }
                finally
                {
                    // PaletteSet visibility crosses the native host boundary and can throw during
                    // shutdown. Event ownership must still be released when hiding fails.
                    UnsubscribeFromDocumentActivation();
                }
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
            var panel = _panel;
            if (_palette == null || !_palette.Visible || panel == null) return;

            try
            {
                // Bind display state to the document carried by this activation event. Re-querying
                // MdiActiveDocument here can observe a later host transition and render the wrong DWG.
                panel.RefreshFromDocument(e.Document ?? Application.DocumentManager.MdiActiveDocument);
            }
            catch (Exception)
            {
                try
                {
                    e.Document?.Editor.WriteMessage("\nQS3DSTART refresh could not update the Start Center.");
                }
                catch
                {
                    // Optional Start Center diagnostics must never escape document activation.
                }
            }
        }
    }
}
