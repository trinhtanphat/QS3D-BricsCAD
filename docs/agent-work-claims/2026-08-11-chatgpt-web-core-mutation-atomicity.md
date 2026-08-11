# Work claim — Core mutation atomicity audit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-core-atomicity-20260811-1930`
- Registered: `2026-08-11T19:30:00+07:00`
- Scope expanded: `2026-08-11` after Navigation/Review/Rules audit closed without a proven defect
- Baseline main SHA: `44dae5d5f3a6184cadf93d27661d1b71dc9bc860`
- Priority: continue the existing source-safe Core persistence/mutation hardening lane and prevent APIs from reporting failure after partially mutating canonical project state or durable persistence state

## Reserved scope

Audit and harden remaining `QS3D.Core` project-mutation boundaries for atomic failure behavior, especially cases where canonical state is changed before `ProjectState.Touch()` or a bound audit mutation can fail. The first Navigation/Review/Interchange/Rules pass is closed without a source-proven product defect. The current focused continuation is Core persistence/session atomicity: QSDB primary/backup recovery semantics and `ProjectSession.Save()` behavior at the `ChangeVersion` overflow boundary. Only confirmed defects receive product changes.

## Expected surfaces

- `src/QS3D.Core/Navigation/**` only when a confirmed project mutation defect exists
- `src/QS3D.Core/Review/**` only when a confirmed project mutation defect exists
- `src/QS3D.Core/Interchange/**` only when a confirmed project mutation defect exists
- `src/QS3D.Core/Rules/**` only when a confirmed project mutation defect exists
- `src/QS3D.Core/Persistence/QsdbProjectStore.cs` for the focused QSDB recovery audit only
- `src/QS3D.Core/Services/ProjectSession.cs` for the focused save/overflow atomicity audit only
- narrowly related persistence/session helpers as read-only evidence unless this claim is updated again before any write
- focused `tests/QS3D.Core.SmokeTests/**` regression files and smoke registration
- this claim file for close-out status

## Excluded scope

- No edits to `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs` or broad schema/migration architecture; the existing validator remains authoritative for structural XML validation.
- No Direct Draw, Create Similar, selection ownership or Family activation command workflow owned by the active Create Similar claim.
- No Core reporting/schedule identity surfaces owned by the active schedule-reporting claim.
- No LOCAL-003 Level Z-chain surfaces owned by the active local agent claim.
- No Room Auto command-side regeneration surfaces owned by its active claim.
- No agent-registration protocol implementation owned by the completed local bootstrap claim.
- No BricsCAD UI/runtime qualification, geometry-builder redesign, release, installer, signing or GitHub Actions dispatch.
- No speculative refactor where current source does not demonstrate a partial-mutation or persistence-recovery failure path.

## Validation plan

- Re-read exact current source before every edit and preserve concurrent changes.
- For QSDB recovery, first prove the intended primary/`.bak` contract from current implementation, helpers, tests and repository history; add no fallback merely by assumption.
- For `ProjectSession.Save()`, inspect the durable-write/audit/version ordering and use deterministic `ChangeVersion` boundary coverage only if the current ordering can fail after a durable or canonical mutation.
- Add focused smoke/regression coverage for each confirmed failure path, including `ChangeVersion` overflow where it is the deterministic trigger.
- Preserve existing successful-path version/audit semantics unless a defect proves they are wrong.
- Use conflict-safe GitHub writes; on stale SHA/409 re-fetch and re-evaluate instead of overwriting.
- Do not claim BricsCAD runtime, local smoke execution, CI or Actions unless actually available and executed.

## Coordination

The currently active schedule-reporting claim explicitly excludes Core persistence/mutation. The active Room Auto claim reserves only BricsCAD command-side regeneration and explicitly leaves Core transaction primitives to this lane. Start Center, modeless-viewer, Create Similar and LOCAL-003 claims own disjoint surfaces. If a newer claim reserves `QsdbProjectStore.cs`, `ProjectSession.cs`, the same focused smoke files, or the same persistence transaction contract, stop before overlapping implementation and re-scope.

## Completion condition

All confirmed defects found in the reserved Core surfaces are pushed to current `main` with focused regression coverage; speculative candidates are explicitly closed without edits; the QSDB recovery and ProjectSession overflow candidates are resolved from source evidence; and this claim is marked `COMPLETED` with exact implementation SHAs and validation actually performed.