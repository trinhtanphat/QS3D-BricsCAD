# Agent work claim — Foundation Rebar 3D read-only target resolve

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `COMPLETED`
- Scope: make `QS3DFOUNDATIONREBAR3D` resolve selected semantic Foundation targets from read-only project state before canonical mutation binding, then revalidate project/target freshness before the unchanged native builder.
- Files reserved:
  - `src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs`
  - `scripts/preflight-foundation-mesh-single-bind.py`
  - this claim file
- Completed:
  - source commit `1dd7d6c9a6645ccc064309ea24492c6421e437a1` resolves semantic Foundation targets against read-only project state before mutation bind, freezes ProjectId/ChangeVersion/target IDs, binds once and revalidates before the unchanged native builder;
  - regression commit `7b2f9d1e158e1476cc698b6508476da13bae3f4d` locks zero-target no-op, one canonical bind and freshness ordering;
  - builder rectangle/polygon geometry, native ownership, bar limits, transaction/rollback/audit and post-commit UI were not changed;
  - no GitHub Actions dispatched and no BricsCAD V25 runtime PASS claimed from this web session.
- Reservation released.
