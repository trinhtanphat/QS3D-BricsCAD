# Work claim — MTR-05 canonical `none` rounding policy

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr05-none-policy-case-20260813`
- Registered: `2026-08-13T18:54:00+07:00`
- Baseline main SHA: `ed60f400d321474640be2682d7093d4abc54df34`
- Priority: P0 MTR-05 measurement-policy canonicality hardening. Current `MeasurementTrace` treats exact lower-case `none` as the reserved no-rounding policy that must reconcile gross/adjustments/net, but `RequireToken` also accepts case variants such as `NONE`/`None`, which therefore bypass that reserved-policy reconciliation while producing different canonical trace bytes.

## Reserved scope

- `src/QS3D.Core/Measurement/MeasurementTrace.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs`
- this claim file for closeout

## Intended change

Keep `none` as the only canonical spelling of the reserved no-rounding policy. Reject case-insensitive aliases of that reserved token rather than normalizing them silently, while leaving other custom policy tokens (for example `nearest-cent`) unchanged. Preserve the existing exact `none` reconciliation math and MTR1/MTR2 canonical bytes for valid traces.

## Excluded scope

- no changes to reconciliation arithmetic, adjustment semantics, trace schema/versioning, fact/adjustment/message uniqueness, snapshot/delta/inspector behavior;
- no QuantityMath changes (the separate Add signed-zero lane is completed on the baseline lineage);
- no report/UI/export, BricsCAD adapter/native work, sibling Platform migration, GitHub Actions, release or native qualification.

## Validation plan

- refresh `main` after the claim and recheck recent claims/commits for MeasurementTrace overlap;
- add a focused constructor regression proving exact lower-case `none` still accepts reconciled evidence, `NONE`/`None` fail closed, and a non-reserved policy such as `nearest-cent` remains accepted;
- re-fetch exact pushed source/test and inspect the production diff;
- verify claim/source/test ancestry against current moving `main` before closeout;
- report only validation actually executed; do not claim native BricsCAD PASS.

## Coordination

Recent MTR-05 duplicate-evidence and `none` reconciliation lanes are already `COMPLETED`; this lane does not reopen their arithmetic/uniqueness scope. Recent exact commit searches found no current MeasurementTrace rounding-policy case/canonical-token claim. The long-running SE closed-polyline/native-solid lane is disjoint.

## Completion condition

Case variants of the reserved `none` rounding policy cannot bypass the no-rounding reconciliation contract, valid canonical traces retain existing semantics/canonical bytes, focused regression coverage is on current `main`, exact readback/ancestry is verified, and this claim is marked `COMPLETED` with actual validation boundaries recorded.
