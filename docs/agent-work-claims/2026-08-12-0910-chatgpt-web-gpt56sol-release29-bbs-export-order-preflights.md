# Work claim — release #29 BBS export-order preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release29-bbs-export-order-preflights`
- Registered: `2026-08-12T09:10:00+07:00`
- Completed: `2026-08-12T09:12:00+07:00`
- Baseline main SHA: `bfe52704a50a8db13f811598dbb81cdf63c13c82`
- Claim commit: `82d9cb6422e11ef862bc67e5ab3c7dd349342857`
- Implementation commits: `9035da9b36a11e5d6d6673bbddc467f6c4a503e2`, `573e1cd7dfe8da01dd6ca6c94c53f4a12d6d1c85`, `824a29769299a2dc1d411a2aca1e8124c8f9bbe5`
- Priority: QS3D Cloud V25 Preview Build & Release #29 reported three BBS ordering failures from older gates that contradicted both current source and the newer repository-wide `preflight-export-before-save-dialog.py` contract.

## Completed scope

Reconciled the legacy BBS XLSX/CSV ordering gates with the current export contract: validate existing project/read-only detached schedule and finite exportability before opening SaveFileDialog, then perform the file write only after the user confirms the destination. Product/export source remains unchanged.

## Changed surfaces

- `scripts/preflight-bbs-command-arithmetic.py`
- `scripts/preflight-bbs-csv-existing-project.py`
- `scripts/preflight-bbs-csv-export-freshness.py`
- this claim file for close-out

## Canonical evidence

- `src/QS3D.BricsCAD.V25/BbsCsvCommands.cs` validates existing project, detached regeneration, rows and finite aggregate before `SaveFileDialog`, then writes only after `ShowDialog()` succeeds.
- `Commands.ExportBbs()` follows the same BBS XLSX ordering.
- commit `f0d51a65a6aa8fefe61dd5de6e0a63746cd6085f` intentionally changed BBS CSV to `validate BBS CSV before save dialog`.
- later commit `45038141a1c5340be3d1997e084a4f2d295ab8ed` added `preflight-export-before-save-dialog.py`, which explicitly requires ED2/BBS XLSX/BBS CSV exportability validation before the Save dialog and the actual exporter call after confirmation.

## Validation evidence

- Final BBS XLSX arithmetic gate blob: `04aa937839a1cbee492d66607b02c04c7055ea9b`; it requires finite aggregate calculation before the BBS SaveFileDialog, then confirmation before `XlsxRebarScheduleExporter.Export(...)`, while retaining all arithmetic/exporter/spacing regression guards.
- Final BBS CSV lifecycle gate blob: `9522757451bb72f7b23063b3b06a615db08e00e1`; it pins existing read-only lookup -> detached copy -> regeneration -> schedule build -> rows/finite aggregate validation -> Save dialog -> confirmation -> export -> best-effort UI.
- Final BBS CSV freshness gate blob: `0a7f87d21ec14a6c829aa1772dba7f3cf6bfb9b0`; it retains no-project-creation/no-mutation/no-live-regeneration/build prohibitions and explicitly rejects export before Save confirmation.
- The first CSV update attempt received a 409 during rapid concurrent `main` movement; the file was re-fetched and retried without force/overwrite.
- Claim ancestry was verified after publication; the immediate concurrent change touched an unrelated Semantic Sheet preflight.

## Excluded / unchanged

- No edits to `Commands.cs`, `BbsCsvCommands.cs`, exporters, schedule builders, regeneration, project state or UI implementation.
- No weakening of existing-project/read-only detached snapshot guards, finite aggregate validation, no-live-mutation guards, file-write-after-confirmation, or post-export UI isolation.
- No unrelated run #29 failure changes in this lane.
- No GitHub Actions dispatch, release publication or BricsCAD runtime qualification.

## Validation boundary

Remote source/static readback only. This session did not execute these Python gates, the aggregate suite, full .NET build/test or licensed BricsCAD runtime. A newer manual workflow run is required before claiming aggregate PASS.

## Completion condition

Satisfied: the three BBS gates agree with current source and the newer repository-wide pre-save-validation contract without weakening safety, all changes are pushed to `main`, and the reservation is released.
