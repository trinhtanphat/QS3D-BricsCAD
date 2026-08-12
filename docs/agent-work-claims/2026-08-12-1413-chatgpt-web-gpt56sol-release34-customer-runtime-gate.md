# Agent work claim — Release #34 customer runtime gate

- Status: `ACTIVE`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 14:13 Asia/Ho_Chi_Minh`

## Scope

Reconcile `preflight-customer-release.py` with the current shared V25/V26 runtime diagnostics implementation. The gate must pin `ExpectedRuntimeMajor` / `ExpectedRuntimeLabel` compile-time host identity instead of hard-coding literal V25 comparisons inside shared source.

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
