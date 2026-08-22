# Work claim — Issue #1099 duplicate QS3DVERSION and Update gates

- Status: `COMPLETED`
- Phase: `SOURCE_FIXED / EXACT_MAIN_VALIDATED / ISSUE_CLOSED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T08:52:00+07:00`
- Completed: `2026-08-14T10:09:23+07:00`
- Baseline main SHA: `b8df1d0915ea69aa18313c0c593680f44660d3dc`
- Claim SHA: `b0c4122575b2c29a49a381591252c9c669734fbf`
- Source fix SHA: `d92c7eb404b4c5dfeb8aee039905f83ad30bbce5`
- Exact validation SHA: `907a2e9fa2c0c1282c8361ee8f02b49e7b2687ae`
- Validation closeout commit: `16d47ef46c4a955587f398d5597fb84ebce32c2e`
- Issue: `#1099` — CLOSED / completed

## Reserved scope

Fix the remote-safe Update-lane regressions recorded in #1099 without touching LOCAL-002/P10 native probe/runner/docs:

- keep the canonical `QS3DVERSION` command registration in `src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs`;
- remove only the duplicate `QS3DVERSION` registration/method from `src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs` while preserving `QS3DVER` and `QSVER` through `WriteVersionCore`;
- reconcile `scripts/preflight-update-manifest-preclose.py`, `scripts/preflight-update-manual-preview-channel.py`, and `scripts/preflight-v25-netload-update-ux.py` with the current stronger source copy/contracts;
- make stale-gate failure reporting safe on Windows legacy console encodings so an assertion is not hidden by `UnicodeEncodeError`.

## Implemented source correction

`d92c7eb404b4c5dfeb8aee039905f83ad30bbce5` (`fix(update): resolve version command and stale gates`) landed on `main` and was read back there.

- `UpdateCommands.cs` no longer registers `QS3DVERSION`; `QS3DVER` and `QSVER` remain and still call `WriteVersionCore`.
- `RuntimeDiagnosticsCommands.cs` retains the canonical `QS3DVERSION` registration and loaded-binary identity path.
- The three stale Update gates follow the current signed-manifest/manual-preview/update UX contracts.
- Affected failure-output paths are console-encoding safe and no longer mask assertions with `UnicodeEncodeError`.
- Later owner refinements `b4059961315ba7e6b5455ea8d41af65fe3c23227` and `f3b9bf99bbcfd566586dcbfa496fede3346816ee` preserve one concise canonical `QS3DVERSION` and the separate deep `QS3DRUNTIMECHECK` path.
- Focused gate correction `e7c8416e6de2248f85bdbf836447252b1cbc94e8` merged through PR #1110 at `a33240db617541f425163936095d8b136d3b6ad2`.

## Exact validation closeout

Successor claim `docs/agent-work-claims/2026-08-14-codex-issue1099-update-validation-closeout.md` completed at commit `16d47ef46c4a955587f398d5597fb84ebce32c2e` against exact main `907a2e9fa2c0c1282c8361ee8f02b49e7b2687ae`:

- installed-reference V25 `Release|x64` build: PASS, `0 warnings / 0 errors`;
- exact product version: `0.1.0-preview.7+907a2e9fa2c0c1282c8361ee8f02b49e7b2687ae`;
- five duplicate-command gates: PASS;
- all three issue-named Update gates: PASS;
- broader `preflight-auto-update.py`: PASS;
- explicit cp1252 probes for the three affected console-safe helpers: PASS, with no `UnicodeEncodeError`;
- aggregate `scripts/preflight-all.py`: PASS, all 783 discovered feature gates passed;
- no interactive BricsCAD/private-data operation and no GitHub Actions operation was used for this closeout.

GitHub issue #1099 was closed as `completed` after this exact validation. This original source claim no longer reserves any implementation, test, preflight, release, or LOCAL-002 surface.

## Excluded scope

- LOCAL-002/P10 licensed BricsCAD probe, runner, evidence, or docs.
- Release workflow dispatch or any GitHub Actions operation.
- Broad updater behavior/security weakening or unrelated command registration changes.

## Completion condition

Satisfied. Source correction is on `main`, exact full-checkout validation is green, aggregate preflight is green, issue #1099 is closed, and the claim is released for future non-overlapping work.
