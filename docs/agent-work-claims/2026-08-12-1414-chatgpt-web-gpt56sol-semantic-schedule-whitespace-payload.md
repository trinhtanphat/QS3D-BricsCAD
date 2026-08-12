# Semantic Schedule whitespace payload integrity

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: ACTIVE
- Registered: 2026-08-12 14:14 +07:00
- Baseline main: `35a4a59c63025fc11c116e3758a21c6052f718d2`

## Defect

`SemanticScheduleCatalog.Load(...)` currently treats a whitespace-only value under `QS3D.Documentation.SemanticSchedules.v1` as if the catalog were absent because it uses `string.IsNullOrWhiteSpace(payload)`. The writer never emits whitespace-only metadata: an absent catalog is represented by removing the metadata key, while a present catalog is canonical XML. Therefore corrupted/persisted whitespace-only metadata is silently converted into an empty catalog instead of failing closed.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs` — distinguish absent/empty metadata from whitespace-only malformed payload in `Load(...)` only.
- `tests/QS3D.Core.SmokeTests/SemanticScheduleWhitespacePayloadSmoke.cs` — focused regression.
- This claim file.

## Intended contract

- Missing metadata key or exact empty string keeps the existing empty-catalog behavior.
- Whitespace-only persisted payload is parsed/validated and rejected as malformed instead of returning an empty catalog.
- Valid canonical Schedule XML behavior remains unchanged.

## Validation boundary

Remote source/test readback only. No GitHub Actions, full .NET build, Core executable smoke, or licensed BricsCAD V25/V26 runtime PASS is claimed unless separately run and recorded.
