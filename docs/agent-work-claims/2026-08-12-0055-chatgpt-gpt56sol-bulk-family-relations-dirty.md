# Work claim — Bulk family relation dirty completeness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-bulk-family-relations-dirty`
- Registered: `2026-08-12T00:55:00+07:00`
- Last Updated: `2026-08-12T00:55:00+07:00`
- Baseline main SHA: `c6acf7a3b338cd94dc4de58103f2b141d6508490`
- Priority: deterministic semantic freshness mismatch found during owner-requested continue-all audit
- Task Key: `CORE-BULK-FAMILY-RELATIONS-DIRTY`

## Confirmed defect

`BulkEditService.AssignFamily(...)` changes each selected element's semantic `FamilyId`, but its dirty flags currently start with `Properties | Quantity` and only conditionally add `Geometry`. The operation therefore omits `ElementDirtyFlags.Relations` even though Family identity is a semantic relation.

The canonical project-aware `ProjectFamilyService.Assign(...)` marks a real Family reassignment with `ElementDirtyFlags.All`, which includes `Relations`. Downstream consumers that use the relation dirty bit can therefore observe inconsistent freshness depending on which supported Core API performed the same Family relation mutation.

## Reserved scope

Add `Relations` to the dirty flags emitted by real `BulkEditService.AssignFamily(...)` mutations while preserving:

- existing canonical same-family no-op behavior;
- inherited/override property transfer semantics;
- existing Quantity dirty behavior;
- existing conditional Geometry dirty behavior;
- existing batch preflight/atomicity, ownership, target bound and dangling-family guards.

## Expected surfaces

- `src/QS3D.Core/Services/BulkEditService.cs`
- one focused isolated Core smoke under `tests/QS3D.Core.SmokeTests/`
- module-initializer registration for that smoke if needed
- this claim file

## Coordination / exclusions

- Do **not** modify `ProjectFamilyService.cs`; the active Family assignment/null-target lane owns that canonical service surface.
- Do not modify previous Bulk Edit target-bound/null-target/canonicalization smokes or their preflight scripts.
- No WPF/native selection UI, BricsCAD adapter/runtime, persistence schema, quantity engine or family catalog architecture changes.
- No GitHub Actions/build/release dispatch and no licensed BricsCAD runtime PASS claim.

## Validation plan

- A real bulk Family reassignment sets `Relations | Properties | Quantity` at minimum.
- If the Family/property change affects generated geometry, existing `Geometry` dirty behavior remains present.
- A canonical same-family assignment remains a true no-op and must not introduce Relations dirty or project version movement.
- Batch validation/atomicity semantics remain unchanged.
- Re-fetch current source after claim publication, review exact PR diff against moving `main`, and read back merge commit/source. Do not claim smoke execution unless actually run.

## Completion condition

Current `main` marks a FamilyId mutation through `BulkEditService.AssignFamily(...)` as relation-dirty consistently with the canonical Family service, with focused deterministic regression source and exact merge evidence.
