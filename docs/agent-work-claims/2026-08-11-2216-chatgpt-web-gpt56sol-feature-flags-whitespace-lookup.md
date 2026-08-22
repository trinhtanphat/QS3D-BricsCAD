# Agent work claim — feature flags whitespace lookup normalization

- Status: `COMPLETED`
- Owner: ChatGPT Web / GPT-5.6 Sol
- Track: Core feature-flag lookup integrity
- Mode: Remote source-safe
- Started: 2026-08-11 22:16 +07
- Completed: 2026-08-11 22:26 +07
- Baseline main SHA: `2f8027ad3999785daee16a6cd326aae9f33d5b66`

## Reason

`FeatureFlags.Set()` canonicalized nonblank names with `Trim()` before storing them, while `FeatureFlags.IsEnabled()` looked up the caller-provided string without the same trim. The same logical feature could therefore be stored successfully and then reported disabled solely because the lookup contained surrounding whitespace.

## Claimed paths

- `src/QS3D.Core/Features/FeatureFlags.cs`
- `tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs`
- `docs/agent-work-claims/2026-08-11-2216-chatgpt-web-gpt56sol-feature-flags-whitespace-lookup.md`

## Implemented change

- `FeatureFlags.IsEnabled()` now trims nonblank lookup names before dictionary lookup, matching `Set()` while preserving the existing `StringComparer.OrdinalIgnoreCase` semantics.
- The already-registered `HardeningRegressionSmoke` suite now covers whitespace-equivalent and case-insensitive enabled lookup plus normalized disabled-state lookup.

## Implementation commits

- `89c7eac66780d3f1c362303d0252d30b1873605d` — normalize `FeatureFlags.IsEnabled()` lookup names.
- `aef0b062c68cdb33f5a28f0a830858a230f3be59` — add the hardening smoke regression.
- Final observed `main` before claim close: `aef0b062c68cdb33f5a28f0a830858a230f3be59`.

## Excluded scope

- Licensing feature signature/canonical-payload policy.
- BricsCAD V25 runtime/UI/native geometry/signing validation.
- Unrelated feature gates or product behavior.
- GitHub Actions dispatch.

## Validation actually performed

- Re-read `src/QS3D.Core/Features/FeatureFlags.cs` from `main` at `aef0b062c68cdb33f5a28f0a830858a230f3be59` and confirmed `IsEnabled()` calls `name.Trim()` before lookup.
- Re-read `tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs` from the same `main` SHA and confirmed `QS3D.Core.Features` is imported, `FeatureFlagsNormalizeLookupNames()` is invoked by `Run()`, and the regression covers trimmed/case-insensitive enabled lookup and normalized disabled lookup.
- Confirmed `89c7eac66780d3f1c362303d0252d30b1873605d` is an ancestor of `aef0b062c68cdb33f5a28f0a830858a230f3be59`.
- Concurrent `main` movement was handled without force-push; stale publish attempts were rejected/rebased, and no unrelated history was overwritten.
- Local executable build/smoke was not run because this hosted environment could not obtain a usable repository checkout from GitHub (CLI/network limitation).
- GitHub Actions were not dispatched or rerun, consistent with `CI_POLICY.md`.

## Coordination

- Reviewed the claim inventory and the related `license-feature-canonicalization` lane; that lane was released and scoped to `LicenseVerifier`, not generic `FeatureFlags`.
- The regression was intentionally placed in the already-registered `HardeningRegressionSmoke` suite to avoid editing the shared smoke registry hot spot.

## Completion condition

Satisfied: the same nonblank feature name now resolves consistently across `Set()` and `IsEnabled()` when callers vary surrounding whitespace or letter case, with deterministic regression coverage published on `main`.
