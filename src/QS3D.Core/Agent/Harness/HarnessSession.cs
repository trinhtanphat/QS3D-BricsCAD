using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Agent.Harness
{
    public sealed class HarnessSession
    {
        private readonly List<HarnessTraceEvent> _trace = new List<HarnessTraceEvent>();
        private DateTime _lastTimestampUtc = DateTime.MinValue;

        public HarnessSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session id is required.", nameof(sessionId));
            SessionId = sessionId.Trim();
        }

        public string SessionId { get; }
        public IReadOnlyList<HarnessTraceEvent> Trace => _trace.ToArray();

        public HarnessTraceEvent AppendTrace(
            string kind,
            string summary,
            string? sourceIdentity,
            IDictionary<string, string>? metadata)
        {
            if (string.IsNullOrWhiteSpace(kind))
                throw new ArgumentException("Trace kind is required.", nameof(kind));
            if (string.IsNullOrWhiteSpace(summary))
                throw new ArgumentException("Trace summary is required.", nameof(summary));

            var now = DateTime.UtcNow;
            if (now < _lastTimestampUtc)
                now = _lastTimestampUtc;
            _lastTimestampUtc = now;

            var safeMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (metadata != null)
            {
                foreach (var pair in metadata)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                        continue;
                    safeMetadata[pair.Key] = IsSensitiveKey(pair.Key) ? "[REDACTED]" : pair.Value ?? string.Empty;
                }
            }

            var item = new HarnessTraceEvent(
                SessionId,
                _trace.Count + 1L,
                kind.Trim(),
                now,
                summary.Trim(),
                sourceIdentity,
                new ReadOnlyDictionary<string, string>(safeMetadata));
            _trace.Add(item);
            return item;
        }

        private static bool IsSensitiveKey(string key)
        {
            return Contains(key, "secret")
                || Contains(key, "token")
                || Contains(key, "password")
                || Contains(key, "credential")
                || Contains(key, "authorization")
                || Contains(key, "api-key")
                || Contains(key, "apikey");
        }

        private static bool Contains(string value, string term)
        {
            return value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
