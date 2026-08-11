# Agent work claim — feature flags whitespace lookup normalization

- Status: `ACTIVE`
- Owner: ChatGPT Web / GPT-5.6 Sol
- Track: Core feature-flag lookup integrity
- Mode: Remote source-safe
- Started: 2026-08-11 22:16 +07
- Baseline main SHA: `2f8027ad3999785daee16a6cd326aae9f33d5b66`

## Reason

`FeatureFlags.Set()` canonicalizes nonblank names with `Trim()` before storing them, while `FeatureFlags.IsEnabled()` currently looks up the caller-provided string without the same trim. The same logical feature can therefore be stored successfully and then reported disabled solely because the lookup contains surrounding whitespace.

## Claimed paths

- `src/QS3D.Core/Features/FeatureFlags.cs`
- `tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs`
- `docs/agent-work-claims/2026-08-11-2216-chatgpt-web-gpt56sol-feature-flags-whitespace-lookup.md`

## Expected change

- Make `IsEnabled()` use the same surrounding-whitespace canonicalization already used by `Set()` while retaining the existing case-insensitive dictionary semantics.
- Add a deterministic Core smoke regression in the already-registered hardening suite covering whitespace-equivalent and case-insensitive lookup, including disabled-state behavior.

## Excluded scope

- Licensing feature signature/canonical-payload policy.
- BricsCAD V25 runtime/UI/native geometry/signing validation.
- Unrelated feature gates or product behavior.
- GitHub Actions dispatch.

## Validation plan

- Re-read current `main` before implementation and reject stale/overlapping writes.
- Review the exact source and smoke diff after publication.
- Confirm the implementation commit remains an ancestor of current `main` after publication.
- Do not dispatch GitHub Actions without explicit owner authorization, per `CI_POLICY.md`.
- Local executable smoke/build cannot be claimed unless the environment gains a usable repository checkout and .NET toolchain.

## Coordination

- Reviewed the current claim inventory for filename-level overlap and the related `license-feature-canonicalization` claim; that lane is `RELEASED` and is scoped to `LicenseVerifier`, not generic `FeatureFlags`.
- The regression is intentionally placed in the already-registered `HardeningRegressionSmoke` suite to avoid editing the shared smoke registry hot spot.

## Completion condition

The same nonblank feature name must resolve consistently across `Set()` and `IsEnabled()` when callers vary surrounding whitespace or letter case, with regression coverage published on `main`.
