# Agent Work Claim — ProjectState name invariant

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11` (UTC+7)
- Baseline main SHA: `12e9ecbf3b260dee6a887d6db744b3d4e7d4b85c`

## Confirmed defect

`ProjectState` normalizes/guards the project name in its constructor, but the public `Name` setter is an unguarded auto-property. After construction, callers can therefore assign `null`, whitespace, or an untrimmed value and place the canonical project model in a state the constructor itself rejects/normalizes.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — project-name invariant only.
- `tests/QS3D.Core.SmokeTests/ProjectStateNameInvariantSmoke.cs` — focused regression coverage.
- `tests/QS3D.Core.SmokeTests/ProjectStateNameInvariantSmokeRegistration.cs` — module registration for that smoke only.
- this claim file for completion evidence.

## Intended fix

Keep the existing constructor compatibility (`null`/blank input falls back to `QS3D Project`) while making subsequent assignments fail closed for null/blank values and normalize accepted names with `Trim()`. Do not alter project identity, persistence schema, or unrelated domain mutation semantics.

## Validation boundary

- Source/static review and deterministic Core smoke coverage only.
- No GitHub Actions dispatch.
- No BricsCAD V25 runtime qualification claim.
- Re-read current `main` immediately before implementation and stop/re-scope if a newer claim overlaps these exact paths.
