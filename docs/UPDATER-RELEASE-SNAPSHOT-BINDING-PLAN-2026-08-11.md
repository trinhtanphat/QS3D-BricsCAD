# Final updater release-snapshot binding plan — 2026-08-11

## Goal

Prevent a post-close manifest replacement from changing the GitHub release/version that the Update Center approved before BricsCAD closed.

## Stable scheduling anchor

The detached worker already receives and freezes the selected release's manifest URL before host close. For official one-click updates that URL has this exact identity:

`https://github.com/trinhtanphat/QS3D-BricsCAD/releases/download/<tag>/QS3D-BricsCAD-V25.update.json`

Rather than introduce a second release-tag parameter that could itself drift from the manifest URL, the final updater will derive the expected tag from this immutable manifest URI and bind all re-fetched metadata back to it.

## Contract

1. After parsing `ManifestUri` as HTTPS, detect the official QS3D GitHub release-download path using exact host/repository/asset structure, no user-info/query/fragment, one decoded tag segment, and no extra path segments.
2. For that official path, decode the tag and require it to be strict `v<SemVer>`; derive the expected productVersion by removing exactly one leading `v`.
3. After the manifest is re-fetched, require `manifest.productVersion` to equal the derived expected productVersion exactly.
4. Require `manifest.packageUri` to be the exact official package path for the same repository, same decoded tag and exact `QS3D-BricsCAD-V25.zip` asset; reject another repo/tag/asset, credentials, query or fragment.
5. Run these snapshot checks before package download and before any installer invocation.
6. Preserve direct/manual updater compatibility for non-official manifest hosts/paths: existing HTTPS/allowed-host/signer/hash/monotonic checks remain authoritative there. Official GitHub one-click/direct manifests receive the stronger snapshot binding automatically.
7. Preserve all current per-user mutex, archive, signer, product-version, stale-installed-state and transactional installer contracts.

## Why derive from ManifestUri

The manifest URI is already captured by `SecureUpdateLauncher` before the graceful host-close request and passed as a literal into the detached worker. Binding final metadata to that frozen URL avoids adding a parallel expected-tag argument and makes official direct invocation gain the same protection.

## Regression gate

Add `scripts/preflight-update-release-snapshot.py` requiring:

- exact official manifest-path parser;
- strict decoded `v<SemVer>` tag derivation;
- manifest productVersion equality to the derived tag;
- exact same-tag official package path validation;
- ordering before package `Invoke-WebRequest` and installer invocation;
- retention of signer/hash/monotonicity and mutex boundaries.

## Validation boundary

Static/source checks can prove final identity binding and ordering. Actual GitHub asset replacement timing and signed post-close update execution remain `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS will be claimed.
