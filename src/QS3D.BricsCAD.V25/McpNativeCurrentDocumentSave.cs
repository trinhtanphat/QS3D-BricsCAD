using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Synchronous MCP facade over BricsCAD's host-owned native QSAVE lifecycle.
    ///
    /// The active DWG is already owned/open by BricsCAD, so current-document save must not
    /// reopen or write that same path through Database.Save/SaveAs. Native QSAVE is executed in
    /// BricsCAD command context, then persistent DBMOD content bits are verified before success.
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
            McpDiagnosticHub.InvokeInCadContext(() =>
            {
                operation.ScheduleInCadContext();
                return string.Empty;
            });

            var completion = operation.Completion;
            if (completion == null)
                throw new InvalidOperationException("Native BricsCAD QSAVE was not scheduled in command context.");

            if (Task.WaitAny(new[] { completion }, CommandCompletionTimeoutMilliseconds) < 0)
                throw new TimeoutException(
                    "Timed out waiting for native BricsCAD QSAVE command-context completion; save completion is uncertain. "
                    + "Do not retry automatically. Inspect the drawing, DBMOD, audit state and filesystem before another save attempt.");

            try
            {
                completion.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Native BricsCAD QSAVE failed in command context; save completion was not confirmed. Do not retry automatically.",
                    ex);
            }

            var dbmodAfterSave = WaitForCleanDbmod(operation, ensureRunning);
            audit?.Invoke("native QSAVE completed; fileName=" + SafeLeaf(operation.FullPath)
                + "; dbmodAfterSave=" + dbmodAfterSave.ToString(CultureInfo.InvariantCulture));
            return new SaveResult(SafeLeaf(operation.FullPath), dbmodAfterSave);
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
                    operation.EnsureSameActiveDocumentAndPath();

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
                "Native BricsCAD QSAVE completed in command context but persistent DBMOD content bits did not settle; "
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

            internal NativeSaveOperation(Action ensureRunning, Action<string>? audit)
            {
                _ensureRunning = ensureRunning;
                _audit = audit;
            }

            internal Document? Document { get; private set; }
            internal string FullPath { get; private set; } = string.Empty;
            internal Task? Completion { get; private set; }

            internal void ScheduleInCadContext()
            {
                _ensureRunning();
                var document = Application.DocumentManager.MdiActiveDocument;
                if (document == null) throw new InvalidOperationException("No active BricsCAD document.");

                var filename = document.Database.Filename ?? string.Empty;
                if (!Path.IsPathRooted(filename))
                    throw new InvalidOperationException("Active drawing has no existing local path. Use cad_save_as first.");
                if (document.IsReadOnly)
                    throw new InvalidOperationException(
                        "Active drawing is read-only. Native QSAVE was not started; use an explicit writable Save As target instead.");

                int commandActive;
                try
                {
                    commandActive = Convert.ToInt32(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Could not read BricsCAD CMDACTIVE before native QSAVE; save was not started.", ex);
                }
                if (commandActive != 0)
                    throw new InvalidOperationException(
                        "Cannot save while a BricsCAD command is active. Wait for idle or cancel the active command before retrying.");

                Document = document;
                FullPath = Path.GetFullPath(filename);
                _audit?.Invoke("native QSAVE scheduled in command context; fileName=" + SafeLeaf(FullPath));
                Completion = Application.DocumentManager.ExecuteInCommandContextAsync(
                    _ => ExecuteQsaveInCommandContext(),
                    null);
            }

            private Task ExecuteQsaveInCommandContext()
            {
                _ensureRunning();
                var document = Document;
                if (document == null)
                    throw new InvalidOperationException("Native QSAVE lost its active document before command execution.");
                EnsureSameActiveDocumentAndPath();

                int commandActive;
                try
                {
                    commandActive = Convert.ToInt32(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Could not read BricsCAD CMDACTIVE in QSAVE command context.", ex);
                }
                if (commandActive != 0)
                    throw new InvalidOperationException(
                        "BricsCAD became busy before QSAVE entered command context; save was not started.");

                _audit?.Invoke("native QSAVE command-context start; fileName=" + SafeLeaf(FullPath));
                document.Editor.Command("_.QSAVE");
                _ensureRunning();
                EnsureSameActiveDocumentAndPath();
                _audit?.Invoke("native QSAVE command-context end; fileName=" + SafeLeaf(FullPath));
                return Task.CompletedTask;
            }

            internal void EnsureSameActiveDocumentAndPath()
            {
                var document = Document;
                if (document == null || !ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    throw new InvalidOperationException(
                        "The active BricsCAD document changed while native QSAVE was executing or being verified.");

                var currentPath = document.Database.Filename ?? string.Empty;
                if (!Path.IsPathRooted(currentPath) || !SamePath(currentPath, FullPath))
                    throw new InvalidOperationException(
                        "The active BricsCAD document path changed while native QSAVE was executing or being verified.");
            }
        }
    }
}
