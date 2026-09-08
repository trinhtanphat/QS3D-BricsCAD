#nullable enable
using System;
using System.Windows.Interop;
using System.Windows.Media;

namespace QS3D.LocalQualification
{
    // Opt-in diagnosis only. This is never an acceptance workaround: the caller
    // emits DIAGNOSTIC_ONLY, creates no authoring requests, and restores the mode.
    internal sealed class UiRenderExperiment : IDisposable
    {
        private readonly DateTime _started;
        private DateTime _last;
        private readonly RenderMode _original;
        private bool _changed;
        private bool _restored;
        private bool _disposed;

        public UiRenderExperiment(DateTime now)
        {
            _started = _last = now;
            _original = RenderOptions.ProcessRenderMode;
            if (_original != RenderMode.Default)
                throw new InvalidOperationException("render_experiment_baseline_not_default");
        }

        public string Advance(DateTime now)
        {
            if (_disposed || now < _last)
                throw new InvalidOperationException("render_experiment_clock_or_lifetime");
            if ((now - _last).TotalSeconds > 15)
                throw new InvalidOperationException("render_experiment_tick_gap");
            _last = now;
            var seconds = (now - _started).TotalSeconds;
            if (seconds >= 120 && !_changed)
            {
                if (RenderOptions.ProcessRenderMode != _original)
                    throw new InvalidOperationException("render_experiment_external_mode_change");
                // Mark before setting so exceptional setters also enter restoration.
                _changed = true;
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }
            if (seconds >= 240 && !_restored) Restore();
            var expected = _changed && !_restored ? RenderMode.SoftwareOnly : _original;
            if (RenderOptions.ProcessRenderMode != expected)
                throw new InvalidOperationException("render_experiment_mode_not_confirmed");
            if (seconds >= 300) return "complete";
            if (_restored) return "restored_default";
            return _changed ? "software_only" : "baseline_default";
        }

        private void Restore()
        {
            if (_changed && !_restored)
            {
                var current = RenderOptions.ProcessRenderMode;
                if (current != RenderMode.SoftwareOnly && current != _original)
                    throw new InvalidOperationException("render_experiment_restore_external_change");
                RenderOptions.ProcessRenderMode = _original;
                if (RenderOptions.ProcessRenderMode != _original)
                    throw new InvalidOperationException("render_experiment_restore_failed");
            }
            _restored = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Restore();
            _disposed = true;
        }
    }
}
