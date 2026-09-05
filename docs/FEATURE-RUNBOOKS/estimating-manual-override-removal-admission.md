# Estimating manual override removal admission

Issue: #5838

## Defect

`EstimatingWorkflowService.RemoveManualRateOverride` historically accepted an estimating line with an existing manual override even when that line was `Blocked` or `Stale`. The method then built a replacement portfolio and appended `rate-override-removed` audit state. That was inconsistent with bulk rate assignment and manual override creation, which treat blocked/stale commercial rows as non-committable.

## Contract

Manual override removal is admitted only when the target line has an override and is neither blocked nor stale. Admission is evaluated before `WithoutOverride`, portfolio replacement, or audit publication. Rejected operations must preserve the immutable input portfolio and append no audit event.

A current overridden line remains valid: removal restores the referenced/base rate, returns `EstimatingReadinessState.Priced`, and emits exactly one `rate-override-removed` audit event.

## Deterministic validation

`EstimatingManualOverrideStaleSmoke` covers stale rejection, blocked rejection, zero-audit/unchanged-input behavior, and the valid current control. `scripts/preflight-estimating-manual-override-removal-admission.py` pins the fail-closed admission ordering before replacement and audit mutation.

## Runtime boundary

REMOTE_SAFE / NOT_APPLICABLE for licensed BricsCAD. This is deterministic managed-Core commercial state correctness. Hosted Core smoke and protected repository CI are authoritative; no remote `LOCAL_PASS` claim is applicable.
