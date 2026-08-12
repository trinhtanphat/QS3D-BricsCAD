# Semantic Schedule whitespace payload integrity

- Agent: ChatGPT Web / GPT-5.6 Sol
- Status: COMPLETED
- Registered: 2026-08-12 14:14 +07:00
- Baseline main: `35a4a59c63025fc11c116e3758a21c6052f718d2`
- Claim commit: `b31affb2d01cc320a53d29c660e2a42d0d4063c6`
- Source fix: `9c69a559162920eb6b3b2cefc8a8953a36963df4`
- Regression smoke: `36ee7c1799df6edcc14b27746078234bd1917633`

## Defect

`SemanticScheduleCatalog.Load(...)` treated a whitespace-only value under `QS3D.Documentation.SemanticSchedules.v1` as if the catalog were absent because it used `string.IsNullOrWhiteSpace(payload)`. The writer never emits whitespace-only metadata: an absent catalog is represented by removing the metadata key, while a present catalog is canonical XML. Therefore corrupted/persisted whitespace-only metadata was silently converted into an empty catalog instead of failing closed.

## Completed change

- `Load(...)` now keeps the existing empty-catalog behavior only for a missing key, `null`, or exact empty string via `string.IsNullOrEmpty(payload)`.
- Whitespace-only metadata proceeds through the existing bounded XML parser and is rejected as malformed.
- Valid Schedule XML and the CDATA strict-grammar fix remain unchanged.

## Regression coverage

`SemanticScheduleWhitespacePayloadSmoke` verifies:

- missing metadata loads as an empty catalog;
- exact empty metadata loads as an empty catalog;
- whitespace-only persisted metadata throws `InvalidDataException` instead of returning an empty catalog.

The smoke is auto-registered with `[ModuleInitializer]` under the existing SDK compile glob.

## Readback verification

Readback on current `main` confirmed `Load(...)` uses `string.IsNullOrEmpty(payload)` and the regression source remains present.

## Validation boundary

Remote source/test readback only. No GitHub Actions, full .NET build, Core executable smoke, or licensed BricsCAD V25/V26 runtime PASS is claimed.
