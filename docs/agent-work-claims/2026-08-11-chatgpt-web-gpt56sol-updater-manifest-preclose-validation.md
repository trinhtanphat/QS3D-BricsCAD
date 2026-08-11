# Agent Work Claim — validate update manifest before host close

- Claim ID: `UPDATER-MANIFEST-PRECLOSE-VALIDATION-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T21:45:30+07:00`
- Released: `2026-08-11T21:49:00+07:00`
- Baseline main SHA: `5fe1601e8a6878f4130669b4d70639331b665d94`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

The updater treated the mere presence of `QS3D-BricsCAD-V25.update.json` as sufficient to enable one-click update after the running plugin trust anchor was validated. The actual manifest schema/productVersion/signer/package URL/hash fields were not parsed until the detached updater ran after BricsCAD had been asked to close.

A malformed, stale, wrong-tag or otherwise incompatible manifest asset could therefore make the UI enable **Cập nhật ngay**, start the detached worker and close BricsCAD, only for `update-v25.ps1` to reject the manifest afterward. The final installer remained fail-closed, but the host closed unnecessarily and the user got only a detached failure log.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/UpdateManifestProbe.cs`
- `src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs`
- `scripts/preflight-update-manifest-preclose.py`
- this claim file

## Completed changes

### Bounded manifest probe — `cd30a7eb022ce800cd7b6024b1f8715c5920afd4`, null-safe refinement `0a9b0799e808db39042eabd08c570de9e256cca1`

Added `UpdateManifestProbe` which, before one-click eligibility:

- fetches only the selected release's exact GitHub manifest asset path and bounds the response to 64 KiB with updater timeouts/User-Agent;
- requires schemaVersion 2, product `QS3D`, target `BricsCAD V25 x64`;
- requires strict non-`v` `productVersion` exactly equal to the selected release tag with its one leading `v` removed and SemVer-equivalent to the already parsed release version;
- requires manifest assembly major/minor/build to match the selected release core;
- requires a 40-hex manifest signer exactly equal to the WinVerifyTrust-approved running-plugin signer;
- requires a 64-hex package SHA-256;
- requires both manifest and package URLs to be credential/query/fragment-free HTTPS `github.com` release-download paths for exactly `trinhtanphat/QS3D-BricsCAD`, the same tag and exact expected asset names;
- models every untrusted JSON string field as nullable and validates trimmed non-empty values before Version/URI parsing.

This is deliberately a pre-close eligibility probe. The downloaded ZIP, internal hashes, Authenticode signatures, product/assembly version binding and install transaction are still independently re-verified by `update-v25.ps1` after BricsCAD exits.

### Coordinator gating — `72b2601692238e7250e074ab6d54610a259632a6`

- preserves the completed restart single-flight and schedule lifecycle generation guards;
- after a newer release + manifest asset are found, first obtains the WinVerifyTrust-approved running signer;
- awaits `UpdateManifestProbe.ValidateAsync(...)` before constructing any `UpdateAvailable` result;
- probe failure returns `ManualInstallRequired` with the release still visible/manual-link-capable, so one-click remains disabled and BricsCAD is not asked to close;
- successful state explicitly reports that the signed manifest was validated pre-close while final package/signature/hash verification remains post-close;
- `ScheduleLatestAsync()` still performs a fresh `CheckAsync(false)` before lifecycle-linearized `SecureUpdateLauncher.TrySchedule(...)`, so a scheduling click re-probes current release manifest state.

### Regression gate — `c278501af8d190b4b4ad14f34d802cd1b9f91c0b`

Added auto-discovered `scripts/preflight-update-manifest-preclose.py` requiring:

- bounded schema-v2 manifest fetch;
- exact repo/tag/asset URL binding;
- product/target/productVersion/assembly/signer/SHA checks;
- nullable untrusted manifest fields without nullable suppression;
- running signer -> manifest probe -> `UpdateAvailable` ordering;
- fresh `CheckAsync(false)` before the existing lifecycle-authorized scheduling side effect.

## Validation / coordination

- Re-read the completed updater scheduling-lifecycle implementation before this lane and preserved its generation-linearized scheduling boundary.
- Re-fetched `UpdateManifestProbe.cs` and explicitly hardened nullable `version`/`packageUri` parsing after inspection.
- Compare at final source refinement `0a9b0799e808db39042eabd08c570de9e256cca1` reported current `main` identical (`ahead_by: 0`, `behind_by: 0`).
- No force-push, reset or rebase was used.
- No GitHub Actions workflow was dispatched and no release was published.
- No live GitHub-manifest/BricsCAD signed update was executed in this connector lane, so no native/runtime PASS is claimed.

## Result

One-click update eligibility no longer relies on manifest asset presence alone. A release manifest must pass a bounded identity/publisher/version/package-path probe while BricsCAD is still open; malformed or incompatible manifests fall back to manual install without closing the host. The hardened post-close updater remains the final security authority.
