# Work claim — Issue #1099 duplicate QS3DVERSION and Update gates

- Status: `SOURCE_FIXED / PENDING_FRESH_VALIDATION`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T08:52:00+07:00`
- Baseline main SHA: `b8df1d0915ea69aa18313c0c593680f44660d3dc`
- Claim SHA: `b0c4122575b2c29a49a381591252c9c669734fbf`
- Source fix SHA: `d92c7eb404b4c5dfeb8aee039905f83ad30bbce5`
- Issue: `#1099`

## Reserved scope

Fix the remote-safe Update-lane regressions recorded in #1099 without touching LOCAL-002/P10 native probe/runner/docs:

- keep the canonical `QS3DVERSION` command registration in `src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs`;
- remove only the duplicate `QS3DVERSION` registration/method from `src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs` while preserving `QS3DVER` and `QSVER` through `WriteVersionCore`;
- reconcile `scripts/preflight-update-manifest-preclose.py`, `scripts/preflight-update-manual-preview-channel.py`, and `scripts/preflight-v25-netload-update-ux.py` with the current stronger source copy/contracts;
- make stale-gate failure reporting safe on Windows legacy console encodings so an assertion is not hidden by `UnicodeEncodeError`.

## Source fix evidence

`d92c7eb404b4c5dfeb8aee039905f83ad30bbce5` (`fix(update): resolve version command and stale gates`) landed atomically on `main` and was read back there.

- `UpdateCommands.cs` no longer registers `QS3DVERSION`; `QS3DVER` and `QSVER` remain and still call `WriteVersionCore`.
- `RuntimeDiagnosticsCommands.cs` still owns `[CommandMethod("QS3DVERSION", CommandFlags.Modal)]` and routes it through `VersionCheck()` -> `RuntimeCheck()`, preserving loaded-binary identity diagnostics.
- manifest-preclose gate now checks the current `Gói cập nhật ký số đã được xác minh trước khi đóng BricsCAD` contract.
- manual-preview gate now checks the current preview/manual-install wording and the stronger `QS3D không hạ kiểm tra bảo mật để tự động thay DLL chưa ký.` security boundary.
- NETLOAD/update UX gate now explicitly rejects a second `QS3DVERSION` registration in `UpdateCommands`, requires exactly one canonical registration in runtime diagnostics, retains both short aliases, and checks the current `Run QS3DUPDATE to check GitHub Releases.` guidance.
- failure output in the affected gates uses stdout-encoding-aware `backslashreplace`; a cp1252 simulation preserved Vietnamese assertion text as escaped output without raising `UnicodeEncodeError`.

## Validation status

The current tool runtime cannot clone/download the full repository through its shell network, so the issue-requested installed-reference V25 build, five broad duplicate-command preflights, three Update preflights as executable files in a full checkout, and aggregate `preflight-all.py` have **not** been claimed as executed in this lane.

Read-back/static contract validation above is complete. A fresh full-checkout validation is still required before this claim or #1099 is marked completed.

No GitHub Actions workflow was dispatched for this work, matching both repository CI policy and #1099's `no GitHub Actions operation` requirement.

## Excluded scope

- LOCAL-002/P10 licensed BricsCAD probe, runner, evidence, or docs.
- Release workflow dispatch or any GitHub Actions operation.
- Broad updater behavior/security weakening or unrelated command registration changes.

## Completion condition

Run the issue-named installed-reference V25 build, the eight named preflights and aggregate `preflight-all.py` against `d92c7eb404b4c5dfeb8aee039905f83ad30bbce5` or a descendant that preserves these four changed files. If they pass, record exact evidence and mark this claim `COMPLETED`; otherwise reopen source work on the concrete failing contract.
