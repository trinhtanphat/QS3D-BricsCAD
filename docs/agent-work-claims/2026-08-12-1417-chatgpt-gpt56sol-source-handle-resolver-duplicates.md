# Work claim — SourceHandleResolver duplicate SourceHandles

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-source-handle-resolver-duplicates-20260812-1417`
- Registered: `2026-08-12T14:17:00+07:00`
- Priority: P1 ownership integrity parity

## Confirmed defect

`ProjectElement.SourceHandles` does not enforce uniqueness. `SourceHandleResolver` currently accumulates per-element source handles through a case-insensitive `HashSet`, which can silently deduplicate direct duplicates such as `ABCD` + `ABCD` or case aliases such as `ABCD` + `abcd`. The sibling semantic ownership path treats duplicate source handles as ownership corruption and fails closed, so resolver behavior is inconsistent with the established model invariant.

## Reserved scope

- `src/QS3D.Core/Rooms/SourceHandleResolver.cs`
- one focused Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

Reject duplicate `SourceHandles` within the same element using ordinal-ignore-case identity before merging that element's handles into the resolver ownership set. Preserve existing cross-element ownership semantics and semantic-handle behavior. Error text should identify the first and current duplicate indices so corruption remains diagnosable.

## Validation boundary

Add focused regression coverage for exact and case-varied duplicates plus a unique-handle control. Source-safe readback only unless an executable local smoke is actually run; no GitHub Actions/full build/BricsCAD runtime PASS claimed without execution.
