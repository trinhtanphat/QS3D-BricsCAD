# QS3D Release Naming and Versioning Policy

This document is the canonical naming contract for public QS3D for BricsCAD Git tags, GitHub Releases, prerelease channels, and downloadable packages.

The goal is simple: a public version must be short, sortable, deterministic, and independent from CI counters or the BricsCAD host-major package name.

## 1. Product name

The canonical product display name is:

```text
QS3D for BricsCAD
```

Do not append `V25`, `V26`, Windows architecture, CI run numbers, dates, commit counts, or other build details to the product name itself.

BricsCAD host compatibility belongs in release notes and package asset names, not in the public product/version identity.

## 2. Git tag format

Public tags use SemVer with a leading `v`.

Stable:

```text
vMAJOR.MINOR.PATCH
```

Prerelease:

```text
vMAJOR.MINOR.PATCH-CHANNEL.N
```

Allowed prerelease channels are:

- `preview` — early owner/tester builds; functionality may still change.
- `alpha` — structural stabilization before broader testing.
- `beta` — substantially complete feature set under broader validation.
- `rc` — release candidate intended to become the matching stable version if no blocker is found.

`N` is a positive decimal sequence starting at `1` for a new base version/channel and increasing by exactly one for each published public prerelease in that series.

Examples:

```text
v0.1.1-preview.1
v0.1.1-preview.2
v0.1.1-beta.1
v0.1.1-rc.1
v0.1.1
```

## 3. GitHub Release title

The GitHub Release title is derived directly from the tag and must use exactly this shape:

```text
QS3D for BricsCAD <TAG>
```

Examples:

```text
QS3D for BricsCAD v0.1.1-preview.1
QS3D for BricsCAD v0.1.1-rc.1
QS3D for BricsCAD v0.1.1
```

Do not independently invent release-title version text. In particular, do not produce titles such as:

```text
QS3D for BricsCAD V25 v0.1.0-preview.10014
QS3D V25 Preview Build 10014
QS3D for BricsCAD 2026-08-14 build 31797100976
```

The title must not encode information that is absent from the canonical public tag.

## 4. Host compatibility is not version identity

QS3D supports host-major-specific BricsCAD plugin assemblies. `V25` and `V26` identify compatible package/runtime surfaces; they do not define the QS3D SemVer version.

A release may therefore contain separate host-specific assets under one QS3D tag.

Recommended package names:

```text
QS3D-BricsCAD-V25-<TAG>-win-x64.zip
QS3D-BricsCAD-V26-<TAG>-win-x64.zip
QS3D-BricsCAD-V25-<TAG>-win-x64.zip.sha256
QS3D-BricsCAD-V26-<TAG>-win-x64.zip.sha256
```

Example:

```text
QS3D-BricsCAD-V25-v0.1.1-preview.1-win-x64.zip
QS3D-BricsCAD-V26-v0.1.1-preview.1-win-x64.zip
```

Release notes must state which host majors are actually included and qualified. Never relabel a V25 assembly/package as V26 or vice versa.

## 5. CI/build identifiers must stay out of public versions

The following are traceability metadata, not public version components:

- GitHub Actions run number;
- GitHub Actions run ID/job ID;
- commit count;
- timestamp/date sequence;
- arbitrary monotonic build counter;
- full or short Git SHA.

Do not use any of them as the prerelease ordinal merely because the value is available in CI.

For traceability, record them in release notes, checksums/manifests, provenance, or internal artifact names. At minimum, release notes should retain the exact source commit SHA used to produce the published package.

## 6. Published tags are immutable identities

Once a public tag/release has been published and consumed, do not move the tag to another commit, overwrite its package with different bytes, or silently reuse the same version for a different source tree.

If a corrected public build is required, publish a new valid version according to this policy.

Historical names that predate this policy may remain for audit/history. They do not become examples for future naming.

## 7. Migration from `v0.1.0-preview.10014`

The historical tag:

```text
v0.1.0-preview.10014
```

already exists. Under SemVer numeric prerelease ordering, `preview.15` is older than `preview.10014`; therefore the project must **not** attempt to "clean up" the same `0.1.0` series by publishing `v0.1.0-preview.15`, `v0.1.0-preview.1`, or any other smaller numeric ordinal after it.

Preferred migration:

1. Keep the historical `v0.1.0-preview.10014` tag/release unchanged for auditability.
2. Start the clean numbering convention at the next owner-approved base version, for example:

   ```text
   v0.1.1-preview.1
   ```

3. From that point onward, increment prerelease ordinals sequentially (`.1`, `.2`, `.3`, ...), never from CI run/build counters.

If the owner explicitly requires another public prerelease on the old `0.1.0-preview` line, its numeric ordinal must remain greater than `10014`; that is a compatibility exception for the historical line, not the convention for new version lines.

## 8. Automation requirements

Any workflow or release agent that creates a public tag or GitHub Release must follow this document.

Before publishing, automation should verify all of the following:

1. the tag matches the stable or allowed prerelease format above;
2. the release title is exactly `QS3D for BricsCAD <TAG>`;
3. prerelease ordinals are not derived from CI/build counters;
4. host-major information is placed in asset names and release compatibility notes rather than the public version identity;
5. the exact source commit is recorded;
6. an existing public tag is never silently moved or overwritten.

If an automatically generated name violates this policy, publishing should fail rather than create another ad-hoc public version.

## 9. Quick reference

| Item | Canonical example |
| --- | --- |
| Preview tag | `v0.1.1-preview.1` |
| RC tag | `v0.1.1-rc.1` |
| Stable tag | `v0.1.1` |
| Preview release title | `QS3D for BricsCAD v0.1.1-preview.1` |
| Stable release title | `QS3D for BricsCAD v0.1.1` |
| V25 asset | `QS3D-BricsCAD-V25-v0.1.1-preview.1-win-x64.zip` |
| V26 asset | `QS3D-BricsCAD-V26-v0.1.1-preview.1-win-x64.zip` |
| CI run ID | Release notes/provenance only |
| Commit SHA | Release notes/provenance only |

This policy applies to all future public QS3D for BricsCAD releases unless the repository owner explicitly changes the versioning strategy.
