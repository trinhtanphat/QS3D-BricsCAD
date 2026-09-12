using System;
using System.Collections.Generic;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class ContractAdministrationWorkflowSmoke
    {
        public static void Run()
        {
            EventNoticeEotClaimLifecycleIsRevisionLinkedAndAudited();
            DeadlineStatusIsDeterministic();
            InvalidTransitionsAndUnknownLinksFailClosed();
            StaleRevisionChainFailsClosed();
        }

        private static void EventNoticeEotClaimLifecycleIsRevisionLinkedAndAudited()
        {
            var workflow = new ContractAdministrationWorkflow();
            var evidence = Evidence("EVID-001", "correspondence", "site-instruction-001.pdf");
            var eventRecord = ContractEventRecord.Create(
                "EVT-001",
                "R1",
                "delay",
                "Late design information",
                "Structural detail issued after the planned information date.",
                Utc(2026, 1, 1),
                "Contractor",
                new[] { evidence });
            workflow.AppendEvent(eventRecord, "QS", "Register contract event", Utc(2026, 1, 2));

            var pendingNotice = ContractNoticeRecord.CreatePending(
                "NTC-001",
                "R1",
                eventRecord.Revision,
                ContractNoticeDirection.Outgoing,
                "Clause 20.2",
                Utc(2026, 1, 7),
                "Contractor",
                "Engineer",
                "Notice of delay event",
                new[] { evidence });
            workflow.AppendNotice(pendingNotice, "QS", "Prepare contractual notice", Utc(2026, 1, 2));

            var issuedNotice = pendingNotice.MarkIssued(
                "R2",
                Utc(2026, 1, 5),
                "NTC-001 issued by email and CDE",
                new[] { evidence });
            workflow.AppendNotice(issuedNotice, "QS", "Issue contractual notice", Utc(2026, 1, 5));
            Equal(ContractDeadlineStatus.OnTime, issuedNotice.DeadlineStatus(Utc(2026, 1, 9)), "Issued notice must remain on-time after completion.");

            var eotDraft = EotSubmissionRecord.CreateDraft(
                "EOT-001",
                "R1",
                eventRecord.Revision,
                issuedNotice.Revision,
                10,
                "Ten calendar days requested for critical delay.",
                new[] { evidence });
            workflow.AppendEot(eotDraft, "QS", "Create EOT submission", Utc(2026, 1, 6));
            var eotSubmitted = eotDraft.Submit("R2");
            workflow.AppendEot(eotSubmitted, "QS", "Submit EOT", Utc(2026, 1, 7));
            var eotReview = eotSubmitted.StartReview("R3");
            workflow.AppendEot(eotReview, "Engineer", "Start EOT review", Utc(2026, 1, 8));
            var eotAssessed = eotReview.Assess("R4", 8, "Eight days supported by the programme evidence.");
            workflow.AppendEot(eotAssessed, "Engineer", "Assess EOT", Utc(2026, 1, 9));
            var eotDecided = eotAssessed.Decide("R5", 7, "Seven days granted after concurrency assessment.");
            workflow.AppendEot(eotDecided, "Contract Administrator", "Decide EOT", Utc(2026, 1, 10));
            Equal(EotAdministrationStatus.Decided, eotDecided.Status, "EOT lifecycle must end in Decided.");
            Equal(7, eotDecided.DecidedDays!.Value, "EOT decision days mismatch.");

            var claimDraft = ContractClaimRecord.CreateDraft(
                "CLM-001",
                "R1",
                eventRecord.Revision,
                issuedNotice.Revision,
                eotDecided.Revision,
                "time-related",
                "Claim record linked to event, notice and EOT evidence without monetary recalculation.",
                new[] { evidence });
            workflow.AppendClaim(claimDraft, "QS", "Create claim record", Utc(2026, 1, 11));
            var claimSubmitted = claimDraft.Submit("R2");
            workflow.AppendClaim(claimSubmitted, "QS", "Submit claim record", Utc(2026, 1, 12));
            var claimReviewed = claimSubmitted.Review("R3", "Entitlement and evidence reviewed.");
            workflow.AppendClaim(claimReviewed, "Commercial Manager", "Review claim", Utc(2026, 1, 13));
            var claimDecided = claimReviewed.Decide("R4", "Claim administration decision recorded; settlement remains outside this workflow.");
            workflow.AppendClaim(claimDecided, "Contract Administrator", "Decide claim", Utc(2026, 1, 14));

            Equal(ContractClaimStatus.Decided, claimDecided.Status, "Claim lifecycle must end in Decided.");
            Equal("contract-eot", claimDecided.EotRevision!.SourceKind, "Claim must retain EOT revision provenance.");
            Equal("R5", claimDecided.EotRevision.RevisionId, "Claim must retain exact EOT decision revision.");
            Equal(12, workflow.AuditLog.Events.Count, "Every accepted contract-administration revision must append one audit record.");
            var lastAudit = workflow.AuditLog.Events[workflow.AuditLog.Events.Count - 1];
            Equal("contract-claim", lastAudit.EntityType, "Claim audit entity type mismatch.");
            Equal(4, lastAudit.SourceRevisions.Count, "Claim audit must preserve event, notice, EOT and claim revision provenance.");
        }

        private static void DeadlineStatusIsDeterministic()
        {
            var eventRevision = new CommercialRevisionRef("contract-event", "EVT-D", "R1");
            var pending = ContractNoticeRecord.CreatePending(
                "NTC-D",
                "R1",
                eventRevision,
                ContractNoticeDirection.Outgoing,
                "Clause 8.4",
                Utc(2026, 2, 10),
                "Contractor",
                "Engineer",
                "Delay notice",
                Array.Empty<ContractEvidenceRef>());

            Equal(ContractDeadlineStatus.Open, pending.DeadlineStatus(Utc(2026, 2, 9)), "Pending notice before deadline must be Open.");
            Equal(ContractDeadlineStatus.DueToday, pending.DeadlineStatus(Utc(2026, 2, 10)), "Pending notice on deadline date must be DueToday.");
            Equal(ContractDeadlineStatus.Overdue, pending.DeadlineStatus(Utc(2026, 2, 11)), "Pending notice after deadline must be Overdue.");

            var late = pending.MarkIssued("R2", Utc(2026, 2, 11), "Late notice", Array.Empty<ContractEvidenceRef>());
            Equal(ContractDeadlineStatus.Late, late.DeadlineStatus(Utc(2026, 2, 12)), "Completed notice after deadline must be Late.");
        }

        private static void InvalidTransitionsAndUnknownLinksFailClosed()
        {
            var evidence = Evidence("EVID-X", "letter", "letter-x.pdf");
            var eventRecord = ContractEventRecord.Create(
                "EVT-X",
                "R1",
                "instruction",
                "Instruction",
                "Instruction with time impact.",
                Utc(2026, 3, 1),
                "Engineer",
                new[] { evidence });
            var incoming = ContractNoticeRecord.CreatePending(
                "NTC-X",
                "R1",
                eventRecord.Revision,
                ContractNoticeDirection.Incoming,
                "Clause 3.5",
                Utc(2026, 3, 5),
                "Engineer",
                "Contractor",
                "Incoming notice",
                new[] { evidence });

            Expect<InvalidOperationException>(
                () => incoming.MarkIssued("R2", Utc(2026, 3, 2), "wrong direction", new[] { evidence }),
                "Incoming notice must not be marked Issued.");

            var eot = EotSubmissionRecord.CreateDraft(
                "EOT-X",
                "R1",
                eventRecord.Revision,
                incoming.Revision,
                5,
                "Five days requested.",
                new[] { evidence });
            Expect<InvalidOperationException>(
                () => eot.StartReview("R2"),
                "Draft EOT must not skip submission.");
            Expect<InvalidOperationException>(
                () => eot.Decide("R2", 1, "Invalid direct decision"),
                "Draft EOT must not skip review and assessment.");

            var workflow = new ContractAdministrationWorkflow();
            Expect<InvalidOperationException>(
                () => workflow.AppendNotice(incoming, "QS", "Unknown event should fail", Utc(2026, 3, 2)),
                "Notice with an event revision not admitted to the register must fail closed.");
        }

        private static void StaleRevisionChainFailsClosed()
        {
            var workflow = new ContractAdministrationWorkflow();
            var eventR1 = ContractEventRecord.Create(
                "EVT-R",
                "R1",
                "change",
                "Change event",
                "Initial event record.",
                Utc(2026, 4, 1),
                "Employer",
                Array.Empty<ContractEvidenceRef>());
            workflow.AppendEvent(eventR1, "QS", "Initial event", Utc(2026, 4, 1));

            var eventR2 = eventR1.Revise(
                "R2",
                "change",
                "Change event revised",
                "Updated particulars.",
                Utc(2026, 4, 1),
                "Employer",
                Array.Empty<ContractEvidenceRef>());
            workflow.AppendEvent(eventR2, "QS", "Revise event", Utc(2026, 4, 2));

            var staleR3 = eventR1.Revise(
                "R3",
                "change",
                "Stale branch",
                "Revision incorrectly linked to R1.",
                Utc(2026, 4, 1),
                "Employer",
                Array.Empty<ContractEvidenceRef>());
            Expect<InvalidOperationException>(
                () => workflow.AppendEvent(staleR3, "QS", "Reject stale branch", Utc(2026, 4, 3)),
                "Revision chain must link to the latest admitted revision.");
        }

        private static ContractEvidenceRef Evidence(string id, string kind, string reference) =>
            new ContractEvidenceRef(id, kind, reference);

        private static DateTime Utc(int year, int month, int day) =>
            new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Expect<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new Exception(message);
        }
    }
}
