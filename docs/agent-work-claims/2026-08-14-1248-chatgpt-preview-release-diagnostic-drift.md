# Agent work claim — preview release diagnostic drift

- Agent: `chatgpt-web-gpt56sol-preview-release-diagnostic-drift`
- Date: 2026-08-14
- Status: `ACTIVE`
- Baseline main SHA: `fc7e4d2ecf6abd165d65146ee61e991ad3e579ec`

## Goal

Remove stale, already-published preview tag examples from the executable preview-version synchronization/preparation diagnostics. Release operators should be shown the accepted tag shape, not a concrete historical tag that now deterministically fails the duplicate-tag guard.

## Evidence

- Current V25 product identity is `0.1.0-preview.9` and the published prerelease list already contains `v0.1.0-preview.9`, `preview.8`, and earlier preview tags.
- `scripts/prepare-v25-cloud-release.ps1` still tells an invalid caller to use `v0.1.0-preview.7`.
- `scripts/sync-preview-release-version.ps1` still tells an invalid caller to use `v0.1.0-preview.6`.
- Those concrete examples are historical release identities, not syntax placeholders.

## Reserved paths

- `scripts/prepare-v25-cloud-release.ps1`
- `scripts/sync-preview-release-version.ps1`
- `scripts/preflight-preview-release-diagnostics.py`
- `docs/agent-work-claims/2026-08-14-1248-chatgpt-preview-release-diagnostic-drift.md`

Read-only evidence surfaces:

- `.github/workflows/release-v25-cloud.yml`
- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj`

## Boundaries

- Do not change release regex semantics, version synchronization, automatic release commit creation, tag publication, package contents, signing, or GitHub Actions behavior.
- Do not edit the cloud workflow in this lane because other release/UI coordination is moving rapidly; this lane fixes executable helper diagnostics only and guards them against concrete `v0.1.0-preview.N` examples.
- Do not dispatch GitHub Actions.
- Refresh `main` before every write and stop if another claim touches either reserved script.

## Validation

Add an auto-discovered static preflight that requires both helpers to describe the generic `v<major>.<minor>.<patch>-preview.<n>` shape and rejects concrete `v0.1.0-preview.<number>` examples in those executable diagnostics. Perform remote read-back; local PowerShell/preflight execution is not available through this connector and must not be claimed.
