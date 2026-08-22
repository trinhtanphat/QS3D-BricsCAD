# Work claim — Release #37 generated-health null-policy gate reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release37-generated-null-gates-20260812-1525`
- Registered: `2026-08-12T15:25:00+07:00`
- Baseline main SHA: `da81d86bdff14edd5c7e86e520fdde1435a7215d`
- Priority: P1 release preflight / stale contradictory source guards

## Confirmed mismatch

Release #37 contained older gates that required generated-health providers to silently skip `null` semantic entries (`if (element == null) continue;`, `IgnoresNullSemanticEntry`). Current production and newer focused regressions intentionally use the stronger fail-visible policy: standalone providers reject corrupt null entries with `InvalidOperationException`, while aggregate health isolates provider failures as diagnostics.

The slab-mesh policy was explicitly landed by source `3870c5bc8c4a4bc5afa1bd6685d467c30dd217d7`, focused gate `3531367f947a9ecc46adf4a280b976b8fa1edd9f`, closeout `e67534125a3c0f9e2a0eafbf8280ebf243c44354`. `GeneratedSlabMeshHealthSmoke` now runs `RejectsNullSemanticEntry()`. `StandaloneGeneratedHealthNullSafetySmoke` likewise calls all five standalone providers through `RequireFailVisible(...)`.

## Integrated reconciliation

- Claim: `9f24459c78c179569579709e81829b68ed993f49`
- Slab-mesh gate: `d768ce269759c78c867a457023a9ac59388423ac`
- Standalone generated-health gate: `cff801bb2b99cda56c26a4b1cad4084ec4c0f1b1`

`preflight-slab-mesh-health.py` now pins the fail-visible source throw and `RejectsNullSemanticEntry()` regression while preserving footprint-mode, ownership conflict and registration contracts. `preflight-standalone-generated-health.py` now forbids silent null skipping and pins the shared `RequireFailVisible` smoke across all five providers while preserving Curtain config diagnostics.

## Readback

Current gate readback confirmed the new fail-visible tokens and absence of the legacy `IgnoresNullSemanticEntry()` requirement. Production provider source and smoke semantics were not changed by this lane.

## Limitations

- GitHub Actions were not rerun or dispatched.
- No aggregate preflight/build/package/release PASS is claimed.
- No licensed BricsCAD runtime qualification is claimed.
