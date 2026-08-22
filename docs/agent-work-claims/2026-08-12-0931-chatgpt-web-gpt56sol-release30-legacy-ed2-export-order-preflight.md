# Work claim — release #30 legacy ED2 export-order preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release30-legacy-ed2-export-order-preflight`
- Registered: `2026-08-12T09:31:00+07:00`
- Completed: `2026-08-12T09:33:00+07:00`
- Baseline main SHA: `ebe2ac7272e98ba24d8bb16550085ea6a9ed14d5`
- Claim commit: `4fef968864ae2f79a34a0681f7afbcf77c9951cc`
- Implementation commit: `903680ba55068ddfeaf14bf9a703013de6c6e2a8`
- Priority: QS3D Cloud V25 Preview Build & Release #30 still failed the legacy command lifecycle gate because its ED2 section required Save confirmation before project lookup, while the canonical ED2 export gate validates an existing detached report/live-handle set before SaveFileDialog and writes only after confirmation.

## Completed scope

Reconciled only the QS3DED2 ordering assertions in `scripts/preflight-legacy-command-project-lifecycle.py`. Production commands and every other legacy command lifecycle check remained unchanged.

## Implemented contract

- ED2 still requires an existing project read-only and a detached project snapshot.
- ED2 still requires Detail/Group report building and live-handle validation.
- The gate now requires existing project -> detached report validation -> live handles -> SaveFileDialog -> confirmation -> `XlsxQuantityExporter.ExportEd2(...)`.
- The gate explicitly rejects any ED2 XLSX write before Save confirmation.
- `ProjectContextCoordinator.GetOrCreate(doc)` remains forbidden across the legacy read-only sections.
- BQ/BBS/Health/Locate/Link Host requirements were preserved.

## Validation performed

- Verified claim commit `4fef968864ae2f79a34a0681f7afbcf77c9951cc` remained an ancestor of moving `main`; intervening commits were unrelated Curtain/local documentation updates.
- Re-fetched the exact gate before implementation and read it back from `main` afterward at blob `a75a065394508e9e58b5600b71b57922ed1c196f`.
- No product source was changed.
- No GitHub Actions/build/release dispatch was performed and no BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed. The legacy lifecycle gate now agrees with the current ED2 validate-before-Save/write-after-confirmation contract while retaining non-creating read-only semantics, and this reservation is released.
