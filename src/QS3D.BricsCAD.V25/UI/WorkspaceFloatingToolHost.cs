using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using QS3D.Core.Features;

namespace QS3D.BricsCAD.V25.UI
{
    internal sealed class WorkspaceFloatingToolHost : IDisposable
    {
        private sealed class HostedTool
        {
            public HostedTool(InteractionSurfaceBinding binding, Window window)
            {
                Binding = binding;
                Window = window;
            }

            public InteractionSurfaceBinding Binding { get; set; }
            public Window Window { get; }
        }

        private readonly Dictionary<string, HostedTool> _tools = new Dictionary<string, HostedTool>(StringComparer.Ordinal);
        private readonly Dictionary<string, FloatingToolBounds> _rememberedBounds = new Dictionary<string, FloatingToolBounds>(StringComparer.Ordinal);
        private bool _disposing;

        public int OpenCount => _tools.Count;

        public void OpenOrFocus(InteractionSurfaceBinding binding, object content)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (binding.Surface != InteractionSurface.FloatingTool)
                throw new InvalidOperationException("WorkspaceFloatingToolHost accepts only FloatingTool surface bindings.");

            if (_tools.TryGetValue(binding.ContentKey, out var existing))
            {
                existing.Binding = binding;
                existing.Window.Content = content;
                if (!existing.Window.IsVisible) existing.Window.Show();
                existing.Window.Activate();
                return;
            }

            var window = CreateWindow(binding.ContentKey, content);
            var hosted = new HostedTool(binding, window);
            _tools.Add(binding.ContentKey, hosted);
            window.Closed += (_, __) => OnWindowClosed(binding.ContentKey, hosted);
            window.Show();
            window.Activate();
        }

        public void Reconcile(InteractionSurfaceSnapshot snapshot, Func<InteractionSurfaceBinding, object> contentFactory)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (contentFactory == null) throw new ArgumentNullException(nameof(contentFactory));

            var desired = new HashSet<string>(snapshot.FloatingTools.Select(x => x.ContentKey), StringComparer.Ordinal);
            foreach (var key in _tools.Keys.Where(x => !desired.Contains(x)).ToArray())
                Close(key);

            foreach (var binding in snapshot.FloatingTools)
            {
                if (_tools.TryGetValue(binding.ContentKey, out var existing))
                {
                    existing.Binding = binding;
                    existing.Window.Content = contentFactory(binding);
                }
                else
                {
                    OpenOrFocus(binding, contentFactory(binding));
                }
            }
        }

        public bool Close(string contentKey)
        {
            if (string.IsNullOrWhiteSpace(contentKey))
                throw new ArgumentException("Floating tool key cannot be blank.", nameof(contentKey));
            if (!_tools.TryGetValue(contentKey.Trim(), out var hosted)) return false;

            RememberBounds(contentKey.Trim(), hosted.Window);
            _tools.Remove(contentKey.Trim());
            hosted.Window.Close();
            return true;
        }

        public void InvalidateContext()
        {
            foreach (var key in _tools.Keys.ToArray()) Close(key);
        }

        public void Dispose()
        {
            if (_disposing) return;
            _disposing = true;
            try
            {
                foreach (var key in _tools.Keys.ToArray()) Close(key);
            }
            finally
            {
                _disposing = false;
            }
        }

        private Window CreateWindow(string contentKey, object content)
        {
            var window = new Window
            {
                Title = contentKey,
                Content = content,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                SizeToContent = SizeToContent.Manual,
                MinWidth = FloatingToolWindowPolicy.MinimumWidth,
                MinHeight = FloatingToolWindowPolicy.MinimumHeight
            };

            var requested = _rememberedBounds.TryGetValue(contentKey, out var remembered)
                ? remembered
                : new FloatingToolBounds(double.NaN, double.NaN, double.NaN, double.NaN);
            ApplyVisibleBounds(window, requested);
            return window;
        }

        private static void ApplyVisibleBounds(Window window, FloatingToolBounds requested)
        {
            var workArea = SystemParameters.WorkArea;
            var normalized = FloatingToolWindowPolicy.Normalize(
                requested,
                new[] { new FloatingToolBounds(workArea.Left, workArea.Top, workArea.Width, workArea.Height) });
            window.Left = normalized.Left;
            window.Top = normalized.Top;
            window.Width = normalized.Width;
            window.Height = normalized.Height;
        }

        private void OnWindowClosed(string key, HostedTool hosted)
        {
            RememberBounds(key, hosted.Window);
            if (_tools.TryGetValue(key, out var current) && ReferenceEquals(current, hosted))
                _tools.Remove(key);
        }

        private void RememberBounds(string key, Window window)
        {
            if (window.WindowState == WindowState.Minimized) return;
            var requested = new FloatingToolBounds(window.Left, window.Top, window.ActualWidth > 0d ? window.ActualWidth : window.Width,
                window.ActualHeight > 0d ? window.ActualHeight : window.Height);
            var workArea = SystemParameters.WorkArea;
            _rememberedBounds[key] = FloatingToolWindowPolicy.Normalize(
                requested,
                new[] { new FloatingToolBounds(workArea.Left, workArea.Top, workArea.Width, workArea.Height) });
        }
    }
}
