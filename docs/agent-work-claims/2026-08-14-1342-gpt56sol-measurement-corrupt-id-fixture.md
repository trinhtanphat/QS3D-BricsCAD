# Work claim — Measurement coverage corrupt-ID fixture reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-measurement-corrupt-id`
- Registered: `2026-08-14T13:42:00+07:00`
- Baseline main SHA: `74f4bc57c1e13f746d983007dc52edab11836aed`
- Owner request: continue all; keep advancing deterministic Core blockers without weakening production validation.
- Implementation SHA: `4aef6890a1464f2e2ddf21dd88bf1cfd58e1288f`

## Concrete blocker

After the Material Usage collision fixture was reconciled, the complete registered Core smoke advances to `MeasurementWorkItemCoverageSmoke.CorruptProjectStateFailsClosed`. Its `controlIdentity` case attempts to construct `ProjectElement("Bad\u0001Id", ...)`, but the current `ProjectElement` constructor correctly rejects control-character IDs before the evaluator-under-test can receive a deliberately corrupted project state.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageSmoke.cs`
- this claim file

## Implementation boundary

- Test-only corruption-fixture repair; do not relax `ProjectElement.RequireId` or persistability rules.
- Construct a valid clean element first, then use bounded test reflection to corrupt only its backing ID after construction, matching the file's existing reflection-based undefined-category corruption pattern.
- Preserve the assertion that `MeasurementWorkItemCoverageEvaluator.Evaluate` fails closed when a corrupt control-character element ID exists in an already-materialized project state.
- Fail explicitly if the `ProjectElement.Id` backing field layout changes, so the corruption mechanism cannot silently stop testing the intended state.
- No production/domain, Level/rebar LOCAL-003, Source Reconcile, Curtain, release workflow, BricsCAD runtime, private-data, or GitHub Actions changes.

## Validation and closeout

- `4aef6890a1464f2e2ddf21dd88bf1cfd58e1288f` constructs a valid `ProjectElement`, corrupts only `<Id>k__BackingField` to `Bad\u0001Id`, verifies the corrupted value reaches the evaluator, and retains the expected fail-closed `InvalidOperationException` assertion.
- GitHub readback confirms the implementation touches only `tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageSmoke.cs`.
- The implementation commit is an ancestor of the refreshed `main` head observed during closeout.
- GitHub Actions were not dispatched for this claim; manual-only CI policy remains intact.
- No licensed BricsCAD runtime PASS is claimed from this test-only correction.
