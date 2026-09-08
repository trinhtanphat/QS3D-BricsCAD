#nullable enable
using System;
using System.Globalization;
using System.Threading;
using System.Windows.Threading;

namespace QS3D.LocalQualification
{
    // Passive notification counts only: no forced layout, render, queue work or
    // acceptance verdict. Priority can change; these are NOT matched pending counts.
    internal sealed class UiRenderDiagnostics : IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private long _posted, _completed, _aborted;
        private int _disposed;

        public UiRenderDiagnostics(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _dispatcher.Hooks.OperationPosted += OnPosted;
            _dispatcher.Hooks.OperationCompleted += OnCompleted;
            _dispatcher.Hooks.OperationAborted += OnAborted;
        }

        private void OnPosted(object sender, DispatcherHookEventArgs args)
        {
            if (Volatile.Read(ref _disposed) == 0 && args.Operation.Priority == DispatcherPriority.Render)
                Interlocked.Increment(ref _posted);
        }
        private void OnCompleted(object sender, DispatcherHookEventArgs args)
        {
            if (Volatile.Read(ref _disposed) == 0 && args.Operation.Priority == DispatcherPriority.Render)
                Interlocked.Increment(ref _completed);
        }
        private void OnAborted(object sender, DispatcherHookEventArgs args)
        {
            if (Volatile.Read(ref _disposed) == 0 && args.Operation.Priority == DispatcherPriority.Render)
                Interlocked.Increment(ref _aborted);
        }

        public string Snapshot(int? commandActive, Dispatcher? workspaceDispatcher)
        {
            return "diagnostic_only=true command_active=" + (commandActive?.ToString(CultureInfo.InvariantCulture) ?? "unavailable") +
                " controller_thread=" + _dispatcher.Thread.ManagedThreadId.ToString(CultureInfo.InvariantCulture) +
                " workspace_thread=" + (workspaceDispatcher?.Thread.ManagedThreadId.ToString(CultureInfo.InvariantCulture) ?? "unavailable") +
                " same_dispatcher=" + (workspaceDispatcher == null ? "unbound" : ReferenceEquals(_dispatcher, workspaceDispatcher) ? "true" : "false") +
                " render_posted=" + Interlocked.Read(ref _posted).ToString(CultureInfo.InvariantCulture) +
                " render_completed=" + Interlocked.Read(ref _completed).ToString(CultureInfo.InvariantCulture) +
                " render_aborted=" + Interlocked.Read(ref _aborted).ToString(CultureInfo.InvariantCulture) +
                " disposed=" + (Volatile.Read(ref _disposed) == 0 ? "false" : "true");
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _dispatcher.Hooks.OperationPosted -= OnPosted;
            _dispatcher.Hooks.OperationCompleted -= OnCompleted;
            _dispatcher.Hooks.OperationAborted -= OnAborted;
        }
    }
}
