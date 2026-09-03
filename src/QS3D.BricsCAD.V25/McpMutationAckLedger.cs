using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Process-global retry identity and durable acknowledgement ledger for bounded MCP CAD
    /// mutations. This component never replaces the CAD writer coordinator: it only decides
    /// whether a logical action is new/replayed and records monotonic acknowledgement state.
    /// </summary>
    internal static class McpMutationAckLedger
    {
        internal const int MaxActionIdLength = 128;
        internal const int MaxDurableRecords = 1024;
        internal const int MaxLedgerBytes = 1024 * 1024;
        internal const int MaxStoredResultBytes = 16 * 1024;
        private const int MaxLiveRecords = 2048;
        private const string LedgerFileName = "mcp-mutation-ack-ledger-v1.txt";
        private const string LedgerHeader = "QS3D-MCP-MUTATION-ACK|1";

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, AckRecord> Records =
            new Dictionary<string, AckRecord>(StringComparer.Ordinal);
        private static readonly AsyncLocal<string?> CurrentAction = new AsyncLocal<string?>();

        internal static string LedgerFilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QS3D",
                    LedgerFileName);
            }
        }

        internal static string CurrentActionId
        {
            get { return CurrentAction.Value ?? string.Empty; }
        }

        internal static Reservation ReserveOrReplay(string tool, string arguments)
        {
            var body = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            var supplied = McpTopLevelJson.HasProperty(body, "actionId");
            var requested = supplied ? McpTopLevelJson.ExtractString(body, "actionId") : string.Empty;
            var actionId = supplied ? ValidateActionId(requested) : "auto-" + Guid.NewGuid().ToString("N");
            var fingerprint = ComputeFingerprint(tool, body);

            lock (Sync)
            {
                AckRecord existing;
                if (Records.TryGetValue(actionId, out existing))
                {
                    if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
                        throw new InvalidOperationException("actionId was already used for a different mutation request; retry identity reuse is rejected.");
                    return new Reservation(existing, true);
                }

                if (Records.Count >= MaxLiveRecords)
                    throw new InvalidOperationException("Mutation acknowledgement ledger is full; save or restart after durable acknowledgement before submitting more mutations.");

                var record = new AckRecord
                {
                    ActionId = actionId,
                    Fingerprint = fingerprint,
                    Tool = NormalizeTool(tool),
                    State = AckState.Accepted,
                    AcceptedUtc = DateTime.UtcNow
                };
                Records.Add(actionId, record);
                return new Reservation(record, false);
            }
        }

        internal static IDisposable EnterActionContext(string actionId)
        {
            var normalized = ValidateActionId(actionId);
            var previous = CurrentAction.Value;
            CurrentAction.Value = normalized;
            return new ActionContext(previous);
        }

        internal static void MarkAcceptedResult(string actionId, string result)
        {
            lock (Sync)
            {
                AckRecord record;
                if (!Records.TryGetValue(ValidateActionId(actionId), out record))
                    throw new InvalidOperationException("Mutation acknowledgement record was not found.");
                if (record.State != AckState.Accepted) return;
                record.Result = BoundResult(result);
            }
        }

        internal static void MarkApplied(string actionId, string result, Document? document)
        {
            lock (Sync)
            {
                AckRecord record;
                if (!Records.TryGetValue(ValidateActionId(actionId), out record))
                    throw new InvalidOperationException("Mutation acknowledgement record was not found.");
                if (record.State == AckState.Durable) return;
                if (record.State != AckState.Accepted && record.State != AckState.Applied)
                    throw new InvalidOperationException("Mutation acknowledgement state cannot transition to applied.");
                record.Result = BoundResult(result);
                record.LiveDocument = document;
                record.DocumentIdentity = BuildStableDocumentIdentity(document);
                record.State = AckState.Applied;
                record.AppliedUtc = DateTime.UtcNow;
            }
        }

        internal static void MarkNativeCommandTerminal(
            McpCadMutationCoordinator.PendingNativeCommand completed,
            string terminalState)
        {
            if (completed == null) return;
            var actionId = completed.ActionId ?? string.Empty;
            if (actionId.Length == 0) return;
            if (string.Equals(terminalState, "ended", StringComparison.OrdinalIgnoreCase))
            {
                lock (Sync)
                {
                    AckRecord record;
                    if (!Records.TryGetValue(actionId, out record) || record.State != AckState.Accepted) return;
                    record.LiveDocument = completed.Document;
                    record.DocumentIdentity = BuildStableDocumentIdentity(completed.Document);
                    record.State = AckState.Applied;
                    record.AppliedUtc = DateTime.UtcNow;
                    if (string.IsNullOrWhiteSpace(record.Result))
                        record.Result = "{\"commandCompleted\":true}";
                }
                return;
            }

            // A cancelled/failed native command did not reach the mutation success boundary.
            Abandon(actionId);
        }

        internal static PromotionResult PromoteDurableForDocument(Document? document)
        {
            if (document == null) return new PromotionResult(0, true);
            var stableIdentity = BuildStableDocumentIdentity(document);
            if (stableIdentity.Length == 0)
            {
                McpDiagnosticHub.Record("mcp", "warning", "mutation-ack-not-durable",
                    "Verified save completed but a stable drawing identity could not be established; ACK remains applied.", document);
                return new PromotionResult(0, false);
            }

            lock (Sync)
            {
                var staged = new List<AckRecord>();
                var now = DateTime.UtcNow;
                foreach (var record in Records.Values)
                {
                    if (record.State != AckState.Applied || !ReferenceEquals(record.LiveDocument, document)) continue;
                    record.State = AckState.Durable;
                    record.DurableUtc = now;
                    record.DocumentIdentity = stableIdentity;
                    staged.Add(record);
                }

                if (staged.Count == 0) return new PromotionResult(0, true);
                try
                {
                    PersistDurableLocked();
                    foreach (var record in staged) record.LiveDocument = null;
                    return new PromotionResult(staged.Count, true);
                }
                catch (Exception ex)
                {
                    foreach (var record in staged)
                    {
                        record.State = AckState.Applied;
                        record.DurableUtc = null;
                    }
                    McpDiagnosticHub.Record("mcp", "warning", "mutation-ack-persist-failed",
                        "CAD save succeeded but durable ACK persistence failed: " + SafeDiagnostic(ex.Message), document);
                    return new PromotionResult(0, false);
                }
            }
        }

        internal static bool HasAccepted(string actionId)
        {
            lock (Sync)
            {
                AckRecord record;
                return Records.TryGetValue(actionId ?? string.Empty, out record) && record.State == AckState.Accepted;
            }
        }

        internal static string StatusJson(string actionId)
        {
            var normalized = ValidateActionId(actionId);
            lock (Sync)
            {
                AckRecord record;
                if (!Records.TryGetValue(normalized, out record))
                    return "{\"actionId\":\"" + Escape(normalized)
                           + "\",\"ackState\":\"unknown\",\"accepted\":false,\"applied\":false,\"durable\":false}";
                return BuildEnvelope(record, false, false);
            }
        }

        internal static string BuildResponse(Reservation reservation, bool replayed, int durablePromotedCount)
        {
            if (reservation == null) throw new ArgumentNullException(nameof(reservation));
            lock (Sync)
            {
                AckRecord record;
                if (!Records.TryGetValue(reservation.ActionId, out record)) record = reservation.Record;
                return BuildEnvelope(record, replayed, true, durablePromotedCount);
            }
        }

        internal static void Abandon(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId)) return;
            lock (Sync)
            {
                AckRecord record;
                if (!Records.TryGetValue(actionId, out record)) return;
                if (record.State == AckState.Accepted) Records.Remove(actionId);
            }
        }

        internal static void ResetForServerStart()
        {
            lock (Sync)
            {
                Records.Clear();
                CurrentAction.Value = null;
                LoadDurableLocked();
            }
        }

        internal static void ResetVolatile()
        {
            lock (Sync)
            {
                var remove = new List<string>();
                foreach (var pair in Records)
                    if (pair.Value.State != AckState.Durable) remove.Add(pair.Key);
                foreach (var actionId in remove) Records.Remove(actionId);
                CurrentAction.Value = null;
            }
        }

        private static string BuildEnvelope(AckRecord record, bool replayed, bool includeResult, int durablePromotedCount = 0)
        {
            var accepted = record.State == AckState.Accepted || record.State == AckState.Applied || record.State == AckState.Durable;
            var applied = record.State == AckState.Applied || record.State == AckState.Durable;
            var durable = record.State == AckState.Durable;
            var state = durable ? "durable" : applied ? "applied" : "accepted";
            var output = new StringBuilder(256)
                .Append("{\"actionId\":\"").Append(Escape(record.ActionId))
                .Append("\",\"ackState\":\"").Append(state)
                .Append("\",\"accepted\":").Append(Bool(accepted))
                .Append(",\"applied\":").Append(Bool(applied))
                .Append(",\"durable\":").Append(Bool(durable))
                .Append(",\"replayed\":").Append(Bool(replayed));
            if (durablePromotedCount > 0)
                output.Append(",\"durablePromotedCount\":").Append(durablePromotedCount.ToString(CultureInfo.InvariantCulture));
            if (includeResult && !string.IsNullOrWhiteSpace(record.Result))
            {
                output.Append(",\"result\":");
                var trimmed = record.Result.Trim();
                if ((trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                    || (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal)))
                    output.Append(trimmed);
                else
                    output.Append('"').Append(Escape(trimmed)).Append('"');
            }
            return output.Append('}').ToString();
        }

        private static string ValidateActionId(string value)
        {
            var actionId = (value ?? string.Empty).Trim();
            if (actionId.Length == 0 || actionId.Length > MaxActionIdLength)
                throw new InvalidOperationException("actionId must contain 1 to 128 ASCII-safe characters.");
            foreach (var ch in actionId)
            {
                if (ch < 0x21 || ch > 0x7e || ch == '/' || ch == '\\')
                    throw new InvalidOperationException("actionId contains a forbidden character.");
            }
            return actionId;
        }

        private static string ComputeFingerprint(string tool, string arguments)
        {
            var canonical = CanonicalJson.CanonicalizeArguments(arguments ?? "{}");
            var bytes = StrictUtf8.GetBytes(NormalizeTool(tool) + "\n" + canonical);
            using (var sha = SHA256.Create())
            {
                var digest = sha.ComputeHash(bytes);
                var output = new StringBuilder(digest.Length * 2);
                foreach (var b in digest) output.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return output.ToString();
            }
        }

        private static string BoundResult(string result)
        {
            var value = result ?? string.Empty;
            var bytes = StrictUtf8.GetBytes(value);
            if (bytes.Length <= MaxStoredResultBytes) return value;
            string digest;
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var text = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) text.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                digest = text.ToString();
            }
            return "{\"resultTruncated\":true,\"resultDigest\":\"sha256:" + digest + "\",\"originalBytes\":"
                   + bytes.Length.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string BuildStableDocumentIdentity(Document? document)
        {
            if (document == null) return string.Empty;
            var database = document.Database;
            if (database == null) return string.Empty;
            var fingerprint = string.Empty;
            try
            {
                var property = database.GetType().GetProperty("FingerprintGuid");
                var value = property == null ? null : property.GetValue(database, null);
                if (value != null) fingerprint = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
            catch { fingerprint = string.Empty; }

            var path = string.Empty;
            try
            {
                var candidate = database.Filename ?? string.Empty;
                if (Path.IsPathRooted(candidate)) path = Path.GetFullPath(candidate).Trim();
            }
            catch { path = string.Empty; }

            if (fingerprint.Length == 0 && path.Length == 0) return string.Empty;
            return "fingerprint=" + fingerprint.Trim().ToLowerInvariant() + ";path=" + path.ToLowerInvariant();
        }

        private static void PersistDurableLocked()
        {
            TrimDurableToBoundsLocked();
            var directory = Path.GetDirectoryName(LedgerFilePath) ?? string.Empty;
            if (directory.Length == 0) throw new InvalidOperationException("Mutation ACK ledger directory is unavailable.");
            Directory.CreateDirectory(directory);

            while (true)
            {
                var content = SerializeDurableLocked();
                if (StrictUtf8.GetByteCount(content) <= MaxLedgerBytes)
                {
                    AtomicWrite(LedgerFilePath, content);
                    return;
                }
                if (!RemoveOldestDurableLocked())
                    throw new InvalidOperationException("Mutation ACK ledger cannot be serialized within the configured 1 MiB bound.");
            }
        }

        private static string SerializeDurableLocked()
        {
            var durable = DurableNewestFirstLocked();
            var output = new StringBuilder(LedgerHeader).Append('\n');
            foreach (var record in durable)
            {
                output.Append(Base64(record.ActionId)).Append('|')
                    .Append(Base64(record.Fingerprint)).Append('|')
                    .Append(Base64(record.Tool)).Append('|')
                    .Append(Base64(record.DocumentIdentity)).Append('|')
                    .Append((record.DurableUtc ?? DateTime.UtcNow).Ticks.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(Base64(BoundResult(record.Result))).Append('\n');
            }
            return output.ToString();
        }

        private static void LoadDurableLocked()
        {
            var path = LedgerFilePath;
            try
            {
                if (!File.Exists(path)) return;
                var info = new FileInfo(path);
                if (info.Length < 0 || info.Length > MaxLedgerBytes)
                    throw new InvalidDataException("Mutation ACK ledger exceeds the 1 MiB bound.");
                var content = File.ReadAllText(path, StrictUtf8);
                var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                if (lines.Length == 0 || !string.Equals(lines[0], LedgerHeader, StringComparison.Ordinal))
                    throw new InvalidDataException("Mutation ACK ledger header is invalid.");
                var loaded = 0;
                for (var i = 1; i < lines.Length && loaded < MaxDurableRecords; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var fields = lines[i].Split('|');
                    if (fields.Length != 6) throw new InvalidDataException("Mutation ACK ledger record is malformed.");
                    long ticks;
                    if (!long.TryParse(fields[4], NumberStyles.None, CultureInfo.InvariantCulture, out ticks))
                        throw new InvalidDataException("Mutation ACK durable timestamp is invalid.");
                    var actionId = ValidateActionId(FromBase64(fields[0]));
                    var fingerprint = FromBase64(fields[1]);
                    var tool = FromBase64(fields[2]);
                    var documentIdentity = FromBase64(fields[3]);
                    var result = FromBase64(fields[5]);
                    if (fingerprint.Length != 64 || documentIdentity.Length == 0)
                        throw new InvalidDataException("Mutation ACK durable identity is invalid.");
                    Records[actionId] = new AckRecord
                    {
                        ActionId = actionId,
                        Fingerprint = fingerprint,
                        Tool = tool,
                        DocumentIdentity = documentIdentity,
                        Result = BoundResult(result),
                        State = AckState.Durable,
                        DurableUtc = new DateTime(ticks, DateTimeKind.Utc),
                        AcceptedUtc = new DateTime(ticks, DateTimeKind.Utc),
                        AppliedUtc = new DateTime(ticks, DateTimeKind.Utc)
                    };
                    loaded++;
                }
            }
            catch (Exception ex)
            {
                Records.Clear();
                McpDiagnosticHub.Record("mcp", "warning", "mutation-ack-ledger-ignored",
                    "Durable ACK ledger was ignored because it was corrupt or out of bounds: " + SafeDiagnostic(ex.Message));
            }
        }

        private static void TrimDurableToBoundsLocked()
        {
            while (DurableCountLocked() > MaxDurableRecords)
                if (!RemoveOldestDurableLocked()) break;
        }

        private static int DurableCountLocked()
        {
            var count = 0;
            foreach (var record in Records.Values) if (record.State == AckState.Durable) count++;
            return count;
        }

        private static bool RemoveOldestDurableLocked()
        {
            AckRecord? oldest = null;
            foreach (var record in Records.Values)
            {
                if (record.State != AckState.Durable) continue;
                if (oldest == null || Nullable.Compare(record.DurableUtc, oldest.DurableUtc) < 0
                    || (Nullable.Compare(record.DurableUtc, oldest.DurableUtc) == 0
                        && string.CompareOrdinal(record.ActionId, oldest.ActionId) < 0))
                    oldest = record;
            }
            if (oldest == null) return false;
            Records.Remove(oldest.ActionId);
            return true;
        }

        private static List<AckRecord> DurableNewestFirstLocked()
        {
            var records = new List<AckRecord>();
            foreach (var record in Records.Values) if (record.State == AckState.Durable) records.Add(record);
            records.Sort((left, right) =>
            {
                var byTime = Nullable.Compare(right.DurableUtc, left.DurableUtc);
                return byTime != 0 ? byTime : string.CompareOrdinal(left.ActionId, right.ActionId);
            });
            return records;
        }

        private static void AtomicWrite(string path, string content)
        {
            var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temp, content, StrictUtf8);
            try
            {
                if (File.Exists(path))
                {
                    var backup = path + ".bak";
                    try { File.Replace(temp, path, backup, true); }
                    finally { try { if (File.Exists(backup)) File.Delete(backup); } catch { } }
                }
                else File.Move(temp, path);
            }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
        }

        private static string Base64(string value)
        {
            return Convert.ToBase64String(StrictUtf8.GetBytes(value ?? string.Empty));
        }

        private static string FromBase64(string value)
        {
            return StrictUtf8.GetString(Convert.FromBase64String(value ?? string.Empty));
        }

        private static string NormalizeTool(string tool)
        {
            var value = (tool ?? string.Empty).Trim();
            if (value.Length == 0 || value.Length > 160) throw new InvalidOperationException("Mutation tool name is invalid.");
            return value;
        }

        private static string SafeDiagnostic(string value)
        {
            var text = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            return text.Length <= 300 ? text : text.Substring(0, 300);
        }

        private static string Escape(string value) { return McpEmbeddedServer.JsonEscape(value ?? string.Empty); }
        private static string Bool(bool value) { return value ? "true" : "false"; }

        internal sealed class Reservation
        {
            internal Reservation(AckRecord record, bool replayed)
            {
                Record = record;
                Replayed = replayed;
            }
            internal AckRecord Record { get; private set; }
            internal string ActionId { get { return Record.ActionId; } }
            internal bool Replayed { get; private set; }
        }

        internal sealed class PromotionResult
        {
            internal PromotionResult(int promotedCount, bool persisted)
            {
                PromotedCount = promotedCount;
                Persisted = persisted;
            }
            internal int PromotedCount { get; private set; }
            internal bool Persisted { get; private set; }
        }

        internal enum AckState
        {
            Accepted = 0,
            Applied = 1,
            Durable = 2
        }

        internal sealed class AckRecord
        {
            internal string ActionId = string.Empty;
            internal string Fingerprint = string.Empty;
            internal string Tool = string.Empty;
            internal AckState State;
            internal string Result = string.Empty;
            internal Document? LiveDocument;
            internal string DocumentIdentity = string.Empty;
            internal DateTime AcceptedUtc;
            internal DateTime? AppliedUtc;
            internal DateTime? DurableUtc;
        }

        private sealed class ActionContext : IDisposable
        {
            private readonly string? _previous;
            private int _disposed;
            internal ActionContext(string? previous) { _previous = previous; }
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                CurrentAction.Value = _previous;
            }
        }

        private sealed class CanonicalJson
        {
            private static readonly HashSet<string> ExcludedTopLevel = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "actionId",
                "writerToken",
                "confirmMutation",
                "executionMode",
                "execution_mode"
            };

            private readonly string _text;
            private int _index;

            private CanonicalJson(string text) { _text = text ?? string.Empty; }

            internal static string CanonicalizeArguments(string text)
            {
                var parser = new CanonicalJson(text);
                parser.SkipWhitespace();
                var value = parser.ParseValue(0, true);
                parser.SkipWhitespace();
                if (parser._index != parser._text.Length) throw new InvalidOperationException("Unexpected content after mutation JSON arguments.");
                if (!value.StartsWith("{", StringComparison.Ordinal))
                    throw new InvalidOperationException("Mutation arguments must be a JSON object.");
                return value;
            }

            private string ParseValue(int depth, bool topLevel)
            {
                if (depth > 64) throw new InvalidOperationException("Mutation JSON exceeds maximum nesting depth.");
                SkipWhitespace();
                if (_index >= _text.Length) throw new InvalidOperationException("Mutation JSON ended unexpectedly.");
                var ch = _text[_index];
                if (ch == '{') return ParseObject(depth + 1, topLevel);
                if (ch == '[') return ParseArray(depth + 1);
                if (ch == '"') return Quote(ReadString());
                if (StartsWith("true")) { _index += 4; return "true"; }
                if (StartsWith("false")) { _index += 5; return "false"; }
                if (StartsWith("null")) { _index += 4; return "null"; }
                return ReadNumber();
            }

            private string ParseObject(int depth, bool topLevel)
            {
                _index++;
                SkipWhitespace();
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var entries = new List<KeyValuePair<string, string>>();
                if (Consume('}')) return "{}";
                while (true)
                {
                    SkipWhitespace();
                    if (_index >= _text.Length || _text[_index] != '"')
                        throw new InvalidOperationException("Mutation JSON object property name must be a string.");
                    var name = ReadString();
                    if (!names.Add(name)) throw new InvalidOperationException("duplicate top-level JSON property: " + name);
                    SkipWhitespace();
                    Require(':');
                    var value = ParseValue(depth, false);
                    if (!topLevel || !ExcludedTopLevel.Contains(name))
                        entries.Add(new KeyValuePair<string, string>(name, value));
                    SkipWhitespace();
                    if (Consume('}')) break;
                    Require(',');
                }
                entries.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
                var output = new StringBuilder("{");
                for (var i = 0; i < entries.Count; i++)
                {
                    if (i > 0) output.Append(',');
                    output.Append(Quote(entries[i].Key)).Append(':').Append(entries[i].Value);
                }
                return output.Append('}').ToString();
            }

            private string ParseArray(int depth)
            {
                _index++;
                SkipWhitespace();
                var output = new StringBuilder("[");
                if (Consume(']')) return "]";
                var first = true;
                while (true)
                {
                    if (!first) output.Append(',');
                    first = false;
                    output.Append(ParseValue(depth, false));
                    SkipWhitespace();
                    if (Consume(']')) break;
                    Require(',');
                }
                return output.Append(']').ToString();
            }

            private string ReadString()
            {
                Require('"');
                var output = new StringBuilder();
                while (_index < _text.Length)
                {
                    var ch = _text[_index++];
                    if (ch == '"') return output.ToString();
                    if (ch != '\\')
                    {
                        if (ch < 0x20) throw new InvalidOperationException("Mutation JSON string contains a control character.");
                        output.Append(ch);
                        continue;
                    }
                    if (_index >= _text.Length) throw new InvalidOperationException("Mutation JSON string escape ended unexpectedly.");
                    var escaped = _text[_index++];
                    switch (escaped)
                    {
                        case '"': output.Append('"'); break;
                        case '\\': output.Append('\\'); break;
                        case '/': output.Append('/'); break;
                        case 'b': output.Append('\b'); break;
                        case 'f': output.Append('\f'); break;
                        case 'n': output.Append('\n'); break;
                        case 'r': output.Append('\r'); break;
                        case 't': output.Append('\t'); break;
                        case 'u': output.Append(ReadUnicodeEscape()); break;
                        default: throw new InvalidOperationException("Mutation JSON contains an invalid string escape.");
                    }
                }
                throw new InvalidOperationException("Mutation JSON string ended unexpectedly.");
            }

            private char ReadUnicodeEscape()
            {
                if (_index + 4 > _text.Length) throw new InvalidOperationException("Mutation JSON unicode escape ended unexpectedly.");
                var value = 0;
                for (var i = 0; i < 4; i++)
                {
                    var ch = _text[_index++];
                    var digit = ch >= '0' && ch <= '9' ? ch - '0'
                        : ch >= 'a' && ch <= 'f' ? ch - 'a' + 10
                        : ch >= 'A' && ch <= 'F' ? ch - 'A' + 10
                        : -1;
                    if (digit < 0) throw new InvalidOperationException("Mutation JSON unicode escape is invalid.");
                    value = (value << 4) | digit;
                }
                return (char)value;
            }

            private string ReadNumber()
            {
                var start = _index;
                if (Consume('-')) { }
                if (_index >= _text.Length) throw new InvalidOperationException("Mutation JSON number is invalid.");
                if (_text[_index] == '0') _index++;
                else
                {
                    if (_text[_index] < '1' || _text[_index] > '9') throw new InvalidOperationException("Mutation JSON value is invalid.");
                    while (_index < _text.Length && _text[_index] >= '0' && _text[_index] <= '9') _index++;
                }
                if (_index < _text.Length && _text[_index] == '.')
                {
                    _index++;
                    var fraction = _index;
                    while (_index < _text.Length && _text[_index] >= '0' && _text[_index] <= '9') _index++;
                    if (_index == fraction) throw new InvalidOperationException("Mutation JSON number fraction is invalid.");
                }
                if (_index < _text.Length && (_text[_index] == 'e' || _text[_index] == 'E'))
                {
                    _index++;
                    if (_index < _text.Length && (_text[_index] == '+' || _text[_index] == '-')) _index++;
                    var exponent = _index;
                    while (_index < _text.Length && _text[_index] >= '0' && _text[_index] <= '9') _index++;
                    if (_index == exponent) throw new InvalidOperationException("Mutation JSON number exponent is invalid.");
                }
                return _text.Substring(start, _index - start);
            }

            private bool StartsWith(string value)
            {
                return _index + value.Length <= _text.Length
                       && string.CompareOrdinal(_text, _index, value, 0, value.Length) == 0;
            }

            private void SkipWhitespace()
            {
                while (_index < _text.Length)
                {
                    var ch = _text[_index];
                    if (ch != ' ' && ch != '\t' && ch != '\r' && ch != '\n') break;
                    _index++;
                }
            }

            private void Require(char expected)
            {
                SkipWhitespace();
                if (_index >= _text.Length || _text[_index] != expected)
                    throw new InvalidOperationException("Mutation JSON expected '" + expected + "'.");
                _index++;
            }

            private bool Consume(char expected)
            {
                SkipWhitespace();
                if (_index >= _text.Length || _text[_index] != expected) return false;
                _index++;
                return true;
            }

            private static string Quote(string value)
            {
                var output = new StringBuilder((value ?? string.Empty).Length + 8).Append('"');
                foreach (var ch in value ?? string.Empty)
                {
                    switch (ch)
                    {
                        case '"': output.Append("\\\""); break;
                        case '\\': output.Append("\\\\"); break;
                        case '\b': output.Append("\\b"); break;
                        case '\f': output.Append("\\f"); break;
                        case '\n': output.Append("\\n"); break;
                        case '\r': output.Append("\\r"); break;
                        case '\t': output.Append("\\t"); break;
                        default:
                            if (ch < 0x20) output.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                            else output.Append(ch);
                            break;
                    }
                }
                return output.Append('"').ToString();
            }
        }
    }
}
