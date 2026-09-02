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
        private const int CadDispatchTimeoutMilliseconds = 5000;
        private const int DefaultLeaseSeconds = 120;
        private const int MinLeaseSeconds = 15;
        private const int MaxLeaseSeconds = 300;
        private const int PendingNativeCommandMaximumSeconds = 45;

        private static readonly SemaphoreSlim MutationGate = new SemaphoreSlim(1, 1);
        private static readonly object Sync = new object();
        private static readonly AsyncLocal<long?> CurrentOperationId = new AsyncLocal<long?>();
        private static readonly AsyncLocal<NativeCommandReservation?> PreparedNativeCommand =
            new AsyncLocal<NativeCommandReservation?>();

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
            var token = NormalizeRequiredToken(writerToken);
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
            var prepared = PreparedNativeCommand.Value;
            var acquiredHere = prepared == null;
            if (acquiredHere && !MutationGate.Wait(MutationAcquireTimeoutMilliseconds))
                throw new InvalidOperationException("DWG writer is busy with another mutation. Read-only tools remain available; retry this mutation later.");
            if (prepared != null && !prepared.OwnsMutationGate)
                throw new InvalidOperationException("Prepared native command no longer owns the MCP DWG mutation gate.");

            try
            {
                lock (Sync)
                {
                    var now = DateTime.UtcNow;
                    CleanupExpiredStateLocked(now);
                    if (_pending != null && (prepared == null || !prepared.Owns(_pending)))
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
                    if (prepared != null) prepared.TransferMutationGate();
                    audit?.Invoke("writer mutation entered; tool=" + SafeTool(tool)
                        + "; lease=" + (_lease == null ? "ephemeral" : "explicit"));
                    return new MutationScope(operationId, previous, audit);
                }
            }
            catch
            {
                if (acquiredHere) MutationGate.Release();
                throw;
            }
        }

        /// <summary>
        /// Arms the post-return barrier before a runtime call that is known to queue a native
        /// command. Preparation also owns MutationGate until EnterMutation transfers that gate
        /// to the request, closing the race between transport pre-arm and runtime dispatch.
        /// </summary>
        internal static NativeCommandReservation? PrepareNativeCommand(string tool, string arguments, Action<string>? audit)
        {
            string command;
            if (string.Equals(tool, "cad_command_sequence", StringComparison.Ordinal))
            {
                command = NormalizeCommand(McpTopLevelJson.ExtractString(arguments ?? "{}", "command"));
                if (string.Equals(command, "QSAVE", StringComparison.Ordinal)) return null;
            }
            else if (string.Equals(tool, "qs3d_run_command", StringComparison.Ordinal))
            {
                command = NormalizeCommand(McpTopLevelJson.ExtractString(arguments ?? "{}", "command"));
            }
            else return null;

            if (command.Length == 0) return null;
            if (PreparedNativeCommand.Value != null)
                throw new InvalidOperationException("Nested native-command preparation is not supported.");
            if (!MutationGate.Wait(MutationAcquireTimeoutMilliseconds))
                throw new InvalidOperationException("DWG writer is busy with another mutation. Read-only tools remain available; retry this native command later.");

            try
            {
                var reservation = InvokeInCadContext(() => ArmNativeCommandInCadContext(command, audit, true));
                PreparedNativeCommand.Value = reservation;
                return reservation;
            }
            catch
            {
                MutationGate.Release();
                throw;
            }
        }

        /// <summary>
        /// Runtime-level helper for the classic cad_command_sequence path. If transport already
        /// prepared the same command, reuse that reservation rather than creating a second barrier.
        /// </summary>
        internal static void QueueNativeCommand(Document document, string command, Action enqueue, Action<string>? audit)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (enqueue == null) throw new ArgumentNullException(nameof(enqueue));
            if (!CurrentOperationId.Value.HasValue)
                throw new InvalidOperationException("Native command queueing requires the active MCP DWG writer mutation scope.");

            var reservation = PreparedNativeCommand.Value;
            if (reservation != null)
            {
                if (!reservation.Matches(document, command))
                    throw new InvalidOperationException("Prepared native-command barrier does not match the command being queued.");
            }
            else
            {
                reservation = ArmNativeCommandInCadContext(command, audit, false);
            }

            try
            {
                enqueue();
                reservation.Commit();
                audit?.Invoke("native command queued; command=" + SafeTool(command));
            }
            catch
            {
                reservation.Dispose();
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
            var prepared = PreparedNativeCommand.Value;
            PreparedNativeCommand.Value = null;
            if (prepared != null) prepared.Dispose();
        }

        private static NativeCommandReservation ArmNativeCommandInCadContext(string command, Action<string>? audit, bool ownsMutationGate)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) throw new InvalidOperationException("No active BricsCAD document is available for native command coordination.");
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
            audit?.Invoke("native command barrier armed; command=" + SafeTool(command));
            return new NativeCommandReservation(pending, ownsMutationGate);
        }

        private static T InvokeInCadContext<T>(Func<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var work = new CadContextWork<T>(action);
            Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadContextWork<T>, work);
            if (!work.Done.Wait(CadDispatchTimeoutMilliseconds))
                throw new TimeoutException("Timed out while arming the MCP native-command writer barrier in BricsCAD application context.");
            try
            {
                if (work.Error != null) throw new InvalidOperationException(work.Error.Message, work.Error);
                return work.Result!;
            }
            finally { work.Done.Dispose(); }
        }

        private static void ExecuteCadContextWork<T>(object state)
        {
            var work = (CadContextWork<T>)state;
            try { work.Result = work.Action(); }
            catch (Exception ex) { work.Error = ex; }
            finally { work.Done.Set(); }
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

        private static string NormalizeRequiredToken(string value)
        {
            var token = (value ?? string.Empty).Trim();
            if (token.Length != 32)
                throw new InvalidOperationException("writerToken must be the 32-character token returned by cad_writer_acquire.");
            ValidateHexToken(token);
            return token;
        }

        private static string NormalizeOptionalToken(string value)
        {
            var token = (value ?? string.Empty).Trim();
            if (token.Length == 0) return string.Empty;
            if (token.Length != 32)
                throw new InvalidOperationException("writerToken must be the 32-character token returned by cad_writer_acquire.");
            ValidateHexToken(token);
            return token;
        }

        private static void ValidateHexToken(string token)
        {
            for (var i = 0; i < token.Length; i++)
            {
                var c = token[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    throw new InvalidOperationException("writerToken must be hexadecimal.");
            }
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

        internal sealed class NativeCommandReservation : IDisposable
        {
            private readonly PendingNativeCommand _pending;
            private readonly bool _ownsMutationGate;
            private int _gateTransferredOrReleased;
            private int _committed;
            private int _disposed;

            internal NativeCommandReservation(PendingNativeCommand pending, bool ownsMutationGate)
            {
                _pending = pending;
                _ownsMutationGate = ownsMutationGate;
            }

            internal bool OwnsMutationGate
            {
                get { return _ownsMutationGate && Volatile.Read(ref _gateTransferredOrReleased) == 0; }
            }

            internal bool Owns(PendingNativeCommand pending)
            {
                return ReferenceEquals(_pending, pending);
            }

            internal bool Matches(Document document, string command)
            {
                return ReferenceEquals(_pending.Document, document)
                       && string.Equals(_pending.Command, NormalizeCommand(command), StringComparison.OrdinalIgnoreCase);
            }

            internal void TransferMutationGate()
            {
                if (!_ownsMutationGate)
                    throw new InvalidOperationException("Prepared native command does not own the mutation gate.");
                if (Interlocked.CompareExchange(ref _gateTransferredOrReleased, 1, 0) != 0)
                    throw new InvalidOperationException("Prepared native command mutation gate was already transferred or released.");
            }

            internal void Commit()
            {
                Volatile.Write(ref _committed, 1);
                if (ReferenceEquals(PreparedNativeCommand.Value, this)) PreparedNativeCommand.Value = null;
            }

            public void Dispose()
            {
                if (ReferenceEquals(PreparedNativeCommand.Value, this)) PreparedNativeCommand.Value = null;
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

                if (Volatile.Read(ref _committed) == 0)
                {
                    lock (Sync)
                    {
                        if (ReferenceEquals(_pending, McpCadMutationCoordinator._pending))
                        {
                            DetachPendingLocked(_pending);
                            McpCadMutationCoordinator._pending = null;
                        }
                    }
                }

                if (_ownsMutationGate && Interlocked.CompareExchange(ref _gateTransferredOrReleased, 1, 0) == 0)
                    MutationGate.Release();
            }
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

        private sealed class CadContextWork<T>
        {
            internal CadContextWork(Func<T> action) { Action = action; }
            internal Func<T> Action { get; private set; }
            internal T? Result;
            internal Exception? Error;
            internal readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
        }
    }
}
