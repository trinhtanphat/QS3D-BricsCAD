# Agent Work Claim — bind final updater to selected GitHub release snapshot

- Claim ID: `UPDATER-RELEASE-SNAPSHOT-BINDING-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T23:02:30+07:00`
- Updated: `2026-08-11T23:04:30+07:00`
- Baseline main SHA: `0581b5db3a0e185b6855d1dbfce58282439c74e6`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

The pre-close manifest probe binds the selected GitHub release tag to schema/productVersion/package URL before enabling one-click. After BricsCAD closes, however, the detached worker re-fetches the manifest. The final updater verifies that the newly fetched manifest/package is internally consistent, signed and newer than installed state, but it does not bind those re-fetched fields back to the release identity that was approved before host close.

If the release manifest asset changes between pre-close validation and post-close fetch, a different valid QS3D package signed by the same publisher and newer than the installed version can satisfy the final updater. The UI can therefore approve release A while the post-close worker installs release B. This is a release-identity TOCTOU/mix-and-match gap, even though publisher and monotonic-version security remain intact.

## Reserved scope

- `scripts/update-v25.ps1`
- `scripts/preflight-update-release-snapshot.py` (new)
- `scripts/preflight-auto-update.py` / product-version gates only if narrow compatibility updates are required
- `docs/UPDATER-RELEASE-SNAPSHOT-BINDING-PLAN-2026-08-11.md` (new)
- this claim file

`SecureUpdateLauncher.cs` was initially reserved but no edit is required: it already freezes the selected release's exact manifest URI into the detached worker before graceful host close. The safer design derives the expected tag from that immutable URI instead of adding a second parallel tag parameter.

## Non-overlap / preservation

- Preserve current pre-close manifest probe, readiness/cancellation, cross-process/manual-entry mutexes, post-failure restart, WinVerifyTrust/current signer pinning, installed updater Authenticode validation, archive/hash/product-version gates and installer rollback.
- Do not edit SecureUpdateLauncher, GitHubReleaseClient, UpdateCoordinator/UI, manifest generator, release workflow or unrelated lanes.
- No Actions dispatch or release publication.

## Intended contract

1. Final updater recognizes the frozen official manifest URI shape `https://github.com/trinhtanphat/QS3D-BricsCAD/releases/download/<tag>/QS3D-BricsCAD-V25.update.json` and derives the expected decoded release tag from that already-scheduled URI.
2. The derived tag must be exact lowercase `v` + strict SemVer; final updater derives expected `productVersion` by removing that one leading `v`.
3. Re-fetched manifest `productVersion` must equal the derived expected productVersion exactly.
4. Re-fetched `packageUri` must be the exact QS3D-BricsCAD release-download path for the same decoded tag and exact `QS3D-BricsCAD-V25.zip`, rejecting another repo/tag/asset even when it is signed by the same publisher.
5. Direct/manual updater compatibility remains for non-official manifest hosts/paths; official QS3D GitHub manifests automatically receive the stronger snapshot binding.
6. Any post-close release-snapshot mismatch fails before package download/install; existing worker failure-restart restores BricsCAD best effort.

## Validation / release conditions

- Planning MD `docs/UPDATER-RELEASE-SNAPSHOT-BINDING-PLAN-2026-08-11.md` was committed before implementation.
- Add auto-discovered regression coverage proving exact official manifest-path derivation and final productVersion/package-path binding before package download/install.
- Re-fetch source/gates and verify ancestry with `behind_by: 0`.
- Native TOCTOU/update behavior remains `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS claim.
- Release claim only after source + gate are on `main`.