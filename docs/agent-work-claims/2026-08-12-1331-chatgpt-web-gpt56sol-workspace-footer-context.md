# Agent Work Claim — Workspace Footer Context

- Status: ACTIVE
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 13:31 Asia/Ho_Chi_Minh
- Scope: Add canonical read-only Project / Zone / Floor context to the Workspace footer using the current ExistingProjectMutationContext.
- Owned paths:
  - src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FooterContext.cs
  - scripts/preflight-workspace-footer-context.py
- Released paths:
  - src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml
  - src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs
  - src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs
- Contract:
  - Resolve the live project through ExistingProjectMutationContext.TryGet(...).
  - Resolve zone from ActiveZoneId via FindZone(...).
  - Resolve floor from ActiveFloorId via FindFloor(...).
  - UI refresh must be read-only: no SetActive*, Touch(), or ChangeVersion mutation.
  - Gracefully render empty/unavailable context when there is no active project/zone/floor.
- Verification: source regression/preflight only; no claim of BricsCAD V25 runtime PASS from remote execution.
