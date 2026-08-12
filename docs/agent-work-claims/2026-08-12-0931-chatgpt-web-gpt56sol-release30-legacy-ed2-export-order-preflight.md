# Work claim — release #30 legacy ED2 export-order preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-legacy-ed2-export-order-preflight`
- Registered: `2026-08-12T09:31:00+07:00`
- Baseline main SHA: `ebe2ac7272e98ba24d8bb16550085ea6a9ed14d5`
- Priority: QS3D Cloud V25 Preview Build & Release #30 still fails the legacy command lifecycle gate because its ED2 section requires Save confirmation before project lookup, while the canonical ED2 export gate now validates an existing detached report/live-handle set before SaveFileDialog and writes only after confirmation.

## Reserved scope

Reconcile only the QS3DED2 ordering assertions in `scripts/preflight-legacy-command-project-lifecycle.py`. Preserve all production commands and every other legacy command lifecycle check unchanged.

## Canonical evidence

- Run #30 passes `preflight-command-xlsx-export-freshness.py`, which requires ED2 existing read-only project -> detached regeneration -> detail/summary -> live-handle validation -> SaveFileDialog/confirmation -> persistent XLSX write.
- Current `Commands.ExportEd2Workflow()` follows that contract.
- The legacy lifecycle gate still includes `if (dialog.ShowDialog() != true) return;` before project lookup in its required tuple and separately errors unless dialog confirmation precedes project lookup.
- ED2 remains non-creating/read-only against live project state; only detached preview/report work occurs before Save confirmation.

## Expected surfaces

- `scripts/preflight-legacy-command-project-lifecycle.py`
- this claim file for close-out

## Excluded scope

- No edits to `Commands.cs`, unit workflow, ED2 report builders/exporter, BQ/BBS/Health/Locate/Link Host lifecycle checks or other run #30 failures.
- No GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Keep ED2 existing-project, detached-copy, Detail and Group requirements.
- Require Save confirmation to remain present and the persistent `XlsxQuantityExporter.ExportEd2(...)` call to remain after confirmation.
- Replace destination-before-project ordering with project -> detached report validation -> Save confirmation -> export.
- Keep `GetOrCreate` forbidden for all legacy read-only sections.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active reservation for this legacy lifecycle preflight. This lane is independent from completed #29/#30 export gate reconciliations and does not reopen product source.

## Completion condition

The legacy lifecycle gate agrees with the current ED2 validate-before-Save/write-after-confirmation contract while retaining non-creating read-only semantics, is pushed to `main`, and this claim is closed with exact evidence.
