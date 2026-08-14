# Work claim — Measurement coverage corrupt-ID fixture reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-measurement-corrupt-id`
- Registered: `2026-08-14T13:42:00+07:00`
- Baseline main SHA: `74f4bc57c1e13f746d983007dc52edab11836aed`
- Owner request: continue all; keep advancing deterministic Core blockers without weakening production validation.

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

## Validation plan

- Read back `ProjectElement` constructor validation and current smoke corruption block.
- Verify the updated test reaches `Evaluate` with a truly corrupted ID rather than expecting a constructor path that is now unreachable.
- Re-check the complete Core smoke's next independent blocker from available validated evidence; no licensed runtime claim.
