# Work claim — release #29 B4D capture-readiness preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release29-b4d-capture-readiness-preflight`
- Registered: `2026-08-12T09:07:00+07:00`
- Baseline main SHA: `3436a5515b912db3cd9c9b59467ad48c4866fe1a`
- Priority: QS3D Cloud V25 Preview Build & Release #29 reports `recognition engine missing token: x.IsCaptureReady`; current source preserves capture-readiness gating but now evaluates it through `IsAutoAccepted(result)` with `result.IsCaptureReady` rather than an inline LINQ `x.IsCaptureReady` literal.

## Reserved scope

Reconcile only `scripts/preflight-b4d-unit-proxy-safety.py` with the current `RecognitionBatch` capture-readiness contract. Preserve Recognition/Core production behavior, UI behavior and proxy eligibility semantics unchanged.

## Expected surfaces

- `scripts/preflight-b4d-unit-proxy-safety.py`
- this claim file for close-out

## Excluded scope

- No edits to `RecognitionEngine.cs`, `RecognitionWindow.xaml.cs`, capture eligibility, B4D commands, unit policy or proxy handling.
- No changes to confidence/margin logic or current concurrent Recognition claims.
- No unrelated run #29 failures, GitHub Actions dispatch or licensed BricsCAD runtime qualification.

## Validation plan

- Keep requiring `public bool IsCaptureReady`, `!IsCaptureReady`, and `capture-blocked:` in recognition source.
- Replace the stale `x.IsCaptureReady` engine literal with the current `result.IsCaptureReady` auto-accept guard while retaining the separate UI `x.IsCaptureReady` requirement.
- Also pin the current partitioning path through `IsAutoAccepted(x)` / `private bool IsAutoAccepted(RecognitionResult result)` so capture readiness cannot be removed merely by renaming a local variable.
- Re-fetch current gate/source immediately before writing and preserve all other unit/proxy checks unchanged.
- Read back final gate and close with exact SHA. No aggregate PASS claim without a newer manual run.

## Coordination

Recent Recognition work changes candidate validation/projections, not this static B4D preflight. Current observed active claims do not reserve `scripts/preflight-b4d-unit-proxy-safety.py`.

## Completion condition

The B4D unit/proxy gate recognizes the current auto-accept helper while still fail-closing if capture-readiness is removed from engine or UI, the change is pushed to `main`, and this claim is closed with exact evidence.
