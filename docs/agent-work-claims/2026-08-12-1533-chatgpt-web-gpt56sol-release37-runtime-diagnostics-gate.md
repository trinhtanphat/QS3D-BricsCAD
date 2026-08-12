# Work claim — Release #37 runtime diagnostics host-major gate

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release37-runtime-diagnostics-gate-20260812-1533`
- Registered: `2026-08-12T15:33:00+07:00`
- Baseline main SHA: `f321acd7a56699f7d3104c52b3c4e4a175776fcb`
- Priority: P1 release preflight / stale host-major literal

## Confirmed mismatch

`RuntimeDiagnosticsCommands` is shared across V25/V26 through compile-time `ExpectedRuntimeMajor`/`ExpectedRuntimeLabel`. Runtime qualification is still independent of semantic-project presence: it computes `expectedRuntime` from Brx/TD host majors, combines that only with x64/package-version checks, and uses project state solely for optional read-only diagnostics.

Release #37 `preflight-runtime-diagnostics-readonly.py` still requires the obsolete V25-only local `v25Runtime`, so it reports a false failure against the current stronger host-major contract.

## Reserved scope

- `scripts/preflight-runtime-diagnostics-readonly.py`
- this claim file

## Expected reconciliation

Pin `ExpectedRuntimeMajor`, the shared `expectedRuntime` host-major check, project-read-only lookup, no project creation, and `ok = expectedRuntime && x64Runtime && packageVersionMatches`.

## Excluded scope

- no runtime production changes;
- no package/signing changes;
- no Actions rerun/dispatch;
- no licensed runtime qualification claim.

## Completion condition

Gate is integrated/read back and claim closed with exact SHA evidence.
