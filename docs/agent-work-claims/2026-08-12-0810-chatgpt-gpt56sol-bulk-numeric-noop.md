# Work claim — Bulk numeric lexical no-op preservation

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-bulk-numeric-noop`
- Registered: `2026-08-12T08:10:00+07:00`
- Last Updated: `2026-08-12T08:10:00+07:00`
- Baseline main SHA: `68517455c46f688a74f4a1d6632c9b93e8d4bb3a`
- Priority: deterministic Core freshness/no-op mismatch found during owner-requested `continue all`
- Task Key: `CORE-BULK-NUMERIC-NOOP-LEXICAL-PRESERVATION`

## Confirmed defect

`BulkEditService.MultiplyNumericProperty(...)` currently formats the multiplied `double` and decides whether the operation is a no-op by comparing that formatted text with the stored property text. A numerically unchanged value such as explicit instance `WidthM="1.0"` multiplied by `1d` therefore becomes `"1"`, is reported as changed, marks semantic/geometry freshness dirty and advances `ProjectState.ChangeVersion` even though the numeric value did not change.

The separate selection-oriented `SemanticSelectionBulkEditService` already established the correct exact-numeric no-op contract in merged PR #633 / `4e08d2c671039ee7509ccd5bc51db8495ef52248`: compare the computed numeric value with the parsed numeric value before formatting, preserving lexical text and inherited-state semantics when the number is unchanged. This lane aligns the distinct legacy/Core `BulkEditService` API without modifying the selection implementation.

## Reserved scope

Change only `BulkEditService.MultiplyNumericProperty(...)` no-op detection so exact numeric equality (`next.Equals(current)`) returns no mutation before `ToString("R", ...)` formatting. Preserve all existing editable-key validation, bounded target materialization, parsing, non-finite/overflow rejection, all-or-nothing mutation executor, dirty-flag policy and real multiplication behavior.

## Expected surfaces

- `src/QS3D.Core/Services/BulkEditService.cs`
- one focused CAD-independent Core smoke source under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- `src/QS3D.Core/Selection/SemanticSelectionBulkEditService.cs` and PR #633 behavior.
- `BulkEditService.AssignFamily(...)`, Family property canonicality and previous completed Bulk Family lanes.
- `SetProperty(...)`, property presence semantics, persistence schemas, adapters/WPF/native BricsCAD runtime.
- tolerance-based equality; only exact parsed/computed `double` equality is owned here.
- GitHub Actions, build/release dispatch, V25/V26 runtime qualification.

## Validation plan

- Explicit instance `WidthM="1.0"` x1 returns zero changed ids, preserves exact lexical text, project `ChangeVersion`/`UpdatedUtc`, element `Dirty`/`UpdatedUtc`, and does not create generated-geometry stale state.
- A non-geometry editable numeric property with lexical text such as `Scale="1.0"` x1 is likewise a complete no-op.
- A real `WidthM` x2 change still formats the computed value, returns the changed element id, advances project revision exactly once and marks the existing expected dirty flags.
- Existing parse, non-finite and overflow behavior remains unchanged by source review; no runtime PASS is claimed without execution.
- Re-fetch moving `main` and the exact target blob after claim publication and immediately before integration; review the exact PR diff before merge.

## Coordination

The completed `CORE-BULK-FAMILY-PROPERTY-CANONICALITY` claim explicitly excluded unrelated BulkEdit property/numeric operations. PR #633 applies to the separate Selection service. No current open PR or discovered active claim owns this `BulkEditService.MultiplyNumericProperty(...)` no-op contract at registration time.

## Completion condition

Current `main` preserves lexical text and freshness for exact numeric BulkEdit no-ops while retaining real numeric mutation semantics, with focused regression source and this claim marked `COMPLETED` with exact integration evidence.
