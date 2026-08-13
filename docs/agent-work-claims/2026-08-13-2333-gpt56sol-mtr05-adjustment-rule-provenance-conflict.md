# Work claim — MTR-05 adjustment rule-provenance conflict

- Status: `RELEASED`
- Agent: `gpt56sol-mtr05-adjustment-rule-provenance-conflict-20260813-2333`
- Registered: `2026-08-13T23:33:00+07:00`
- Released: `2026-08-13T23:43:00+07:00`
- Baseline main SHA: `b1d7b205284670f90e4a555fbd86dee781edf201`
- Claim commit: `746c1b75f01885952b8bf4fae7d0b6c0d0bfd442`
- Priority: `P0` MeasurementTrace provenance integrity under MTR-05 continuous hardening.

## Reserved scope

Audit whether otherwise equal adjustment evidence with different rule ID/version should fail closed.

## Release result

No source or test changes were made under this claim. After publication, current `MeasurementTraceContractSmoke.AdjustmentRuleIdentity()` was read in full. It deliberately creates adjustment rows with equal kind, amount, unit, reason, and source identity but different rule IDs, then asserts deterministic canonical ordering and trace equality. The proposed restriction would contradict that explicit current regression contract, so the hypothesis is rejected and this lane is released without implementation.

## Validation actually performed

- Refreshed current `main` after the claim commit.
- Re-read current `MeasurementTrace.cs` adjustment snapshot/ordering behavior.
- Re-read current `MeasurementTraceContractSmoke.cs` adjustment rule regression.
- No GitHub Actions were dispatched; no BricsCAD/native PASS is claimed.

## Coordination

This claim no longer reserves implementation scope.
