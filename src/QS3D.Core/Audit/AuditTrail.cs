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
        private const int MaxStoredEvents = 10_000;
        // Keep audit text materially below the existing 64 MiB QSDB file ceiling so
        // routine audit operations fail closed before a pathological history can
        // dominate later XML materialization. This is an aggregate safety budget,
        // not a claim that every history below it is guaranteed to serialize below
        // the project-file byte ceiling.
        private const long MaxStoredTextCharacters = 8L * 1024L * 1024L;

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
                var storedCount = RequireSupportedHistoryCount(requireAppendCapacity: false);
                var snapshot = new List<AuditEvent>(storedCount);
                var observed = 0;
                long textCharacters = 0L;
                using (var enumerator = _events.GetEnumerator())
                {
                    while (true)
                    {
                        RequireStableHistoryCount(storedCount);
                        if (!enumerator.MoveNext())
                        {
                            RequireStableHistoryCount(storedCount);
                            break;
                        }

                        RequireStableHistoryCount(storedCount);
                        RequireCanReadCurrent(storedCount, observed);
                        var item = enumerator.Current;
                        RequireStableHistoryCount(storedCount);
                        observed++;
                        if (item == null)
                            throw new InvalidOperationException("Audit trail contains a null event.");

                        // Resource integrity wins before XML/canonical scans or cloning.
                        AccumulateTextCharacters(item, ref textCharacters);
                        var validationError = GetStoredEventValidationError(item);
                        if (validationError != null) throw new InvalidOperationException(validationError);
                        snapshot.Add(Clone(item));
                    }
                }
                RequireObservedHistoryCount(storedCount, observed);
                RequireStableHistoryCount(storedCount);
                return snapshot.AsReadOnly();
            }
        }

        public static AuditTrail ForProject(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return new AuditTrail(project.AuditEvents, project);
        }

        internal static void ValidateSnapshotHistory(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            new AuditTrail(project.AuditEvents, project).ValidateExistingHistory(
                requireAppendCapacity: false,
                allowNullActionBacking: true);
        }

        public void Record(string action, string elementId, string detail, string actor = "", string correlationId = "")
        {
            var rawAction = action ?? string.Empty;
            if (rawAction.Length > MaxStoredTextCharacters)
                throw new ArgumentException("Audit action exceeds the supported audit text budget.", nameof(action));
            var normalizedAction = rawAction.Trim();
            if (normalizedAction.Length == 0)
                throw new ArgumentException("Audit action is required.", nameof(action));
            if (ContainsControlCharacter(normalizedAction))
                throw new ArgumentException("Audit action cannot contain control characters.", nameof(action));

            var safeElementId = elementId ?? string.Empty;
            var safeDetail = detail ?? string.Empty;
            var safeActor = actor ?? string.Empty;
            var safeCorrelationId = correlationId ?? string.Empty;
            var newTextCharacters = CountTextCharacters(
                normalizedAction,
                safeElementId,
                safeDetail,
                safeActor,
                safeCorrelationId);
            if (newTextCharacters > MaxStoredTextCharacters)
                throw new ArgumentException("Audit event exceeds the supported aggregate text budget.");

            RequireCanonicalOptionalIdentity(safeElementId, nameof(elementId), "Audit element id");
            RequireCanonicalOptionalIdentity(safeCorrelationId, nameof(correlationId), "Audit correlation id");
            RequireXmlCharacters(normalizedAction, nameof(action), "Audit action");
            RequireXmlCharacters(safeElementId, nameof(elementId), "Audit element id");
            RequireXmlCharacters(safeDetail, nameof(detail), "Audit detail");
            RequireXmlCharacters(safeActor, nameof(actor), "Audit actor");
            RequireXmlCharacters(safeCorrelationId, nameof(correlationId), "Audit correlation id");

            ValidateExistingHistory(requireAppendCapacity: true, additionalTextCharacters: newTextCharacters);

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
            var observed = ValidateExistingHistory(requireAppendCapacity: false);
            if (observed == 0) return;
            _project?.Touch();
            _events.Clear();
        }

        private int ValidateExistingHistory(
            bool requireAppendCapacity,
            long additionalTextCharacters = 0L,
            bool allowNullActionBacking = false)
        {
            if (additionalTextCharacters < 0L || additionalTextCharacters > MaxStoredTextCharacters)
                throw new InvalidOperationException("Audit trail additional text exceeds the supported aggregate text budget.");

            var storedCount = RequireSupportedHistoryCount(requireAppendCapacity);

            var observed = 0;
            long textCharacters = 0L;
            using (var enumerator = _events.GetEnumerator())
            {
                while (true)
                {
                    RequireStableHistoryCount(storedCount);
                    if (!enumerator.MoveNext())
                    {
                        RequireStableHistoryCount(storedCount);
                        break;
                    }

                    RequireStableHistoryCount(storedCount);
                    RequireCanReadCurrent(storedCount, observed);
                    var existing = enumerator.Current;
                    RequireStableHistoryCount(storedCount);
                    observed++;
                    if (existing == null)
                        throw new InvalidOperationException("Audit trail contains a null event. Repair the existing audit history before modifying it.");

                    // Reject aggregate abuse before XML/canonical scans of the event.
                    AccumulateTextCharacters(existing, ref textCharacters);
                    var validationError = allowNullActionBacking
                        ? GetStoredEventValidationError(existing, allowNullActionBacking: true)
                        : GetStoredEventValidationError(existing);
                    if (validationError != null)
                        throw new InvalidOperationException(validationError + " Repair the existing audit history before modifying it.");
                }
            }

            RequireObservedHistoryCount(storedCount, observed);
            RequireStableHistoryCount(storedCount);
            if (additionalTextCharacters > MaxStoredTextCharacters - textCharacters)
                throw TextBudgetExceeded();
            if (requireAppendCapacity && observed >= MaxStoredEvents)
                throw AppendCapacityExceeded();
            return observed;
        }

        private int RequireSupportedHistoryCount(bool requireAppendCapacity)
        {
            var storedCount = _events.Count;
            if (storedCount < 0)
                throw new InvalidOperationException("Audit trail exposes an invalid negative event count. Repair the existing audit history before reading or modifying it.");
            if (storedCount > MaxStoredEvents)
                throw TooManyEvents();
            if (requireAppendCapacity && storedCount >= MaxStoredEvents)
                throw AppendCapacityExceeded();
            return storedCount;
        }

        private static void RequireCanReadCurrent(int storedCount, int observed)
        {
            if (observed >= storedCount)
                throw HistoryCountMismatch();
            if (observed >= MaxStoredEvents)
                throw TooManyEvents();
        }

        private void RequireStableHistoryCount(int admittedCount)
        {
            var reboundCount = _events.Count;
            if (reboundCount < 0)
                throw new InvalidOperationException("Audit trail exposes an invalid negative event count. Repair the existing audit history before reading or modifying it.");
            if (reboundCount > MaxStoredEvents)
                throw TooManyEvents();
            if (reboundCount != admittedCount)
                throw HistoryCountMismatch();
        }

        private static void RequireObservedHistoryCount(int storedCount, int observed)
        {
            if (observed != storedCount)
                throw HistoryCountMismatch();
        }

        private static InvalidOperationException HistoryCountMismatch()
            => new InvalidOperationException("Audit trail event count does not match stored history traversal. Repair the existing audit history before reading or modifying it.");

        private static InvalidOperationException TooManyEvents()
            => new InvalidOperationException("Audit trail contains more than 10000 events. Repair the existing audit history before reading or modifying it.");

        private static InvalidOperationException AppendCapacityExceeded()
            => new InvalidOperationException("Audit trail already contains 10000 events and cannot record another event.");

        private static InvalidOperationException TextBudgetExceeded()
            => new InvalidOperationException("Audit trail text exceeds the supported aggregate text budget. Repair the existing audit history before reading or modifying it.");

        private static void AccumulateTextCharacters(AuditEvent item, ref long total)
        {
            var itemCharacters = CountTextCharacters(
                item.Action,
                item.ElementId,
                item.Detail,
                item.Actor,
                item.CorrelationId);
            if (itemCharacters > MaxStoredTextCharacters - total)
                throw TextBudgetExceeded();
            total += itemCharacters;
        }

        private static long CountTextCharacters(
            string? action,
            string? elementId,
            string? detail,
            string? actor,
            string? correlationId)
        {
            return (long)(action?.Length ?? 0) +
                   (elementId?.Length ?? 0) +
                   (detail?.Length ?? 0) +
                   (actor?.Length ?? 0) +
                   (correlationId?.Length ?? 0);
        }

        private static string? GetStoredEventValidationError(
            AuditEvent? item,
            bool allowNullActionBacking = false)
        {
            if (item == null)
                return "Audit trail contains a null event.";
            if (item.Utc.Kind != DateTimeKind.Utc)
                return "Audit trail contains a non-UTC event timestamp.";

            var action = item.Action;
            if (action == null)
            {
                if (!allowNullActionBacking)
                    return "Audit trail contains a non-canonical action.";
            }
            else if (string.IsNullOrWhiteSpace(action) ||
                     !string.Equals(action, action.Trim(), StringComparison.Ordinal) ||
                     ContainsControlCharacter(action) ||
                     ContainsInvalidXmlCharacters(action))
            {
                return "Audit trail contains a non-canonical action.";
            }

            var elementId = item.ElementId ?? string.Empty;
            if (!IsCanonicalOptionalIdentity(elementId))
                return "Audit trail contains a non-canonical element id.";
            if (ContainsInvalidXmlCharacters(elementId))
                return "Audit trail contains an XML-invalid element id.";
            if (ContainsInvalidXmlCharacters(item.Detail ?? string.Empty))
                return "Audit trail contains XML-invalid detail.";
            if (ContainsInvalidXmlCharacters(item.Actor ?? string.Empty))
                return "Audit trail contains XML-invalid actor.";

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
