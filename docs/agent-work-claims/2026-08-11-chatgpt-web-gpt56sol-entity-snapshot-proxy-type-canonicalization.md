# Work claim — EntitySnapshot proxy type canonicalization

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-entity-snapshot-proxy-type-canonicalization`
- Registered: `2026-08-11T22:22:00+07:00`
- Baseline main SHA: `adb8bd419871cdc64aedfde9b5431b76ec06b7f4`
- Priority: prevent padded CAD entity-type text from bypassing ProxyEntity capture safety.

## Confirmed defect

`EntitySnapshot` rejects blank `entityType` values but currently stores the original nonblank string unchanged. `EntitySnapshotCaptureEligibility.IsReady(...)` intentionally applies stricter metric requirements when `snapshot.EntityType` equals `ProxyEntity`, using an ordinal-ignore-case comparison without trimming.

A snapshot constructed with `entityType = " ProxyEntity "` therefore passes constructor validation but is treated as a normal non-proxy entity by the capture-eligibility gate. A metricless proxy can consequently bypass the review-only/capture-blocking safety contract solely because of surrounding whitespace.

## Reserved scope

- `src/QS3D.Core/Model/EntitySnapshot.cs`
- `tests/QS3D.Core.SmokeTests/ProxyCaptureEligibilitySmoke.cs`
- this claim file

## Intended contract

- `EntitySnapshot.EntityType` is canonicalized to its trimmed nonblank representation at construction.
- Padded/case-varied `ProxyEntity` values remain subject to the same finite positive primary-metric capture gate as canonical `ProxyEntity`.
- Existing entity-type compatibility behavior and normal non-proxy recognition remain unchanged.
- Do not modify `RecognitionEngine.cs` or other currently active recognition workstreams.

## Exclusions

- No adapter/native entity extraction changes.
- No recognition scoring/rules/category-policy changes.
- No B4D/quantity/unit policy changes.
- No BricsCAD V25 runtime qualification.
- No shared smoke registry edit; `ProxyCaptureEligibilitySmoke` is already registered.
- No GitHub Actions dispatch.

## Validation plan

- Extend the existing proxy capture smoke with padded/case-varied `ProxyEntity` input and prove it remains review-only and `EnsureReady` rejects it without a primary metric.
- Preserve measured proxy and non-proxy success paths.
- Re-fetch reserved blobs immediately before writes and refuse stale overwrite/force-push.

## Coordination

Recent recognition-category work reserves separate recognition-engine surfaces. This claim deliberately limits implementation to the model constructor boundary and the already-registered proxy safety regression.

## Completion condition

Padded ProxyEntity type text can no longer bypass capture eligibility, focused regression source is merged, and this claim is closed with exact implementation SHAs and truthful validation scope.
