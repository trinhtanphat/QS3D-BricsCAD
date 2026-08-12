# Work claim — ProjectFamilyService.Assign lazy-input freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-assign-input-freshness-20260812-1002`
- Registered: `2026-08-12T10:02:00+07:00`
- Baseline main SHA: `790af584a2b356c04303913cfd750991a0f13961`
- Priority: P1 — prevent Family assignment from applying a stale target/property plan after lazy target enumeration mutates the project.

## Confirmed defect

`ProjectFamilyService.Assign(project, familyId, IEnumerable<ProjectElement>)` resolves the target Family and snapshots its properties before enumerating the caller-supplied target sequence. `ResolveOwnedElements(...)` then enumerates that potentially lazy sequence without pinning `ProjectState.ChangeVersion`. A target enumerable can mutate/touch the project while being consumed; assignment then continues using the pre-enumeration target Family snapshot and may mutate elements again, or can return a false no-op after the project changed during enumeration. `BulkEditService` already guards the same lazy-input boundary with a before/after ChangeVersion check, and current Floor/Zone lanes are applying the same contract.

## Reserved surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` — Assign target-enumeration freshness only plus one private helper
- `tests/QS3D.Core.SmokeTests/FamilyAssignInputFreshnessSmoke.cs` — new focused regression
- this claim file

## Intended fix

- Capture `project.ChangeVersion` immediately before `ResolveOwnedElements(project, elements, target)`.
- After the enumerable has been fully materialized/validated, require the same ChangeVersion before planning assignments.
- Fail even when a mutating lazy enumerable yields no targets, so project changes cannot be misreported as a Family-assignment no-op.
- Preserve target/previous Family property canonicality, category/ownership checks, inherited override behavior, duplicate target collapsing and normal one-revision assignment semantics.
- Add focused smoke for stable lazy input, touch-then-yield input and touch-then-stop empty input.

## Coordination

Floor/Zone mutation-input freshness lanes own their respective files. BulkEditService already has this guard and is not modified. No native/UI/persistence files are in scope.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed.
