# Work claim — semantic mutation relation canonical rollback fixture

- Status: `COMPLETED`
- Agent: `codex-root`
- Registered: `2026-08-14T16:05:00+07:00`
- Baseline main SHA: `6aa34b7ac`
- Priority: next deterministic full Core smoke blocker after Material Catalog fixture closeout

## Confirmed fixture drift

`ProjectSemanticMutationExecutorSmoke.MutableRelationWhitespaceRollsBackExactly` assigns padded Family/Floor/Zone relation IDs through public `ProjectElement` setters and later expects the padded values after a forced rollback. The current authoritative relation-persistability contract normalizes those setters before the mutation starts, so the pre-mutation state is already `FAM-1`, `FLOOR-1`, `ZONE-1`. The failure occurs without demonstrating a rollback defect.

The completed snapshot-fidelity contract still requires rollback to restore the exact reachable pre-operation relation state. This fixture will therefore assert the public setter normalization before execution and assert that the same canonical values are restored after the injected mutation failure.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectSemanticMutationExecutorSmoke.cs`
- this claim document only

## Planned change

- rename the focused method to describe canonical relation rollback fidelity;
- capture/assert the canonical stored Family/Floor/Zone values immediately before the mutation;
- retain the forced mutation failure and assert the exact captured canonical values after rollback;
- preserve journal ordering, complete ProjectState rollback, interchange pre-commit rollback, saturation and invalid-operation coverage unchanged.

## Explicit exclusions

- no changes to `ProjectElement`, `ProjectStateSnapshot`, mutation executor/journal, QSDB/interchange persistence, Material Catalog, or any production source;
- no reflection/corrupt-state fixture in this lane because supported setters and current persistence normalize these relation IDs;
- no native BricsCAD, LOCAL runners/probes, workflows, release, private data or GitHub Actions;
- report the next independent full-smoke blocker instead of expanding scope.

## Validation

- Core Release build with zero warnings/errors;
- full registered Core smoke;
- focused semantic-mutation/interchange/persistence preflights available on the final exact SHA;
- exact diff/readback proving only the reserved smoke behavior changed.

## Completion record

- Claim PR #1251 merged as `968761f9cf97850cb3e43f3b5e009e04b7765f07` (claim head `0089859fb36e717e42639dd2c929f3eeb555579c`).
- Implementation commit `9e8efaf137db7d527fed76e824be291194aab789` merged through PR #1256 as `5b73758ad4a3a0ae3e50f8f782fd96a6ec3a03c1`.
- The diff changes only the reserved smoke: padded public relation writes are asserted canonical before the injected mutation, and rollback is checked against the exact captured canonical state.
- Core SmokeTests Release build passed with 0 warnings / 0 errors; semantic-mutation, generic and manual-only preflights passed.
- Full registered Core smoke advanced past this fixture and next stopped independently at `ProjectStatePersistedScalarVersioningSmoke`, whose ActiveZone expectation still assumes pre-canonicalization padded storage.
- No production, native BricsCAD, LOCAL, workflow, private-data or GitHub Actions surface changed or ran.
