# QS3D GitHub Release Auto-Update — Implementation Plan

Date: 2026-08-11
Owner: ChatGPT Web / GPT-5.6 Sol
Claim: `GITHUB-RELEASE-AUTO-UPDATE-20260811`

## 1. Product goal

Give users of the QS3D BricsCAD V25 plugin a first-class update experience without asking them to browse GitHub manually for every release:

1. When the QS3D assembly is first loaded in a BricsCAD session, check the repository's published GitHub Releases in the background.
2. Compare the newest applicable release tag against the running assembly informational/product version.
3. If a newer release exists, surface a small Update Center with release version, publication time, release notes summary, and update eligibility.
4. Provide `QS3DUPDATE` so the user can open the Update Center at any time.
5. Provide **Kiểm tra lại** and **Cập nhật ngay** buttons.
6. Never replace an assembly while BricsCAD has it loaded. A one-click update schedules a detached worker first, requests a normal main-window close so BricsCAD keeps its own save/cancel prompts, waits until every BricsCAD process has really exited, runs the repository's existing hardened updater, and restarts the same BricsCAD executable only after success.

## 2. Existing release/security contract to preserve

The repository already has the hard parts of a safe installer/update chain:

- `scripts/package-v25.ps1` builds the V25 ZIP and automatically discovers `[CommandMethod]` names into `COMMANDS.txt`.
- `scripts/install-v25-autoload.ps1` validates internal SHA-256, optional/required Authenticode publisher identity, stages payload atomically, updates DemandLoad registration, and rolls back on failure.
- `scripts/update-v25.ps1` requires HTTPS, validates a bounded manifest, blocks downgrade, constrains package hosts, validates ZIP size/path traversal/expanded size/count, verifies SHA-256 and Authenticode, validates version binding, then calls the atomic installer.
- `scripts/new-v25-update-manifest.ps1` creates the signed-package update manifest with package URL, SHA-256, assembly version, and signer thumbprint.

The plugin updater will orchestrate these surfaces; it will not introduce a parallel installer.

## 3. Update channel policy

### Stable running build

If the running informational version is stable, ignore GitHub prereleases and select the newest stable SemVer only.

### Prerelease running build

If the running informational version contains a prerelease suffix, allow both prereleases and stable releases. A stable release at the same numeric core outranks a prerelease, and normal SemVer prerelease precedence is honored.

### Invalid/unrecognized tags

Ignore tags that are not strict `vMAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]` SemVer. Never fall back to lexical comparison. Also reject a GitHub release whose `prerelease` flag disagrees with the parsed SemVer tag so channel metadata cannot silently contradict the version label.

## 4. GitHub API contract

Use the public endpoint:

`https://api.github.com/repos/trinhtanphat/QS3D-BricsCAD/releases?per_page=20`

Requirements:

- HTTPS only.
- Repository owner/name is compiled into the updater; no arbitrary feed URL in normal UI.
- Set an explicit QS3D User-Agent and GitHub JSON Accept header.
- Ignore drafts.
- Reject release-page and asset URLs that are not HTTPS or do not belong to approved GitHub hosts.
- Limit response size before JSON parsing.
- Never send GitHub credentials from the plugin; public releases require none.

## 5. Release asset contract

One-click update eligibility requires an asset named exactly:

`QS3D-BricsCAD-V25.update.json`

The manual signed release workflow will generate it only when `sign_package=true`, using:

`https://github.com/<owner>/<repo>/releases/download/<tag>/QS3D-BricsCAD-V25.zip`

The release will publish three primary assets for a signed release:

- `QS3D-BricsCAD-V25.zip`
- `QS3D-BricsCAD-V25.zip.sha256`
- `QS3D-BricsCAD-V25.update.json`

Unsigned preview releases intentionally omit the update manifest and are therefore detectable but not one-click-installable.

## 6. Trust model

### Running plugin publisher anchor

One-click update requires the running QS3D plugin DLL to have a readable Authenticode signer certificate. Its normalized 40-hex certificate thumbprint becomes the expected publisher passed to `update-v25.ps1`.

This prevents a compromised release manifest/package from switching the updater to an unrelated publisher.

### Installed updater script

Before invoking the installed `update-v25.ps1`, the detached worker checks its Authenticode status and signer thumbprint and requires an exact match with the running plugin publisher.

### Target package

`update-v25.ps1` remains responsible for package SHA-256, archive safety, internal hashes, signed executable payloads, signer equality, metadata/assembly version binding, downgrade blocking, and atomic installation. The plugin may use `-AllowSameVersion` only after it has independently proven the GitHub SemVer is newer; this supports prereleases that intentionally share the same numeric AssemblyVersion while retaining the updater's independent downgrade block.

### Unsigned current preview

An unsigned currently installed preview has no trustworthy publisher anchor. It may check and display releases but **must not** enable one-click automatic install. The user must manually install one signed QS3D release once; subsequent signed updates can then be one-click.

## 7. Runtime architecture

New isolated namespace/folder: `QS3D.BricsCAD.V25.Updates`.

Planned components:

- `SemanticReleaseVersion` — strict SemVer parse/compare for GitHub tags and current informational version.
- `GitHubReleaseClient` — bounded public GitHub release query and DTO parsing.
- `UpdateReleaseInfo` / `UpdateCheckResult` — immutable update state presented to UI.
- `UpdateCoordinator` — session-wide single-flight auto/manual checks, current version discovery, channel selection, UI dispatch, and state events.
- `SecureUpdateLauncher` — validates install/update paths and current publisher, creates a detached encoded PowerShell worker, requests graceful BricsCAD close, waits for every BricsCAD process to exit, revalidates updater signer, invokes `update-v25.ps1`, logs the result, and restarts BricsCAD only after success.
- `UpdateCenterWindow` — code-only WPF window so the lane does not touch shared XAML/theme surfaces; shows current/latest version, state, release notes, manual release link, refresh button, and update button.
- `UpdateCommands` — `QS3DUPDATE` command.
- `UpdateBootstrapper` — starts automatic check from the plugin lifecycle and shuts down cleanly.

Integration changes to `PluginEntry.cs` are limited to one start and one stop call.

## 8. UX states

The Update Center must distinguish:

- **Đang kiểm tra...**
- **Đã cập nhật** — no newer eligible release.
- **Có bản mới** — newer release detected.
- **Có bản mới nhưng cần cài thủ công** — unsigned current build, missing signed update manifest, or otherwise not one-click eligible.
- **Đã lên lịch cập nhật** — detached updater is waiting for BricsCAD to close.
- **Lỗi kiểm tra cập nhật** — network/API/parse failure; current plugin continues normally.

Network failure must never block plugin initialization or drawing work.

## 9. One-click update sequence

1. User clicks **Cập nhật ngay**.
2. Coordinator re-checks that the selected release is still newer and has the required signed-manifest asset.
3. Resolve the running plugin DLL and its installation directory from the actual assembly location, not a hard-coded AppData path.
4. Require the installed `update-v25.ps1` beside the plugin DLL.
5. Resolve the current `bricscad.exe` path from the host process.
6. Read and pin the running plugin Authenticode signer thumbprint.
7. Start a detached PowerShell worker with only fixed script logic and safely encoded literal inputs.
8. Request `CloseMainWindow()` on the current BricsCAD host. This is a graceful window-close request, not process termination; BricsCAD remains responsible for unsaved-document save/cancel prompts.
9. Worker waits for all BricsCAD processes to exit. If the user cancels BricsCAD shutdown, the worker keeps waiting and never kills the host.
10. Worker validates the installed updater script Authenticode signature and exact signer thumbprint.
11. Worker runs `update-v25.ps1` with the GitHub manifest URL, expected signer, current install directory, approved `github.com` package host, and `-AllowSameVersion` only for the already-proven newer GitHub SemVer handoff.
12. Existing updater downloads/verifies/installs atomically.
13. On success, worker restarts the exact BricsCAD executable used for the session.
14. Worker writes a per-update transcript under `%LOCALAPPDATA%\QS3D\UpdateLogs`, outside the replaceable plugin directory.

## 10. Release workflow change

Only the existing manual self-hosted signed release workflow is changed:

- after signing/finalizing the package and before publishing the GitHub Release, generate `QS3D-BricsCAD-V25.update.json` when `sign_package=true`;
- include the manifest in build artifacts when present;
- upload the manifest as a GitHub Release asset only for signed releases;
- require it before publishing a signed release;
- keep unsigned prerelease behavior compatible by not generating/publishing an auto-update manifest.

No workflow is dispatched by this implementation session.

## 11. Multi-agent isolation

This implementation intentionally does not edit:

- shared `Commands.cs`;
- Ribbon files;
- active Start Center files;
- Core semantic/persistence files;
- Quantity/Workspace/Room/Family/modeless-viewer lanes.

`QS3DUPDATE` is discovered automatically by `package-v25.ps1`, so no shared command manifest edit is needed.

## 12. Failure handling

- GitHub unavailable/rate-limited: record state and allow retry; no host failure.
- Malformed release JSON/tag or contradictory prerelease metadata: ignore invalid release; fail closed if response contract is unusable.
- No update manifest: manual-only release.
- Non-HTTPS or unexpected host: reject.
- Current plugin unsigned: manual-only.
- Missing/tampered updater script: reject one-click launch.
- Multiple update clicks: single scheduled worker per session.
- Graceful close request rejected/unavailable: tell the user to close BricsCAD normally; worker remains safely queued.
- User cancels unsaved-document close prompt: worker waits; it does not force termination.
- Updater fails: do not restart automatically; preserve log and existing atomic installer rollback behavior.

## 13. Source regression gate

Add `scripts/preflight-auto-update.py`, auto-discovered by `preflight-all.py`, to assert at minimum:

- fixed HTTPS GitHub releases endpoint and repo identity;
- strict SemVer implementation, stable/prerelease channel policy, and GitHub/tag prerelease consistency;
- exact update-manifest asset name;
- one-click gate requires signed current plugin + manifest asset;
- detached worker waits for BricsCAD exit rather than killing processes;
- one-click path uses `CloseMainWindow()` and forbids `Stop-Process`, `taskkill`, and `.Kill(`;
- worker checks updater Authenticode signer before execution;
- same-AssemblyVersion prerelease handoff is explicit and only reachable after newer SemVer selection;
- `PluginEntry.Initialize/Terminate` call updater bootstrap start/stop;
- `QS3DUPDATE` command exists in isolated updater file;
- signed manual workflow generates and uploads the update manifest;
- unsigned release path does not claim one-click eligibility.

## 14. Native validation handoff

Connector/source verification can prove source contracts but cannot honestly prove native BricsCAD behavior. Before calling the feature production-qualified, the existing `LOCAL-009 — clean-machine install/sign/update qualification` lane should verify on a signed candidate:

- automatic check does not stall BricsCAD startup;
- Update Center opens modelessly and remains responsive;
- update notification appears once per session;
- signed release is detected correctly;
- clicking update never overwrites loaded DLLs;
- graceful close preserves BricsCAD save/cancel prompts and a cancelled close leaves the worker waiting;
- normal close triggers signature verification, update and restart;
- tampered updater/package/signature is rejected;
- failed update preserves previous installation and provides a useful log;
- DemandLoad/SECURELOAD behavior remains intact.

## 15. Definition of done for this source lane

- Work claim committed first.
- This planning document committed before implementation.
- Updater source, command, UI, plugin lifecycle wiring, signed-release manifest publication, graceful one-click close, and static regression gate committed on current `main`.
- No forbidden/overlapping active-claim paths changed.
- No GitHub Actions dispatched.
- Claim closed with exact commit evidence and any remaining LOCAL_ONLY runtime proof stated explicitly.