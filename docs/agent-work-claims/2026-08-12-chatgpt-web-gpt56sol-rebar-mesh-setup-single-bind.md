# Agent work claim — Rebar Mesh Setup single canonical bind

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: make `QS3DREBARMESHSETUP` resolve its single supported semantic target from read-only project state before mutation binding, then bind/revalidate canonical state exactly once before opening the modeless setup window.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/RebarMeshSetupCommands.cs`
  - `scripts/preflight-rebar-mesh-setup-single-bind.py`
  - this claim file
- Contract:
  - acquire PICKFIRST/interactive snapshots before any project bind;
  - normalize non-empty source handles and resolve Slab/StructuralWall/Foundation targets against `TryGetReadOnly` preview state;
  - missing project, zero targets or ambiguous target selection returns without `ExistingProjectMutationContext.Require`;
  - freeze preview `ProjectId` + `ChangeVersion` and target id/category;
  - canonical mutation context is bound exactly once, then project/version and selected target identity are revalidated before window creation;
  - existing `RebarMeshSetupWindow` modeless stale-DWG/project/element write guards remain unchanged;
  - callback/UI behavior and mesh quantity/geometry semantics remain unchanged;
  - no GitHub Actions dispatch and no BricsCAD V25 runtime PASS from this web session.
