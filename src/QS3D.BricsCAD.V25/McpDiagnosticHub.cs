using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Bounded diagnostics bridge shared by MCP, QS3D and the BricsCAD host. Events are
    /// appended to the existing MCP audit JSONL so ChatGPT can retrieve them with
    /// cad_audit_tail without gaining arbitrary local-file access.
    /// </summary>
    internal static class McpDiagnosticHub
    {
        private const long MaxDiagnosticBytes = 4L * 1024L * 1024L;
        private const int MaxMessageCharacters = 1800;
        private const int MaxProjectAuditSnapshotEvents = 25;
        private const int CadReadTimeoutMilliseconds = 8000;
        private const int CadReadQueued = 0;
        private const int CadReadRunning = 1;
        private const int CadReadCancelledBeforeStart = 2;
        private static readonly object Gate = new object();
        private static readonly object WriteGate = new object();
        private static readonly Dictionary<Document, DocumentSubscription> Subscriptions =
            new Dictionary<Document, DocumentSubscription>();
        private static readonly Regex AuthorizationRegex = new Regex(
            "(?i)(authorization\\s*[:=]\\s*bearer\\s+)[^\\s;,\\\"]+",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex SecretRegex = new Regex(
            "(?i)((?:access[_-]?token|refresh[_-]?token|bearer|token|secret|password|client[_-]?secret)\\s*[:=]\\s*)[^\\s;,\\\"]+",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex SequenceRegex = new Regex(
            @"""sequence""\s*:\s*(?<value>[0-9]+)",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static Timer? _pollTimer;
        private static bool _started;
        private static bool _unhandledExceptionMayBeSubscribed;
        private static bool _unobservedTaskExceptionMayBeSubscribed;
        private static bool _documentBecameCurrentMayBeSubscribed;
        private static long _sequence;
        private static string _lastMcpError = string.Empty;
        private static DateTime _lastOAuthActivityUtc = DateTime.MinValue;

        private sealed class DocumentSubscription
        {
            private bool _willStartMayBeSubscribed;
            private bool _endedMayBeSubscribed;
            private bool _cancelledMayBeSubscribed;
            private bool _failedMayBeSubscribed;
            private int _acceptCallbacks;
            private readonly object _handlerGate = new object();

            public DocumentSubscription(Document document)
            {
                Document = document;
                WillStart = (sender, args) => OnCommand(this, "start", args, false);
                Ended = (sender, args) => OnCommand(this, "end", args, false);
                Cancelled = (sender, args) => OnCommand(this, "cancelled", args, true);
                Failed = (sender, args) => OnCommand(this, "failed", args, true);
            }

            public Document Document { get; private set; }
            public CommandEventHandler WillStart { get; private set; }
            public CommandEventHandler Ended { get; private set; }
            public CommandEventHandler Cancelled { get; private set; }
            public CommandEventHandler Failed { get; private set; }
            public bool AcceptCallbacks => Volatile.Read(ref _acceptCallbacks) != 0;
            public bool HasMayBeSubscribedHandlers
            {
                get
                {
                    lock (_handlerGate)
                        return _willStartMayBeSubscribed || _endedMayBeSubscribed || _cancelledMayBeSubscribed || _failedMayBeSubscribed;
                }
            }

            public void Subscribe()
            {
                lock (_handlerGate)
                {
                    if (_willStartMayBeSubscribed || _endedMayBeSubscribed || _cancelledMayBeSubscribed || _failedMayBeSubscribed)
                        throw new InvalidOperationException("Diagnostic command subscription still owns unresolved native handlers; attach was not repeated.");
                    try
                    {
                        _willStartMayBeSubscribed = true;
                        Document.CommandWillStart += WillStart;
                        _endedMayBeSubscribed = true;
                        Document.CommandEnded += Ended;
                        _cancelledMayBeSubscribed = true;
                        Document.CommandCancelled += Cancelled;
                        _failedMayBeSubscribed = true;
                        Document.CommandFailed += Failed;
                        Volatile.Write(ref _acceptCallbacks, 1);
                    }
                    catch
                    {
                        Volatile.Write(ref _acceptCallbacks, 0);
                        throw;
                    }
                }
            }

            public bool DetachBestEffort()
            {
                Volatile.Write(ref _acceptCallbacks, 0);
                lock (_handlerGate)
                {
                    var detached = true;
                    if (_willStartMayBeSubscribed)
                    {
                        try { Document.CommandWillStart -= WillStart; _willStartMayBeSubscribed = false; }
                        catch { detached = false; }
                    }
                    if (_endedMayBeSubscribed)
                    {
                        try { Document.CommandEnded -= Ended; _endedMayBeSubscribed = false; }
                        catch { detached = false; }
                    }
                    if (_cancelledMayBeSubscribed)
                    {
                        try { Document.CommandCancelled -= Cancelled; _cancelledMayBeSubscribed = false; }
                        catch { detached = false; }
                    }
                    if (_failedMayBeSubscribed)
                    {
                        try { Document.CommandFailed -= Failed; _failedMayBeSubscribed = false; }
                        catch { detached = false; }
                    }
                    return detached && !(_willStartMayBeSubscribed || _endedMayBeSubscribed || _cancelledMayBeSubscribed || _failedMayBeSubscribed);
                }
            }
        }

        private sealed class CadReadWorkItem
        {
            public Func<string> Action = null!;
            public string Result = string.Empty;
            public Exception? Error;
            public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
            public int State = CadReadQueued;
            public int Abandoned;
        }

        internal static void Start()
        {
            lock (Gate)
            {
                if (_started) return;
                if (HasGlobalSubscriptionOwnershipLocked())
                    throw new InvalidOperationException("Diagnostic global subscription cleanup remains unresolved; start was not repeated.");
                _sequence = Math.Max(_sequence, LoadLatestPersistedSequence());
                _lastMcpError = string.Empty;
                _lastOAuthActivityUtc = DateTime.MinValue;
            }

            try
            {
                lock (Gate) _unhandledExceptionMayBeSubscribed = true;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                lock (Gate) _unobservedTaskExceptionMayBeSubscribed = true;
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
                lock (Gate) _documentBecameCurrentMayBeSubscribed = true;
                Application.DocumentManager.DocumentBecameCurrent += OnDocumentBecameCurrent;
                lock (Gate) _started = true;
            }
            catch
            {
                DetachGlobalSubscriptionsBestEffort();
                throw;
            }

            Record("qs3d", "info", "diagnostics-start", "Unified MCP/QS3D/BricsCAD diagnostics bridge started.");
            QueueAttachActiveDocument();
            _pollTimer = new Timer(Poll, null, 750, 1000);
        }

        internal static void Stop()
        {
            Timer? timer;
            DocumentSubscription[] subscriptions;
            lock (Gate)
            {
                if (!_started && Subscriptions.Count == 0 && !HasGlobalSubscriptionOwnershipLocked()) return;
                _started = false;
                timer = _pollTimer;
                _pollTimer = null;
                subscriptions = new List<DocumentSubscription>(Subscriptions.Values).ToArray();
            }

            try { if (timer != null) timer.Dispose(); } catch { }
            foreach (var subscription in subscriptions)
            {
                var detached = subscription.DetachBestEffort();
                lock (Gate)
                {
                    DocumentSubscription? current;
                    if (detached && Subscriptions.TryGetValue(subscription.Document, out current)
                        && ReferenceEquals(current, subscription))
                        Subscriptions.Remove(subscription.Document);
                }
                if (!detached)
                    Record("bricscad", "warning", "command-monitor-detach-pending", "Native command monitor detach remains unresolved; ownership was retained for retry.", subscription.Document);
            }
            if (!DetachGlobalSubscriptionsBestEffort())
                Record("qs3d", "warning", "diagnostics-global-detach-pending", "Global diagnostics detach remains unresolved; ownership was retained for retry.");
            Record("qs3d", "info", "diagnostics-stop", "Unified diagnostics bridge stopped.");
        }

        private static bool HasGlobalSubscriptionOwnershipLocked()
        {
            return _unhandledExceptionMayBeSubscribed
                || _unobservedTaskExceptionMayBeSubscribed
                || _documentBecameCurrentMayBeSubscribed;
        }

        private static bool DetachGlobalSubscriptionsBestEffort()
        {
            var detached = true;
            lock (Gate)
            {
                if (_documentBecameCurrentMayBeSubscribed)
                {
                    try { Application.DocumentManager.DocumentBecameCurrent -= OnDocumentBecameCurrent; _documentBecameCurrentMayBeSubscribed = false; }
                    catch { detached = false; }
                }
                if (_unobservedTaskExceptionMayBeSubscribed)
                {
                    try { TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException; _unobservedTaskExceptionMayBeSubscribed = false; }
                    catch { detached = false; }
                }
                if (_unhandledExceptionMayBeSubscribed)
                {
                    try { AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException; _unhandledExceptionMayBeSubscribed = false; }
                    catch { detached = false; }
                }
                return detached && !HasGlobalSubscriptionOwnershipLocked();
            }
        }

        internal static string InvokeInCadContext(Func<string> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var item = new CadReadWorkItem { Action = action };
            Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadRead, item);
            if (!item.Done.Wait(CadReadTimeoutMilliseconds))
            {
                Interlocked.Exchange(ref item.Abandoned, 1);
                var cancelled = Interlocked.CompareExchange(ref item.State, CadReadCancelledBeforeStart, CadReadQueued) == CadReadQueued;
                if (cancelled)
                    throw new TimeoutException("Timed out waiting for BricsCAD application context; queued diagnostic read was cancelled before start.");
                throw new TimeoutException("Timed out after diagnostic CAD-context work started; completion is uncertain.");
            }
            try
            {
                if (item.Error != null) throw new InvalidOperationException(item.Error.Message, item.Error);
                return item.Result;
            }
            finally { item.Done.Dispose(); }
        }

        internal static void Record(string source, string severity, string eventName, string message, Document? document = null)
        {
            try
            {
                var safeSource = NormalizeToken(source, "qs3d", 32);
                var safeSeverity = NormalizeSeverity(severity);
                var safeEvent = NormalizeToken(eventName, "event", 80);
                var safeMessage = Redact(message);
                var documentName = SafeDocumentName(document);
                var sequence = Interlocked.Increment(ref _sequence);
                var line = new StringBuilder(512)
                    .Append("{\"utc\":\"").Append(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture))
                    .Append("\",\"sequence\":").Append(sequence.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"source\":\"").Append(Escape(safeSource))
                    .Append("\",\"severity\":\"").Append(Escape(safeSeverity))
                    .Append("\",\"event\":\"").Append(Escape(safeEvent))
                    .Append("\",\"message\":\"").Append(Escape(safeMessage)).Append('"');
                if (documentName.Length > 0)
                    line.Append(",\"document\":\"").Append(Escape(documentName)).Append('"');
                line.Append('}').Append(Environment.NewLine);
                AppendLine(line.ToString());
            }
            catch
            {
                // Diagnostics must never destabilize BricsCAD or a CAD mutation.
            }
        }

        internal static void CaptureSnapshot(string reason)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            Record("diagnostics", "info", "snapshot", "Requested diagnostics snapshot: " + (reason ?? string.Empty), document);

            try { Record("mcp", "info", "state", McpEmbeddedServer.Describe(), document); }
            catch (Exception ex) { Record("mcp", "warning", "state-read-failed", ex.Message, document); }

            try { Record("theme", "info", "state", Qs3dThemeCoordinator.Describe(), document); }
            catch (Exception ex) { Record("theme", "warning", "state-read-failed", ex.Message, document); }

            if (document == null)
            {
                Record("bricscad", "warning", "active-document", "No active BricsCAD document.");
                return;
            }

            try
            {
                var colorTheme = Convert.ToString(Application.GetSystemVariable("COLORTHEME"), CultureInfo.InvariantCulture) ?? string.Empty;
                var commandActive = Convert.ToString(Application.GetSystemVariable("CMDACTIVE"), CultureInfo.InvariantCulture) ?? string.Empty;
                Record("bricscad", "info", "host-state", "COLORTHEME=" + colorTheme + "; CMDACTIVE=" + commandActive, document);
            }
            catch (Exception ex)
            {
                Record("bricscad", "warning", "host-state-read-failed", ex.Message, document);
            }

            CaptureProjectAudit(document);
        }

        private static void ExecuteCadRead(object state)
        {
            var item = (CadReadWorkItem)state;
            try
            {
                if (Interlocked.CompareExchange(ref item.State, CadReadRunning, CadReadQueued) != CadReadQueued) return;
                item.Result = item.Action();
            }
            catch (Exception ex) { item.Error = ex; }
            finally
            {
                try { item.Done.Set(); }
                finally
                {
                    if (Volatile.Read(ref item.Abandoned) != 0)
                    {
                        try { item.Done.Dispose(); } catch (ObjectDisposedException) { }
                    }
                }
            }
        }

        private static void CaptureProjectAudit(Document document)
        {
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Record("qs3d", "info", "project-audit", "No existing QS3D project is attached to the active document.", document);
                    return;
                }

                var events = AuditTrail.ForProject(project).Events;
                Record("qs3d", "info", "project-audit-summary", "events=" + events.Count.ToString(CultureInfo.InvariantCulture), document);
                var start = Math.Max(0, events.Count - MaxProjectAuditSnapshotEvents);
                for (var i = start; i < events.Count; i++)
                {
                    var item = events[i];
                    Record(
                        "qs3d-audit",
                        "info",
                        item.Action,
                        "utc=" + item.Utc.ToString("o", CultureInfo.InvariantCulture)
                        + "; element=" + item.ElementId
                        + "; actor=" + item.Actor
                        + "; correlation=" + item.CorrelationId
                        + "; detail=" + item.Detail,
                        document);
                }
            }
            catch (Exception ex)
            {
                Record("qs3d", "warning", "project-audit-read-failed", ex.Message, document);
            }
        }

        private static void Poll(object? state)
        {
            bool active;
            lock (Gate) active = _started;
            if (!active) return;

            try
            {
                var error = McpEmbeddedServer.LastError ?? string.Empty;
                if (error.Length > 0 && !string.Equals(error, _lastMcpError, StringComparison.Ordinal))
                {
                    _lastMcpError = error;
                    Record("mcp", "error", "transport-error", error);
                }

                var activityUtc = McpEmbeddedServer.LastOAuthMcpActivityUtc;
                if (activityUtc > _lastOAuthActivityUtc)
                {
                    _lastOAuthActivityUtc = activityUtc;
                    Record(
                        "mcp",
                        "info",
                        "oauth-mcp-activity",
                        "utc=" + activityUtc.ToString("o", CultureInfo.InvariantCulture)
                        + "; method=" + (McpEmbeddedServer.LastOAuthMcpMethod ?? string.Empty));
                }
            }
            catch (Exception ex)
            {
                Record("mcp", "warning", "diagnostic-poll-failed", ex.Message);
            }
        }

        private static void OnDocumentBecameCurrent(object sender, DocumentCollectionEventArgs e)
        {
            QueueAttachActiveDocument();
        }

        private static void QueueAttachActiveDocument()
        {
            try { Application.DocumentManager.ExecuteInApplicationContext(AttachActiveDocumentInCadContext, null); }
            catch { }
        }

        private static void AttachActiveDocumentInCadContext(object state)
        {
            try { Attach(Application.DocumentManager.MdiActiveDocument); }
            catch (Exception ex) { Record("bricscad", "warning", "command-monitor-attach-failed", ex.Message); }
        }

        private static void Attach(Document? document)
        {
            if (document == null) return;
            DocumentSubscription subscription;
            lock (Gate)
            {
                if (!_started) return;
                DocumentSubscription? existing;
                if (Subscriptions.TryGetValue(document, out existing))
                {
                    if (existing.AcceptCallbacks) return;
                    if (!existing.DetachBestEffort())
                        throw new InvalidOperationException("Previous diagnostic command subscription cleanup remains unresolved; attach was not repeated.");
                    Subscriptions.Remove(document);
                }

                subscription = new DocumentSubscription(document);
                Subscriptions.Add(document, subscription);
                try
                {
                    subscription.Subscribe();
                }
                catch
                {
                    if (subscription.DetachBestEffort())
                    {
                        DocumentSubscription? current;
                        if (Subscriptions.TryGetValue(document, out current) && ReferenceEquals(current, subscription))
                            Subscriptions.Remove(document);
                    }
                    throw;
                }
            }
            Record("bricscad", "info", "command-monitor-attached", "Command lifecycle monitor attached.", document);
        }

        private static void OnCommand(DocumentSubscription subscription, string phase, CommandEventArgs? args, bool important)
        {
            if (!subscription.AcceptCallbacks) return;
            var document = subscription.Document;
            lock (Gate)
            {
                DocumentSubscription? current;
                if (!_started || !Subscriptions.TryGetValue(document, out current) || !ReferenceEquals(current, subscription)) return;
            }
            var command = (args == null ? string.Empty : args.GlobalCommandName) ?? string.Empty;
            command = command.Trim();
            var isQs3d = command.StartsWith("QS3D", StringComparison.OrdinalIgnoreCase);
            if (!important && !isQs3d) return;
            var severity = phase == "failed" ? "error" : phase == "cancelled" ? "warning" : "info";
            Record(isQs3d ? "qs3d" : "bricscad", severity, "command-" + phase, "command=" + command, document);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Record("qs3d", "error", "unhandled-exception", exception == null ? "Unhandled runtime exception." : exception.ToString());
        }

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Record("qs3d", "error", "unobserved-task-exception", e.Exception == null ? "Unobserved task exception." : e.Exception.ToString());
        }

        private static long LoadLatestPersistedSequence()
        {
            var latest = 0L;
            var path = McpCadAgentRuntime.AuditFilePath;
            latest = Math.Max(latest, ReadLatestSequence(path + ".1"));
            latest = Math.Max(latest, ReadLatestSequence(path));
            return latest;
        }

        private static long ReadLatestSequence(string path)
        {
            if (!File.Exists(path)) return 0L;
            var latest = 0L;
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, false))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0 || line.Length > 8192) continue;
                        var match = SequenceRegex.Match(line);
                        if (!match.Success) continue;
                        long value;
                        if (long.TryParse(match.Groups["value"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out value)
                            && value > latest)
                            latest = value;
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return latest;
        }

        private static void AppendLine(string line)
        {
            lock (WriteGate)
            {
                var path = McpCadAgentRuntime.AuditFilePath;
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                Rotate(path);
                var payload = new UTF8Encoding(false).GetBytes(line);
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                        {
                            stream.Write(payload, 0, payload.Length);
                            stream.Flush();
                        }
                        return;
                    }
                    catch (IOException)
                    {
                        if (attempt == 2) return;
                        Thread.Sleep(10 * (attempt + 1));
                    }
                }
            }
        }

        private static void Rotate(string path)
        {
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length < MaxDiagnosticBytes) return;
                var previous = path + ".1";
                try { if (File.Exists(previous)) File.Delete(previous); } catch { }
                try { File.Move(path, previous); }
                catch { File.WriteAllText(path, string.Empty, new UTF8Encoding(false)); }
            }
            catch { }
        }

        private static string Redact(string value)
        {
            var text = value ?? string.Empty;
            text = AuthorizationRegex.Replace(text, "$1[REDACTED]");
            text = SecretRegex.Replace(text, "$1[REDACTED]");
            if (text.Length > MaxMessageCharacters) text = text.Substring(0, MaxMessageCharacters) + "…";
            var builder = new StringBuilder(text.Length);
            foreach (var ch in text)
                builder.Append(ch < 32 && ch != '\t' ? ' ' : ch);
            return builder.ToString();
        }

        private static string NormalizeSeverity(string value)
        {
            var text = (value ?? string.Empty).Trim().ToLowerInvariant();
            return text == "error" || text == "warning" || text == "debug" ? text : "info";
        }

        private static string NormalizeToken(string value, string fallback, int maximum)
        {
            var text = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (text.Length == 0) text = fallback;
            if (text.Length > maximum) text = text.Substring(0, maximum);
            var builder = new StringBuilder(text.Length);
            foreach (var ch in text)
                builder.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '-');
            return builder.ToString();
        }

        private static string SafeDocumentName(Document? document)
        {
            if (document == null) return string.Empty;
            try
            {
                var value = document.Name ?? string.Empty;
                var leaf = Path.GetFileName(value);
                return Redact(string.IsNullOrWhiteSpace(leaf) ? value : leaf);
            }
            catch { return string.Empty; }
        }

        private static string Escape(string value)
        {
            var builder = new StringBuilder((value ?? string.Empty).Length + 16);
            foreach (var ch in value ?? string.Empty)
            {
                switch (ch)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (ch < 32) builder.Append(' ');
                        else builder.Append(ch);
                        break;
                }
            }
            return builder.ToString();
        }
    }

    public sealed class McpDiagnosticCommands
    {
        [CommandMethod("QS3DDIAGNOSTICSSNAPSHOT", CommandFlags.Modal)]
        public void CaptureSnapshot()
        {
            McpDiagnosticHub.CaptureSnapshot("command");
            try
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D diagnostics snapshot captured. ChatGPT can retrieve it with MCP cad_audit_tail.");
            }
            catch { }
        }
    }
}