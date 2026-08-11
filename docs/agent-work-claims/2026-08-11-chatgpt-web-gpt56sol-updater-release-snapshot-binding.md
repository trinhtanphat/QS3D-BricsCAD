# Agent Work Claim — bind final updater to selected GitHub release snapshot

- Claim ID: `UPDATER-RELEASE-SNAPSHOT-BINDING-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T23:02:30+07:00`
- Baseline main SHA: `0581b5db3a0e185b6855d1dbfce58282439c74e6`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

The pre-close manifest probe binds the selected GitHub release tag to schema/productVersion/package URL before enabling one-click. After BricsCAD closes, however, the detached worker re-fetches the manifest and invokes `update-v25.ps1` with only the manifest URI/current signer/install directory. The final updater verifies that the newly fetched manifest/package is internally consistent, signed and newer than installed state, but it is not told which release tag/productVersion the UI approved.

If the release manifest asset changes between pre-close validation and post-close fetch, a different valid QS3D package signed by the same publisher and newer than the installed version can satisfy the final updater. The UI can therefore approve release A while the post-close worker installs release B. This is a release-identity TOCTOU/mix-and-match gap, even though publisher and monotonic-version security remain intact.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs`
- `scripts/update-v25.ps1`
- `scripts/preflight-update-release-snapshot.py` (new)
- `scripts/preflight-auto-update.py` / product-version gates only if narrow compatibility updates are required
- `docs/UPDATER-RELEASE-SNAPSHOT-BINDING-PLAN-2026-08-11.md` (new)
- this claim file

## Non-overlap / preservation

- Preserve current pre-close manifest probe, readiness/cancellation, cross-process/manual-entry mutexes, post-failure restart, WinVerifyTrust/current signer pinning, installed updater Authenticode validation, archive/hash/product-version gates and installer rollback.
- Do not edit GitHubReleaseClient, UpdateCoordinator/UI, manifest generator, release workflow or unrelated lanes.
- No Actions dispatch or release publication.

## Intended contract

1. `SecureUpdateLauncher.TrySchedule` captures the exact selected release tag before worker launch.
2. Worker passes that expected release tag to `update-v25.ps1` as immutable scheduling input.
3. Final updater derives the expected productVersion from that tag using strict SemVer rules and requires the re-fetched manifest `productVersion` to match it exactly.
4. For official GitHub one-click updates, final updater also requires the re-fetched manifest package URI to be the exact QS3D-BricsCAD release-download path for the same expected tag and `QS3D-BricsCAD-V25.zip`, rejecting another repo/tag/asset even when it is signed by the same publisher.
5. Direct/manual updater usage may omit the expected-release snapshot and retain its existing signed/monotonic behavior; one-click always supplies it.
6. Any post-close release-snapshot mismatch fails before package download/install; existing worker failure-restart restores BricsCAD best effort.

## Validation / release conditions

- Commit planning MD before implementation.
- Add auto-discovered regression coverage proving release tag handoff and exact final productVersion/package-path binding before package download/install.
- Re-fetch source/gates and verify ancestry with `behind_by: 0`.
- Native TOCTOU/update behavior remains `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS claim.
- Release claim only after source + gate are on `main`.