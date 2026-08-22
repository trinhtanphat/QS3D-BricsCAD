# Work claim — EntitySnapshot proxy type canonicalization

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-entity-snapshot-proxy-type-canonicalization`
- Registered: `2026-08-11T22:22:00+07:00`
- Completed: `2026-08-11T22:24:00+07:00`
- Baseline main SHA: `adb8bd419871cdc64aedfde9b5431b76ec06b7f4`
- Reservation commit: `4a76a8623cc3b526d32b7d8d5006561537e31e37`
- Priority: prevent padded CAD entity-type text from bypassing ProxyEntity capture safety.

## Defect fixed

`EntitySnapshot` previously rejected blank `entityType` values but stored nonblank values unchanged. `EntitySnapshotCaptureEligibility.IsReady(...)` applies stricter metric requirements to `ProxyEntity` using a case-insensitive comparison, so surrounding whitespace could cause a metricless proxy to be treated as an ordinary entity.

`EntitySnapshot` now trims `EntityType` at the model boundary. Padded/case-varied proxy type text therefore reaches the existing `ProxyEntity` safety gate in canonical form.

## Reserved scope

- `src/QS3D.Core/Model/EntitySnapshot.cs`
- `tests/QS3D.Core.SmokeTests/ProxyCaptureEligibilitySmoke.cs`
- this claim file

## Delivered contract

- `EntitySnapshot.EntityType` is canonicalized to its trimmed nonblank representation at construction.
- Padded/case-varied `ProxyEntity` values remain subject to the same finite positive primary-metric capture gate as canonical `ProxyEntity`.
- Existing measured-proxy and normal non-proxy paths remain unchanged.
- `RecognitionEngine.cs` and other active recognition workstreams were not modified.

## Published commits

- `b843af2662d5dace1a362dd951e7ebc7c927da37` — trim `EntitySnapshot.EntityType` at construction.
- `e29035176df16ecef94012982123f6b547273010` — add padded/case-varied metricless ProxyEntity regression coverage.

## Validation notes

- Exact source diff is one constructor assignment change; exact regression diff adds only the padded proxy case to the already-registered proxy safety smoke.
- The regression asserts canonicalized type text, review-only recognition, zero auto-accept and `EnsureReady` rejection for an unmeasured padded/case-varied proxy.
- The current execution environment has no `dotnet`, so the smoke executable was not run locally in this session.
- GitHub Actions were not dispatched; repository CI remains manual-only.
- No force-push was used.

## Exclusions

- No adapter/native entity extraction changes.
- No recognition scoring/rules/category-policy changes.
- No B4D/quantity/unit policy changes.
- No BricsCAD V25 runtime qualification.

## Completion condition

Satisfied for the source/static contract. Executable Core/V25 qualification remains a separate exact-SHA gate.
