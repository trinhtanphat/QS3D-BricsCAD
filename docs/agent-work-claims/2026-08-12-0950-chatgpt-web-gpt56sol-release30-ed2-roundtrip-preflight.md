# Work claim — release #30 ED2 round-trip preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-ed2-roundtrip-preflight`
- Registered: `2026-08-12T09:50:00+07:00`
- Baseline main SHA: `a41dbc76dc1e8f7454c7ebea95eba952b5dadc9e`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reports three ED2 round-trip exact-token failures after quantity grouping moved to collision-safe canonical keys and XLSX legacy-handle detection gained stricter schema/fingerprint conditions.

## Reserved scope

Reconcile only `scripts/preflight-ed2-excel-roundtrip.py` with current `ProjectQuantityReportBuilder` grouping and `XlsxHandleReader` legacy-detection contracts. Preserve production reporting/export/reader behavior unchanged.

## Canonical evidence

- Quantity grouped rows use `CanonicalGroupKey(floorId, zoneId, category, familyId, material, DensityKey(densityKgM3))` rather than delimiter-concatenated Zone/category/material/density strings.
- `CanonicalGroupKey` length-prefixes every component (`value.Length + ":" + value`) before joining, preventing separator collisions while preserving all grouping dimensions.
- ED2 detail rows remain per-element (`ELEMENT\u001f` + elementId) and source/report provenance is unchanged.
- `XlsxHandleReader` now computes `preferLegacy = !isModernSchema && handleColumns.Count == 0 && decimalHandles.Count > 0 && string.IsNullOrWhiteSpace(drawingFingerprint)` and returns legacy decimal handles only through that stricter predicate.
- ED2 CHI_TIET still rejects non-modern schema before legacy fallback.

## Expected surfaces

- `scripts/preflight-ed2-excel-roundtrip.py`
- this claim file for close-out

## Excluded scope

- No edits to ProjectQuantityReportBuilder.cs, XlsxHandleReader.cs, ED2 commands/exporter, Excel locate service or smoke tests.
- No changes to grouping dimensions, ED2 schema, handle interpretation, drawing fingerprint rules or quantity arithmetic.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Replace the two obsolete delimiter key literals with the current `CanonicalGroupKey(...)` invocation plus length-prefix helper tokens and all six grouping dimensions.
- Retain material/density/effective quantity and row provenance checks.
- Replace obsolete `!isModernSchema && decimalHandles.Count > 0` token with the full `preferLegacy` predicate and `if (preferLegacy)` return path.
- Retain ED2-detail modern-schema rejection and all existing modern identity/fingerprint/live-handle checks.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active reservation for ED2 round-trip, ProjectQuantityReportBuilder grouping or XlsxHandleReader. Current Recognition/Regeneration claims are unrelated.

## Completion condition

The ED2 round-trip gate recognizes collision-safe grouping and stricter legacy handle detection without weakening schema/provenance/quantity guarantees, is pushed to `main`, and this claim is closed with exact evidence.
