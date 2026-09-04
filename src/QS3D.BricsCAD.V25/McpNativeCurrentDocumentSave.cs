using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Synchronous MCP facade over BricsCAD's host-owned native QSAVE lifecycle.
    ///
    /// The active DWG is already owned/open by BricsCAD, so current-document save must not
    /// reopen or write that same path through Database.Save/SaveAs. Native QSAVE is queued in
    /// application context, its terminal event is awaited outside that callback, and persistent
    /// DBMOD content bits are then verified before success is reported.
    /// </summary>
    internal static class McpNativeCurrentDocumentSave
    {
        private const int DbmodPersistentContentMask = 1 | 4 | 32;
        private const int CommandCompletionTimeoutMilliseconds = 30000;
        private const int DbmodSettleTimeoutMilliseconds = 3000;
        private const int PollMilliseconds = 25;

        internal sealed class SaveResult
        {
            internal SaveResult(string fileName, int dbmodAfterSave)
            {
                FileName = fileName;
                DbmodAfterSave = dbmodAfterSave;
            }

            internal string FileName { get; private set; }
            internal int DbmodAfterSave { get; private set; }
        }

        internal static SaveResult SaveCurrentDocument(Action ensureRunning, Action<string>? audit)
        {
            if (ensureRunning == null) throw new ArgumentNullException(nameof(ensureRunning));
            ensureRunning();

            var operation = new NativeSaveOperation(ensureRunning, audit);
            try
            {
                McpDiagnosticHub.InvokeInCadContext(() =>
                {
                    operation.QueueInCadContext();
                    return string.Empty;
                });

                if (!operation.Done.Wait(CommandCompletionTimeoutMilliseconds))
                    throw new TimeoutException(
                        "Timed out waiting for native BricsCAD QSAVE to reach a terminal event; save completion is uncertain. "
                        + "Do not retry automatically. Inspect the drawing, DBMOD, audit state and filesystem before another save attempt.");

                // A terminal event alone is not a truthful success boundary. The callback attempts
                // to unsubscribe in CAD context, but native event removal can fail. Re-enter CAD
                // context as a serialization barrier and require all handler ownership to be
                // proven released before interpreting terminal state or DBMOD as a successful save.
                if (!operation.DetachBestEffort())
                    throw new InvalidOperationException(
                        "Native BricsCAD QSAVE reached a terminal event but terminal handler cleanup could not be confirmed; "
                        + "save completion is uncertain. Do not retry automatically. Inspect the drawing, DBMOD, audit state and filesystem before another save attempt.");

                if (!string.IsNullOrEmpty(operation.TerminalError))
                    throw new InvalidOperationException(operation.TerminalError);

                var dbmodAfterSave = WaitForCleanDbmod(operation, ensureRunning);
                audit?.Invoke("native QSAVE completed; fileName=" + SafeLeaf(operation.FullPath)
                    + "; dbmodAfterSave=" + dbmodAfterSave.ToString(CultureInfo.InvariantCulture));
                return new SaveResult(SafeLeaf(operation.FullPath), dbmodAfterSave);
            }
            finally
            {
                // Always serialize cleanup through BricsCAD application context before disposing
                // the wait handle. If a long-running QSAVE or failed native unsubscribe prevents
                // proven cleanup, intentionally keep the handle alive for a late callback/retry.
                if (operation.DetachBestEffort()) operation.Done.Dispose();
            }
        }

        private static int WaitForCleanDbmod(NativeSaveOperation operation, Action ensureRunning)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(DbmodSettleTimeoutMilliseconds);
            var lastDbmod = -1;
            do
            {
                ensureRunning();
                var raw = McpDiagnosticHub.InvokeInCadContext(() =>
                {
                    ensureRunning();
                    var document = Application.DocumentManager.MdiActiveDocument;
                    if (document == null || !ReferenceEquals(document, operation.Document))
                        throw new InvalidOperationException(
                            "The active BricsCAD document changed while native QSAVE completion was being verified.");

                    var currentPath = document.Database.Filename ?? string.Empty;
                    if (!Path.IsPathRooted(currentPath)
                        || !SamePath(currentPath, operation.FullPath))
                        throw new InvalidOperationException(
                            "The active BricsCAD document path changed while native QSAVE completion was being verified.");

                    object value;
                    try { value = Application.GetSystemVariable("DBMOD"); }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            "Could not read BricsCAD DBMOD after native QSAVE; save completion cannot be confirmed.", ex);
                    }

                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                });

                int dbmod;
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out dbmod) && dbmod >= 0)
                {
                    lastDbmod = dbmod;
                    if ((dbmod & DbmodPersistentContentMask) == 0)
                        return dbmod;
                }

                Thread.Sleep(PollMilliseconds);
            }
            while (DateTime.UtcNow < deadline);

            throw new InvalidOperationException(
                "Native BricsCAD QSAVE reached a terminal event but persistent DBMOD content bits did not settle; "
                + "save completion was not confirmed. DBMOD="
                + (lastDbmod >= 0 ? lastDbmod.ToString(CultureInfo.InvariantCulture) : "unavailable") + ".");
        }

        private static bool SamePath(string left, string right)
        {
            try
            {
                return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static string SafeLeaf(string path)
        {
            try { return Path.GetFileName(path) ?? string.Empty; }
            catch { return string.Empty; }
        }

        private sealed class NativeSaveOperation
        {
            private readonly Action _ensureRunning;
            private readonly Action<string>? _audit;
            private int _terminalSet;
            private bool _commandEndedAttached;
            private bool _commandCancelledAttached;
            private bool _commandFailedAttached;

            internal NativeSaveOperation(Action ensureRunning, Action<string>? audit)
            {
                _ensureRunning = ensureRunning;
                _audit = audit;
            }

            internal readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
            internal Document? Document { get; private set; }
            internal string FullPath { get; private set; } = string.Empty;
            internal string TerminalError { get; private set; } = string.Empty;

            internal void QueueInCadContext()
            {
                _ensureRunning();
                var document = Application.DocumentManager.MdiActiveDocument;
                if (document == null) throw new InvalidOperationException("No active BricsCAD document.");

                var filename = document.Database.Filename ?? string.Empty;
                if (!Path.IsPathRooted(filename))
                    throw new InvalidOperationException("Active drawing has no existing local path. Use cad_save_as first.");
                if (document.IsReadOnly)
                    throw new InvalidOperationException(
                        "Active drawing is read-only. Native QSAVE was not queued; use an explicit writable Save As target instead.");

                int commandActive;
                try
                {
                    commandActive = Convert.ToInt32(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Could not read BricsCAD CMDACTIVE before native QSAVE; save was not queued.", ex);
                }
                if (commandActive != 0)
                    throw new InvalidOperationException(
                        "Cannot save while a BricsCAD command is active. Wait for idle or cancel the active command before retrying.");

                Document = document;
                FullPath = Path.GetFullPath(filename);
                AttachHandlers(document);
                try
                {
                    McpCadMutationCoordinator.QueueNativeCommand(
                        document,
                        "QSAVE",
                        () => document.SendStringToExecute("_.QSAVE\n", true, false, true),
                        _audit);
                    _audit?.Invoke("native QSAVE queued; fileName=" + SafeLeaf(FullPath));
                }
                catch
                {
                    DetachInCadContext();
                    throw;
                }
            }

            internal bool DetachBestEffort()
            {
                if (Document == null) return true;
                try
                {
                    // Even when the terminal handler already detached itself, this empty CAD
                    // callback is a serialization barrier proving that handler has returned before
                    // the worker thread disposes Done. Per-handler ownership remains published
                    // until the matching native unsubscribe actually succeeds.
                    var detached = false;
                    McpDiagnosticHub.InvokeInCadContext(() =>
                    {
                        detached = DetachInCadContext();
                        return string.Empty;
                    });
                    return detached;
                }
                catch
                {
                    // Coordinator-owned terminal handlers remain authoritative for writer safety.
                    // Keep Done alive because a late helper terminal callback may still need it.
                    return false;
                }
            }

            private void AttachHandlers(Document document)
            {
                try
                {
                    document.CommandEnded += OnCommandEnded;
                    _commandEndedAttached = true;
                    document.CommandCancelled += OnCommandCancelled;
                    _commandCancelledAttached = true;
                    document.CommandFailed += OnCommandFailed;
                    _commandFailedAttached = true;
                }
                catch
                {
                    if (!DetachInCadContext())
                        throw new InvalidOperationException(
                            "Native QSAVE terminal handler attachment failed and rollback could not prove all subscriptions detached.");
                    throw;
                }
            }

            private bool DetachInCadContext()
            {
                var document = Document;
                if (document == null) return true;

                if (_commandEndedAttached)
                {
                    try
                    {
                        document.CommandEnded -= OnCommandEnded;
                        _commandEndedAttached = false;
                    }
                    catch { }
                }
                if (_commandCancelledAttached)
                {
                    try
                    {
                        document.CommandCancelled -= OnCommandCancelled;
                        _commandCancelledAttached = false;
                    }
                    catch { }
                }
                if (_commandFailedAttached)
                {
                    try
                    {
                        document.CommandFailed -= OnCommandFailed;
                        _commandFailedAttached = false;
                    }
                    catch { }
                }

                return !_commandEndedAttached && !_commandCancelledAttached && !_commandFailedAttached;
            }

            private void OnCommandEnded(object sender, CommandEventArgs e)
            {
                Complete(sender, e, string.Empty, "ended");
            }

            private void OnCommandCancelled(object sender, CommandEventArgs e)
            {
                Complete(sender, e, "Native BricsCAD QSAVE was cancelled; save completion was not confirmed.", "cancelled");
            }

            private void OnCommandFailed(object sender, CommandEventArgs e)
            {
                Complete(sender, e, "Native BricsCAD QSAVE failed; save completion was not confirmed.", "failed");
            }

            private void Complete(object sender, CommandEventArgs e, string error, string state)
            {
                if (!Matches(sender, e)) return;
                if (Interlocked.CompareExchange(ref _terminalSet, 1, 0) != 0) return;
                TerminalError = error;
                try
                {
                    DetachInCadContext();
                    try { _audit?.Invoke("native QSAVE " + state); }
                    catch { }
                }
                finally
                {
                    Done.Set();
                }
            }

            private bool Matches(object sender, CommandEventArgs e)
            {
                if (Document == null || !ReferenceEquals(sender, Document)) return false;
                var command = NormalizeCommand(e == null ? string.Empty : e.GlobalCommandName);
                return string.Equals(command, "QSAVE", StringComparison.OrdinalIgnoreCase);
            }

            private static string NormalizeCommand(string command)
            {
                var value = (command ?? string.Empty).Trim();
                var index = 0;
                while (index < value.Length && (value[index] == '_' || value[index] == '.')) index++;
                return value.Substring(index).ToUpperInvariant();
            }
        }
    }
}
