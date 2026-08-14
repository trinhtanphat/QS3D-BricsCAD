# Work claim — Issue #1099 duplicate QS3DVERSION and Update gates

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T08:52:00+07:00`
- Baseline main SHA: `b8df1d0915ea69aa18313c0c593680f44660d3dc`
- Issue: `#1099`

## Reserved scope

Fix the remote-safe Update-lane regressions recorded in #1099 without touching LOCAL-002/P10 native probe/runner/docs:

- keep the canonical `QS3DVERSION` command registration in `src/QS3D.BricsCAD.V25/RuntimeDiagnosticsCommands.cs`;
- remove only the duplicate `QS3DVERSION` registration/method from `src/QS3D.BricsCAD.V25/Updates/UpdateCommands.cs` while preserving `QS3DVER` and `QSVER` through `WriteVersionCore`;
- reconcile `scripts/preflight-update-manifest-preclose.py`, `scripts/preflight-update-manual-preview-channel.py`, and `scripts/preflight-v25-netload-update-ux.py` with the current stronger source copy/contracts;
- make stale-gate failure reporting safe on Windows legacy console encodings so an assertion is not hidden by `UnicodeEncodeError`.

## Excluded scope

- LOCAL-002/P10 licensed BricsCAD probe, runner, evidence, or docs.
- Release workflow dispatch or any GitHub Actions operation.
- Broad updater behavior/security weakening or unrelated command registration changes.

## Validation plan

- prove exactly one `QS3DVERSION` registration remains and the two aliases remain registered;
- run/inspect the eight issue-named preflight contracts where remote-safe;
- ensure failure-print paths escape/encode safely instead of masking the underlying assertion;
- no native BricsCAD PASS claim and no GitHub Actions dispatch.

## Coordination

No #1099 issue comment or repository search result showed an existing reservation at claim time. Refresh `main` and ACTIVE/BLOCKED claims before every write; abort or integrate if another agent lands on the same paths.

## Completion condition

The duplicate command is removed, the three stale Update gates match the current stronger source contract, encoding-safe diagnostics are guarded, source changes are pushed to `main`, and this claim records exact source/validation SHAs plus any remaining non-remote acceptance gate.
