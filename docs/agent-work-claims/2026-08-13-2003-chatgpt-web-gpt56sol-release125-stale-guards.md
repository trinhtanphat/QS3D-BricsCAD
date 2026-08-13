# Claim: release #125 stale source guards

Status: ACTIVE
Agent: ChatGPT Web / GPT-5.6 Sol
Started: 2026-08-13 20:03 UTC+7

## Scope

Only these files are owned by this claim:

- `scripts/preflight-product-boundary.py`
- `scripts/preflight-runtime-product-version-identity.py`
- this claim file for closeout

## Evidence

Fresh release workflow run #125 (`31698863598`, job `94442845584`, head `1ee73b982a80ce21cc8ec962129dfa414b02fe41`) failed the aggregate source guard with exactly these two preflights.

The product-boundary guard still requires the pre-2026-08-13 exact `QS3D.exe` sentence even though `docs/PRODUCT-BOUNDARY.md` was deliberately updated by the sibling-product boundary clarification while preserving the BricsCAD-hosted shipping invariant.

The runtime-product-version guard still requires every host `PluginEntry` to call `PaletteCoordinator.EnsureCreated()` directly. V25 now initializes UI through `RibbonInitializationCoordinator.Start()`, while `RuntimeDiagnosticsCommands.CaptureLoadedBinaryIdentity()` still runs before UI/runtime startup. V26 still uses the direct palette startup form.

## Intended fix

Synchronize only the two stale preflights with the current authoritative source/docs contracts. Preserve fail-closed product/version/hash checks and require loaded-binary capture before the recognized UI startup form. Do not change production runtime behavior to satisfy a stale assertion.

## Coordination

Do not modify unrelated production code, local-only BricsCAD acceptance lanes, MAP/IFC/Zone-Floor claims, or rerun stale workflow #125 after source changes. Any release validation must use a fresh workflow run from current `main`.
