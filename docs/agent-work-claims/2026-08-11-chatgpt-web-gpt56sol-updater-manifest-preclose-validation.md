# Agent Work Claim — validate update manifest before host close

- Claim ID: `UPDATER-MANIFEST-PRECLOSE-VALIDATION-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T21:45:30+07:00`
- Baseline main SHA: `5fe1601e8a6878f4130669b4d70639331b665d94`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

The updater currently treats the mere presence of `QS3D-BricsCAD-V25.update.json` as sufficient to enable one-click update after the running plugin trust anchor is validated. The actual manifest schema/productVersion/signer/package URL/hash fields are not parsed until the detached updater runs after BricsCAD has been asked to close.

A malformed, stale, wrong-tag or otherwise incompatible manifest asset can therefore make the UI enable **Cập nhật ngay**, start the detached worker and close BricsCAD, only for `update-v25.ps1` to reject the manifest afterward. The final installer remains fail-closed, but the host closes unnecessarily and the user gets only a detached failure log.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/UpdateManifestProbe.cs` (new)
- `src/QS3D.BricsCAD.V25/Updates/UpdateCoordinator.cs`
- `scripts/preflight-update-manifest-preclose.py` (new)
- this claim file

## Explicit non-overlap

- The earlier updater scheduling-lifecycle claim is `COMPLETED`; its generation-linearized `ScheduleLatestAsync()` authorization must be preserved.
- Do not edit `SecureUpdateLauncher.cs`, `GitHubReleaseClient.cs`, Update Center UI, PowerShell updater/manifest scripts, release workflow, signing code or unrelated product lanes.

## Intended contract

1. Before returning `UpdateAvailable`, fetch the selected release manifest through its already GitHub-allowlisted HTTPS asset URL with a 64 KiB hard response bound and normal updater timeouts/User-Agent.
2. Require schemaVersion 2, product `QS3D`, target `BricsCAD V25 x64`, strict non-`v` productVersion exactly equal to the selected GitHub release tag after removing its single leading `v`, valid assembly `version` whose major/minor/build match the release core, 64-hex SHA-256, and signer thumbprint equal to the WinVerifyTrust-approved running plugin signer.
3. Require the manifest asset URL and package URL to be exact GitHub release-download paths for this repository, the same release tag, and the exact expected asset names; reject credentials/query/fragment or another repo/tag.
4. Any manifest download/parse/validation failure leaves the release visible but returns `ManualInstallRequired`; one-click remains disabled and BricsCAD stays open.
5. `ScheduleLatestAsync()` performs a fresh `CheckAsync(false)`, so its existing lifecycle-linearized scheduling side effect can only run after a fresh manifest probe succeeds.
6. Preserve final `update-v25.ps1` verification as the authoritative post-close/package-security gate; this probe is a pre-close usability/integrity filter, not a replacement.

## Validation / release conditions

- Add a focused auto-discovered preflight requiring manifest probing before `UpdateAvailable` and before `SecureUpdateLauncher.TrySchedule` can become reachable.
- Re-fetch current coordinator/new probe after writes; preserve nullable and lifecycle contracts.
- Verify implementation commits remain ancestors of current `main` with `behind_by: 0`.
- Do not dispatch GitHub Actions or publish a release.
- Native network/BricsCAD/signed-update proof remains local/manual qualification; no remote runtime PASS claim.
- Mark `RELEASED` only after source + regression gate are on `main`.
