# Work claim — Curtain path frame input preflight bound

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-path-frame-input-bound-20260812-0906`
- Registered: `2026-08-12T09:06:00+07:00`
- Baseline main SHA: `075bc1001ae4756901f6a1d9d15bdca1b92b6353`
- Priority: evidence-driven Core resource preflight during owner-requested review/fix continuation

## Confirmed defect

`CurtainPathFramePlanner.Plan(...)` already limited generated mapping to `MaxPieces = 20,000`, and every valid input frame must map to at least one piece or the call fails. Therefore any `frames.Count > 20,000` can never succeed, yet the implementation first built/validated the host path and then entered per-frame/per-segment mapping before the piece guard became effective.

## Implemented fix

`Plan(...)` now rejects `frames.Count > MaxPieces` immediately after null checks and before `BuildPath(...)`. The existing 8,192-point host-path bound, path geometry/projection, finite numeric validation, split mapping, per-frame must-map behavior, piece ordering and generated-piece capacity remain unchanged.

## Integration evidence

- Claim registration: `03a34e6efb85db04b20d6de13c23e5de530cc7bd`.
- Source fix: `1faceca21d78b0f8f0b44c14fb34f27ff2fed7e2`.
- Focused smoke: `34d9adac162f7e8e599a33eb8b434954edddf066`.
- Source read-back on moving `main` confirmed the >20,000 frame preflight occurs before `BuildPath(...)`.
- Smoke read-back confirmed 20,001 frames fail closed and an ordinary one-frame/two-point path still maps one piece with unchanged station/center/elevation/height values.

## Validation boundary

Deterministic source and focused smoke coverage were committed and read back. No GitHub Actions were dispatched, no executable full Core smoke/build PASS is claimed, and no licensed BricsCAD runtime qualification is claimed.