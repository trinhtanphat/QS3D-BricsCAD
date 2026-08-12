# Work claim — release #29 BBS export-order preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release29-bbs-export-order-preflights`
- Registered: `2026-08-12T09:10:00+07:00`
- Baseline main SHA: `bfe52704a50a8db13f811598dbb81cdf63c13c82`
- Priority: QS3D Cloud V25 Preview Build & Release #29 reports three BBS ordering failures from older gates that now contradict both current source and the newer repository-wide `preflight-export-before-save-dialog.py` contract.

## Reserved scope

Reconcile the legacy BBS XLSX/CSV ordering gates with the current export contract: validate existing project/read-only detached schedule and finite exportability before opening SaveFileDialog, then perform the file write only after the user confirms the destination. Preserve all source/export behavior unchanged.

## Expected surfaces

- `scripts/preflight-bbs-command-arithmetic.py`
- `scripts/preflight-bbs-csv-existing-project.py`
- `scripts/preflight-bbs-csv-export-freshness.py`
- this claim file for close-out

## Canonical evidence

- `src/QS3D.BricsCAD.V25/BbsCsvCommands.cs` currently validates existing project, detached regeneration, rows and finite aggregate before `SaveFileDialog`, then writes only after `ShowDialog()` succeeds.
- `Commands.ExportBbs()` follows the same BBS XLSX ordering.
- commit `f0d51a65a6aa8fefe61dd5de6e0a63746cd6085f` intentionally changed BBS CSV to `validate BBS CSV before save dialog`.
- later commit `45038141a1c5340be3d1997e084a4f2d295ab8ed` added `preflight-export-before-save-dialog.py`, which explicitly requires ED2/BBS XLSX/BBS CSV exportability validation before the Save dialog and the actual exporter call after confirmation.

## Excluded scope

- No edits to `Commands.cs`, `BbsCsvCommands.cs`, exporters, schedule builders, regeneration, project state or UI implementation.
- No weakening of existing-project/read-only detached snapshot guards, finite aggregate validation, no-live-mutation guards, file-write-after-confirmation, or post-export UI isolation.
- No unrelated run #29 failures, GitHub Actions dispatch, release publication or BricsCAD runtime qualification.

## Validation plan

- BBS XLSX arithmetic gate must require finite aggregate calculation before SaveFileDialog and exporter invocation after `ShowDialog()` confirmation.
- BBS CSV lifecycle/freshness gates must require existing read-only lookup -> detached copy -> regeneration -> schedule build -> rows/aggregate validation -> SaveFileDialog -> confirmation -> export -> best-effort UI.
- Preserve prohibitions on project creation, mutation binding, live-project regeneration/build and post-export UI exception leakage.
- Re-fetch all three gate blobs before writes and never overwrite concurrent work.
- Read back all final gates and close with exact implementation SHAs. No aggregate PASS claim without a newer manual run.

## Coordination

Current observed active claims concern Takeoff, QSDB, runtime-health and other independent surfaces. No current reservation discovered for these three BBS preflight scripts. This claim intentionally does not own BBS product source.

## Completion condition

The three BBS gates agree with current source and the newer repository-wide pre-save-validation contract without weakening safety, all changes are pushed to `main`, and this claim is closed with exact evidence.
