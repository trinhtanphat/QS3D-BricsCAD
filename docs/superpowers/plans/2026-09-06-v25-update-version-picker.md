# V25 Selectable Preview Update Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let V25 users search and select a published QS3D release, install exactly that selected release, and diagnose stale/duplicate DLL loading after BricsCAD restarts.

**Architecture:** Keep `GitHubReleaseClient` as the single release-history source and keep the existing verified preview worker unchanged. `UpdateCenterWindow` owns the release picker and pins an `UpdateReleaseInfo` object through download/stage; a small local receipt records the selected version plus the originating BricsCAD process identity and expected adapter path, then the next BricsCAD process compares that receipt with the assembly actually loaded.

**Tech Stack:** C# / WPF / .NET Framework-compatible V25 adapter code, existing GitHub release/downloader/updater pipeline, Python source-contract preflight, GitHub Actions Shared CI.

**Spec:** GitHub issue #5957 and the approved Update Center design in the associated ChatGPT session.

## Global Constraints

- Preserve existing SHA-256 verification, bounded package handling, backup/rollback, breakaway worker, and restart behavior.
- Do not re-query latest release when the install button is clicked; install the release object selected in the UI.
- Search is case-insensitive across release tag and name.
- Default selection is the coordinator's latest compatible release when available, otherwise the first compatible published release.
- Reinstall and downgrade are deliberate supported choices when the selected release has a verified V25 ZIP + SHA-256.
- Post-restart mismatch diagnostics must show expected version, actual loaded version, and actual DLL path.
- Same-process pending installs must not be reported as stale-version failures.

---

### Task 1: Regression Contract

**Files:**
- Create: `scripts/preflight-v25-update-version-picker.py`
- Test: `scripts/preflight-v25-update-version-picker.py`

**Interfaces:**
- Consumes: `UpdateCenterWindow.cs`, `PreviewInstallReceipt.cs`.
- Produces: source-contract gate automatically discovered by `scripts/preflight-all.py`.

- [ ] **Step 1: Write the failing test** requiring searchable picker tokens, selected-release download pinning, removal of click-time latest refresh, receipt persistence, and stale-load diagnostics.
- [ ] **Step 2: Run through Shared CI / `python scripts/preflight-v25-update-version-picker.py` and verify RED** because production support is absent.
- [ ] **Step 3: Commit the RED regression guard.**

### Task 2: Process-Aware Install Receipt

**Files:**
- Create: `src/QS3D.BricsCAD.V25/Updates/PreviewInstallReceipt.cs`
- Test: `scripts/preflight-v25-update-version-picker.py`

**Interfaces:**
- Consumes: selected release tag, current process ID/start ticks, target adapter path.
- Produces: `TryWrite`, `TryRead`, `IsFromCurrentProcess`, `MatchesLoadedAssembly`, and mismatch-description behavior.

- [ ] **Step 1: Implement bounded, atomic local receipt persistence** under `%LOCALAPPDATA%/QS3D/UpdateState` with version, PID, process-start ticks, and expected DLL path.
- [ ] **Step 2: Normalize semantic display versions by stripping leading `v` and build metadata before comparison.**
- [ ] **Step 3: Keep same-process receipts pending; only a later BricsCAD process is eligible for post-restart verification.**
- [ ] **Step 4: Delete a receipt after a verified match; retain it when a mismatch must remain visible.**

### Task 3: Searchable Release Picker and Pinned Install

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs`
- Test: `scripts/preflight-v25-update-version-picker.py`

**Interfaces:**
- Consumes: `GitHubReleaseClient.GetPublishedReleasesAsync()` and existing `UpdateReleaseInfo` objects.
- Produces: searchable `TextBox` + `ComboBox`, `_selectedRelease`, and selected-release-aware primary action.

- [ ] **Step 1: Add `Phiên bản cài đặt` picker UI** with search box `Tìm phiên bản…` and a dropdown showing tag, newest/current badges, and publication date.
- [ ] **Step 2: Filter picker choices case-insensitively** and preserve the active selection while filtering.
- [ ] **Step 3: Default to latest compatible release** and refresh notes/release-page target when selection changes.
- [ ] **Step 4: Replace click-time latest refresh with the selected `UpdateReleaseInfo` object** and pass that exact object to `DownloadPreviewAsync`.
- [ ] **Step 5: Render target-aware button text** including reinstall for the currently loaded version and install for another selected version.
- [ ] **Step 6: Write the expected-version receipt before scheduling; delete it if staging fails.**
- [ ] **Step 7: On a later BricsCAD process, compare receipt expected version/path to the loaded adapter and surface a clear stale/duplicate-load diagnostic.**

### Task 4: Fresh Verification and Integration

**Files:**
- Verify all files above.

**Interfaces:**
- Consumes: exact PR head SHA.
- Produces: fresh Shared CI green evidence and merged `main` commit.

- [ ] **Step 1: Run the new feature gate and full discovered preflights through Shared CI.**
- [ ] **Step 2: Run V25 build on the exact head through Shared CI.**
- [ ] **Step 3: Fix any regression or lane collision on the same canonical branch.**
- [ ] **Step 4: Refresh `main` and PR head; require fresh green checks for the exact final head.**
- [ ] **Step 5: Merge the canonical PR to `main` and verify the merged commit contains the feature.**
