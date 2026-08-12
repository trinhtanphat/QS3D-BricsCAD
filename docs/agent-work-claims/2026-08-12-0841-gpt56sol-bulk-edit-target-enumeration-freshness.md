# Work claim — Bulk edit target enumeration freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bulk-edit-target-enumeration-freshness-20260812-0841`
- Registered: `2026-08-12T08:41:00+07:00`
- Baseline main SHA: `5b366b0ee39af8fbfed1a05cbd91a10093d7f86d`
- Priority: P1 evidence-backed Core mutation freshness at a remote-safe boundary

## Confirmed defect

`BulkEditService` enumerates caller-controlled target sequences while building its pending mutation plan, before `ProjectSemanticMutationExecutor` captures the rollback snapshot. A lazy target enumerable can advance `ProjectState.ChangeVersion` while yielding otherwise valid targets; the bulk edit then silently accepts that changed project as the executor baseline and applies an additional mutation. Target enumeration is executable caller code, so a version change during enumeration must fail closed before any bulk-edit mutation is applied.

## Reserved scope

Guard only project-version freshness across caller-controlled BulkEdit target enumeration. Cover object-target `SetProperty` / `MultiplyNumericProperty` and id-target `SetProperty` / `AssignFamily` paths without changing existing target bounds, ownership, property canonicality, no-op, family inheritance or dirty-flag semantics.

## Expected surfaces

- `src/QS3D.Core/Services/BulkEditService.cs`
- focused CAD-independent smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- No changes to `RegenerationEngine` or its active dirty-subset freshness claim.
- No redesign of `ProjectSemanticMutationExecutor`, rollback snapshots, BulkEdit target limits, Family semantics, selection UI, persistence or BricsCAD adapter/runtime.
- No GitHub Actions dispatch and no BricsCAD V25/V26 runtime qualification.

## Validation plan

- A lazy `IEnumerable<ProjectElement>` that calls `project.Touch()` while yielding a valid target must fail before SetProperty or numeric multiplication changes the target.
- A lazy `IEnumerable<string>` that advances the project while yielding a valid id must fail before id-based SetProperty or Family assignment changes semantic state.
- Side-effect-free target collections must preserve existing behavior.
- Re-fetch the exact source after the claim push and before each write; preserve concurrent `main` history.

## Coordination

This reservation is intentionally disjoint from the ACTIVE Regeneration dirty-subset freshness lane: it owns only `BulkEditService` target enumeration and focused BulkEdit regression coverage. Previously completed BulkEdit numeric no-op, empty-property, family canonicality/dirty and target-bound lanes remain untouched.

## Completion condition

Completed when current `main` fails closed on project-version changes caused during BulkEdit target enumeration, focused regression coverage is committed, exact implementation/test SHAs are recorded here, and the claim is marked `COMPLETED` with truthful remote validation evidence.