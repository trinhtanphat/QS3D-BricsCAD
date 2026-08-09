using System;
using System.Collections.Generic;

namespace QS3D.Core.Audit
{
    public sealed class AuditEvent
    {
        public DateTime Utc { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ElementId { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }

    public sealed class AuditTrail
    {
        private readonly List<AuditEvent> _events = new List<AuditEvent>();
        public IReadOnlyList<AuditEvent> Events => _events;
        public void Record(string action, string elementId, string detail)
        {
            _events.Add(new AuditEvent { Utc = DateTime.UtcNow, Action = action ?? string.Empty, ElementId = elementId ?? string.Empty, Detail = detail ?? string.Empty });
        }
        public void Clear() => _events.Clear();
    }
}
