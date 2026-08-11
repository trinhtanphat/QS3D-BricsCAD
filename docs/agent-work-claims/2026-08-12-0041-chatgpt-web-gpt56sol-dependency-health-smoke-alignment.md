# Work claim — dependency health smoke/source alignment

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-dependency-health-smoke-alignment`
- Registered: `2026-08-12T00:41:00+07:00`
- Baseline main SHA: `87f28b33cc89cb7f7ab8576606ce1a5328ec31a3`
- Priority: deterministic auto-registered smoke contradiction found during owner-requested continue-all audit

## Confirmed defect

Current `DependencyHealthService.Inspect(...)` emits `DEPENDENCY_TARGET_MISSING` Error for unresolved semantic dependency IDs. Dedicated `DependencyHealthMissingTargetSmoke` also asserts that behavior. However the older auto-registered `DependencyHealthSmoke.MissingDependencyIsNotMisclassifiedAsCycle()` still asserts that the service returns no issues for a missing target. `DependencyHealthRegistration` invokes that stale smoke at module initialization, so the current smoke suite contains contradictory assertions and can fail deterministically without any product defect.

## Reserved scope

Align the stale assertion with the current source contract: a missing target must produce exactly one `DEPENDENCY_TARGET_MISSING` Error for the referencing element and must not be misclassified as `DEPENDENCY_CYCLE` or `DEPENDENCY_SELF_REFERENCE`.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/DependencyHealthSmoke.cs` (one stale test case only)
- this claim file

## Excluded scope

- No `DependencyHealthService`, `ModelHealthService`, `DependencyGraph`, registration, Health All, Release Check or native code changes.
- No change to missing/blank/ambiguous/cycle severity policy.
- No GitHub Actions dispatch or V25 runtime work.

## Validation plan

- Read back current source confirming `DEPENDENCY_TARGET_MISSING` Error is emitted.
- Read back dedicated missing-target smoke confirming the same contract.
- Update only the stale old test expectation.
- Preserve its original purpose: missing target is not a cycle/self-reference.
- Inspect exact commit diff and re-fetch current test from `main`.

## Coordination

Historical source/test commits `654210559a9e330509ea3c87134bdc1e4a2b4ad0` and `c5d769b05fa3bda5fb83ea33ef09f445132789d5` established missing-target health behavior, but the older auto-registered cycle smoke retained its pre-change expectation. Recent commit search found no active claim for this stale assertion alignment.

## Completion condition

Current `main` has no contradictory dependency-health smoke expectations for missing targets, the auto-registered cycle smoke still guards non-cycle classification, and this claim is closed `COMPLETED`.
