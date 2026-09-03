using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Process-local, bounded repair identity ledger for MCP tools/call failures.
    /// This class never performs a repair itself; it only emits fail-closed metadata
    /// that a supervising client can use to correct a call, retry, open a source
    /// repair carrier, or stop for human review.
    /// </summary>
    internal static class McpSelfHealingRepairRuntime
    {
        private const int CircuitOpenOccurrence = 4;
        private const int MaxTickets = 256;

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, TicketState> Tickets =
            new Dictionary<string, TicketState>(StringComparer.Ordinal);

        public static string RecordFailure(
            string tool,
            string code,
            string lane,
            string message,
            Exception? exception,
            bool contractFailure)
        {
            var callerOrPolicyFailure = contractFailure || IsCallerOrPolicyFailure(code, message);
            var transientFailure = !callerOrPolicyFailure && IsTransientFailure(code, message);
            var sourceRepairEligible = !callerOrPolicyFailure
                                       && !transientFailure
                                       && IsSourceRepairFailure(code, message);

            var exceptionType = exception == null
                ? string.Empty
                : exception.GetType().FullName ?? string.Empty;
            var fingerprintMessage = sourceRepairEligible
                ? BuildSourceRepairIdentity(exception, message)
                : message;
            var fingerprint = BuildFingerprint(tool, code, lane, exceptionType, fingerprintMessage);
            var now = DateTime.UtcNow;

            int occurrenceCount;
            DateTime firstSeenUtc;
            DateTime lastSeenUtc;

            lock (Sync)
            {
                TicketState? ticket;
                var ephemeralTicket = false;
                if (!Tickets.TryGetValue(fingerprint, out ticket) || ticket == null)
                {
                    if (Tickets.Count >= MaxTickets)
                    {
                        var evictionCandidate = SelectEvictionCandidateLocked(sourceRepairEligible);
                        if (evictionCandidate == null)
                        {
                            ephemeralTicket = true;
                        }
                        else
                        {
                            Tickets.Remove(evictionCandidate);
                        }
                    }

                    if (ephemeralTicket)
                    {
                        occurrenceCount = 1;
                        firstSeenUtc = now;
                        lastSeenUtc = now;
                    }
                    else
                    {
                        ticket = new TicketState
                        {
                            FirstSeenUtc = now,
                            LastSeenUtc = now,
                            OccurrenceCount = 0,
                            SourceRepairEligible = sourceRepairEligible
                        };
                        Tickets[fingerprint] = ticket;

                        ticket.OccurrenceCount++;
                        ticket.LastSeenUtc = now;
                        occurrenceCount = ticket.OccurrenceCount;
                        firstSeenUtc = ticket.FirstSeenUtc;
                        lastSeenUtc = ticket.LastSeenUtc;
                    }
                }
                else
                {
                    ticket.OccurrenceCount++;
                    ticket.LastSeenUtc = now;
                    occurrenceCount = ticket.OccurrenceCount;
                    firstSeenUtc = ticket.FirstSeenUtc;
                    lastSeenUtc = ticket.LastSeenUtc;
                }
            }

            var circuitOpen = sourceRepairEligible && occurrenceCount >= CircuitOpenOccurrence;
            var humanReviewRequired = circuitOpen;

            string recommendedAction;
            if (circuitOpen) recommendedAction = "human_review";
            else if (sourceRepairEligible) recommendedAction = "open_source_repair";
            else if (callerOrPolicyFailure) recommendedAction = "correct_call_or_refresh_tools";
            else if (transientFailure) recommendedAction = "retry_transient";
            else recommendedAction = "diagnose_before_repair";

            return "{\"ticketId\":\"QS3D-REPAIR-" + fingerprint.Substring(0, 12).ToUpperInvariant()
                   + "\",\"fingerprint\":\"" + fingerprint
                   + "\",\"occurrenceCount\":" + occurrenceCount.ToString(CultureInfo.InvariantCulture)
                   + ",\"sourceRepairEligible\":" + JsonBool(sourceRepairEligible)
                   + ",\"circuitOpen\":" + JsonBool(circuitOpen)
                   + ",\"humanReviewRequired\":" + JsonBool(humanReviewRequired)
                   + ",\"recommendedAction\":\"" + JsonEscape(recommendedAction)
                   + "\",\"firstSeenUtc\":\"" + firstSeenUtc.ToString("o", CultureInfo.InvariantCulture)
                   + "\",\"lastSeenUtc\":\"" + lastSeenUtc.ToString("o", CultureInfo.InvariantCulture) + "\"}";
        }

        private static string? SelectEvictionCandidateLocked(bool incomingSourceRepair)
        {
            string? oldestNonSourceKey = null;
            var oldestNonSourceSeenUtc = DateTime.MaxValue;
            foreach (var pair in Tickets)
            {
                if (pair.Value.SourceRepairEligible) continue;
                if (!IsEarlierTicket(pair.Key, pair.Value.LastSeenUtc, oldestNonSourceKey, oldestNonSourceSeenUtc)) continue;
                oldestNonSourceKey = pair.Key;
                oldestNonSourceSeenUtc = pair.Value.LastSeenUtc;
            }

            if (oldestNonSourceKey != null) return oldestNonSourceKey;
            if (!incomingSourceRepair) return null;

            string? oldestSourceKey = null;
            var oldestSourceSeenUtc = DateTime.MaxValue;
            foreach (var pair in Tickets)
            {
                if (!pair.Value.SourceRepairEligible) continue;
                if (!IsEarlierTicket(pair.Key, pair.Value.LastSeenUtc, oldestSourceKey, oldestSourceSeenUtc)) continue;
                oldestSourceKey = pair.Key;
                oldestSourceSeenUtc = pair.Value.LastSeenUtc;
            }

            return oldestSourceKey;
        }

        private static bool IsEarlierTicket(
            string candidateKey,
            DateTime candidateSeenUtc,
            string? currentKey,
            DateTime currentSeenUtc)
        {
            if (candidateSeenUtc < currentSeenUtc) return true;
            if (candidateSeenUtc > currentSeenUtc) return false;
            if (currentKey == null) return true;
            return StringComparer.Ordinal.Compare(candidateKey, currentKey) < 0;
        }

        internal static string BuildFingerprint(
            string tool,
            string code,
            string lane,
            string exceptionType,
            string message)
        {
            var canonical = string.Join("|", new[]
            {
                Normalize(tool),
                Normalize(code),
                Normalize(lane),
                Normalize(exceptionType),
                Normalize(message)
            });

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var value in hash)
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static string Normalize(string value)
        {
            return Regex.Replace(
                    (value ?? string.Empty).Trim(),
                    "\\s+",
                    " ",
                    RegexOptions.CultureInvariant)
                .ToUpperInvariant();
        }

        private static string BuildSourceRepairIdentity(Exception? exception, string message)
        {
            var sourceSite = string.Empty;
            try
            {
                var targetSite = exception == null ? null : exception.TargetSite;
                if (targetSite != null)
                {
                    var declaringType = targetSite.DeclaringType;
                    sourceSite = (declaringType == null ? string.Empty : declaringType.FullName ?? string.Empty)
                                 + "." + targetSite.Name;
                }
            }
            catch
            {
                // Failure metadata must never replace the original MCP tool failure.
                sourceSite = string.Empty;
            }

            return Normalize(sourceSite) + "|" + CanonicalizeSourceRepairMessage(message);
        }

        private static string CanonicalizeSourceRepairMessage(string message)
        {
            var value = Normalize(message);
            if (value.Length == 0) return value;

            // Normalize high-entropy values that routinely change between manifestations of
            // the same source defect. Keep surrounding semantic text so different failures at
            // the same source site remain independently diagnosable.
            value = Regex.Replace(
                value,
                "\\b[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}\\b",
                "<GUID>",
                RegexOptions.CultureInvariant);
            value = Regex.Replace(
                value,
                "(?:[A-Z]:\\\\|\\\\\\\\)[^ \\t\\r\\n,;]+",
                "<PATH>",
                RegexOptions.CultureInvariant);
            value = Regex.Replace(
                value,
                "\\b0X[0-9A-F]+\\b|\\b[0-9A-F]{12,}\\b",
                "<HEX>",
                RegexOptions.CultureInvariant);
            value = Regex.Replace(
                value,
                "\\b(?:REQUEST|ATTEMPT|SEQUENCE|SEQ|EPOCH|OBJECT|HANDLE|ID)\\s*(?:#|=|:)?\\s*-?\\d+\\b|\\b\\d{8,}\\b",
                "<NUMBER>",
                RegexOptions.CultureInvariant);
            return Normalize(value);
        }

        private static bool IsCallerOrPolicyFailure(string code, string message)
        {
            var value = Normalize((code ?? string.Empty) + " " + (message ?? string.Empty));
            return ContainsAny(
                value,
                "UNKNOWN MCP CAD TOOL",
                "CONFIRMMUTATION",
                "INVALID_ARGUMENT",
                "INVALID ARGUMENT",
                "BAD_REQUEST",
                "BAD REQUEST",
                "UNAUTHORIZED",
                "FORBIDDEN",
                "AUTHORIZATION",
                "AUTHENTICATION",
                "POLICY",
                "CONFIRMATION",
                "SCHEMA",
                "VALIDATION",
                "REQUIRES OBJECT PARAMS",
                "PARAMS.NAME",
                "PARAMS.ARGUMENTS",
                "NOT ALLOWED",
                "MUST BE",
                " IS REQUIRED",
                "MUST MATCH",
                "EXCEEDS");
        }

        private static bool IsTransientFailure(string code, string message)
        {
            var value = Normalize((code ?? string.Empty) + " " + (message ?? string.Empty));
            return ContainsAny(
                value,
                "TIMEOUT",
                "TIMED OUT",
                "DOCUMENT_LOCK",
                "DOCUMENT LOCK",
                "ECANTOPENFILE",
                "DISCONNECTED",
                "TRANSPORT",
                "BUSY",
                "WRITER LEASE",
                "LOCK VIOLATION");
        }

        private static bool IsSourceRepairFailure(string code, string message)
        {
            var value = Normalize((code ?? string.Empty) + " " + (message ?? string.Empty));
            return ContainsAny(
                value,
                "NOT_IMPLEMENTED",
                "NOT IMPLEMENTED",
                "MISSING_CAPABILITY",
                "MISSING CAPABILITY",
                "IMPLEMENTATION",
                "INTERNAL",
                "TOOL_FAILED",
                "TOOL FAILED",
                "CAPABILITY");
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            foreach (var token in tokens)
            {
                if (value.IndexOf(token, StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }

        private static string JsonBool(bool value) { return value ? "true" : "false"; }

        private static string JsonEscape(string value)
        {
            if (value == null) return string.Empty;
            var builder = new StringBuilder(value.Length + 8);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 32)
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(c);
                        break;
                }
            }
            return builder.ToString();
        }

        private sealed class TicketState
        {
            public int OccurrenceCount { get; set; }
            public DateTime FirstSeenUtc { get; set; }
            public DateTime LastSeenUtc { get; set; }
            public bool SourceRepairEligible { get; set; }
        }
    }
}
