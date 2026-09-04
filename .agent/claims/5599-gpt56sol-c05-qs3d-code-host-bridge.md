# Agent work claim — Issue #5599

- Lane-Key: `issue-5599`
- Reservation-Protocol: `v2`
- Canonical owner/session: `account:trinhtanphat|session:gpt56sol-chat-20260904T1116Z-hostbridge`
- Canonical carrier: `agent/gpt56sol-c05-20260904-0913-hostbridge/issue-5599-qs3d-code-host-bridge`
- Ownership-Key: `qs3d-code.bricscad-host-bridge-v1`
- Baseline protected main: `0fa476669b2c07854d10f60555b319055aaaa847`
- Parent: #5545
- Plan: `docs/superpowers/plans/2026-09-04-qs3d-code-embedded-agent-harness.md`

## Expected paths

- `src/QS3D.BricsCAD.V25/Qs3dCodeHostBridge.cs`
- `src/QS3D.BricsCAD.V25/Qs3dCodeHostService.cs`
- `src/QS3D.BricsCAD.V25/Qs3dCodeLocalIpcServer.cs`
- `src/QS3D.BricsCAD.V25/Qs3dCodeHostContracts.cs`
- `scripts/preflight-qs3d-code-host-bridge.py`
- `src/QS3D.BricsCAD.V25/PluginEntry.cs`
- `src/QS3D.BricsCAD.V26/PluginEntry.cs`
- `.agent/claims/5599-gpt56sol-c05-qs3d-code-host-bridge.md`

## Runtime boundary

Remote/source/static and available V25/V26 compile evidence are admissible. Licensed interactive BricsCAD, real named-pipe lifecycle, drawing switching, bounded CAD mutation and shutdown cleanup remain `LOCAL_ONLY`; no hosted evidence is a native runtime PASS.
