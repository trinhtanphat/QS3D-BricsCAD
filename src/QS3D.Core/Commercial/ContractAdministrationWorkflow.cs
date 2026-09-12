using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Commercial
{
    public enum ContractNoticeDirection
    {
        Outgoing = 0,
        Incoming = 1
    }

    public enum ContractNoticeState
    {
        Pending = 0,
        Issued = 1,
        Received = 2
    }

    public enum ContractDeadlineStatus
    {
        Open = 0,
        DueToday = 1,
        Overdue = 2,
        OnTime = 3,
        Late = 4
    }

    public enum EotAdministrationStatus
    {
        Draft = 0,
        Submitted = 1,
        UnderReview = 2,
        Assessed = 3,
        Decided = 4
    }

    public enum ContractClaimStatus
    {
        Draft = 0,
        Submitted = 1,
        Reviewed = 2,
        Decided = 3
    }

    public sealed class ContractEvidenceRef
    {
        public ContractEvidenceRef(string evidenceId, string evidenceKind, string reference)
        {
            EvidenceId = CommercialGuard.RequireToken(evidenceId, nameof(evidenceId));
            EvidenceKind = CommercialGuard.RequireToken(evidenceKind, nameof(evidenceKind));
            Reference = CommercialGuard.RequireCanonicalText(reference, nameof(reference));
        }

        public string EvidenceId { get; }
        public string EvidenceKind { get; }
        public string Reference { get; }
    }

    public sealed class ContractEventRecord
    {
        private ContractEventRecord(
            string eventId,
            CommercialRevisionRef revision,
            CommercialRevisionRef? previousRevision,
            string eventType,
            string title,
            string description,
            DateTime occurredUtc,
            string responsibleParty,
            IEnumerable<ContractEvidenceRef> evidence)
        {
            EventId = CommercialGuard.RequireToken(eventId, nameof(eventId));
            Revision = ContractAdministrationGuard.RequireRevision(revision, "contract-event", EventId, nameof(revision));
            PreviousRevision = ContractAdministrationGuard.RequireOptionalPreviousRevision(previousRevision, "contract-event", EventId, Revision, nameof(previousRevision));
            EventType = CommercialGuard.RequireToken(eventType, nameof(eventType));
            Title = CommercialGuard.RequireCanonicalText(title, nameof(title));
            Description = CommercialGuard.RequireCanonicalText(description, nameof(description));
            OccurredUtc = CommercialGuard.RequireUtc(occurredUtc, nameof(occurredUtc));
            ResponsibleParty = CommercialGuard.RequireCanonicalText(responsibleParty, nameof(responsibleParty));
            Evidence = ContractAdministrationGuard.SnapshotEvidence(evidence, nameof(evidence));
        }

        public string EventId { get; }
        public CommercialRevisionRef Revision { get; }
        public CommercialRevisionRef? PreviousRevision { get; }
        public string EventType { get; }
        public string Title { get; }
        public string Description { get; }
        public DateTime OccurredUtc { get; }
        public string ResponsibleParty { get; }
        public IReadOnlyList<ContractEvidenceRef> Evidence { get; }

        public static ContractEventRecord Create(
            string eventId,
            string revisionId,
            string eventType,
            string title,
            string description,
            DateTime occurredUtc,
            string responsibleParty,
            IEnumerable<ContractEvidenceRef> evidence)
        {
            return new ContractEventRecord(
                eventId,
                new CommercialRevisionRef("contract-event", eventId, revisionId),
                null,
                eventType,
                title,
                description,
                occurredUtc,
                responsibleParty,
                evidence);
        }

        public ContractEventRecord Revise(
            string revisionId,
            string eventType,
            string title,
            string description,
            DateTime occurredUtc,
            string responsibleParty,
            IEnumerable<ContractEvidenceRef> evidence)
        {
            return new ContractEventRecord(
                EventId,
                new CommercialRevisionRef("contract-event", EventId, revisionId),
                Revision,
                eventType,
                title,
                description,
                occurredUtc,
                responsibleParty,
                evidence);
        }
    }

    public sealed class ContractNoticeRecord
    {
        private ContractNoticeRecord(
            string noticeId,
            CommercialRevisionRef revision,
            CommercialRevisionRef? previousRevision,
            CommercialRevisionRef eventRevision,
            ContractNoticeDirection direction,
            ContractNoticeState state,
            string contractClause,
            DateTime requiredByUtc,
            DateTime? actionUtc,
            string fromParty,
            string toParty,
            string reference,
            IEnumerable<ContractEvidenceRef> evidence)
        {
            NoticeId = CommercialGuard.RequireToken(noticeId, nameof(noticeId));
            Revision = ContractAdministrationGuard.RequireRevision(revision, "contract-notice", NoticeId, nameof(revision));
            PreviousRevision = ContractAdministrationGuard.RequireOptionalPreviousRevision(previousRevision, "contract-notice", NoticeId, Revision, nameof(previousRevision));
            EventRevision = ContractAdministrationGuard.RequireRevisionKind(eventRevision, "contract-event", nameof(eventRevision));
            if (!Enum.IsDefined(typeof(ContractNoticeDirection), direction)) throw new ArgumentOutOfRangeException(nameof(direction));
            if (!Enum.IsDefined(typeof(ContractNoticeState), state)) throw new ArgumentOutOfRangeException(nameof(state));
            ContractClause = CommercialGuard.RequireCanonicalText(contractClause, nameof(contractClause));
            RequiredByUtc = CommercialGuard.RequireUtc(requiredByUtc, nameof(requiredByUtc));
            ActionUtc = actionUtc.HasValue ? CommercialGuard.RequireUtc(actionUtc.Value, nameof(actionUtc)) : (DateTime?)null;
            FromParty = CommercialGuard.RequireCanonicalText(fromParty, nameof(fromParty));
            ToParty = CommercialGuard.RequireCanonicalText(toParty, nameof(toParty));
            Reference = CommercialGuard.RequireCanonicalText(reference, nameof(reference));
            Evidence = ContractAdministrationGuard.SnapshotEvidence(evidence, nameof(evidence));
            ValidateState();
        }

        public string NoticeId { get; }
        public CommercialRevisionRef Revision { get; }
        public CommercialRevisionRef? PreviousRevision { get; }
        public CommercialRevisionRef EventRevision { get; }
        public ContractNoticeDirection Direction { get; }
        public ContractNoticeState State { get; }
        public string ContractClause { get; }
        public DateTime RequiredByUtc { get; }
        public DateTime? ActionUtc { get; }
        public string FromParty { get; }
        public string ToParty { get; }
        public string Reference { get; }
        public IReadOnlyList<ContractEvidenceRef> Evidence { get; }

        public static ContractNoticeRecord CreatePending(
            string noticeId,
            string revisionId,
            CommercialRevisionRef eventRevision,
            ContractNoticeDirection direction,
            string contractClause,
            DateTime requiredByUtc,
            string fromParty,
            string toParty,
            string reference,
            IEnumerable<ContractEvidenceRef> evidence)
        {
            return new ContractNoticeRecord(
                noticeId,
                new CommercialRevisionRef("contract-notice", noticeId, revisionId),
                null,
                eventRevision,
                direction,
                ContractNoticeState.Pending,
                contractClause,
                requiredByUtc,
                null,
                fromParty,
                toParty,
                reference,
                evidence);
        }

        public ContractNoticeRecord MarkIssued(string revisionId, DateTime issuedUtc, string reference, IEnumerable<ContractEvidenceRef> evidence)
        {
            if (Direction != ContractNoticeDirection.Outgoing)
                throw new InvalidOperationException("Only an outgoing contractual notice can be marked Issued.");
            return Complete(revisionId, ContractNoticeState.Issued, issuedUtc, reference, evidence);
        }

        public ContractNoticeRecord MarkReceived(string revisionId, DateTime receivedUtc, string reference, IEnumerable<ContractEvidenceRef> evidence)
        {
            if (Direction != ContractNoticeDirection.Incoming)
                throw new InvalidOperationException("Only an incoming contractual notice can be marked Received.");
            return Complete(revisionId, ContractNoticeState.Received, receivedUtc, reference, evidence);
        }

        public ContractDeadlineStatus DeadlineStatus(DateTime asOfUtc)
        {
            var asOf = CommercialGuard.RequireUtc(asOfUtc, nameof(asOfUtc));
            if (ActionUtc.HasValue)
                return ActionUtc.Value <= RequiredByUtc ? ContractDeadlineStatus.OnTime : ContractDeadlineStatus.Late;
            if (asOf > RequiredByUtc)
                return ContractDeadlineStatus.Overdue;
            return asOf.Date == RequiredByUtc.Date ? ContractDeadlineStatus.DueToday : ContractDeadlineStatus.Open;
        }

        private ContractNoticeRecord Complete(
            string revisionId,
            ContractNoticeState completedState,
            DateTime actionUtc,
            string reference,
            IEnumerable<ContractEvidenceRef> evidence)
        {
            if (State != ContractNoticeState.Pending)
                throw new InvalidOperationException("A contractual notice can only be completed from Pending.");
            return new ContractNoticeRecord(
                NoticeId,
                new CommercialRevisionRef("contract-notice", NoticeId, revisionId),
                Revision,
                EventRevision,
                Direction,
                completedState,
                ContractClause,
                RequiredByUtc,
                actionUtc,
                FromParty,
                ToParty,
                reference,
                evidence);
        }

        private void ValidateState()
        {
            if (State == ContractNoticeState.Pending)
            {
                if (ActionUtc.HasValue)
                    throw new ArgumentException("Pending contractual notice cannot carry an action timestamp.", nameof(ActionUtc));
                return;
            }
            if (!ActionUtc.HasValue)
                throw new ArgumentException("Completed contractual notice requires an action timestamp.", nameof(ActionUtc));
            if (State == ContractNoticeState.Issued && Direction != ContractNoticeDirection.Outgoing)
                throw new ArgumentException("Issued notice must be outgoing.", nameof(State));
            if (State == ContractNoticeState.Received && Direction != ContractNoticeDirection.Incoming)
                throw new ArgumentException("Received notice must be incoming.", nameof(State));
        }
    }

    public sealed class EotSubmissionRecord
    {
        private EotSubmissionRecord(
            string eotId,
            CommercialRevisionRef revision,
            CommercialRevisionRef? previousRevision,
            CommercialRevisionRef eventRevision,
            CommercialRevisionRef noticeRevision,
            EotAdministrationStatus status,
            int requestedDays,
            int? assessedDays,
            int? decidedDays,
            string narrative,
            string assessmentNote,
            string decisionNote,
            IEnumerable<ContractEvidenceRef> evidence)
        {
            EotId = CommercialGuard.RequireToken(eotId, nameof(eotId));
            Revision = ContractAdministrationGuard.RequireRevision(revision, "contract-eot", EotId, nameof(revision));
            PreviousRevision = ContractAdministrationGuard.RequireOptionalPreviousRevision(previousRevision, "contract-eot", EotId, Revision, nameof(previousRevision));
            EventRevision = ContractAdministrationGuard.RequireRevisionKind(eventRevision, "contract-event", nameof(eventRevision));
            NoticeRevision = ContractAdministrationGuard.RequireRevisionKind(noticeRevision, "contract-notice", nameof(noticeRevision));
            if (!Enum.IsDefined(typeof(EotAdministrationStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            if (requestedDays <= 0 || requestedDays > 3650) throw new ArgumentOutOfRangeException(nameof(requestedDays));
            if (assessedDays.HasValue && (assessedDays.Value < 0 || assessedDays.Value > requestedDays)) throw new ArgumentOutOfRangeException(nameof(assessedDays));
            if (decidedDays.HasValue && (decidedDays.Value < 0 || decidedDays.Value > requestedDays)) throw new ArgumentOutOfRangeException(nameof(decidedDays));
            Status = status;
            RequestedDays = requestedDays;
            AssessedDays = assessedDays;
            DecidedDays = decidedDays;
            Narrative = CommercialGuard.RequireCanonicalText(narrative, nameof(narrative));
            AssessmentNote = CommercialGuard.RequireOptionalCanonicalText(assessmentNote, nameof(assessmentNote));
            DecisionNote = CommercialGuard.RequireOptionalCanonicalText(decisionNote, nameof(decisionNote));
            Evidence = ContractAdministrationGuard.SnapshotEvidence(evidence, nameof(evidence));
            ValidateState();
        }

        public string EotId { get; }
        public CommercialRevisionRef Revision { get; }
        public CommercialRevisionRef? PreviousRevision { get; }
        public CommercialRevisionRef EventRevision { get; }
        public CommercialRevisionRef NoticeRevision { get; }
        public EotAdministrationStatus Status { get; }
        public int RequestedDays { get; }
        public int? AssessedDays { get; }
        public int? DecidedDays { get; }
        public string Narrative { get; }
        public string AssessmentNote { get; }
        public string DecisionNote { get; }
        public IReadOnlyList<ContractEvidenceRef> Evidence { get; }

        public static EotSubmissionRecord CreateDraft(
            string eotId,
            string revisionId,
            CommercialRevisionRef eventRevision,
            CommercialRevisionRef noticeRevision,
            int requestedDays,
            string narrative,
            IEnumerable<ContractEvidenceRef> evidence)
        {
            return new EotSubmissionRecord(
                eotId,
                new CommercialRevisionRef("contract-eot", eotId, revisionId),
                null,
                eventRevision,
                noticeRevision,
                EotAdministrationStatus.Draft,
                requestedDays,
                null,
                null,
                narrative,
                string.Empty,
                string.Empty,
                evidence);
        }

        public EotSubmissionRecord Submit(string revisionId)
        {
            RequireStatus(EotAdministrationStatus.Draft, "submitted");
            return Next(revisionId, EotAdministrationStatus.Submitted, null, null, string.Empty, string.Empty);
        }

        public EotSubmissionRecord StartReview(string revisionId)
        {
            RequireStatus(EotAdministrationStatus.Submitted, "placed under review");
            return Next(revisionId, EotAdministrationStatus.UnderReview, null, null, string.Empty, string.Empty);
        }

        public EotSubmissionRecord Assess(string revisionId, int assessedDays, string assessmentNote)
        {
            RequireStatus(EotAdministrationStatus.UnderReview, "assessed");
            return Next(
                revisionId,
                EotAdministrationStatus.Assessed,
                assessedDays,
                null,
                CommercialGuard.RequireCanonicalText(assessmentNote, nameof(assessmentNote)),
                string.Empty);
        }

        public EotSubmissionRecord Decide(string revisionId, int decidedDays, string decisionNote)
        {
            RequireStatus(EotAdministrationStatus.Assessed, "decided");
            return Next(
                revisionId,
                EotAdministrationStatus.Decided,
                AssessedDays,
                decidedDays,
                AssessmentNote,
                CommercialGuard.RequireCanonicalText(decisionNote, nameof(decisionNote)));
        }

        private EotSubmissionRecord Next(
            string revisionId,
            EotAdministrationStatus status,
            int? assessedDays,
            int? decidedDays,
            string assessmentNote,
            string decisionNote)
        {
            return new EotSubmissionRecord(
                EotId,
                new CommercialRevisionRef("contract-eot", EotId, revisionId),
                Revision,
                EventRevision,
                NoticeRevision,
                status,
                RequestedDays,
                assessedDays,
                decidedDays,
                Narrative,
                assessmentNote,
                decisionNote,
                Evidence);
        }

        private void RequireStatus(EotAdministrationStatus required, string operation)
        {
            if (Status != required)
                throw new InvalidOperationException("EOT submission can only be " + operation + " from " + required + ".");
        }

        private void ValidateState()
        {
            if (Status == EotAdministrationStatus.Draft || Status == EotAdministrationStatus.Submitted || Status == EotAdministrationStatus.UnderReview)
            {
                if (AssessedDays.HasValue || DecidedDays.HasValue || AssessmentNote.Length != 0 || DecisionNote.Length != 0)
                    throw new ArgumentException("Pre-assessment EOT state cannot carry assessment or decision metadata.");
                return;
            }
            if (!AssessedDays.HasValue || AssessmentNote.Length == 0)
                throw new ArgumentException("Assessed EOT state requires assessed days and assessment note.");
            if (Status == EotAdministrationStatus.Assessed)
            {
                if (DecidedDays.HasValue || DecisionNote.Length != 0)
                    throw new ArgumentException("Assessed EOT state cannot carry decision metadata.");
                return;
            }
            if (!DecidedDays.HasValue || DecisionNote.Length == 0)
                throw new ArgumentException("Decided EOT state requires decided days and decision note.");
        }
    }

    public sealed class ContractClaimRecord
    {
        private ContractClaimRecord(
            string claimId,
            CommercialRevisionRef revision,
            CommercialRevisionRef? previousRevision,
            CommercialRevisionRef eventRevision,
            CommercialRevisionRef noticeRevision,
            CommercialRevisionRef? eotRevision,
            string claimType,
            ContractClaimStatus status,
            string narrative,
            string reviewNote,
            string decisionNote,
            IEnumerable<ContractEvidenceRef> evidence)
        {
            ClaimId = CommercialGuard.RequireToken(claimId, nameof(claimId));
            Revision = ContractAdministrationGuard.RequireRevision(revision, "contract-claim", ClaimId, nameof(revision));
            PreviousRevision = ContractAdministrationGuard.RequireOptionalPreviousRevision(previousRevision, "contract-claim", ClaimId, Revision, nameof(previousRevision));
            EventRevision = ContractAdministrationGuard.RequireRevisionKind(eventRevision, "contract-event", nameof(eventRevision));
            NoticeRevision = ContractAdministrationGuard.RequireRevisionKind(noticeRevision, "contract-notice", nameof(noticeRevision));
            EotRevision = eotRevision == null ? null : ContractAdministrationGuard.RequireRevisionKind(eotRevision, "contract-eot", nameof(eotRevision));
            ClaimType = CommercialGuard.RequireToken(claimType, nameof(claimType));
            if (!Enum.IsDefined(typeof(ContractClaimStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
            Narrative = CommercialGuard.RequireCanonicalText(narrative, nameof(narrative));
            ReviewNote = CommercialGuard.RequireOptionalCanonicalText(reviewNote, nameof(reviewNote));
            DecisionNote = CommercialGuard.RequireOptionalCanonicalText(decisionNote, nameof(decisionNote));
            Evidence = ContractAdministrationGuard.SnapshotEvidence(evidence, nameof(evidence));
            ValidateState();
        }

        public string ClaimId { get; }
        public CommercialRevisionRef Revision { get; }
        public CommercialRevisionRef? PreviousRevision { get; }
        public CommercialRevisionRef EventRevision { get; }
        public CommercialRevisionRef NoticeRevision { get; }
        public CommercialRevisionRef? EotRevision { get; }
        public string ClaimType { get; }
        public ContractClaimStatus Status { get; }
        public string Narrative { get; }
        public string ReviewNote { get; }
        public string DecisionNote { get; }
        public IReadOnlyList<ContractEvidenceRef> Evidence { get; }

        public static ContractClaimRecord CreateDraft(
            string claimId,
            string revisionId,
            CommercialRevisionRef eventRevision,
            CommercialRevisionRef noticeRevision,
            CommercialRevisionRef? eotRevision,
            string claimType,
            string narrative,
            IEnumerable<ContractEvidenceRef> evidence)
        {
            return new ContractClaimRecord(
                claimId,
                new CommercialRevisionRef("contract-claim", claimId, revisionId),
                null,
                eventRevision,
                noticeRevision,
                eotRevision,
                claimType,
                ContractClaimStatus.Draft,
                narrative,
                string.Empty,
                string.Empty,
                evidence);
        }

        public ContractClaimRecord Submit(string revisionId)
        {
            RequireStatus(ContractClaimStatus.Draft, "submitted");
            return Next(revisionId, ContractClaimStatus.Submitted, string.Empty, string.Empty);
        }

        public ContractClaimRecord Review(string revisionId, string reviewNote)
        {
            RequireStatus(ContractClaimStatus.Submitted, "reviewed");
            return Next(revisionId, ContractClaimStatus.Reviewed, CommercialGuard.RequireCanonicalText(reviewNote, nameof(reviewNote)), string.Empty);
        }

        public ContractClaimRecord Decide(string revisionId, string decisionNote)
        {
            RequireStatus(ContractClaimStatus.Reviewed, "decided");
            return Next(revisionId, ContractClaimStatus.Decided, ReviewNote, CommercialGuard.RequireCanonicalText(decisionNote, nameof(decisionNote)));
        }

        private ContractClaimRecord Next(string revisionId, ContractClaimStatus status, string reviewNote, string decisionNote)
        {
            return new ContractClaimRecord(
                ClaimId,
                new CommercialRevisionRef("contract-claim", ClaimId, revisionId),
                Revision,
                EventRevision,
                NoticeRevision,
                EotRevision,
                ClaimType,
                status,
                Narrative,
                reviewNote,
                decisionNote,
                Evidence);
        }

        private void RequireStatus(ContractClaimStatus required, string operation)
        {
            if (Status != required)
                throw new InvalidOperationException("Contract claim can only be " + operation + " from " + required + ".");
        }

        private void ValidateState()
        {
            if (Status == ContractClaimStatus.Draft || Status == ContractClaimStatus.Submitted)
            {
                if (ReviewNote.Length != 0 || DecisionNote.Length != 0)
                    throw new ArgumentException("Pre-review claim state cannot carry review or decision metadata.");
                return;
            }
            if (ReviewNote.Length == 0)
                throw new ArgumentException("Reviewed claim state requires a review note.");
            if (Status == ContractClaimStatus.Reviewed)
            {
                if (DecisionNote.Length != 0)
                    throw new ArgumentException("Reviewed claim state cannot carry a decision note.");
                return;
            }
            if (DecisionNote.Length == 0)
                throw new ArgumentException("Decided claim state requires a decision note.");
        }
    }

    public sealed class ContractAdministrationWorkflow
    {
        private const int MaximumRecordsPerRegister = 5000;
        private readonly List<ContractEventRecord> _events = new List<ContractEventRecord>();
        private readonly List<ContractNoticeRecord> _notices = new List<ContractNoticeRecord>();
        private readonly List<EotSubmissionRecord> _eotSubmissions = new List<EotSubmissionRecord>();
        private readonly List<ContractClaimRecord> _claims = new List<ContractClaimRecord>();
        private readonly Dictionary<string, CommercialRevisionRef> _latestEventRevision = new Dictionary<string, CommercialRevisionRef>(StringComparer.Ordinal);
        private readonly Dictionary<string, CommercialRevisionRef> _latestNoticeRevision = new Dictionary<string, CommercialRevisionRef>(StringComparer.Ordinal);
        private readonly Dictionary<string, CommercialRevisionRef> _latestEotRevision = new Dictionary<string, CommercialRevisionRef>(StringComparer.Ordinal);
        private readonly Dictionary<string, CommercialRevisionRef> _latestClaimRevision = new Dictionary<string, CommercialRevisionRef>(StringComparer.Ordinal);
        private readonly HashSet<string> _knownRevisionKeys = new HashSet<string>(StringComparer.Ordinal);

        public ContractAdministrationWorkflow()
            : this(new CommercialAuditLog())
        {
        }

        public ContractAdministrationWorkflow(CommercialAuditLog auditLog)
        {
            AuditLog = auditLog ?? throw new ArgumentNullException(nameof(auditLog));
        }

        public CommercialAuditLog AuditLog { get; }
        public IReadOnlyList<ContractEventRecord> Events => new ReadOnlyCollection<ContractEventRecord>(_events.ToArray());
        public IReadOnlyList<ContractNoticeRecord> Notices => new ReadOnlyCollection<ContractNoticeRecord>(_notices.ToArray());
        public IReadOnlyList<EotSubmissionRecord> EotSubmissions => new ReadOnlyCollection<EotSubmissionRecord>(_eotSubmissions.ToArray());
        public IReadOnlyList<ContractClaimRecord> Claims => new ReadOnlyCollection<ContractClaimRecord>(_claims.ToArray());

        public void AppendEvent(ContractEventRecord record, string actor, string reason, DateTime auditUtc)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            RequireCapacity(_events.Count, "contract event register");
            AdmitRevision(record.EventId, record.Revision, record.PreviousRevision, _latestEventRevision);
            _events.Add(record);
            AppendAudit(
                "contract-event",
                record.EventId,
                "event-recorded",
                record.Revision,
                actor,
                reason,
                auditUtc,
                "type=" + record.EventType + ";occurredUtc=" + record.OccurredUtc.ToString("O") + ";evidence=" + record.Evidence.Count,
                new[] { record.Revision });
        }

        public void AppendNotice(ContractNoticeRecord record, string actor, string reason, DateTime auditUtc)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            RequireCapacity(_notices.Count, "contract notice register");
            RequireKnownRevision(record.EventRevision, "Contract notice references an unknown event revision.");
            AdmitRevision(record.NoticeId, record.Revision, record.PreviousRevision, _latestNoticeRevision);
            _notices.Add(record);
            AppendAudit(
                "contract-notice",
                record.NoticeId,
                "notice-" + record.State.ToString().ToLowerInvariant(),
                record.Revision,
                actor,
                reason,
                auditUtc,
                "direction=" + record.Direction + ";state=" + record.State + ";requiredByUtc=" + record.RequiredByUtc.ToString("O") + ";deadline=" + record.DeadlineStatus(auditUtc),
                new[] { record.EventRevision, record.Revision });
        }

        public void AppendEot(EotSubmissionRecord record, string actor, string reason, DateTime auditUtc)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            RequireCapacity(_eotSubmissions.Count, "EOT register");
            RequireKnownRevision(record.EventRevision, "EOT submission references an unknown event revision.");
            RequireKnownRevision(record.NoticeRevision, "EOT submission references an unknown notice revision.");
            AdmitRevision(record.EotId, record.Revision, record.PreviousRevision, _latestEotRevision);
            _eotSubmissions.Add(record);
            AppendAudit(
                "contract-eot",
                record.EotId,
                "eot-" + record.Status.ToString().ToLowerInvariant(),
                record.Revision,
                actor,
                reason,
                auditUtc,
                "status=" + record.Status + ";requestedDays=" + record.RequestedDays + ";assessedDays=" + OptionalInt(record.AssessedDays) + ";decidedDays=" + OptionalInt(record.DecidedDays),
                new[] { record.EventRevision, record.NoticeRevision, record.Revision });
        }

        public void AppendClaim(ContractClaimRecord record, string actor, string reason, DateTime auditUtc)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            RequireCapacity(_claims.Count, "claim register");
            RequireKnownRevision(record.EventRevision, "Claim references an unknown event revision.");
            RequireKnownRevision(record.NoticeRevision, "Claim references an unknown notice revision.");
            if (record.EotRevision != null)
                RequireKnownRevision(record.EotRevision, "Claim references an unknown EOT revision.");
            AdmitRevision(record.ClaimId, record.Revision, record.PreviousRevision, _latestClaimRevision);
            _claims.Add(record);

            var sources = new List<CommercialRevisionRef> { record.EventRevision, record.NoticeRevision };
            if (record.EotRevision != null) sources.Add(record.EotRevision);
            sources.Add(record.Revision);
            AppendAudit(
                "contract-claim",
                record.ClaimId,
                "claim-" + record.Status.ToString().ToLowerInvariant(),
                record.Revision,
                actor,
                reason,
                auditUtc,
                "type=" + record.ClaimType + ";status=" + record.Status + ";evidence=" + record.Evidence.Count,
                sources);
        }

        private void AdmitRevision(
            string entityId,
            CommercialRevisionRef revision,
            CommercialRevisionRef? previousRevision,
            IDictionary<string, CommercialRevisionRef> latest)
        {
            var revisionKey = ContractAdministrationGuard.RevisionKey(revision);
            if (_knownRevisionKeys.Contains(revisionKey))
                throw new InvalidOperationException("Contract administration revision already exists: " + revisionKey + ".");

            if (latest.TryGetValue(entityId, out var current))
            {
                if (previousRevision == null || !ContractAdministrationGuard.RevisionEquals(previousRevision, current))
                    throw new InvalidOperationException("Contract administration revision must link to the current previous revision.");
            }
            else if (previousRevision != null)
            {
                throw new InvalidOperationException("Initial contract administration revision cannot declare a previous revision.");
            }

            _knownRevisionKeys.Add(revisionKey);
            latest[entityId] = revision;
        }

        private void RequireKnownRevision(CommercialRevisionRef revision, string message)
        {
            if (!_knownRevisionKeys.Contains(ContractAdministrationGuard.RevisionKey(revision)))
                throw new InvalidOperationException(message);
        }

        private void AppendAudit(
            string entityType,
            string entityId,
            string action,
            CommercialRevisionRef revision,
            string actor,
            string reason,
            DateTime auditUtc,
            string afterSummary,
            IEnumerable<CommercialRevisionRef> sources)
        {
            var timestamp = CommercialGuard.RequireUtc(auditUtc, nameof(auditUtc));
            var eventId = "contract-admin-" + entityType + "-" + entityId + "-" + revision.RevisionId;
            AuditLog.Append(new CommercialAuditRecord(
                eventId,
                entityType,
                entityId,
                action,
                CommercialGuard.RequireCanonicalText(actor, nameof(actor)),
                timestamp,
                CommercialGuard.RequireCanonicalText(reason, nameof(reason)),
                "contract-admin:" + entityId,
                revision.RevisionId,
                afterSummary,
                sources));
        }

        private static void RequireCapacity(int count, string registerName)
        {
            if (count >= MaximumRecordsPerRegister)
                throw new InvalidOperationException(registerName + " supports at most " + MaximumRecordsPerRegister + " revisions.");
        }

        private static string OptionalInt(int? value) => value.HasValue ? value.Value.ToString() : string.Empty;
    }

    internal static class ContractAdministrationGuard
    {
        internal static CommercialRevisionRef RequireRevision(CommercialRevisionRef revision, string expectedKind, string expectedId, string paramName)
        {
            var admitted = RequireRevisionKind(revision, expectedKind, paramName);
            if (!string.Equals(admitted.SourceId, expectedId, StringComparison.Ordinal))
                throw new ArgumentException("Revision source id must match the contract administration entity id.", paramName);
            return admitted;
        }

        internal static CommercialRevisionRef RequireRevisionKind(CommercialRevisionRef revision, string expectedKind, string paramName)
        {
            if (revision == null) throw new ArgumentNullException(paramName);
            if (!string.Equals(revision.SourceKind, expectedKind, StringComparison.Ordinal))
                throw new ArgumentException("Revision source kind must be " + expectedKind + ".", paramName);
            return revision;
        }

        internal static CommercialRevisionRef? RequireOptionalPreviousRevision(
            CommercialRevisionRef? previousRevision,
            string expectedKind,
            string expectedId,
            CommercialRevisionRef currentRevision,
            string paramName)
        {
            if (previousRevision == null) return null;
            var admitted = RequireRevision(previousRevision, expectedKind, expectedId, paramName);
            if (RevisionEquals(admitted, currentRevision))
                throw new ArgumentException("Previous revision must differ from current revision.", paramName);
            return admitted;
        }

        internal static IReadOnlyList<ContractEvidenceRef> SnapshotEvidence(IEnumerable<ContractEvidenceRef> evidence, string paramName)
        {
            return CommercialGuard.SnapshotStableGeneration(evidence, paramName, 256, EvidenceEquals);
        }

        internal static bool RevisionEquals(CommercialRevisionRef left, CommercialRevisionRef right)
        {
            return left != null && right != null &&
                string.Equals(left.SourceKind, right.SourceKind, StringComparison.Ordinal) &&
                string.Equals(left.SourceId, right.SourceId, StringComparison.Ordinal) &&
                string.Equals(left.RevisionId, right.RevisionId, StringComparison.Ordinal);
        }

        internal static string RevisionKey(CommercialRevisionRef revision)
        {
            if (revision == null) throw new ArgumentNullException(nameof(revision));
            return revision.SourceKind + ":" + revision.SourceId + "@" + revision.RevisionId;
        }

        private static bool EvidenceEquals(ContractEvidenceRef left, ContractEvidenceRef right)
        {
            return left != null && right != null &&
                string.Equals(left.EvidenceId, right.EvidenceId, StringComparison.Ordinal) &&
                string.Equals(left.EvidenceKind, right.EvidenceKind, StringComparison.Ordinal) &&
                string.Equals(left.Reference, right.Reference, StringComparison.Ordinal);
        }
    }
}
