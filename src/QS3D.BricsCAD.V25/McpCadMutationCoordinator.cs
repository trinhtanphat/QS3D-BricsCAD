using System;
using System.Globalization;
using System.Threading;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Process-global coordination boundary for MCP CAD mutations. Many MCP clients may
    /// remain connected and perform read-only work, but only one mutation call may enter
    /// the BricsCAD write lane at a time. An optional writer lease keeps ownership across
    /// a multi-call workflow so another chat/session cannot interleave DWG mutations.
    /// </summary>
    internal static class McpCadMutationCoordinator
    {
        private const int MutationAcquireTimeoutMilliseconds = 750;
        private const int DefaultLeaseSeconds = 120;
        private const int MinLeaseSeconds = 15;
        private const int MaxLeaseSeconds = 300;
        private const int PendingNativeCommandMaximumSeconds = 45;

        private static readonly SemaphoreSlim MutationGate = new SemaphoreSlim(1, 1);
        private static readonly object Sync = new object();
        private static readonly AsyncLocal<long?> CurrentOperationId = new AsyncLocal<long?>();

        private static WriterLease? _lease;
        private static PendingNativeCommand? _pending;
        private static long _operationSequence;

        internal static string AcquireWriterLease(int leaseSeconds, Action<string>? audit)
        {
            var seconds = Math.Max(MinLeaseSeconds, Math.Min(MaxLeaseSeconds,
                leaseSeconds <= 0 ? DefaultLeaseSeconds : leaseSeconds));
            if (!MutationGate.Wait(MutationAcquireTimeoutMilliseconds))
                throw new InvalidOperationException("DWG writer is busy with another mutation. Retry after the current write call completes.");
            try
            {
                lock (Sync)
                {
                    CleanupExpiredStateLocked(DateTime.UtcNow);
                    if (_pending != null)
                        throw new InvalidOperationException("DWG writer cannot be acquired while a queued native command is still active or awaiting its terminal event.");
                    if (_lease != null)
                        throw new InvalidOperationException("DWG writer lease is already owned by another MCP workflow. Read-only tools remain available; wait for release or lease expiry before mutating.");

                    var now = DateTime.UtcNow;
                    var token = Guid.NewGuid().ToString("N");
                    _lease = new WriterLease(token, now, now.AddSeconds(seconds), seconds);
                    audit?.Invoke("writer lease acquired; leaseSeconds=" + seconds.ToString(CultureInfo.InvariantCulture));
                    return "{\"acquired\":true,\"writerToken\":\"" + Escape(token)
                           + "\",\"leaseSeconds\":" + seconds.ToString(CultureInfo.InvariantCulture)
                           + ",\"expiresUtc\":\"" + _lease.ExpiresUtc.ToString("o", CultureInfo.InvariantCulture) + "\"}";
                }
            }
            finally { MutationGate.Release(); }
        }

        internal static string ReleaseWriterLease(string writerToken, Action<string>? audit)
        {
            var token = NormalizeToken(writerToken);
            if (!MutationGate.Wait(MutationAcquireTimeoutMilliseconds))
                throw new InvalidOperationException("DWG writer is busy with another mutation. Retry release after the current write call completes.");
            try
            {
                lock (Sync)
                {
                    CleanupExpiredStateLocked(DateTime.UtcNow);
                    if (_lease == null)
                        return "{\"released\":false,\"reason\":\"no-active-lease\"}";
                    RequireLeaseTokenLocked(token);
                    if (_pending != null)
                    {
                        _lease.ReleaseWhenIdle = true;
                        audit?.Invoke("writer lease release deferred until queued native command terminates");
                        return "{\"released\":false,\"releaseWhenIdle\":true,\"pendingNativeCommand\":true}";
                    }
                    _lease = null;
                    audit?.Invoke("writer lease released");
                    return "{\"released\":true}";
                }
            }
            finally { MutationGate.Release(); }
        }

        internal static IDisposable EnterMutation(string writerToken, string tool, Action<string>? audit)
        {
            var token = NormalizeOptionalToken(writerToken);
            if (!MutationGate.Wait(MutationAcquireTimeoutMilliseconds))
                throw new InvalidOperationException("DWG writer is busy with another mutation. Read-only tools remain available; retry this mutation later.");

            try
            {
                lock (Sync)
                {
                    var now = DateTime.UtcNow;
                    CleanupExpiredStateLocked(now);
                    if (_pending != null)
                        throw new InvalidOperationException("A queued native BricsCAD command still owns the DWG write lane. Wait for command completion/cancellation before another mutation.");

                    if (_lease != null)
                    {
                        RequireLeaseTokenLocked(token);
                        _lease.ExpiresUtc = now.AddSeconds(_lease.LeaseSeconds);
                    }
                    else if (token.Length != 0)
                    {
                        throw new InvalidOperationException("writerToken is stale or invalid because no explicit DWG writer lease is active.");
                    }

                    var previous = CurrentOperationId.Value;
                    var operationId = Interlocked.Increment(ref _operationSequence);
                    CurrentOperationId.Value = operationId;
                    audit?.Invoke("writer mutation entered; tool=" + SafeTool(tool)
                        + "; lease=" + (_lease == null ? "ephemeral" : "explicit"));
                    return new MutationScope(operationId, previous, audit);
                }
            }
            catch
            {
                MutationGate.Release();
                throw;
            }
        }

        internal static void QueueNativeCommand(Document document, string command, Action enqueue, Action<string>? audit)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (enqueue == null) throw new ArgumentNullException(nameof(enqueue));
            var operationId = CurrentOperationId.Value;
            if (!operationId.HasValue)
                throw new InvalidOperationException("Native command queueing requires the active MCP DWG writer mutation scope.");

            PendingNativeCommand pending;
            lock (Sync)
            {
                CleanupExpiredStateLocked(DateTime.UtcNow);
                if (_pending != null)
                    throw new InvalidOperationException("Another queued native command already owns the DWG write lane.");
                pending = new PendingNativeCommand(document, NormalizeCommand(command), DateTime.UtcNow, audit);
                pending.WillStartHandler = OnCommandWillStart;
                pending.EndedHandler = OnCommandEnded;
                pending.CancelledHandler = OnCommandCancelled;
                pending.FailedHandler = OnCommandFailed;
                document.CommandWillStart += pending.WillStartHandler;
                document.CommandEnded += pending.EndedHandler;
                document.CommandCancelled += pending.CancelledHandler;
                document.CommandFailed += pending.FailedHandler;
                _pending = pending;
            }

            try
            {
                enqueue();
                audit?.Invoke("native command queued; command=" + SafeTool(command));
            }
            catch
            {
                lock (Sync)
                {
                    if (ReferenceEquals(_pending, pending))
                    {
                        DetachPendingLocked(pending);
                        _pending = null;
                    }
                }
                throw;
            }
        }

        internal static string StatusJson()
        {
            lock (Sync)
            {
                var now = DateTime.UtcNow;
                CleanupExpiredStateLocked(now);
                var leaseActive = _lease != null;
                var pending = _pending != null;
                var expiresIn = leaseActive ? Math.Max(0, (int)Math.Ceiling((_lease!.ExpiresUtc - now).TotalSeconds)) : 0;
                return "{\"mode\":\"single-writer\",\"multiSessionReads\":true,\"leaseActive\":"
                       + Bool(leaseActive) + ",\"leaseExpiresInSeconds\":" + expiresIn.ToString(CultureInfo.InvariantCulture)
                       + ",\"pendingNativeCommand\":" + Bool(pending)
                       + (pending ? ",\"pendingCommand\":\"" + Escape(_pending!.Command) + "\"" : string.Empty)
                       + "}";
            }
        }

        internal static void Reset()
        {
            lock (Sync)
            {
                if (_pending != null) DetachPendingLocked(_pending);
                _pending = null;
                _lease = null;
                CurrentOperationId.Value = null;
            }
        }

        private static void OnCommandWillStart(object sender, CommandEventArgs e)
        {
            lock (Sync)
            {
                if (_pending == null || !PendingMatchesLocked(sender, e)) return;
                _pending.Started = true;
                _pending.Audit?.Invoke("native command started; command=" + SafeTool(_pending.Command));
            }
        }

        private static void OnCommandEnded(object sender, CommandEventArgs e) { CompletePending(sender, e, "ended"); }
        private static void OnCommandCancelled(object sender, CommandEventArgs e) { CompletePending(sender, e, "cancelled"); }
        private static void OnCommandFailed(object sender, CommandEventArgs e) { CompletePending(sender, e, "failed"); }

        private static void CompletePending(object sender, CommandEventArgs e, string terminalState)
        {
            lock (Sync)
            {
                if (_pending == null || !PendingMatchesLocked(sender, e)) return;
                var completed = _pending;
                DetachPendingLocked(completed);
                _pending = null;
                completed.Audit?.Invoke("native command " + terminalState + "; command=" + SafeTool(completed.Command));
                if (_lease != null && _lease.ReleaseWhenIdle)
                    _lease = null;
                else
                    CleanupExpiredLeaseLocked(DateTime.UtcNow);
            }
        }

        private static bool PendingMatchesLocked(object sender, CommandEventArgs e)
        {
            if (_pending == null || !ReferenceEquals(sender, _pending.Document)) return false;
            var eventName = NormalizeCommand(e == null ? string.Empty : e.GlobalCommandName);
            return eventName.Length != 0 && string.Equals(eventName, _pending.Command, StringComparison.OrdinalIgnoreCase);
        }

        private static void CleanupExpiredStateLocked(DateTime now)
        {
            if (_pending != null && (now - _pending.QueuedUtc).TotalSeconds > PendingNativeCommandMaximumSeconds)
            {
                var stale = _pending;
                DetachPendingLocked(stale);
                _pending = null;
                stale.Audit?.Invoke("native command barrier expired without terminal event; command=" + SafeTool(stale.Command));
                if (_lease != null && _lease.ReleaseWhenIdle) _lease = null;
            }
            if (_pending == null) CleanupExpiredLeaseLocked(now);
        }

        private static void CleanupExpiredLeaseLocked(DateTime now)
        {
            if (_lease != null && now >= _lease.ExpiresUtc)
                _lease = null;
        }

        private static void RequireLeaseTokenLocked(string token)
        {
            if (_lease == null || token.Length == 0 || !string.Equals(token, _lease.Token, StringComparison.Ordinal))
                throw new InvalidOperationException("DWG writer lease is owned by another MCP workflow. Supply the matching writerToken or use read-only tools until the lease is released.");
        }

        private static void DetachPendingLocked(PendingNativeCommand pending)
        {
            try { pending.Document.CommandWillStart -= pending.WillStartHandler; } catch { }
            try { pending.Document.CommandEnded -= pending.EndedHandler; } catch { }
            try { pending.Document.CommandCancelled -= pending.CancelledHandler; } catch { }
            try { pending.Document.CommandFailed -= pending.FailedHandler; } catch { }
        }

        private static string NormalizeToken(string value)
        {
            var token = NormalizeOptionalToken(value);
            if (token.Length != 32)
                throw new InvalidOperationException("writerToken must be the 32-character token returned by cad_writer_acquire.");
            for (var i = 0; i < token.Length; i++)
            {
                var c = token[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    throw new InvalidOperationException("writerToken must be hexadecimal.");
            }
            return token;
        }

        private static string NormalizeOptionalToken(string value)
        {
            var token = (value ?? string.Empty).Trim();
            if (token.Length == 0) return string.Empty;
            if (token.Length > 64) throw new InvalidOperationException("writerToken exceeds the allowed bound.");
            return NormalizeToken(token);
        }

        private static string NormalizeCommand(string command)
        {
            var value = (command ?? string.Empty).Trim();
            var index = 0;
            while (index < value.Length && (value[index] == '_' || value[index] == '.')) index++;
            return value.Substring(index).ToUpperInvariant();
        }

        private static string SafeTool(string value)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length > 80) text = text.Substring(0, 80);
            return text.Replace("\r", " ").Replace("\n", " ");
        }

        private static string Bool(bool value) { return value ? "true" : "false"; }
        private static string Escape(string value) { return McpEmbeddedServer.JsonEscape(value ?? string.Empty); }

        private sealed class WriterLease
        {
            public WriterLease(string token, DateTime acquiredUtc, DateTime expiresUtc, int leaseSeconds)
            {
                Token = token;
                AcquiredUtc = acquiredUtc;
                ExpiresUtc = expiresUtc;
                LeaseSeconds = leaseSeconds;
            }
            public string Token { get; private set; }
            public DateTime AcquiredUtc { get; private set; }
            public DateTime ExpiresUtc { get; set; }
            public int LeaseSeconds { get; private set; }
            public bool ReleaseWhenIdle { get; set; }
        }

        private sealed class PendingNativeCommand
        {
            public PendingNativeCommand(Document document, string command, DateTime queuedUtc, Action<string>? audit)
            {
                Document = document;
                Command = command;
                QueuedUtc = queuedUtc;
                Audit = audit;
            }
            public Document Document { get; private set; }
            public string Command { get; private set; }
            public DateTime QueuedUtc { get; private set; }
            public bool Started { get; set; }
            public Action<string>? Audit { get; private set; }
            public CommandEventHandler WillStartHandler = null!;
            public CommandEventHandler EndedHandler = null!;
            public CommandEventHandler CancelledHandler = null!;
            public CommandEventHandler FailedHandler = null!;
        }

        private sealed class MutationScope : IDisposable
        {
            private readonly long _operationId;
            private readonly long? _previousOperationId;
            private readonly Action<string>? _audit;
            private int _disposed;

            public MutationScope(long operationId, long? previousOperationId, Action<string>? audit)
            {
                _operationId = operationId;
                _previousOperationId = previousOperationId;
                _audit = audit;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                if (CurrentOperationId.Value == _operationId) CurrentOperationId.Value = _previousOperationId;
                _audit?.Invoke("writer mutation exited");
                MutationGate.Release();
            }
        }
    }
}
