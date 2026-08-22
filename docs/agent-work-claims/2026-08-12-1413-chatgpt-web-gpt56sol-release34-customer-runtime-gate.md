# Agent work claim — Release #34 customer runtime gate

- Status: `COMPLETED`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 14:13 Asia/Ho_Chi_Minh`
- Completed: `2026-08-12 14:15 Asia/Ho_Chi_Minh`

## Scope

Reconcile `preflight-customer-release.py` with the current shared V25/V26 runtime diagnostics implementation. The gate now pins `ExpectedRuntimeMajor` / `ExpectedRuntimeLabel` compile-time host identity instead of hard-coding literal V25 comparisons inside shared source.

## Files

- `scripts/preflight-customer-release.py`
- this claim file

## Out of scope

- production `RuntimeDiagnosticsCommands.cs`
- release signing/package behavior
- updater behavior
- licensed BricsCAD runtime qualification

## Acceptance checks

- gate requires V25/V26 compile-time expected-major constants;
- both BrxMgd and TD_Mgd comparisons use `ExpectedRuntimeMajor`;
- x64/package metadata/version/release-order assertions remain unchanged;
- no runtime compatibility boundary is weakened.

## Implementation

- claim: `86c40420936a118f67f7476f88f15af860376215`
- gate reconciliation: `a1e200e457283d3199508ff666ac3af88d082c5b`

## Evidence & limitations

Readback of current production confirms the runtime check derives the expected host major from compile-time V25/V26 identity and compares both BrxMgd and TD_Mgd against it. The gate now preserves that shared-source contract and retains existing x64/package/release assertions. Production runtime code was not changed. No GitHub Actions or licensed BricsCAD runtime was executed.
