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
        InteractionRequired = 3,
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
        }

        internal static McpOAuthConsentResult RequestApproval(string resource, string scope)
        {
            if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(scope))
                return McpOAuthConsentResult.Unavailable;

            // A concurrent foreground consent already owns the interactive lane. Do not queue
            // another modal behind it: return a bounded retryable OAuth state instead.
            if (!ConsentGate.Wait(0)) return McpOAuthConsentResult.InteractionRequired;

            var item = new ConsentWorkItem
            {
                Resource = resource,
                Scope = scope,
            };
            IDisposable? interactionAdmission = null;

            try
            {
                // Reuse the process-global CAD mutation/writer admission boundary so the check
                // and ownership are atomic. An active explicit writer lease, mutation, queued
                // native command, or BricsCAD modal state rejects this admission before the
                // OAuth UI is dispatched. Holding the scope also prevents a new mutation from
                // entering while the explicit foreground consent prompt is visible.
                try
                {
                    interactionAdmission = McpCadMutationCoordinator.EnterMutation(
                        string.Empty,
                        "oauth_interactive_consent",
                        null);
                }
                catch (InvalidOperationException)
                {
                    return McpOAuthConsentResult.InteractionRequired;
                }

                try
                {
                    BricsApplication.DocumentManager.ExecuteInApplicationContext(ShowConsentInCadContext, item);
                }
                catch
                {
                    return McpOAuthConsentResult.Unavailable;
                }

                if (!item.Done.Wait(ConsentTimeoutMilliseconds))
                {
                    // If dispatch is still queued, cancel it before releasing CAD admission so
                    // the delayed callback can never surface a modal after this request returns.
                    if (Interlocked.CompareExchange(
                            ref item.DispatchState,
                            ConsentCancelledBeforeStart,
                            ConsentQueued) == ConsentQueued)
                        return McpOAuthConsentResult.InteractionRequired;

                    // The callback already owns the foreground prompt. Keep CAD admission and
                    // the single-flight gate until that prompt actually closes; otherwise a
                    // mutation could enter underneath a still-visible consent modal.
                    item.Done.Wait();
                }

                return item.Result;
            }
            finally
            {
                try { item.Done.Dispose(); }
                finally
                {
                    if (interactionAdmission != null) interactionAdmission.Dispose();
                    ConsentGate.Release();
                }
            }
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
                item.Done.Set();
            }
        }
    }
}
