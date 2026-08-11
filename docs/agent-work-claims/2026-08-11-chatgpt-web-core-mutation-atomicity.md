# Work claim — Core mutation atomicity audit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-core-atomicity-20260811-1930`
- Registered: `2026-08-11T19:30:00+07:00`
- Baseline main SHA: `44dae5d5f3a6184cadf93d27661d1b71dc9bc860`
- Priority: continue the existing source-safe Core persistence/mutation hardening lane and prevent APIs from reporting failure after partially mutating canonical project state

## Reserved scope

Audit and harden remaining `QS3D.Core` project-mutation boundaries for atomic failure behavior, especially cases where canonical state is changed before `ProjectState.Touch()` or a bound audit mutation can fail. Current focus is the non-Direct-Draw Core surfaces under Navigation, Review, Interchange and Rules, plus narrowly related smoke-test registration required to lock confirmed defects.

## Expected surfaces

- `src/QS3D.Core/Navigation/**` only when a confirmed project mutation defect exists
- `src/QS3D.Core/Review/**` only when a confirmed project mutation defect exists
- `src/QS3D.Core/Interchange/**` only when a confirmed project mutation defect exists
- `src/QS3D.Core/Rules/**` only when a confirmed project mutation defect exists
- focused `tests/QS3D.Core.SmokeTests/**` regression files and smoke registration
- this claim file for close-out status

## Excluded scope

- No Direct Draw, Create Similar, selection ownership or Family activation command workflow owned by the active Create Similar claim.
- No agent-registration protocol implementation owned by the local bootstrap claim.
- No BricsCAD UI/runtime qualification, geometry-builder redesign, release, installer, signing or GitHub Actions dispatch.
- No speculative refactor where current source does not demonstrate a partial-mutation failure path.

## Validation plan

- Re-read exact current source before every edit and preserve concurrent changes.
- Add focused smoke/regression coverage for each confirmed failure path, including `ChangeVersion` overflow where it is the deterministic trigger.
- Preserve existing successful-path version/audit semantics unless a defect proves they are wrong.
- Use conflict-safe GitHub Contents writes; on stale SHA/409 re-fetch and re-evaluate instead of overwriting.
- Do not claim BricsCAD runtime or local smoke execution unless it is actually available and executed.

## Coordination

This lane is disjoint from the active Direct Draw Create Similar claim and the registration-protocol bootstrap claim. If a new claim reserves the same Core mutation/transaction contract or the same source/test surfaces, stop before overlapping implementation and re-scope.

## Completion condition

All confirmed defects found in the reserved Core surfaces are pushed to current `main` with focused regression coverage, speculative candidates are explicitly closed without edits, and this claim is marked `COMPLETED` with exact implementation SHAs and validation actually performed.
