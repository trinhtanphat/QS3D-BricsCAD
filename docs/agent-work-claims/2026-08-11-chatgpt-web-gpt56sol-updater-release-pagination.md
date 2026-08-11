# Agent Work Claim — bounded GitHub release pagination

- Claim ID: `UPDATER-RELEASE-PAGINATION-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T21:30:10+07:00`
- Released: `2026-08-11T21:33:00+07:00`
- Baseline main SHA: `7224baa13b03e5599419bdd6f025ca3dbb2040f3`
- Parent updater lane: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## Verified defect

`GitHubReleaseClient` requested only `releases?per_page=20`, then the coordinator filtered stable/prerelease and sorted those 20 entries by SemVer. GitHub orders release-list pages by release chronology, not by the updater's channel/SemVer policy. A stable update could therefore be hidden beyond the first 20 entries by newer-in-time prereleases, causing a stable client to incorrectly report that it was up to date.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Updates/GitHubReleaseClient.cs`
- `scripts/preflight-update-release-pagination.py`
- this claim file

No edits were made to `scripts/preflight-auto-update.py`, update PowerShell, SecureUpdateLauncher, UpdateCoordinator/UI, release workflow or unrelated product lanes.

## Completed changes

- `21ef752903023e25cd447704c21b868f385486f8` — `fix(updater): scan bounded GitHub release pages`
  - preserves the exact pinned HTTPS first-page endpoint `...releases?per_page=20` used by the existing updater security gate;
  - scans additional pages sequentially with explicit `page=N` and `MaxReleasePages = 10`;
  - applies the existing GitHub headers, 15-second request timeouts, declared response-size guard, streaming 2 MiB byte bound and DTO parsing independently to every page;
  - uses GitHub's `Link` header `rel="next"` as the authoritative continuation signal, avoiding a false overflow when the final page happens to contain exactly 20 items;
  - stops immediately when no next page exists;
  - if GitHub still advertises another page after the tenth bounded page, fails closed with an incomplete-history error instead of deriving a false latest-version result from a truncated history;
  - preserves strict tag SemVer parsing, GitHub/prerelease consistency, GitHub page/asset host allowlist and signed-manifest asset recognition;
  - preserves the completed nullable contracts in the release DTOs and optional manifest/page data.

- `eec308dce45ef16064ef9cdefcc9b12b0b0594f7` — `test(updater): guard bounded release pagination`
  - adds an auto-discovered pagination source gate;
  - requires the hard page bound, sequential `page=N` scan, Link/`rel="next"` continuation, fail-closed scan ceiling and per-page byte bounds;
  - rejects unbounded `while (true)` pagination and `Task.WhenAll` page bursts.

## Validation / coordination

- Re-read `scripts/preflight-updater-nullability.py`; all existing nullable-flow markers for `GitHubReleaseClient.cs` remain present in the pagination implementation.
- Re-read the neighboring manifest-v2 compatibility claim; it closed `SUPERSEDED / NO IMPLEMENTATION REQUIRED` and did not alter this client lane.
- Compare from `eec308dce45ef16064ef9cdefcc9b12b0b0594f7` to then-current `main` reported `behind_by: 0`; later commits preserved the pagination changes.
- No force-push, reset or rebase was used.
- No GitHub Actions workflow was dispatched.
- This connector session did not execute a fresh exact-V25 compile after the pagination commit; the immediately preceding local nullable lane had compiled the updater client before this change, so no post-pagination native/build PASS is claimed here.

## Result

Release discovery no longer silently trusts only the first 20 chronological GitHub releases. It scans a reviewed bounded history window and fails closed if the history is still incomplete, preventing false `UpToDate` decisions caused by prerelease-heavy release histories.
