#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Bricscad.Windows;
using WpfWindow = System.Windows.Window;

namespace QS3D.LocalQualification
{
    // Independent diagnostic content, not a QS3D visual or acceptance assertion.
    internal sealed class UiRenderControls : IDisposable
    {
        private PaletteSet? _palette;
        private WpfWindow? _window;
        private readonly TextBlock _paletteText = NewText();
        private readonly TextBlock _windowText = NewText();
        private Border? _paletteBorder;
        private Border? _windowBorder;
        private bool _windowClosed;
        private bool _disposed;
        private int _sequence = -1;
        private string _stage = "unstarted";

        public UiRenderControls(IntPtr owner, Guid runId)
        {
            if (owner == IntPtr.Zero || runId == Guid.Empty)
                throw new InvalidOperationException("render_controls_identity_missing");
            try
            {
                _paletteBorder = NewBorder(_paletteText);
                _windowBorder = NewBorder(_windowText);
                _palette = new PaletteSet("LOCAL022 palette witness", runId);
                _palette.DockEnabled = DockSides.None;
                _palette.Dock = DockSides.None;
                _palette.KeepFocus = false;
                _palette.MinimumSize = new System.Drawing.Size(360, 150);
                _palette.DeviceIndependentSize = new Size(360, 150);
                _palette.DeviceIndependentLocation = new Point(500, 400);
                _palette.AddVisual("Independent WPF", _paletteBorder, true);
                _palette.Visible = true;
                _window = new WpfWindow
                {
                    Title = "LOCAL022 window witness",
                    Style = null,
                    Width = 360, Height = 150, Left = 500, Top = 220,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ShowInTaskbar = false, ShowActivated = false,
                    AllowsTransparency = false, Topmost = false,
                    ResizeMode = ResizeMode.NoResize,
                    Background = Brushes.White, Content = _windowBorder
                };
                _window.Closed += OnWindowClosed;
                new WindowInteropHelper(_window).Owner = owner;
                _window.Show();
                Update("baseline_default", 0);
            }
            catch (Exception constructionError)
            {
                try { Dispose(); }
                catch (Exception cleanupError) { throw new AggregateException(constructionError, cleanupError); }
                throw;
            }
        }

        private static TextBlock NewText() => new TextBlock
        {
            Style = null, FontFamily = new FontFamily("Segoe UI"), FontSize = 23,
            Foreground = Brushes.Black, TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(12), Text = "Independent WPF"
        };

        private static Border NewBorder(TextBlock text) => new Border
        {
            Style = null, Background = Brushes.LightYellow,
            BorderBrush = Brushes.Black, BorderThickness = new Thickness(2), Child = text
        };

        private void OnWindowClosed(object? sender, EventArgs args) => _windowClosed = true;

        public void Update(string stage, int sequence)
        {
            if (_disposed || _windowClosed || _palette == null || _window == null)
                throw new InvalidOperationException("render_controls_closed");
            if (sequence < 0 || sequence < _sequence ||
                (stage != "baseline_default" && stage != "software_only" && stage != "restored_default" && stage != "complete"))
                throw new InvalidOperationException("render_controls_sequence_or_stage");
            if (sequence == _sequence && stage == _stage) return;
            _sequence = sequence; _stage = stage;
            var value = sequence.ToString("D3", CultureInfo.InvariantCulture) + "\n" + stage;
            _paletteText.Text = "PALETTE " + value;
            _windowText.Text = "WINDOW " + value;
            var color = sequence % 2 == 0 ? Brushes.LightYellow : Brushes.LightCyan;
            _paletteBorder!.Background = color;
            _windowBorder!.Background = color;
            _window.Title = "LOCAL022 window witness " + sequence.ToString("D3", CultureInfo.InvariantCulture) + " " + stage;
        }

        public string Snapshot() => "diagnostic_controls_only=true control_sequence=" +
            _sequence.ToString(CultureInfo.InvariantCulture) + " control_stage=" + _stage +
            " palette_loaded=" + _paletteText.IsLoaded.ToString().ToLowerInvariant() +
            " palette_visible=" + _paletteText.IsVisible.ToString().ToLowerInvariant() +
            " window_loaded=" + _windowText.IsLoaded.ToString().ToLowerInvariant() +
            " window_visible=" + _windowText.IsVisible.ToString().ToLowerInvariant() +
            " palette_source=" + (PresentationSource.FromVisual(_paletteText)?.GetType().Name ?? "none") +
            " window_source=" + (PresentationSource.FromVisual(_windowText)?.GetType().Name ?? "none");

        public void Dispose()
        {
            if (_disposed) return;
            var errors = new List<Exception>();
            if (_window != null)
            {
                try
                {
                    if (!_windowClosed) _window.Close();
                    _window.Closed -= OnWindowClosed;
                    _window = null;
                }
                catch (Exception error) { errors.Add(error); }
            }
            if (_palette != null)
            {
                try { _palette.Dispose(); _palette = null; }
                catch (Exception error) { errors.Add(error); }
            }
            if (errors.Count != 0) throw new AggregateException(errors);
            _disposed = true;
        }
    }
}
