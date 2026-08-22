# Work claim — release #30 ED2 round-trip preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release30-ed2-roundtrip-preflight`
- Registered: `2026-08-12T09:50:00+07:00`
- Completed: `2026-08-12T09:52:00+07:00`
- Baseline main SHA: `a41dbc76dc1e8f7454c7ebea95eba952b5dadc9e`
- Claim commit: `0bb23c81465c383245a6994f02b44dc4c8b04ea1`
- Implementation commit: `835a3c10c569b70f9453c2073928ca050df1f187`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reported three ED2 round-trip exact-token failures after quantity grouping moved to collision-safe canonical keys and XLSX legacy-handle detection gained stricter schema/fingerprint conditions.

## Completed scope

Reconciled only `scripts/preflight-ed2-excel-roundtrip.py` with current `ProjectQuantityReportBuilder` grouping and `XlsxHandleReader` legacy-detection contracts. Production reporting/export/reader behavior remained unchanged.

## Implemented gate contract

- Requires ED2 detail identity to remain per-element.
- Requires grouped quantity identity through `CanonicalGroupKey(floorId, zoneId, category, familyId, material, DensityKey(densityKgM3))`.
- Requires the length-prefixed canonical group helper and explicitly rejects a regression to delimiter-concatenated grouped Zone/category/material/density keys.
- Retains material, effective density/mass, quantity arithmetic and element provenance checks.
- Requires the stricter `preferLegacy` predicate: non-modern schema, no explicit handle column, decimal handles present and blank drawing fingerprint.
- Requires `if (preferLegacy)` and retains ED2 CHI_TIET modern-schema rejection plus all modern identity/fingerprint/live-handle checks.

## Validation performed

- Verified claim commit `0bb23c81465c383245a6994f02b44dc4c8b04ea1` remained an ancestor of moving `main`; intervening changes were unrelated ProjectElement/GeneratedGeometry/other claims.
- Re-fetched the exact gate before implementation.
- Read back the implemented grouping section from `main` at blob `54f91f1199e83636235e6ea1534513a297c5c88f`.
- Re-read current ProjectQuantityReportBuilder and XlsxHandleReader production contracts before the change.
- No production source was changed.
- No GitHub Actions/build/release dispatch was performed and no BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed. The ED2 round-trip gate now recognizes collision-safe grouping and stricter legacy handle detection without weakening schema/provenance/quantity guarantees, and this reservation is released.
