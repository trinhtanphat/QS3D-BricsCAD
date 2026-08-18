using System;
using System.Collections.Generic;
using System.Xml;
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
                    var validationError = GetStoredEventValidationError(item);
                    if (validationError != null) throw new InvalidOperationException(validationError);
                    snapshot.Add(Clone(item!));
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
            var normalizedAction = (action ?? string.Empty).Trim();
            if (normalizedAction.Length == 0)
                throw new ArgumentException("Audit action is required.", nameof(action));
            if (ContainsControlCharacter(normalizedAction))
                throw new ArgumentException("Audit action cannot contain control characters.", nameof(action));

            var safeElementId = elementId ?? string.Empty;
            var safeDetail = detail ?? string.Empty;
            var safeActor = actor ?? string.Empty;
            var safeCorrelationId = correlationId ?? string.Empty;
            RequireCanonicalOptionalIdentity(safeElementId, nameof(elementId), "Audit element id");
            RequireCanonicalOptionalIdentity(safeCorrelationId, nameof(correlationId), "Audit correlation id");
            RequireXmlCharacters(normalizedAction, nameof(action), "Audit action");
            RequireXmlCharacters(safeElementId, nameof(elementId), "Audit element id");
            RequireXmlCharacters(safeDetail, nameof(detail), "Audit detail");
            RequireXmlCharacters(safeActor, nameof(actor), "Audit actor");
            RequireXmlCharacters(safeCorrelationId, nameof(correlationId), "Audit correlation id");
            ValidateExistingHistory();

            var item = new AuditEvent
            {
                Utc = DateTime.UtcNow,
                Action = normalizedAction,
                ElementId = safeElementId,
                Detail = safeDetail,
                Actor = safeActor,
                CorrelationId = safeCorrelationId
            };
            _project?.Touch();
            _events.Add(item);
        }

        public void Clear()
        {
            if (_events.Count == 0) return;
            ValidateExistingHistory();
            _project?.Touch();
            _events.Clear();
        }

        private void ValidateExistingHistory()
        {
            foreach (var existing in _events)
            {
                var validationError = GetStoredEventValidationError(existing);
                if (validationError != null)
                    throw new InvalidOperationException(validationError + " Repair the existing audit history before modifying it.");
            }
        }

        private static string? GetStoredEventValidationError(AuditEvent? item)
        {
            if (item == null)
                return "Audit trail contains a null event.";
            if (item.Utc.Kind != DateTimeKind.Utc)
                return "Audit trail contains a non-UTC event timestamp.";

            var action = item.Action ?? string.Empty;
            if (string.IsNullOrWhiteSpace(action) ||
                !string.Equals(action, action.Trim(), StringComparison.Ordinal) ||
                ContainsControlCharacter(action) ||
                ContainsInvalidXmlCharacters(action))
                return "Audit trail contains a non-canonical action.";

            var elementId = item.ElementId ?? string.Empty;
            if (!IsCanonicalOptionalIdentity(elementId))
                return "Audit trail contains a non-canonical element id.";
            if (ContainsInvalidXmlCharacters(elementId))
                return "Audit trail contains an XML-invalid element id.";
            if (ContainsInvalidXmlCharacters(item.Detail ?? string.Empty))
                return "Audit trail contains XML-invalid detail.";
            if (ContainsInvalidXmlCharacters(item.Actor ?? string.Empty))
                return "Audit trail contains an XML-invalid actor.";

            var correlationId = item.CorrelationId ?? string.Empty;
            if (!IsCanonicalOptionalIdentity(correlationId))
                return "Audit trail contains a non-canonical correlation id.";
            if (ContainsInvalidXmlCharacters(correlationId))
                return "Audit trail contains an XML-invalid correlation id.";

            return null;
        }

        private static void RequireCanonicalOptionalIdentity(string value, string parameterName, string label)
        {
            if (!IsCanonicalOptionalIdentity(value))
                throw new ArgumentException(label + " must be empty or canonical without surrounding whitespace or control characters.", parameterName);
        }

        private static bool IsCanonicalOptionalIdentity(string value)
        {
            return value.Length == 0 ||
                (string.Equals(value, value.Trim(), StringComparison.Ordinal) && !ContainsControlCharacter(value));
        }

        private static void RequireXmlCharacters(string value, string parameterName, string label)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(label + " contains characters that cannot be persisted to QSDB XML.", parameterName, ex);
            }
        }

        private static bool ContainsInvalidXmlCharacters(string value)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value);
                return false;
            }
            catch (XmlException)
            {
                return true;
            }
        }

        private static bool ContainsControlCharacter(string value)
        {
            foreach (var character in value)
                if (char.IsControl(character)) return true;
            return false;
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
