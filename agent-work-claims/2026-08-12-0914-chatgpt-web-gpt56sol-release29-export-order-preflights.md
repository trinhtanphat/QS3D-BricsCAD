# Work claim — release #29 export ordering preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release29-export-order-preflights`
- Registered: `2026-08-12T09:14:00+07:00`
- Completed: `2026-08-12T09:16:00+07:00`
- Baseline main SHA: `f56e0df9d7ccd8981395932990471fe091cf70bf`
- Claim commit: `e64cf8da7e119093a70ecde05c63cf57880cc4c2`
- Implementation commits: `c022c99918782e2091eb8514a48224d6e0376c90`, `c30269d2c54c885c69c26ff88b511c283d204b91`
- Priority: QS3D Cloud V25 Preview Build & Release #29 exposed additional export-order gates that required Save confirmation before read-only validation even though current source and the newer repository-wide pre-save-validation gate require exportability checks first and persistent writes only after confirmation.

## Completed scope

Reconciled only the stale ED2/BBS XLSX and BBS CSV ordering assertions in the shared export static gates while preserving other exporters' current lifecycle contracts unchanged.

## Changed surfaces

- `scripts/preflight-command-xlsx-export-freshness.py`
- `scripts/preflight-export-command-side-effects.py` — BBS CSV case only; Curtain/Door/Material/Room/Template ordering remains destination-first
- this claim file for close-out

## Canonical evidence

- `scripts/preflight-export-before-save-dialog.py` currently requires ED2/BBS XLSX/BBS CSV to validate exportability before SaveFileDialog and to call exporters only after `ShowDialog()` confirmation.
- Current `Commands.ExportEd2Workflow()`, `Commands.ExportBbs()` and `BbsCsvCommands.ExportCsv()` follow that contract.
- run #29 itself showed the canonical pre-save-validation gate PASS while these older gates failed on the opposite ordering.

## Validation evidence

- Final XLSX freshness gate blob `c93f5a424bea02445c7cb13cbb1db13002376993` requires ED2 existing project -> detached regenerate -> detail/summary -> live-handle validation -> SaveFileDialog -> confirmation -> XLSX write; BBS requires existing project -> detached regenerate -> fresh rows -> finite aggregate -> SaveFileDialog -> confirmation -> XLSX write.
- It retains no-create/no-live-regeneration/build prohibitions and explicitly rejects exporter calls before confirmation.
- Final shared side-effect gate blob `9d4d03ccf5c7a9f39c990fb37ebc5c1886bba33f` marks only BBS CSV as `validate_before_dialog=True`; Curtain, Door/Opening, Material, Room finish and Template keep their previous destination-first ordering.
- The shared gate continues to require detached read-only state, aggregate validation, persistent export before best-effort finalization, no project creation/mutation binding/live regeneration, and post-export UI isolation.
- Claim ancestry was verified after publication; the immediate concurrent commit only closed an unrelated EntitySnapshot claim.

## Excluded / unchanged

- No product source edits, no unit-binding semantic changes, no exporter/schedule/report changes.
- No changes to Curtain/Door/Material/Room/Template ordering.
- No unrelated run #29 failure changes in this lane.
- No GitHub Actions dispatch or BricsCAD runtime qualification.

## Validation boundary

Remote source/static readback only. This session did not execute these Python gates, the aggregate suite, full .NET build/test or licensed BricsCAD runtime. A newer manual workflow run is required before claiming aggregate PASS.

## Completion condition

Satisfied: the two static gates no longer contradict the current canonical pre-save-validation lifecycle, retain their safety boundaries, are pushed to `main`, and the reservation is released.
