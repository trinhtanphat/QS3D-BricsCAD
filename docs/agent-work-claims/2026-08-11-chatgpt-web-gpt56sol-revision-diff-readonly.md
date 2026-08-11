# Agent work claim — Revision Diff read-only integrity

Status: COMPLETED

Agent: ChatGPT Web / GPT-5.6 Sol
Date: 2026-08-11 (UTC+7)
Registered: 2026-08-11T21:45:12+07:00
Baseline main SHA observed before reservation: `5cf79385c52e2af3e74c19f8e67043898b2c2b29`
Claim commit: `f2b789829212af01a9f7552521bca7522451ae02`

## Scope

Make `QS3DREVDIFF` a true read-only review command. Preserve its existing non-creating detached snapshot lifecycle, revision comparison UI and locate behavior while removing persisted compare telemetry that binds a mutation context and advances project state merely because a user viewed a diff.

Implementation surfaces:

- `src/QS3D.BricsCAD.V25/ReviewCommands.cs`
- `scripts/preflight-review-snapshot-project-lifecycle.py`
- this claim file

## Concrete defect fixed

`QS3DREVDIFF` already started with `ProjectContextCoordinator.TryGetReadOnly`, captured the current revision through the detached/read-only revision coordinator path and opened a modeless review window. It then called `TryRecordRevisionCompare`, which resolved `ExistingProjectMutationContext` and wrote `AuditTrail.ForProject(project).Record("revision.compare", ...)`. Because audit recording touches the canonical project, simply opening Revision Diff could make the project dirty and advance `ChangeVersion`.

The compare-audit call and helper are now removed. `QS3DREVDIFF` ends after opening the review UI and setting UI status; its locate callback continues to resolve the current project read-only.

## Preserved write contracts

- `QS3DREVBASE` remains a deliberate persisted mutation and retains its `revision.baseline` audit event.
- Recognition apply/skip paths remain deliberate mutations and retain their audit behavior.
- Revision snapshot schema/comparison logic, baseline persistence and modeless locate ownership were not changed.

## Regression contract

`scripts/preflight-review-snapshot-project-lifecycle.py` now isolates the `QS3DREVDIFF` command through the next helper boundary, requires its read-only project/snapshot/review tokens, and rejects `ProjectContextCoordinator.GetOrCreate`, `ExistingProjectMutationContext`, `AuditTrail.ForProject`, `TryRecordRevisionCompare`, canonical regeneration, save/pending-save, `Touch()` and `Record()` from the diff method. The gate also rejects reintroduction of the compare-audit helper anywhere in `ReviewCommands.cs`.

## Product commits

- `1b923a1bbaf1d15665ed69b8c3da7ef32943257a` — `fix(review): keep revision diff fully read-only`
- `5a8d98cee20cbaa593145e30a4e94530bf114842` — `test(review): guard revision diff read-only integrity`

## Validation

- Re-read `ReviewCommands.cs` from remote `main` after both product commits and confirmed `QS3DREVDIFF` contains no mutation context/audit call while baseline and recognition audit paths remain present.
- Re-read the focused preflight from remote `main` and confirmed its forbidden-mutation checks target the isolated Revision Diff method.
- GitHub combined status for `5a8d98cee20cbaa593145e30a4e94530bf114842` returned no status checks.
- GitHub workflow lookup for the same commit returned no workflow runs.
- No full C# build, BricsCAD V25 `NETLOAD`, private-DWG runtime, installer/signing or release qualification is claimed in this lane.
