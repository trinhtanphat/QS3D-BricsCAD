# V25 release-relevant protected-main drift

## Purpose

The V25 commercial release workflow may publish only from an exact qualified workflow SHA that has not been superseded by release-relevant protected-main state. Submodule acquisition metadata is part of that state even when the `external/QS3D-Platform` gitlink itself is unchanged.

## Guarded contract

`.gitmodules` is SHA-256 bound by `scripts/preflight-v25-release-relevant-main-drift.py`. The V25 final publication classifier already treats `scripts/` and `external/` as release-relevant paths. Therefore a legitimate `.gitmodules` edit must update the checked-in fingerprint in the guard in the same candidate; that scripts change makes every older workflow SHA stale at final publication admission.

This binding is intentionally fail-closed. Do not update the expected digest merely to make CI green. Review the submodule URL/path/update semantics, update the digest only with the same reviewed `.gitmodules` change, and run the full protected source validation on that exact candidate.

The guard also pins the existing final-publication sequence: workflow-SHA ancestry against protected main, release-relevant diff classification, a second protected-main identity confirmation, and only then the publish PATCH. It must not weaken or reorder those checks.

## Deterministic validation

Run the auto-discovered source guards through Shared CI. The focused guard mutation-tests both sides of the binding: changing `.gitmodules` without refreshing its scripts fingerprint must fail, and removing `scripts/` from the final release-relevant path classifier must fail.

Protected `preflight` must also keep PowerShell syntax and V25 package-integrity checks green. `core` must complete the normal deterministic smoke, trusted V25 reference acquisition/validation, V25 plugin build, and final build for the exact candidate before integration.

## Publication behavior

If protected main advances only through genuinely non-release paths, the existing workflow may retain its exact release provenance. If protected main changes `.gitmodules`, the paired guard update under `scripts/` makes that advance release-relevant, so a stale commercial release must stop before publication. Start a new release from the newest qualified main rather than bypassing the drift gate.

This runbook is REMOTE_SAFE release-readiness guidance. It does not claim licensed BricsCAD runtime acceptance, signing credentials, or a commercial release was actually published.