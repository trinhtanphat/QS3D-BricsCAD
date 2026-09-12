using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.Core.Commercial;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class ContractAdministrationWindow : Window
    {
        private readonly Document _document;
        private readonly IntPtr _nativeDatabaseIdentity;
        private readonly ContractAdministrationWorkflow _workflow = new ContractAdministrationWorkflow();
        private ContractEventRecord? _event;
        private ContractNoticeRecord? _notice;
        private EotSubmissionRecord? _eot;
        private ContractClaimRecord? _claim;

        public ContractAdministrationWindow(Document document, IntPtr nativeDatabaseIdentity)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            if (nativeDatabaseIdentity == IntPtr.Zero)
                throw new ArgumentException("A live native database identity is required.", nameof(nativeDatabaseIdentity));
            _nativeDatabaseIdentity = nativeDatabaseIdentity;
            InitializeComponent();
            EventDateBox.Text = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            NoticeDeadlineBox.Text = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            RefreshAudit("Contract administration ready.");
        }

        private void RegisterEvent_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            try
            {
                var id = RequireText(EventIdBox.Text, "Event ID");
                var evidence = SessionEvidence("event", id);
                if (_event == null || !string.Equals(_event.EventId, id, StringComparison.Ordinal))
                {
                    _event = ContractEventRecord.Create(
                        id,
                        "R1",
                        RequireText(EventTypeBox.Text, "Event type"),
                        RequireText(EventTitleBox.Text, "Event title"),
                        RequireText(EventDescriptionBox.Text, "Event description"),
                        ParseUtcDate(EventDateBox.Text, "Event date"),
                        RequireText(ResponsiblePartyBox.Text, "Responsible party"),
                        evidence);
                }
                else
                {
                    _event = _event.Revise(
                        NextRevision(_event.Revision.RevisionId),
                        RequireText(EventTypeBox.Text, "Event type"),
                        RequireText(EventTitleBox.Text, "Event title"),
                        RequireText(EventDescriptionBox.Text, "Event description"),
                        ParseUtcDate(EventDateBox.Text, "Event date"),
                        RequireText(ResponsiblePartyBox.Text, "Responsible party"),
                        evidence);
                }

                _workflow.AppendEvent(_event, Actor(), "Register/revise contract event", DateTime.UtcNow);
                RefreshAudit("Event " + _event.EventId + " recorded at " + _event.Revision.RevisionId + ".");
            }
            catch (Exception ex)
            {
                ReportFailure("Unable to register contract event", ex);
            }
        }

        private void CreateNotice_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            try
            {
                if (_event == null) throw new InvalidOperationException("Register an event before creating a notice.");
                var id = RequireText(NoticeIdBox.Text, "Notice ID");
                if (_notice != null && string.Equals(_notice.NoticeId, id, StringComparison.Ordinal))
                    throw new InvalidOperationException("The current notice already exists; complete it before starting another notice identity.");

                _notice = ContractNoticeRecord.CreatePending(
                    id,
                    "R1",
                    _event.Revision,
                    SelectedDirection(),
                    RequireText(NoticeClauseBox.Text, "Contract clause"),
                    ParseUtcDate(NoticeDeadlineBox.Text, "Notice deadline"),
                    RequireText(NoticeFromBox.Text, "From party"),
                    RequireText(NoticeToBox.Text, "To party"),
                    "Pending contractual notice",
                    SessionEvidence("notice", id));
                _workflow.AppendNotice(_notice, Actor(), "Create pending contractual notice", DateTime.UtcNow);
                RefreshAudit("Notice " + id + " pending; deadline status " + _notice.DeadlineStatus(DateTime.UtcNow) + ".");
            }
            catch (Exception ex)
            {
                ReportFailure("Unable to create contractual notice", ex);
            }
        }

        private void IssueNotice_Click(object sender, RoutedEventArgs e)
        {
            CompleteNotice(true);
        }

        private void ReceiveNotice_Click(object sender, RoutedEventArgs e)
        {
            CompleteNotice(false);
        }

        private void CompleteNotice(bool issue)
        {
            if (!EnsureActive()) return;
            try
            {
                if (_notice == null) throw new InvalidOperationException("Create a pending notice first.");
                var revision = NextRevision(_notice.Revision.RevisionId);
                _notice = issue
                    ? _notice.MarkIssued(revision, DateTime.UtcNow, "Issued contractual notice", SessionEvidence("notice", _notice.NoticeId))
                    : _notice.MarkReceived(revision, DateTime.UtcNow, "Received contractual notice", SessionEvidence("notice", _notice.NoticeId));
                _workflow.AppendNotice(_notice, Actor(), issue ? "Issue contractual notice" : "Receive contractual notice", DateTime.UtcNow);
                RefreshAudit("Notice " + _notice.NoticeId + " " + _notice.State + "; deadline status " + _notice.DeadlineStatus(DateTime.UtcNow) + ".");
            }
            catch (Exception ex)
            {
                ReportFailure(issue ? "Unable to issue contractual notice" : "Unable to receive contractual notice", ex);
            }
        }

        private void CreateEot_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            try
            {
                if (_event == null || _notice == null || _notice.State == ContractNoticeState.Pending)
                    throw new InvalidOperationException("Register the event and complete its contractual notice before creating an EOT submission.");
                var id = RequireText(EotIdBox.Text, "EOT ID");
                if (_eot != null && string.Equals(_eot.EotId, id, StringComparison.Ordinal))
                    throw new InvalidOperationException("The current EOT identity already exists.");

                _eot = EotSubmissionRecord.CreateDraft(
                    id,
                    "R1",
                    _event.Revision,
                    _notice.Revision,
                    ParsePositiveInt(RequestedDaysBox.Text, "Requested days"),
                    "Extension-of-time submission linked to the current event and notice revisions.",
                    SessionEvidence("eot", id));
                _workflow.AppendEot(_eot, Actor(), "Create EOT draft", DateTime.UtcNow);
                RefreshAudit("EOT " + id + " created as Draft.");
            }
            catch (Exception ex)
            {
                ReportFailure("Unable to create EOT submission", ex);
            }
        }

        private void AdvanceEot_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            try
            {
                if (_eot == null) throw new InvalidOperationException("Create an EOT submission first.");
                var nextRevision = NextRevision(_eot.Revision.RevisionId);
                switch (_eot.Status)
                {
                    case EotAdministrationStatus.Draft:
                        _eot = _eot.Submit(nextRevision);
                        break;
                    case EotAdministrationStatus.Submitted:
                        _eot = _eot.StartReview(nextRevision);
                        break;
                    case EotAdministrationStatus.UnderReview:
                        _eot = _eot.Assess(nextRevision, ParseNonNegativeInt(EotDecisionDaysBox.Text, "Assessed days"), "Assessment recorded in the contract administration workspace.");
                        break;
                    case EotAdministrationStatus.Assessed:
                        _eot = _eot.Decide(nextRevision, ParseNonNegativeInt(EotDecisionDaysBox.Text, "Decided days"), "Decision recorded in the contract administration workspace.");
                        break;
                    default:
                        throw new InvalidOperationException("The EOT submission is already decided.");
                }

                _workflow.AppendEot(_eot, Actor(), "Advance EOT lifecycle to " + _eot.Status, DateTime.UtcNow);
                RefreshAudit("EOT " + _eot.EotId + " advanced to " + _eot.Status + ".");
            }
            catch (Exception ex)
            {
                ReportFailure("Unable to advance EOT lifecycle", ex);
            }
        }

        private void CreateClaim_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            try
            {
                if (_event == null || _notice == null || _notice.State == ContractNoticeState.Pending)
                    throw new InvalidOperationException("Register the event and complete its contractual notice before creating a claim record.");
                var id = RequireText(ClaimIdBox.Text, "Claim ID");
                if (_claim != null && string.Equals(_claim.ClaimId, id, StringComparison.Ordinal))
                    throw new InvalidOperationException("The current claim identity already exists.");

                _claim = ContractClaimRecord.CreateDraft(
                    id,
                    "R1",
                    _event.Revision,
                    _notice.Revision,
                    _eot?.Revision,
                    RequireText(ClaimTypeBox.Text, "Claim type"),
                    RequireText(ClaimNarrativeBox.Text, "Claim narrative"),
                    SessionEvidence("claim", id));
                _workflow.AppendClaim(_claim, Actor(), "Create claim record", DateTime.UtcNow);
                RefreshAudit("Claim " + id + " created as Draft.");
            }
            catch (Exception ex)
            {
                ReportFailure("Unable to create claim record", ex);
            }
        }

        private void AdvanceClaim_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureActive()) return;
            try
            {
                if (_claim == null) throw new InvalidOperationException("Create a claim record first.");
                var nextRevision = NextRevision(_claim.Revision.RevisionId);
                switch (_claim.Status)
                {
                    case ContractClaimStatus.Draft:
                        _claim = _claim.Submit(nextRevision);
                        break;
                    case ContractClaimStatus.Submitted:
                        _claim = _claim.Review(nextRevision, "Entitlement and supporting evidence reviewed.");
                        break;
                    case ContractClaimStatus.Reviewed:
                        _claim = _claim.Decide(nextRevision, "Contract-administration decision recorded without settlement recalculation.");
                        break;
                    default:
                        throw new InvalidOperationException("The claim record is already decided.");
                }

                _workflow.AppendClaim(_claim, Actor(), "Advance claim lifecycle to " + _claim.Status, DateTime.UtcNow);
                RefreshAudit("Claim " + _claim.ClaimId + " advanced to " + _claim.Status + ".");
            }
            catch (Exception ex)
            {
                ReportFailure("Unable to advance claim lifecycle", ex);
            }
        }

        private bool EnsureActive()
        {
            try
            {
                var active = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (!ReferenceEquals(active, _document))
                {
                    Report("Contract administration is bound to another drawing. Reactivate its drawing or reopen the command.");
                    return false;
                }
                var database = _document.Database;
                if (database == null || database.UnmanagedObject == IntPtr.Zero || database.UnmanagedObject != _nativeDatabaseIdentity)
                {
                    Report("Contract administration drawing generation is stale. Close and reopen the workspace.");
                    return false;
                }
                return true;
            }
            catch
            {
                Report("Contract administration cannot validate the active drawing generation.");
                return false;
            }
        }

        private ContractNoticeDirection SelectedDirection()
        {
            if (NoticeDirectionBox.SelectedItem is ComboBoxItem item && string.Equals(item.Content?.ToString(), "Incoming", StringComparison.Ordinal))
                return ContractNoticeDirection.Incoming;
            return ContractNoticeDirection.Outgoing;
        }

        private IReadOnlyList<ContractEvidenceRef> SessionEvidence(string kind, string id)
        {
            return new[] { new ContractEvidenceRef("UI-" + kind.ToUpperInvariant() + "-" + id, "session-reference", "BricsCAD modeless contract administration entry") };
        }

        private string Actor() => RequireText(ActorBox.Text, "Actor");

        private static string RequireText(string? value, string label)
        {
            var candidate = value ?? string.Empty;
            if (candidate.Length == 0 || !string.Equals(candidate, candidate.Trim(), StringComparison.Ordinal))
                throw new ArgumentException(label + " is required and must be canonically trimmed.");
            return candidate;
        }

        private static DateTime ParseUtcDate(string? value, string label)
        {
            if (!DateTime.TryParseExact(
                    value ?? string.Empty,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
                throw new ArgumentException(label + " must use yyyy-MM-dd.");
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }

        private static int ParsePositiveInt(string? value, string label)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
                throw new ArgumentException(label + " must be a positive integer.");
            return parsed;
        }

        private static int ParseNonNegativeInt(string? value, string label)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
                throw new ArgumentException(label + " must be a non-negative integer.");
            return parsed;
        }

        private static string NextRevision(string current)
        {
            if (current != null && current.Length > 1 && current[0] == 'R' &&
                int.TryParse(current.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number >= 0)
                return "R" + (number + 1).ToString(CultureInfo.InvariantCulture);
            throw new InvalidOperationException("Contract administration revision ids must use R<number> for the V25 workspace.");
        }

        private void RefreshAudit(string status)
        {
            StatusText.Text = status;
            AuditList.Items.Clear();
            var events = _workflow.AuditLog.Events;
            for (var i = 0; i < events.Count; i++)
            {
                var item = events[i];
                AuditList.Items.Add(item.OccurredUtc.ToString("u", CultureInfo.InvariantCulture) + "  " + item.Action + "  " + item.EntityId + "  " + item.AfterSummary);
            }
            Report(status);
        }

        private void ReportFailure(string prefix, Exception exception)
        {
            RefreshAudit(prefix + ": " + exception.GetType().Name + ". Check identity, lifecycle and revision links.");
        }

        private void Report(string message)
        {
            try { _document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
