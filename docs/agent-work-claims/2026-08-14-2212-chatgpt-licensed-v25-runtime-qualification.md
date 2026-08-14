# Work claim — Licensed V25 runtime qualification closeout harness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T22:12:00+07:00`
- Baseline main SHA: `43622c5f98c9e897431088f886e7437a1f7fbe4a`
- Claim-only main commit: `2f3e60cefabf05e9e8cb63ffacb0e6359d3a35df`
- Implementation PR: `#1359`

## Scope

Close the remote-safe qualification-infrastructure gap for `LOCAL-001` without manufacturing native evidence. The existing runner proves source/build/runtime smoke but intentionally leaves the full interactive matrix as `NOT_RUN`; this claim adds a separate exact-SHA/hash-bound closeout layer so a licensed local V25 run can truthfully promote the qualification when every required family really passes.

Implemented surfaces:
- `scripts/test-local-v25-interactive-matrix-evidence.ps1`
- `scripts/complete-local-v25-qualification.ps1`
- `scripts/preflight-local-v25-qualification-closeout.py`
- `docs/LOCAL-V25-INTERACTIVE-MATRIX.example.json`
- `docs/LOCAL-V25-QUALIFICATION-CLOSEOUT.md`

Explicitly out of scope:
- V25 host lifecycle implementation files already reserved by other ACTIVE claims;
- changing any `LOCAL-001` runtime scenario from pending to PASS without licensed V25 evidence;
- proprietary BricsCAD DLLs, private/customer DWGs, screenshots, credentials or unsanitized machine paths;
- GitHub Actions dispatch/release publication.

## Acceptance result

- Matrix evidence fails closed unless all 15 required families are exactly `PASS`, exact Git SHA matches the candidate, plugin SHA-256 matches the exact built DLL, BricsCAD major version is V25, Windows/x64 and licensed/interactive attestations are explicit, and `knownBlockers` is an empty JSON array.
- The shipped example is deliberately non-qualifying (`NOT_TESTED` / false attestations).
- `complete-local-v25-qualification.ps1` always runs the existing licensed runtime gate first and exposes no `-SkipRuntime` path; only after the validator succeeds does it write `fullInteractiveMatrixStatus=PASS` and `licensedV25RuntimeQualified=true` into the local qualification report.
- Stable signed customer-release qualification remains stricter and is not inferred from runtime/source evidence alone.
- `scripts/preflight-local-v25-qualification-closeout.py` is auto-discovered by `preflight-all.py` and guards the fail-closed promotion contract.

## Remaining local-only boundary

This source claim is complete, but `LOCAL-001` itself must remain `IN_PROGRESS` until the new closeout command is actually executed on interactive Windows x64 with licensed BricsCAD V25 against one exact candidate SHA/plugin and every required interactive/private-DWG family passes. No source/static result from this claim is a `LOCAL_PASS`.
