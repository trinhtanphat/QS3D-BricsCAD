# Work claim — Bulk family property canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-bulk-family-property-canonicality`
- Registered: `2026-08-12T07:46:00+07:00`
- Last Updated: `2026-08-12T07:46:00+07:00`
- Baseline main SHA: `0696f3cbcf602e140c3cad23282160641f2e659d`
- Priority: deterministic Core assignment-integrity mismatch found during owner-requested evidence-driven audit
- Task Key: `CORE-BULK-FAMILY-PROPERTY-CANONICALITY`

## Confirmed defect

`ProjectFamilyService.Assign(...)` snapshots and validates target Family defaults plus every previous Family's properties before `ProjectState.Touch()` or instance mutation. It rejects non-canonical property keys and over-bound values so corrupt legacy Family defaults cannot propagate into semantic elements.

`BulkEditService.AssignFamily(...)` currently bypasses that canonical boundary: it iterates `family.Properties` and `previousFamily.Properties` directly, computes inherited keys from raw dictionaries, then writes raw target Family entries directly into `element.Properties`. A malformed Family property that the canonical assignment API rejects can therefore be propagated by the supported BulkEdit assignment path while advancing project revision.

## Reserved scope

Align `BulkEditService.AssignFamily(...)` with the existing canonical Family-property snapshot contract by reusing `ProjectFamilyService.SnapshotProperties(...)` for the target Family and all previous Families before any mutation. Preserve current bulk ownership/category/dangling-family checks, canonical same-Family no-op, inherited/default/override behavior, relation dirty flags, conditional geometry dirty flags and all-or-nothing executor boundary.

## Expected surfaces

- `src/QS3D.Core/Services/BulkEditService.cs`
- dedicated focused Core smoke + isolated registration
- this claim file

## Coordination / exclusions

- The completed Bulk Family relation-dirty lane (`847ee0f...`, closeout claim already `COMPLETED`) must remain intact.
- Do not modify `ProjectFamilyService.cs`; use its existing internal canonical `SnapshotProperties(...)` validator rather than duplicating policy.
- Do not modify WPF/native selection UI, persistence schema, Family create/rename/property editing, or unrelated BulkEdit property/numeric operations.
- Preserve canonical padded/case-varied same-Family no-op behavior exactly.
- Do not overwrite any other ACTIVE claim; no force-push, GitHub Actions/build/release dispatch, or runtime PASS claim.

## Validation plan

- Malformed target Family property key (for example padded key text) causes bulk assignment to fail before changing FamilyId, instance properties, dirty state or project persistence state.
- Malformed previous Family property key likewise fails before reassignment so inherited-default detection cannot consume corrupt metadata.
- Over-bound Family property values are rejected through the same canonical validator.
- Valid reassignment still preserves explicit instance overrides, removes inherited previous defaults absent from the target, applies canonical target defaults, marks the expected dirty flags and touches project exactly once.
- Re-fetch `main`, claim collision and exact source before each write; read back source/test afterward. Do not claim local smoke execution unless actually run.

## Completion condition

Every supported Core Family reassignment path, including BulkEdit, fails before mutation on malformed Family property defaults and shares the same canonical property validation contract.