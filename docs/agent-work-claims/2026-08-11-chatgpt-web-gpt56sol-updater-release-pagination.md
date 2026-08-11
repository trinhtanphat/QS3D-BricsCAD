# Agent Work Claim — bounded GitHub release pagination

- Claim ID: `UPDATER-RELEASE-PAGINATION-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T21:30:10+07:00`
- Baseline main SHA: `7224baa13b03e5599419bdd6f025ca3dbb2040f3`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

`GitHubReleaseClient` currently requests only `releases?per_page=20`, then the coordinator filters stable/prerelease and sorts those 20 entries by SemVer. GitHub orders release-list pages by release chronology, not by the updater's channel/SemVer policy. A stable update can therefore be hidden beyond the first 20 entries by newer-in-time prereleases, causing a stable client to incorrectly report that it is up to date.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/GitHubReleaseClient.cs`
- `scripts/preflight-update-release-pagination.py` (new)
- this claim file

## Explicit non-overlap

- Do not edit `scripts/preflight-auto-update.py`; it is currently owned by the active updater manifest-v2 compatibility claim.
- Preserve its exact pinned `ReleasesEndpoint = ...releases?per_page=20` marker so the existing security gate remains valid.
- Do not edit update manifest/package PowerShell scripts, SecureUpdateLauncher, UpdateCoordinator/UI, release workflow or unrelated product lanes.

## Planned fix

1. Keep the pinned HTTPS repository endpoint and 20-entry page size.
2. Fetch release pages sequentially with explicit `page=N` and a hard maximum page count.
3. Keep the existing per-response byte bound, HTTPS/API headers and JSON validation on every page.
4. Stop as soon as GitHub returns a short page.
5. If the final allowed page is still full, fail closed with a bounded-scan error instead of silently declaring the user up to date from an incomplete history window.
6. Convert/aggregate valid releases only after each page is bounded and parsed; preserve prerelease metadata consistency and GitHub URL allowlists.
7. Add a separate auto-discovered static preflight that requires bounded sequential pagination and the fail-closed scan ceiling.

## Validation / release conditions

- Re-read current `main` before writes and preserve the completed nullable contracts in `GitHubReleaseClient.cs`.
- Re-fetch source after commit and verify ancestry with `behind_by: 0`.
- Do not dispatch GitHub Actions.
- Native/network runtime proof remains local/integration qualification; no remote runtime PASS claim.
- Release this claim only after source + pagination regression gate are on `main`.
