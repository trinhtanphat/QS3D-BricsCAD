# Work claim — MTR-05 canonical `none` rounding policy

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-mtr05-none-policy-case-20260813`
- Registered: `2026-08-13T18:54:00+07:00`
- Completed: `2026-08-13T18:59:00+07:00`
- Baseline main SHA: `ed60f400d321474640be2682d7093d4abc54df34`
- Priority: P0 MTR-05 measurement-policy canonicality hardening. `MeasurementTrace` treated exact lower-case `none` as the reserved no-rounding policy that must reconcile gross/adjustments/net, while the generic token validator also accepted case variants such as `NONE`/`None`; those variants could therefore bypass that reserved-policy reconciliation and produce distinct canonical trace bytes.

## Reserved scope

- `src/QS3D.Core/Measurement/MeasurementTrace.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs`
- this claim file for closeout

## Result

- Implementation: `57afaaee9df67367f486ce90b98b244969bee9c1` (`fix(measurement): canonicalize none rounding policy`).
  - `MeasurementTrace` now validates `RoundingPolicy` through a dedicated `RequireRoundingPolicy` contract.
  - The reserved `none` token is accepted only with its canonical lower-case spelling; case-insensitive aliases are rejected rather than silently normalized.
  - Other valid custom rounding-policy tokens remain unchanged, and the existing exact `none` reconciliation arithmetic is untouched.
- Regression: `9c5a0c49254c4f4ae87acec091cc1991961d6101` (`test(measurement): guard none policy case`).
  - Existing reconciled lower-case `none` success and unreconciled lower-case `none` failure remain covered.
  - Reconciled `NONE` and `None` now explicitly fail closed.
  - `nearest-cent` remains accepted as a non-reserved custom policy.

## Validation actually performed

- Claim was pushed alone, then refreshed/rechecked before source mutation; no competing recent MeasurementTrace rounding-policy case claim was found.
- Exact implementation diff was re-read from GitHub: only the constructor validator call plus the dedicated reserved-token validator changed (apart from final newline state); reconciliation math/schema/evidence code was not altered.
- Exact regression diff was re-read from GitHub and contains only the two case-variant rejection cases inside the existing no-rounding smoke.
- Current-main readback at `5bc15df4e072b5ab6568cc637b3c8051dfb8d240` contains both the production validator and focused regression.
- `9c5a0c49254c4f4ae87acec091cc1991961d6101 -> 5bc15df4e072b5ab6568cc637b3c8051dfb8d240` is a clean ancestor relationship (`ahead_by=1`, `behind_by=0`); the intervening commit only updates another agent's QuantityMath claim file.
- This execution environment has Python 3.13.5 but no `dotnet`, `csc`, `mcs`, `msbuild` or `xbuild`, so the managed smoke assembly was not executed here. No managed-build PASS, GitHub Actions PASS or licensed BricsCAD runtime PASS is claimed.

## Excluded scope preserved

- no reconciliation arithmetic, adjustment semantics, trace schema/versioning, fact/adjustment/message uniqueness, snapshot/delta/inspector changes;
- no QuantityMath changes;
- no report/UI/export, BricsCAD adapter/native work, sibling Platform migration, GitHub Actions, release or native qualification.

## Completion condition

Satisfied for source/static scope: case variants of the reserved `none` rounding policy can no longer bypass the no-rounding reconciliation contract, canonical lower-case behavior and custom-policy behavior are preserved in focused regression source, pushed artifacts/ancestry are verified, and remaining executable/native validation is stated rather than fabricated.
