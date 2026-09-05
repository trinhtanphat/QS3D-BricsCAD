using System;
using System.Collections.Generic;

namespace QS3D.Core.Agent.Harness
{
    public sealed class HarnessTraceEvent
    {
        internal HarnessTraceEvent(
            string sessionId,
            long sequence,
            string kind,
            DateTime timestampUtc,
            string summary,
            string? sourceIdentity,
            IReadOnlyDictionary<string, string> metadata)
        {
            SessionId = sessionId;
            Sequence = sequence;
            Kind = kind;
            TimestampUtc = timestampUtc;
            Summary = summary;
            SourceIdentity = sourceIdentity;
            Metadata = metadata;
        }

        public string SessionId { get; }
        public long Sequence { get; }
        public string Kind { get; }
        public DateTime TimestampUtc { get; }
        public string Summary { get; }
        public string? SourceIdentity { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
    }
}
