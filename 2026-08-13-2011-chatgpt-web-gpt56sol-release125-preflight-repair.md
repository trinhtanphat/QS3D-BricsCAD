# Work claim — release #125 preflight repair

- Status: `RELEASED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T20:11:00+07:00`
- Released: `2026-08-13T20:13:00+07:00`
- Observed pre-write main SHA: `f99cb16562962d62bc096fa83b1f47fb00c62fcb`
- Actual registration commit parent: `b7a30eefe5bddd604e66d3695f0b31b0219f5780`
- Claim commit: `8e847bf9bd24ebdbe300e0d8d59be1b94baf84bb`
- Priority: owner-requested continuation of V25 Cloud release run #125 (`31698863598`) failures

## Release reason

After publishing this reservation, moving-main reconciliation exposed the earlier canonical claim `docs/agent-work-claims/2026-08-13-2003-chatgpt-web-gpt56sol-release125-stale-guards.md`, started at 20:03 UTC+7. That earlier lane already owned the exact same two release #125 preflight files and had landed both source fixes before this reservation was created.

This duplicate reservation is therefore released immediately. No product/runtime/preflight implementation was written under this claim and no existing source fix was duplicated or reverted.

## Existing source fixes reused

- `e1e899657fa8595351c77f11a08c29413b4462fe` — `fix(preflight): sync product boundary sibling wording`
  - aligns `scripts/preflight-product-boundary.py` with the canonical sibling `QS3D-CAD` wording while retaining hosted-plugin, V25/V26 Library and `IExtensionApplication` safeguards.
- `ab9f1022ede0ff03b3d0ebafd7bedc41c83a35f4` — `fix(preflight): follow coordinated runtime startup identity`
  - accepts current V25 `RibbonInitializationCoordinator.Start()` and V26 direct palette startup forms;
  - still requires `CaptureLoadedBinaryIdentity()` before recognized UI/runtime startup;
  - keeps semantic/file/assembly version, startup SHA-256, stale-process, installer, updater and package identity guards intact.

Both fixes are ancestors of the current `main` observed during reconciliation and both current script blobs were read back after the fixes.

## CI state at release

No fresh V25 release workflow run exists after those source fixes. Latest remains run #125 (`31698863598`) on stale head `1ee73b982a80ce21cc8ec962129dfa414b02fe41`, conclusion `failure`.

Do not rerun #125 because that reruns the stale source SHA. The connected GitHub actions exposed here do not include a fresh `workflow_dispatch` operation, so the canonical 20:03 claim remains `SOURCE_FIXED / PENDING_FRESH_CI` until a new release run is started from then-current `main`.

## Scope status

- preflight source implementation: already fixed by earlier claim; not duplicated here
- commit/push of the two fixes: already present on `main`
- fresh release CI evidence: still pending under the earlier canonical claim
- local BricsCAD runtime qualification: out of scope
