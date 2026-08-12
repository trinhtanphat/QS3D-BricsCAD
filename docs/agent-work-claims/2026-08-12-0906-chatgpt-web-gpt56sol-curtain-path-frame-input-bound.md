# Work claim — Curtain path frame input preflight bound

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-path-frame-input-bound-20260812-0906`
- Registered: `2026-08-12T09:06:00+07:00`
- Baseline main SHA: `075bc1001ae4756901f6a1d9d15bdca1b92b6353`
- Priority: evidence-driven Core resource preflight during owner-requested review/fix continuation

## Confirmed defect

`CurtainPathFramePlanner.Plan(...)` already limits the generated mapping to `MaxPieces = 20,000`, and every valid input frame must map to at least one piece or the call fails. Therefore any `frames.Count > 20,000` can never succeed, yet the current implementation first builds/validates the full host path and then enters per-frame/per-segment mapping before the piece guard becomes effective.

## Reserved scope

- `src/QS3D.Core/Geometry/CurtainPathFramePlanner.cs` — frame-count preflight only.
- `tests/QS3D.Core.SmokeTests/CurtainPathFrameInputBoundSmoke.cs` — focused CAD-independent regression.
- this claim file.

## Contract

Fail closed immediately when the input frame collection itself exceeds the existing 20,000 native-piece capacity. Preserve `MaxPathPoints = 8,192`, path geometry/projection, finite numeric validation, split mapping, per-frame must-map behavior, piece ordering and the existing generated-piece capacity.

## Validation plan

Prove 20,001 frame rectangles fail with the existing 20,000 capacity before mapping, while an ordinary one-frame/two-point path still maps exactly one expected piece. Re-fetch exact source before write; never force-push. No GitHub Actions dispatch, executable full Core test PASS or licensed BricsCAD runtime qualification claim.