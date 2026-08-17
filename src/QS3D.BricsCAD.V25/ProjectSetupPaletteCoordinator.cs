using System;
using Bricscad.Windows;
using QS3D.BricsCAD.V25.UI;
using DrawingSize = System.Drawing.Size;
using WpfSize = System.Windows.Size;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Owns the read-only Project Information surface hosted modelessly inside BricsCAD.
    /// Tab visibility is controlled separately by ProjectTabActivationCoordinator.
    /// </summary>
    internal static class ProjectSetupPaletteCoordinator
    {
        private static readonly Guid ProjectSetupGuid =
            new Guid("D9F85CA8-837A-4C40-A60B-3A89B7E1477B");

        private static PaletteSet? _palette;
        private static BltProjectSetupPanel? _panel;

        public static bool IsVisible => _palette != null && _palette.Visible;

        public static void ShowProjectInformation()
        {
            EnsureCreated();

            // Project Information owns the large embedded canvas while visible. Release the
            // other QS3D docked surfaces first instead of stacking palettes over the CAD viewport.
            StartCenterPaletteCoordinator.Hide();
            PaletteCoordinator.Hide();

            _panel?.ShowProjectInformation();
            if (_palette != null)
                _palette.Visible = true;
        }

        public static void Hide()
        {
            if (_palette != null)
                _palette.Visible = false;
        }

        public static void Dispose()
        {
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
    }
}
