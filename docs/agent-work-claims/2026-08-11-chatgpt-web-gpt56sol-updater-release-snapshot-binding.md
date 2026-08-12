# Agent Work Claim — bind final updater to selected GitHub release snapshot

- Claim ID: `UPDATER-RELEASE-SNAPSHOT-BINDING-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T23:02:30+07:00`
- Updated: `2026-08-11T23:04:30+07:00`
- Released: `2026-08-11T23:08:00+07:00`
- Baseline main SHA: `0581b5db3a0e185b6855d1dbfce58282439c74e6`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

The pre-close manifest probe bound the selected GitHub release tag before enabling one-click, but the final post-close updater re-fetched the manifest and previously verified only internal consistency, publisher and monotonicity. A changed manifest could therefore substitute a different newer package signed by the same QS3D publisher after the user had approved another release.

## Completed changes

- `ebf67b9055634e335dd01a47437833c0b76e76ee` — registered this claim before implementation.
- `1d1caae8be1344ff0c6a46f40226b936b33edd19` — committed the final release-snapshot binding plan before code.
- `b21846d1ac51951c797b8e9e9f374d93a5784274` — reconciled the claim to the lower-surface design: derive the expected release tag from the immutable official manifest URI already frozen into the detached worker rather than adding a parallel C# tag argument.
- `2f65d7df702f44960b59677e81ee1b6750bd6d6f` — `update-v25.ps1` now recognizes the exact official QS3D GitHub manifest path, decodes and strictly validates its `v<SemVer>` tag, derives the expected productVersion, requires the re-fetched manifest productVersion to match, and requires the re-fetched package URI to resolve to the exact same repository/tag and `QS3D-BricsCAD-V25.zip` asset before package download.
- `b8d12904b97f580ed95f497b2978f8cbd0c2b3ab` — added auto-discovered `preflight-update-release-snapshot.py`, locking snapshot derivation and product/package identity checks before ZIP download and installer invocation while preserving mutex, signer, hash, monotonic version and stale-installed-state gates.

## Resulting contract

1. Official one-click updates are anchored to the manifest URI frozen before graceful host close.
2. A post-close replacement of that manifest cannot switch the release productVersion or package to another repo/tag/asset, even if that other package is otherwise valid and signed by the same publisher.
3. Snapshot mismatch fails before ZIP download/install; the existing post-close failure recovery can restore BricsCAD best effort.
4. Non-official/manual HTTPS manifests retain their existing signed/hash/monotonic behavior instead of being forced into the GitHub-specific path convention.
5. Current cross-entry mutex, archive safety, Authenticode, package metadata/productVersion and transactional installer contracts remain intact.

## Integration verification

- Re-fetched current updater source after the change; official manifest/package helpers and ordering are present in blob `8c61df921a418e41725541f6e3b459cba5e3ef1e`.
- Compare from `b8d12904b97f580ed95f497b2978f8cbd0c2b3ab` to current `main` reported `ahead_by: 0`, `behind_by: 0` at verification time.
- No GitHub Actions workflow was dispatched and no release was published.

## Validation boundary

Source/static final-release identity is hardened. Actual GitHub asset-replacement timing and signed post-close update execution remain `LOCAL-009 / PENDING_LOCAL`; this lane does not claim native/runtime PASS.