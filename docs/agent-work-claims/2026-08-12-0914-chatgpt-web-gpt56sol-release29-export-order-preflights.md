# Work claim — release #29 export ordering preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release29-export-order-preflights`
- Registered: `2026-08-12T09:14:00+07:00`
- Baseline main SHA: `f56e0df9d7ccd8981395932990471fe091cf70bf`
- Priority: QS3D Cloud V25 Preview Build & Release #29 exposes additional export-order gates that still require Save confirmation before read-only validation even though current source and the newer repository-wide pre-save-validation gate require exportability checks first and persistent writes only after confirmation.

## Reserved scope

Reconcile only the stale ED2/BBS XLSX and BBS CSV ordering assertions in the shared export static gates while preserving other exporters' current lifecycle contracts unchanged.

## Expected surfaces

- `scripts/preflight-command-xlsx-export-freshness.py`
- `scripts/preflight-export-command-side-effects.py` — BBS CSV case only; Curtain/Door/Material/Room/Template ordering remains unchanged
- this claim file for close-out

## Canonical evidence

- `scripts/preflight-export-before-save-dialog.py` currently requires ED2/BBS XLSX/BBS CSV to validate exportability before SaveFileDialog and to call exporters only after `ShowDialog()` confirmation.
- Current `Commands.ExportEd2Workflow()`, `Commands.ExportBbs()` and `BbsCsvCommands.ExportCsv()` follow that contract.
- run #29 itself shows the canonical pre-save-validation gate PASS while these older gates fail on the opposite ordering.

## Excluded scope

- No product source edits, no changes to Curtain/Door/Material/Room/Template ordering, no unit-binding semantic changes, no exporter/schedule/report changes.
- No weakening of existing-project, detached-snapshot, fresh-report/live-handle, finite aggregate, no-live-mutation, post-export UI isolation or write-after-confirmation guards.
- No unrelated run #29 failures, GitHub Actions dispatch or BricsCAD runtime qualification.

## Validation plan

- ED2: require existing project -> detached regenerate -> detail/summary -> live-handle validation -> Save dialog -> confirmation -> XLSX write.
- BBS XLSX: require existing project -> detached regenerate -> fresh rows -> finite aggregate -> Save dialog -> confirmation -> XLSX write.
- Shared side-effect gate: retain old destination-first ordering for Curtain/Door/Material/Room/Template, but require pre-save validation only for the BBS CSV case and still forbid any CSV write before confirmation.
- Preserve all existing no-create/no-live-mutation and UI-isolation checks.
- Re-fetch exact blobs before writes, read back results and close with actual SHAs.

## Coordination

No observed current claim reserves these two preflight scripts. This lane is separate from the already-completed release29 BBS three-gate reconciliation and does not reopen its source scope.

## Completion condition

The two static gates no longer contradict the current canonical pre-save-validation lifecycle, retain all safety boundaries, are pushed to `main`, and this claim is closed with exact evidence.
