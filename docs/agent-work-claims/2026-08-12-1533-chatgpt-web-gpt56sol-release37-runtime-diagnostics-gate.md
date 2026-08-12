# Work claim — Release #37 runtime diagnostics host-major gate

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release37-runtime-diagnostics-gate-20260812-1533`
- Registered: `2026-08-12T15:33:00+07:00`
- Baseline main SHA: `f321acd7a56699f7d3104c52b3c4e4a175776fcb`
- Priority: P1 release preflight / stale host-major literal

## Confirmed mismatch

`RuntimeDiagnosticsCommands` is shared across V25/V26 through compile-time `ExpectedRuntimeMajor`/`ExpectedRuntimeLabel`. Runtime qualification remains independent of semantic-project presence: it computes `expectedRuntime` from Brx/TD host majors, combines that only with x64/package-version checks, and uses project state solely for optional read-only diagnostics.

Release #37 `preflight-runtime-diagnostics-readonly.py` still required the obsolete V25-only local `v25Runtime`, so it reported a false failure against the current stronger host-major contract.

## Integrated reconciliation

- Claim: `bed9618702cf387df3e16bd4b3ef22bd01ae026a`
- Gate fix: `0615b8697ef6e0ecbac26fd3177ea9e1319c15ec`

The gate now pins both V25/V26 compile-time `ExpectedRuntimeMajor` declarations, the shared `expectedRuntime` Brx/TD major check, optional read-only project lookup, the prohibition on project creation, and `ok = expectedRuntime && x64Runtime && packageVersionMatches`.

## Limitations

- Runtime production code was not changed by this lane.
- GitHub Actions were not rerun or dispatched.
- No licensed BricsCAD V25/V26 runtime PASS is claimed.
- No aggregate build/package/release PASS is claimed.
