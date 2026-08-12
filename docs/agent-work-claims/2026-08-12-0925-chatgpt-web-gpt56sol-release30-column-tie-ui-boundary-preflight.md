# Work claim — release #30 Column Tie QTY UI-boundary preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release30-column-tie-ui-boundary-preflight`
- Registered: `2026-08-12T09:25:00+07:00`
- Completed: `2026-08-12T09:27:00+07:00`
- Baseline main SHA: `52cc7ca78284aa7f0fa6b33af175caff7b9bb26a`
- Claim commit: `96f6475f4e62b3c527329663e96d31b9ca1af20a`
- Implementation commit: `0179dc5312733ff1e157ee59885e78a12a55f001`
- Priority: QS3D Cloud V25 Preview Build & Release #30 had contradictory Column Tie QTY gates: the canonical audit-revision gate passed and forbade redundant `project.Touch()`, while the UI-boundary gate still required that obsolete touch.

## Completed scope

Reconciled only `scripts/preflight-column-tie-quantity-ui-boundary.py` with the current audit-owned revision lifecycle. `ColumnTieQuantityCommands.cs` production behavior and `scripts/preflight-column-tie-audit-revision.py` remained unchanged.

## Canonical evidence retained

- `ColumnTieQuantityCommands.CalculateSelectedColumnTies()` snapshots the project before quantity mutation.
- Each mutated Column records `quantity.rebar.column.tie` through `AuditTrail.ForProject(project).Record(...)`, which owns revision advancement.
- Failure restores the project snapshot; successful semantic mutation exits the try/catch before `FinalizeUi(document, message)`.
- The canonical audit-revision gate explicitly rejects a standalone `project.Touch();` after per-target AuditTrail records.

## Implemented gate contract

- Requires snapshot capture, canonical Tie QTY AuditTrail call, snapshot restore and post-boundary `FinalizeUi`.
- Requires ordering snapshot -> audit-owned mutation -> catch/restore -> FinalizeUi.
- Explicitly fails if a standalone `project.Touch();` returns.
- Pins exactly one per-loop `quantity.rebar.column.tie` AuditTrail call site.
- Preserves the prohibition on direct fallible Palette/editor work between rollback boundary and FinalizeUi.
- Preserves FinalizeUi exception isolation, Palette refresh and the committed-UI warning marker.

## Validation performed

- Re-fetched current source, canonical audit gate and stale UI gate before the claim/write.
- Verified the claim commit remained an ancestor of moving `main`; the only intervening change at that check was unrelated Interchange source.
- Read back the implemented UI-boundary gate from `main` at blob `4549933f83f84afa7cf86fdffaed094e1138aef5`.
- No production source was changed.
- No GitHub Actions/build/release dispatch was performed and no BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed. The Column Tie QTY UI-boundary gate now agrees with the audit-owned revision contract, retains rollback/post-commit UI isolation, and this reservation is released.
