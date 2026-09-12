# Contract administration workflow

Issue: #6532  
Ownership-Key: `qs.contract-administration-workflow-v1`

## Purpose

This slice adds a dedicated contract-administration register for contract events, contractual notices, extension-of-time (EOT) administration, and claim records. It reuses the existing commercial revision and audit authorities rather than introducing another settlement engine or project store.

The workflow is intentionally non-monetary. Variation valuation, interim payment certificates, final-account settlement, and their durable project persistence remain owned by the existing commercial lanes.

## Core authority

`ContractAdministrationWorkflow` admits immutable revisions for four linked registers:

1. `ContractEventRecord` — event identity, type, occurrence date, responsible party, evidence, and revision chain.
2. `ContractNoticeRecord` — outgoing/incoming notice state, contractual deadline, issue/receipt timestamp, evidence, and exact event revision.
3. `EotSubmissionRecord` — Draft → Submitted → UnderReview → Assessed → Decided, with requested/assessed/decided days and exact event/notice revisions.
4. `ContractClaimRecord` — Draft → Submitted → Reviewed → Decided, linked to exact event/notice revisions and optionally an exact EOT revision.

Every admitted revision is recorded through the existing `CommercialAuditLog` and `CommercialAuditRecord` contracts. Revision provenance uses the existing `CommercialRevisionRef` type.

## Deadline semantics

Pending notices expose deterministic deadline status for an explicit UTC `asOf` timestamp:

- `Open` before the contractual deadline day;
- `DueToday` on the UTC deadline date while still pending;
- `Overdue` after the deadline while still pending;
- `OnTime` when the notice was issued/received on or before the required timestamp;
- `Late` when completion occurred after the required timestamp.

No wall-clock lookup is hidden inside Core deadline evaluation.

## Revision and evidence rules

A logical entity may advance only from its latest admitted revision. Stale branches fail closed. Notice records must reference an event revision already admitted to the workflow; EOT records require admitted event and notice revisions; claim records require admitted event/notice revisions and, when present, an admitted EOT revision.

Supporting evidence is snapshotted through the existing commercial collection guards. The lane stores references only; durable document storage is outside this feature.

## BricsCAD V25 surface

Commands:

- `QS3DCONTRACTADMIN`
- `QS3DCLAIMS`

Both open the same modeless contract-administration workspace. The window binds to the exact managed `Document` and native database generation that created it and refuses edits when that drawing generation is no longer active.

The V25 surface is deliberately thin: it creates/revises Core records, asks Core for deadline/lifecycle state, and displays `CommercialAuditLog` events. It does not calculate Variation, IPC, or Final Account values.

## Persistence boundary

This carrier does not modify `CommercialQsWindow`, `ProjectMetadataDictionary`, `CommercialSettlementWorkspace`, or shared commercial persistence. The in-window workflow is session-scoped until the dedicated persistence owner integrates these immutable records into the canonical project store.

## Validation boundary

Hosted CI is expected to prove:

- source-boundary preflight;
- deterministic Core smoke coverage;
- Core compile;
- locked-reference BricsCAD V25 compile.

Hosted CI is not a licensed BricsCAD runtime/UI pass. A real modeless-runtime exercise remains `LOCAL_ONLY / NO_RESULT` until executed in a licensed BricsCAD host.
