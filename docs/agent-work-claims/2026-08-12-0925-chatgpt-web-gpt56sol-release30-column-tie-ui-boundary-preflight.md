# Work claim — release #30 Column Tie QTY UI-boundary preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-column-tie-ui-boundary-preflight`
- Registered: `2026-08-12T09:25:00+07:00`
- Baseline main SHA: `52cc7ca78284aa7f0fa6b33af175caff7b9bb26a`
- Priority: QS3D Cloud V25 Preview Build & Release #30 has contradictory Column Tie QTY gates: the canonical audit-revision gate passes and forbids redundant `project.Touch()`, while the UI-boundary gate still requires that obsolete touch.

## Reserved scope

Reconcile only `scripts/preflight-column-tie-quantity-ui-boundary.py` with the current audit-owned revision lifecycle. Preserve `ColumnTieQuantityCommands.cs` production behavior and the existing audit-revision gate unchanged.

## Canonical evidence

- `ColumnTieQuantityCommands.CalculateSelectedColumnTies()` snapshots the project before quantity mutation.
- Each mutated Column records `quantity.rebar.column.tie` through `AuditTrail.ForProject(project).Record(...)`, which owns revision advancement.
- Failure restores the project snapshot; successful semantic mutation exits the try/catch before `FinalizeUi(document, message)`.
- `scripts/preflight-column-tie-audit-revision.py` explicitly rejects a standalone `project.Touch();` after those per-target AuditTrail records and PASSes in run #30.
- Run #30 UI-boundary gate is therefore stale because it requires the exact behavior the canonical audit-revision gate forbids.

## Expected surfaces

- `scripts/preflight-column-tie-quantity-ui-boundary.py`
- this claim file for close-out

## Excluded scope

- No edits to `src/QS3D.BricsCAD.V25/ColumnTieQuantityCommands.cs`.
- No changes to quantity math, selection/binding, rollback, AuditTrail, UI reporting or post-commit behavior.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Require snapshot capture, the canonical `AuditTrail.ForProject(project).Record("quantity.rebar.column.tie", element.Id,` call, snapshot restore, FinalizeUi helper/call, Palette refresh and warning isolation.
- Require source ordering snapshot -> audit mutation -> catch/restore -> post-boundary FinalizeUi -> helper.
- Explicitly fail if the command regains a standalone `project.Touch();`.
- Preserve the existing check that no fallible Palette/editor work occurs directly between rollback boundary and FinalizeUi.
- Re-fetch the exact gate before writing, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Current observed active claims are on unrelated wall-junction and other Core/runtime lanes. Repository search found no current reservation for this Column Tie UI-boundary preflight.

## Completion condition

The Column Tie QTY UI-boundary gate agrees with the canonical audit-owned revision contract, still pins rollback and post-commit UI isolation, is pushed to `main`, and this claim is closed with exact evidence.
