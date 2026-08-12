# Agent work claim — Release #34 runtime-health provider isolation gate

- Status: `ACTIVE`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 13:31 Asia/Ho_Chi_Minh`

## Scope

Reconcile `preflight-runtime-health-provider-isolation.py` with the current fail-visible null-element behavior in `GeneratedSolidRuntimeHealthService`. A corrupt null semantic element must not be silently skipped: the ownership provider throws a recoverable diagnostic failure, `AddProviderSafely` records `RUNTIME_HEALTH_PROVIDER_FAILED`, and subsequent native providers remain isolated.

## Files

- `scripts/preflight-runtime-health-provider-isolation.py`
- this claim file

## Out of scope

- production runtime-health provider implementation
- provider order/addition/removal
- UI commands
- updater/signing/release behavior
- BricsCAD runtime qualification

## Acceptance checks

- gate requires the current null-element fail-visible throw rather than obsolete silent `continue`;
- all existing AddProviderSafely, fatal-exception propagation, provider-order and future-provider isolation assertions remain intact;
- no production diagnostic is weakened.
