# Work claim — EntitySnapshotCaptureEligibility defined-category integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-capture-eligibility-category-definedness`
- Registered: `2026-08-11T23:42:00+07:00`
- Baseline main SHA: `f90209264abc80b644aaff7f21ce93a8bfbbb0f0`
- Priority: P1 — invalid semantic categories must not fail open through capture readiness.

## Confirmed defect

`EntitySnapshotCaptureEligibility.IsReady(...)` accepts a public `ElementCategory` argument. For a normal non-`ProxyEntity` snapshot it returns `true` before any category-specific switch, so `(ElementCategory)999` is accepted as capture-ready. For `ProxyEntity`, the switch `default` branch can also accept an undefined category whenever any positive metric exists.

`ElementCategory` has no Unknown sentinel and current recognition/geometry boundaries reject undefined values. Capture readiness must likewise reject malformed categories before evaluating generated ownership, entity type, or metrics.

## Reserved scope

- `src/QS3D.Core/Recognition/EntitySnapshotCaptureEligibility.cs`
- `tests/QS3D.Core.SmokeTests/EntitySnapshotCaptureCategorySmoke.cs` (new auto-registered smoke)
- this claim file

## Coordination

The current recognition-confidence claim reserves `RecognitionEngine.cs` and explicitly excludes Proxy capture policy. This claim does not touch that source or its smoke file.

## Intended contract

- `IsReady(...)` and `EnsureReady(...)` reject undefined categories with `ArgumentOutOfRangeException` for proxy and non-proxy snapshots.
- Existing generated-output ownership rejection, ProxyEntity metric policy, reasons, and valid-category behavior remain unchanged.

## Excluded scope

- No recognition scoring/confidence/rule changes.
- No ProjectRecognitionService/layer mapping changes.
- No B4D/native UI or adapter changes.
- No shared smoke registry edits and no GitHub Actions dispatch.

## Validation plan

- Add focused auto-registered smoke for undefined category with normal entity, measured ProxyEntity, and `EnsureReady`.
- Preserve representative valid non-proxy and ProxyEntity paths.
- SHA-guard source writes, review exact diffs, no force-push.

## Completion condition

Undefined categories can no longer be capture-ready through direct Core API usage, regression is on `main`, and the claim is closed with exact commit SHAs.
