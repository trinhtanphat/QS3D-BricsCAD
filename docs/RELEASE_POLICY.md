# QS3D Preview Release Policy

This document defines how GitHub-hosted BricsCAD V25 preview releases are batched, built, described, and published.

## Goals

Preview releases should be useful test checkpoints rather than one-release-per-merge noise. The repository therefore keeps validation frequent while batching publication:

- branch and pull-request CI still runs on every applicable change;
- release-relevant landings on `main` are accumulated;
- the automatic V25 preview dispatcher publishes only when the pending batch contains **at least 10 release-relevant main integrations**;
- documentation-only changes do not count toward the release threshold and do not trigger the dispatcher;
- each published preview keeps exact source provenance, deterministic package verification, and generated GitHub release notes.

There is intentionally no time-based automatic release below the 10-change threshold. If fewer than 10 release-relevant integrations are pending, the changes wait for the next qualifying integration.

## What counts as one change

For batching, one change means one **first-parent integration commit on `main`** after the previous published `v0.1.0-preview.*` tag whose introduced paths include at least one release-relevant path.

Release-relevant paths are:

- `src/**`
- `tests/**`
- `scripts/**`
- `Directory.Build.props`
- `QS3D.sln`
- `QS3D.V26.sln`
- `.github/workflows/release-v25-cloud.yml`
- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`

This definition deliberately counts a merged PR as one integration checkpoint even when that PR contains multiple internal commits. It also counts a legitimate direct integration commit if it changes a release-relevant path. Commit-message prefixes do not affect the decision; changed paths are authoritative.

Ordinary Markdown, documentation, issue-template, or repository-presentation updates do not count unless the same integration also changes a release-relevant path.

## Automatic release flow

`.github/workflows/dispatch-v25-cloud-after-main-integration.yml` is the automatic batch gate.

On each watched push to `main`, it:

1. debounces adjacent landings;
2. verifies that the triggering SHA is still an unambiguous exact-main release candidate;
3. fetches published preview tags;
4. refuses to start a second V25 cloud release while one is queued or running;
5. evaluates the pending batch with `scripts/v25-release-batch-gate.py`;
6. exits successfully without reserving a preview number when fewer than 10 release-relevant integrations are pending;
7. once the threshold is met, reserves the next canonical preview ordinal;
8. dispatches `release-v25-cloud.yml` pinned to the exact triggering source SHA.

The lightweight dispatcher may therefore run after many main integrations without publishing anything. That is expected behavior.

## Manual releases

The automatic dispatcher always honors the 10-change threshold. An owner who explicitly needs an exceptional smaller preview can use the existing owner-controlled manual release workflow with its explicit `RELEASE` confirmation. That path is an operational override, not the normal cadence.

Manual publication must still preserve exact source identity, tag/version binding, build gates, package checksums, asset verification, and all other release workflow protections. An empty/no-change preview should not be published merely to advance the ordinal.

## Release notes

GitHub generated release notes remain enabled. `.github/release.yml` groups pull requests into readable categories so the `What's Changed` section emphasizes product work instead of presenting an undifferentiated list.

The fixed build-security explanation is intentionally kept in this policy instead of being repeated as a large paragraph in every release. Individual release pages should stay concise: exact source provenance, preview status, a short verification summary, generated changes, and the full changelog comparison.

## Build and package verification

The GitHub-hosted V25 preview workflow performs repository source guards, deterministic Core build/smoke validation, and a V25 adapter compile on `windows-latest`.

For the V25 compile reference set, the workflow uses the pinned BricsCAD V25.2.10 x64 installer identity. Restored or downloaded installer candidates are re-hashed, require a valid Bricsys Authenticode signer, and must report the expected BricsCAD product name/version before reference extraction.

Before publication, the preview ZIP is checked against its external SHA-256 and internal `SHA256SUMS.txt`. Draft-release assets are downloaded again, compared byte-for-byte by SHA-256 with the local upload sources, and re-verified before the draft is published.

The cloud workflow does **not** claim real licensed BricsCAD `NETLOAD` or native runtime/UI validation. The package is an unsigned prerelease preview and intentionally excludes BricsCAD runtime assemblies.

## Versioning

Automatic V25 previews remain in the canonical series:

`v0.1.0-preview.<ordinal>`

Ordinals are monotonic and must remain inside the repository's supported FileVersion range. Reservation logic prevents concurrent dispatchers from selecting the same preview ordinal.

## Operational expectations

A healthy cycle looks like this:

1. changes are implemented and validated on task branches/PRs;
2. authorized changes land on `main`;
3. the dispatcher records the growing pending batch but publishes nothing for counts 1 through 9;
4. the tenth release-relevant integration triggers one exact-main preview build;
5. the release page contains the accumulated generated changes rather than a one-line changelog;
6. the next batch starts counting after that published preview tag.

If an automatic release fails, fix the underlying defect and let a later qualifying main integration retry from the still-unreleased batch. Do not weaken the threshold, provenance checks, or package verification to make a release pass.
