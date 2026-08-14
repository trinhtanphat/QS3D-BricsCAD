# Work claim — Quantity report FamilyId identity parity

- Status: `ACTIVE`
- Agent: `gpt56sol-rev-familyid-parity-20260814-0804`
- Registered: `2026-08-14T08:04:00+07:00`
- Baseline main SHA: `a82b3c993579d00643bfdad862a4cd6d6610a582`
- Priority: REV/report correctness — prevent false BQ revision changes from identity casing only.

## Confirmed defect

`RevisionService.Compare()` treats `FamilyId` as semantic identity with `StringComparison.OrdinalIgnoreCase`, but `QuantityReportRevisionService.ChangedFields()` currently routes report-row `FamilyId` through the exact ordinal text comparison used for descriptive fields. A case-only representation change of the same Family identity can therefore create a `Changed` BQ revision row even when the authoritative semantic identity comparison reports no FamilyId change.

## Reserved scope

- `src/QS3D.Core/Revisions/QuantityReportRevisionReview.cs`
- `tests/QS3D.Core.SmokeTests/QuantityReportRevisionReviewSmoke.cs`
- this claim file only

## Intended change

Compare only report-row `FamilyId` with case-insensitive identity semantics, matching `RevisionService`; preserve exact text comparison for user-visible Floor/Zone/family/name/material/note fields and preserve all quantity tolerances, stable-key rules and semantic-delta authority. Add focused regression for case-only Family identity representation.

## Excluded scope

- `ProjectFamilyService` / current Family assignment claim
- SourceHandle / Source Reconcile / LOCAL_ONLY native qualification
- MAP/QSDB persistence, IFC/interchange, Cost, V25/V26 UI and release automation
- any normalization of descriptive report text

## Validation plan

- add deterministic focused smoke coverage proving case-only `FamilyId` does not create a report-row change while a real FamilyId change still does
- preserve existing `QuantityReportRevisionReviewSmoke` behavior and source contract
- inspect remote diff and available commit status after push; do not claim unavailable native/runtime validation

## Coordination

Current Family assignment member-map claim reserves only `ProjectFamilyService.cs` and its dedicated smoke and explicitly excludes Revision. V25 release automation, Rebar procurement and IFC-02B are separate. Recheck live `main` and exact-path claims immediately before every write.

## Completion condition

Minimal identity-parity fix and regression are pushed on current `main`, remote readback confirms them, and this claim is updated to `COMPLETED` with actual validation evidence.
