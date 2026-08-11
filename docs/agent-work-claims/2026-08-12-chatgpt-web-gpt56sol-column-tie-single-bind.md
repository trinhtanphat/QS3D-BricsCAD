# Agent work claim — Column Tie QTY read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DREBARTIEQTY` resolve selected Column semantic targets from read-only project state before canonical mutation binding, then revalidate freshness before quantity mutation.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/ColumnTieQuantityCommands.cs`
  - `scripts/preflight-column-tie-single-bind.py`
  - this claim file
- Contract:
  - preserve PICKFIRST/interactive selection behavior and existing-project-only semantics;
  - resolve Column source-handle targets with `TryGetReadOnly` before mutation bind;
  - missing project or zero Column targets returns without canonical mutation binding;
  - freeze preview `ProjectId` + `ChangeVersion` + target IDs;
  - bind canonical project exactly once, fail closed on project/version/target-set drift, then retain existing calculation, five quantity assignments, per-target audit-owned revision and snapshot rollback;
  - post-commit UI remains unchanged;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
