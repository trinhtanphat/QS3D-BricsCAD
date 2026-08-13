# Claim: release #125 stale source guards

Status: SOURCE_FIXED / PENDING_FRESH_CI
Agent: ChatGPT Web / GPT-5.6 Sol
Started: 2026-08-13 20:03 UTC+7
Source fixed: 2026-08-13 20:09 UTC+7

## Scope

The source ownership for these two preflight files is released after the fixes below; this record remains open only for fresh release-CI evidence:

- `scripts/preflight-product-boundary.py`
- `scripts/preflight-runtime-product-version-identity.py`
- this claim file for final CI closeout

## Evidence

Release workflow run #125 (`31698863598`, job `94442845584`, head `1ee73b982a80ce21cc8ec962129dfa414b02fe41`) failed the aggregate source guard with exactly these two preflights.

The product-boundary guard still required the pre-2026-08-13 exact `QS3D.exe` sentence even though `docs/PRODUCT-BOUNDARY.md` was deliberately updated by the sibling-product boundary clarification while preserving this repository's BricsCAD-hosted shipping invariant.

The runtime-product-version guard still required every host `PluginEntry` to call `PaletteCoordinator.EnsureCreated()` directly. V25 now initializes UI through `RibbonInitializationCoordinator.Start()`, while `RuntimeDiagnosticsCommands.CaptureLoadedBinaryIdentity()` still runs before UI/runtime startup. V26 still uses the direct palette startup form.

## Source fixes

- `e1e899657fa8595351c77f11a08c29413b4462fe` — `fix(preflight): sync product boundary sibling wording`
  - follows the canonical sibling-product wording now present in `docs/PRODUCT-BOUNDARY.md`;
  - preserves all existing documentation, host Library target and `IExtensionApplication` checks.
- `ab9f1022ede0ff03b3d0ebafd7bedc41c83a35f4` — `fix(preflight): follow coordinated runtime startup identity`
  - accepts the current V25 coordinator startup and V26 direct-palette startup forms;
  - still requires a recognized UI startup marker;
  - strengthens ordering by requiring `CaptureLoadedBinaryIdentity()` before the earliest recognized UI/runtime startup marker;
  - leaves all semantic product-version, assembly-version, file-version, startup SHA-256, stale-process, updater, installer and package identity checks intact.

Both source commits were read back from GitHub after write. No production runtime code was changed for these stale assertions.

## CI state

No fresh release run exists after the source fixes yet. Latest release remains #125 on stale head `1ee73b982a80ce21cc8ec962129dfa414b02fe41`.

The connected GitHub tool exposes read/log and rerun operations but does not expose a fresh `workflow_dispatch` action. Therefore #125 must **not** be rerun: a fresh run must start from the then-current `main` SHA.

No commit status/check was attached to `ab9f1022ede0ff03b3d0ebafd7bedc41c83a35f4`, so source-fix evidence is not being misreported as a CI PASS.

## Coordination

Do not modify unrelated production code, local-only BricsCAD acceptance lanes, MAP/IFC/Zone-Floor claims, or rerun stale workflow #125 after source changes. Final closeout requires a fresh release workflow run from current `main`; if it fails, inspect that new run's exact log rather than reopening #125.
