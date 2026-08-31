using System;
using System.Threading;
using System.Windows;
using BricsApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    internal enum McpOAuthConsentResult
    {
        Unavailable = 0,
        Approved = 1,
        Denied = 2,
    }

    /// <summary>
    /// Local, explicit approval boundary for remote OAuth authorization requests.
    /// The network worker never owns BricsCAD UI directly; the prompt is marshalled
    /// through BricsCAD's application context and only one consent prompt may exist.
    /// </summary>
    internal static class McpOAuthConsent
    {
        internal const int ConsentTimeoutMilliseconds = 90000;
        private const int ConsentQueued = 0;
        private const int ConsentRunning = 1;
        private const int ConsentCancelledBeforeStart = 2;
        private static readonly SemaphoreSlim ConsentGate = new SemaphoreSlim(1, 1);

        private sealed class ConsentWorkItem
        {
            internal string Resource = string.Empty;
            internal string Scope = string.Empty;
            internal McpOAuthConsentResult Result = McpOAuthConsentResult.Unavailable;
            internal readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
            internal int DispatchState = ConsentQueued;
            internal int Abandoned;
        }

        internal static McpOAuthConsentResult RequestApproval(string resource, string scope)
        {
            if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(scope))
                return McpOAuthConsentResult.Unavailable;
            if (!ConsentGate.Wait(0)) return McpOAuthConsentResult.Unavailable;

            var item = new ConsentWorkItem
            {
                Resource = resource,
                Scope = scope,
            };

            try
            {
                BricsApplication.DocumentManager.ExecuteInApplicationContext(ShowConsentInCadContext, item);
            }
            catch
            {
                ConsentGate.Release();
                item.Done.Dispose();
                return McpOAuthConsentResult.Unavailable;
            }

            if (!item.Done.Wait(ConsentTimeoutMilliseconds))
            {
                Interlocked.Exchange(ref item.Abandoned, 1);
                if (Interlocked.CompareExchange(
                        ref item.DispatchState,
                        ConsentCancelledBeforeStart,
                        ConsentQueued) == ConsentQueued)
                {
                    ConsentGate.Release();
                    item.Done.Dispose();
                }
                // If the prompt had already started, the callback owns gate/event cleanup.
                return McpOAuthConsentResult.Unavailable;
            }

            try { return item.Result; }
            finally { item.Done.Dispose(); }
        }

        private static void ShowConsentInCadContext(object state)
        {
            var item = (ConsentWorkItem)state;
            if (Interlocked.CompareExchange(ref item.DispatchState, ConsentRunning, ConsentQueued) != ConsentQueued)
                return;

            try
            {
                var result = MessageBox.Show(
                    "ChatGPT is requesting access to this running QS3D / BricsCAD session.\n\n"
                    + "MCP resource:\n" + item.Resource + "\n\n"
                    + "Requested scope: " + item.Scope + "\n\n"
                    + "Allow this connection?",
                    "QS3D MCP OAuth authorization",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);
                item.Result = result == MessageBoxResult.Yes
                    ? McpOAuthConsentResult.Approved
                    : McpOAuthConsentResult.Denied;
            }
            catch
            {
                item.Result = McpOAuthConsentResult.Unavailable;
            }
            finally
            {
                try { item.Done.Set(); }
                finally
                {
                    ConsentGate.Release();
                    if (Volatile.Read(ref item.Abandoned) != 0)
                    {
                        try { item.Done.Dispose(); } catch (ObjectDisposedException) { }
                    }
                }
            }
        }
    }
}
