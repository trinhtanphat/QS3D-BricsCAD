# Work claim — revision snapshot XML text preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-revision-snapshot-xml-text-preflight-20260812-0911`
- Registered: `2026-08-12T09:11:00+07:00`
- Baseline main SHA: `82d9cb6422e11ef862bc67e5ab3c7dd349342857`
- Priority: evidence-driven remote-safe revision persistence integrity

## Confirmed defect

`RevisionSnapshotStore.Save(...)` calls `ValidateSnapshot(...)` before filesystem mutation, but current validation only checks canonical whitespace/enum/numeric invariants. Strings are then written directly into XML attributes. XML-invalid control characters or malformed surrogate sequences can therefore pass preflight and fail later during `XDocument` serialization, after the destination directory/temp workflow has begun. Property values are especially exposed because current map validation checks keys only.

## Reserved scope

- Validate XML character legality for revision identity/reference/key/list strings during existing snapshot preflight.
- Validate property values (with null preserving the existing empty-string serialization semantics) before filesystem mutation.
- Fail closed with `InvalidDataException`; do not sanitize or rewrite semantic content.
- Preserve valid supplementary Unicode and all existing schema/ordering/backup behavior.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs`
- one focused `QS3D.Core.SmokeTests` regression file with ModuleInitializer
- this claim file

## Excluded scope

- `QuantityRevisionReport.cs` and its current readonly-result claim.
- Revision semantic compare/capture behavior, snapshot backup policy, XML schema shape, quantities, UI/native runtime, release/signing.
- No GitHub Actions or LOCAL_ONLY qualification.

## Validation plan

- XML-invalid revision ID fails before destination directory creation.
- XML-invalid property value fails before destination directory creation.
- A malformed lone surrogate property value fails closed.
- Valid supplementary Unicode property text remains serializable/round-trippable.
- Exact branch diff and moving-main source blob are rechecked before integration.

## Completion condition

Focused source/regression are merged to current `main`, remote source/test are re-read, and this claim is closed `COMPLETED` with exact integration evidence.
