# Agent work claim — preview release diagnostic drift

- Agent: `chatgpt-web-gpt56sol-preview-release-diagnostic-drift`
- Date: 2026-08-14
- Status: `COMPLETED`
- Baseline main SHA: `fc7e4d2ecf6abd165d65146ee61e991ad3e579ec`

## Goal

Remove stale, already-published preview tag examples from the executable preview-version synchronization/preparation diagnostics. Release operators should be shown the accepted tag shape, not a concrete historical tag that now deterministically fails the duplicate-tag guard.

## Evidence

- Current V25 product identity is `0.1.0-preview.9` and the published prerelease list already contains `v0.1.0-preview.9`, `preview.8`, and earlier preview tags.
- `scripts/prepare-v25-cloud-release.ps1` told an invalid caller to use already-published `v0.1.0-preview.7`.
- `scripts/sync-preview-release-version.ps1` told an invalid caller to use already-published `v0.1.0-preview.6`.
- Those concrete examples were historical release identities, not syntax placeholders.

## Reserved paths

- `scripts/prepare-v25-cloud-release.ps1`
- `scripts/sync-preview-release-version.ps1`
- `scripts/preflight-preview-release-diagnostics.py`
- `docs/agent-work-claims/2026-08-14-1248-chatgpt-preview-release-diagnostic-drift.md`

Read-only evidence surfaces:

- `.github/workflows/release-v25-cloud.yml`
- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj`

## Boundaries

- No release regex semantics, version synchronization, automatic release commit creation, tag publication, package contents, signing, or GitHub Actions behavior was changed.
- The cloud workflow remained read-only in this lane; its separate dispatch-input help text still contains a historical concrete example and is not silently claimed fixed here.
- No GitHub Actions were dispatched.

## Result

- `e3bc5d875328ffd72a77978740819623d52baafc` — `fix(release): make preview preparation diagnostic version-neutral`
- `6bf1fe5466b48bf7b09d426f5576af27d37196e7` — `fix(release): make preview sync diagnostic version-neutral`
- `15e43751f24b053b558044c3a85cb2bef065a6d9` — initial static guard
- `0ccaec484992b3e827b3b9b1298b3dbaf7ec315b` — corrected the guard to recognize the preparation helper's non-capturing preview regex and the synchronization helper's named `preview` group independently

Both executable helpers now preserve their existing bounded regex and rejected input value while reporting the version-neutral shape `v<major>.<minor>.<patch>-preview.<n>`. The static guard rejects future concrete `v0.1.0-preview.N` diagnostics in these helpers.

## Validation

Remote read-back confirmed both helper diagnostics and the corrected preflight source on live `main`. `scripts/preflight-all.py` auto-discovers `preflight-*.py`, so the guard participates in aggregate source validation. Local PowerShell/preflight execution and GitHub Actions were `NOT_RUN`; no executable PASS is fabricated.
