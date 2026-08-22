using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Audit
{
    public sealed class AuditEvent
    {
        public DateTime Utc { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ElementId { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
    }

    public sealed class AuditTrail
    {
        private readonly IList<AuditEvent> _events;
        private readonly ProjectState? _project;

        public AuditTrail() : this(new List<AuditEvent>(), null) { }

        private AuditTrail(IList<AuditEvent> events, ProjectState? project)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _project = project;
        }

        public IReadOnlyList<AuditEvent> Events
        {
            get
            {
                var snapshot = new List<AuditEvent>(_events.Count);
                foreach (var item in _events)
                {
                    if (item == null) throw new InvalidOperationException("Audit trail contains a null event.");
                    snapshot.Add(Clone(item));
                }
                return snapshot.AsReadOnly();
            }
        }

        public static AuditTrail ForProject(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return new AuditTrail(project.AuditEvents, project);
        }

        public void Record(string action, string elementId, string detail, string actor = "", string correlationId = "")
        {
            var item = new AuditEvent
            {
                Utc = DateTime.UtcNow,
                Action = action ?? string.Empty,
                ElementId = elementId ?? string.Empty,
                Detail = detail ?? string.Empty,
                Actor = actor ?? string.Empty,
                CorrelationId = correlationId ?? string.Empty
            };
            _project?.Touch();
            _events.Add(item);
        }

        public void Clear()
        {
            if (_events.Count == 0) return;
            _project?.Touch();
            _events.Clear();
        }

        private static AuditEvent Clone(AuditEvent item)
        {
            return new AuditEvent
            {
                Utc = item.Utc,
                Action = item.Action ?? string.Empty,
                ElementId = item.ElementId ?? string.Empty,
                Detail = item.Detail ?? string.Empty,
                Actor = item.Actor ?? string.Empty,
                CorrelationId = item.CorrelationId ?? string.Empty
            };
        }
    }
}
