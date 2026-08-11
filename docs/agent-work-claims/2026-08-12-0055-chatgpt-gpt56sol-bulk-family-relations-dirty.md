# Work claim — Bulk family relation dirty completeness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-bulk-family-relations-dirty`
- Registered: `2026-08-12T00:55:00+07:00`
- Last Updated: `2026-08-12T01:00:00+07:00`
- Baseline main SHA: `c6acf7a3b338cd94dc4de58103f2b141d6508490`
- Priority: deterministic semantic freshness mismatch found during owner-requested continue-all audit
- Task Key: `CORE-BULK-FAMILY-RELATIONS-DIRTY`
- Implementation PR: `#595`
- Implementation commit on `main`: `847ee0f25c530d0a61bc0fdb813a7d6786def6eb`

## Confirmed defect

`BulkEditService.AssignFamily(...)` changed each selected element's semantic `FamilyId`, but its dirty flags started with `Properties | Quantity` and only conditionally added `Geometry`. The operation therefore omitted `ElementDirtyFlags.Relations` even though Family identity is a semantic relation.

The canonical project-aware `ProjectFamilyService.Assign(...)` marks a real Family reassignment with `ElementDirtyFlags.All`, which includes `Relations`. Downstream consumers using the relation dirty bit could therefore observe inconsistent freshness depending on which supported Core API performed the same Family relation mutation.

## Implemented scope

Real `BulkEditService.AssignFamily(...)` changes now emit `Properties | Relations | Quantity`, with the existing conditional `Geometry` flag preserved for categories that require generated geometry.

Focused isolated smoke coverage verifies:

- ArchitecturalWall Family reassignment marks `Properties | Relations | Quantity | Geometry` and touches project revision once;
- Room Family reassignment marks `Properties | Relations | Quantity` without unnecessary Geometry dirty;
- canonical same-Family identity with a padded/case-varied stored relation remains a true no-op, preserving raw relation text, element freshness and project `ChangeVersion`.

## Surfaces changed

- `src/QS3D.Core/Services/BulkEditService.cs`
- `tests/QS3D.Core.SmokeTests/BulkFamilyRelationDirtySmoke.cs`
- `tests/QS3D.Core.SmokeTests/BulkFamilyRelationDirtySmokeRegistration.cs`
- this claim file

## Coordination / exclusions preserved

- `ProjectFamilyService.cs` was not modified.
- Previous Bulk Edit target-bound/null-target/canonicalization smokes and preflights were not modified.
- No WPF/native selection UI, BricsCAD adapter/runtime, persistence schema, quantity engine or family catalog architecture changed.
- No GitHub Actions/build/release workflow was dispatched and no licensed BricsCAD runtime PASS is claimed.

## Validation evidence

- Claim was published on `main` before source edits at commit `9aeec85f51756be5c978e757267f995ffc2d0e35`.
- Post-claim source readback confirmed `BulkEditService.cs` blob `310b3f89910e4c57e02128c58843036c2a1a15d7` still omitted `Relations` from the Family-assignment dirty mask.
- PR `#595` diff was reviewed before merge and contained exactly three intended files with `+107/-1`; the production behavior change is one dirty-mask expression.
- Server-side squash merge with expected head SHA produced `847ee0f25c530d0a61bc0fdb813a7d6786def6eb`.
- Local build/smoke execution is **not** claimed because this connector-only environment does not provide the project checkout/build runner.

## Completion

`COMPLETED`: current `main` now marks real bulk FamilyId changes as relation-dirty consistently with the canonical semantic relation contract while preserving existing geometry/no-op behavior.
