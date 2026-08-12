# Work claim — Release #37 generated-health null-policy gate reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release37-generated-null-gates-20260812-1525`
- Registered: `2026-08-12T15:25:00+07:00`
- Baseline main SHA: `da81d86bdff14edd5c7e86e520fdde1435a7215d`
- Priority: P1 release preflight / stale contradictory source guards

## Confirmed mismatch

Release #37 contains older gates that require generated-health providers to silently skip `null` semantic entries (`if (element == null) continue;`, `IgnoresNullSemanticEntry`). Current production and newer focused regressions intentionally use the stronger fail-visible policy: standalone providers reject corrupt null entries with `InvalidOperationException`, while aggregate health isolates provider failures as diagnostics.

The slab-mesh policy is explicitly landed by source `3870c5bc8c4a4bc5afa1bd6685d467c30dd217d7`, focused gate `3531367f947a9ecc46adf4a280b976b8fa1edd9f`, closeout `e67534125a3c0f9e2a0eafbf8280ebf243c44354`. `GeneratedSlabMeshHealthSmoke` now runs `RejectsNullSemanticEntry()`. `StandaloneGeneratedHealthNullSafetySmoke` likewise calls all five standalone providers through `RequireFailVisible(...)`.

## Reserved scope

- `scripts/preflight-slab-mesh-health.py`
- `scripts/preflight-standalone-generated-health.py`
- this claim file

## Expected reconciliation

Update only the stale gates so they require the current fail-visible null policy and the existing focused regressions. Preserve all footprint-mode, ownership-conflict, curtain config, aggregate provider-isolation and registration assertions. Do not change production health services or weaken corruption visibility.

## Excluded scope

- no Core/provider source changes;
- no smoke behavior changes;
- no GitHub Actions rerun/dispatch;
- no licensed runtime qualification claim.

## Completion condition

Both gates are integrated on `main`, read back against current source/smokes, and this claim is closed with exact SHA evidence.
