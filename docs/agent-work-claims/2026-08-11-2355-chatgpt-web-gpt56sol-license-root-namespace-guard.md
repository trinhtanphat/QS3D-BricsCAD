# Work claim — license XML root namespace guard

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:55:00+07:00`
- Completed: `2026-08-11T23:57:00+07:00`
- Baseline main SHA observed before reservation: `04ef0469d48b2ab866f17dca13e5ab72a3abf22a`
- Reservation commit: `44ef4e8e936ae614145ccf2c8c86dad1f0bdfb88`
- Priority: continue-all remote-safe licensing parser integrity

## Reserved scope

- `src/QS3D.Core/Licensing/LicenseVerifier.cs`
- `tests/QS3D.Core.SmokeTests/LicenseVerifierSmoke.cs`
- this claim file

## Defect fixed

`LicenseVerifier.Load(...)` previously validated the XML root by `XName.LocalName`. A prefixed namespaced root such as `x:qs3dLicense` could therefore satisfy the root-name check while deliberately unnamespaced child elements remained readable by the loader. The parser now requires both the exact `qs3dLicense` local name and an empty namespace, so persisted licenses with default or prefixed root namespaces fail closed.

## Regression coverage

- Existing canonical unnamespaced signed-license verification remains in the registered smoke suite.
- Added a default-namespaced root rejection case.
- Added the decisive prefixed-namespaced root case with deliberately unnamespaced children; this case would pass the old LocalName-only root guard far enough to load successfully.
- Existing DTD, signature, feature-delimiter and token-whitespace coverage remains intact.

## Published result

- PR `#541` — `fix(licensing): reject namespaced license roots`
- Squash merge commit: `a255325e82f919f3fddf55e3e556b42962620dc1`

## Validation notes

- Re-read the claimed source/test files from `main` before implementation; both were unchanged from the reserved baseline despite unrelated concurrent commits.
- The PR changed exactly the two claimed source/test files and merged successfully with expected head SHA `3f86477598f29b9beb697cd1347147ff5a2a788a`.
- Validation in this session is source/static plus focused smoke coverage inspection only. No executable smoke/build run is claimed.
- GitHub Actions were not dispatched. No BricsCAD V25/native/UI/runtime PASS is claimed.

## Explicit exclusions

- No Project Browser/Start Center/browser XML work.
- No feature canonicalization or token-whitespace work already completed by earlier claims.
- No hardware fingerprint, entitlement, SKU, trial/subscription, signing-key, updater, installer or release policy changes.
- No BricsCAD V25/native/UI/runtime work.
