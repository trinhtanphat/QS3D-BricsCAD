# Work claim — SourceHandleResolver duplicate SourceHandles

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-source-handle-resolver-duplicates-20260812-1417`
- Registered: `2026-08-12T14:17:00+07:00`
- Erroneously cancelled: `2026-08-12T14:46:00+07:00`
- Reactivated: `2026-08-12T14:47:00+07:00`
- Priority: P1 ownership integrity parity

## Confirmed defect

The production resolver is `src/QS3D.Core/Services/SourceHandleResolver.cs`. Its `AddDirectHandles()` validates blank/non-canonical entries but merges every direct source handle directly into one traversal-wide case-insensitive `knownHandles` set. Therefore duplicate `ProjectElement.SourceHandles` entries in the same element, including case aliases such as `ABCD` + `abcd`, are silently deduplicated instead of failing closed as malformed ownership data.

## Cancellation correction

Commit `131b13a7fd13717132bfa9507f1b133c48d8c3d7` cancelled this claim after checking the stale/nonexistent path `src/QS3D.Core/Rooms/SourceHandleResolver.cs`. Git history and PR #784 identify the actual production path as `src/QS3D.Core/Services/SourceHandleResolver.cs`, and direct `main` readback confirms the defect remains present there. The cancellation premise was therefore invalid, so this claim is reactivated rather than replaced by a new overlapping lane.

## Reserved scope

- `src/QS3D.Core/Services/SourceHandleResolver.cs`
- `tests/QS3D.Core.SmokeTests/SourceHandleResolverSafetySmoke.cs`
- this claim file

## Intended contract

Reject duplicate `SourceHandles` within the same semantic element using ordinal-ignore-case identity before merging that element's handles into the traversal-wide resolved-handle set. Preserve existing cross-element deduplication, direct/boundary/generated precedence, dependency traversal and deterministic ordering. Error text should identify the first and current duplicate indices so malformed ownership remains diagnosable.

## Validation boundary

Extend the existing auto-registered `SourceHandleResolverSafetySmoke` with exact-duplicate, case-alias duplicate and unique-handle controls. Source-safe readback only unless an executable local smoke is actually run; no GitHub Actions/full build/licensed BricsCAD runtime PASS is claimed without execution.
