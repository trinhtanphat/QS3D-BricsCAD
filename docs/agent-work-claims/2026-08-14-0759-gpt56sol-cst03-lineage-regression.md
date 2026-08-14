# Work claim — CST-03 measurement-lineage regression completion

- Status: `ACTIVE`
- Agent: `gpt56sol-cst03-lineage-regression-20260814-0759`
- Registered: `2026-08-14T07:59:00+07:00`
- Baseline main SHA: `192c7e794ff0e998f8e49e010fc8e1beea72fe5b`
- Priority: `P1` CST-03 revision cost integrity; closes a verified regression gap left by a prior `RELEASED` claim.

## Reserved scope

Complete deterministic regression coverage for the already-published `EstimateRevisionCostImpact` measurement-lineage guard. Verify that revision cost comparison fails closed when the exact measurement trace identity tuple changes, while preserving existing comparable quantity/rate/commercial-adjustment behavior.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/EstimateRevisionCostImpactSmoke.cs` — focused regression implementation.
- `src/QS3D.Core/Cost/EstimateRevisionCostImpact.cs` — read/verify current guard only; no source rewrite unless a newly proven defect requires a claim amendment first.
- this claim file.

## Excluded scope

- `EstimateLine.cs`, RateBook/rate resolution, mapping/BOQ, persistence, report/UI/native behavior.
- MeasurementTrace/MTR-05 semantics.
- V25 preview packaging/version automation and any current release workflow surfaces.
- GitHub Actions dispatch and BricsCAD native qualification.

## Validation plan

- Refresh current `main` and exact-path claims after this claim-only commit.
- Add deterministic smoke cases for mismatched `SemanticIdentity`, `SourceIdentity`, and `QuantityKey` using otherwise comparable estimate lines.
- Preserve existing positive reconciliation and strict-comparability cases.
- Execute a local managed build/smoke gate if the current execution environment provides the required checkout/.NET toolchain; otherwise record that gate as unexecuted rather than claiming PASS.
- Re-fetch pushed source/test/claim files and verify commit ancestry on current `main`.

## Coordination

The earlier claim `2026-08-13-2345-gpt56sol-cst03-measurement-lineage-integrity.md` is `RELEASED`. Its implementation commit `5a99e10e21e8975f15ae48b5ff979082ac49ba01` remains on current `main`, while the focused regression is still absent. Recent current-main changes do not touch the CST-03 source/test surfaces. The active V25 release-automation lane is explicitly non-overlapping.

## Completion condition

Focused measurement-lineage regression coverage is pushed and read back from current `main`; the existing source guard remains present; actual managed validation evidence is recorded honestly; this claim is closed `COMPLETED` with implementation/test commit SHA and remaining LOCAL/native gates.