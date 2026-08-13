# Work claim — MTR-05 adjustment rule-provenance conflict

- Status: `ACTIVE`
- Agent: `gpt56sol-mtr05-adjustment-rule-provenance-conflict-20260813-2333`
- Registered: `2026-08-13T23:33:00+07:00`
- Baseline main SHA: `b1d7b205284670f90e4a555fbd86dee781edf201`
- Priority: `P0` MeasurementTrace provenance integrity under MTR-05 continuous hardening.

## Reserved scope

Make `MeasurementTrace` fail closed when two adjustments represent the same canonical non-rule evidence row — same kind, amount, unit, reason, and source identity — but carry conflicting rule ID/version provenance. Preserve the existing exact-duplicate rejection and preserve legitimate distinct adjustments when any non-rule evidence field differs.

## Expected surfaces

- `src/QS3D.Core/Measurement/MeasurementTrace.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs`
- this claim file

## Excluded scope

- Quantity preview/rule evaluation surfaces reserved by the concurrent MTR-04 null-element claim.
- Measurement fact payload identity already completed by the earlier MTR-05 claim.
- `MeasurementSnapshotDeltaReason` behavior, quantity formulas, persistence, reports/UI, BricsCAD/native runtime, and other current agent claims.

## Validation plan

- Refresh `main` and claims after this claim-only commit and recheck overlap before source edits.
- Add focused deterministic smoke coverage for conflicting adjustment rule provenance and positive coverage for distinct non-rule evidence rows.
- Preserve exact-duplicate, canonical ordering, reconciliation, equality/hash, and MTR1/MTR2 behavior.
- Read back pushed source/test/claim and verify commit ancestry on current `main`; do not dispatch GitHub Actions and do not claim native BricsCAD PASS.

## Coordination

Current `MeasurementTraceContract.SnapshotAdjustments` rejects only fully equal duplicates. Current `MeasurementSnapshotDeltaReason` explicitly treats kind/amount/unit/reason/source identity as the non-rule adjustment evidence and rule ID/version as provenance associated with that evidence. The concurrent MTR-04 quantity-preview claim explicitly excludes `MeasurementTrace`; no recent adjustment-rule claim remains open in commit history.

## Completion condition

Conflicting rule provenance for one canonical adjustment evidence row fails closed, focused regression coverage is pushed on current `main`, validation actually performed is recorded, and this claim is closed `COMPLETED` with remaining LOCAL/native gates stated.
