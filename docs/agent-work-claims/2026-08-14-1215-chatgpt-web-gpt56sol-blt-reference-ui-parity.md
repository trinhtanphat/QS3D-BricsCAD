# Work claim — BLT-reference clean-room UI/button parity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-ui-parity`
- Registered: `2026-08-14T12:15:00+07:00`
- Baseline main SHA: `8fb008400aaed7581b349a66dd9496f1ce4f5a78`
- Implementation SHA: `2bfd1bb30f265ca301b0b902bf466bf8628e3231`
- Priority: owner supplied four BLT3D reference screenshots and explicitly requested a detailed plan plus source implementation/commit/push on `main`.

## Reserved scope

Perform a clean-room source audit against the owner-provided screenshots and close the remote-safe V25 Ribbon/command-surface gaps without copying proprietary code/assets. Preserve the existing BricsCAD-hosted product boundary and existing QS3D semantic workflows.

The implementation lane is limited to screenshot-facing Ribbon information architecture/button wiring plus thin command adapters needed for screenshot buttons that are not currently exposed. Existing Workspace/RightPanel surfaces are audited for parity and augmented only through presentation-safe surfaces when detailed screenshot labels are missing.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs`
  - preserves the existing TẠO MỚI quick panel;
  - adds/updates VẼ labels and command wiring for Đường thẳng, Theo nét CAD, Chữ nhật, Đường tròn, Biên dạng, Dốc sàn, Cắt sàn, Nối góc and Nối chữ T;
  - adds the screenshot-facing IFC panel: Nhập IFC, Nhập IFC (nhẹ), Xóa IFC, Xuất IFC.
- `src/QS3D.BricsCAD.V25/ReferenceUiCommands.cs`
  - thin adapters to existing QS3D or native BricsCAD command workflows; no duplicate IFC/edit engine.
- `src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs`
  - idempotently completes the screenshot's detailed model-tree labels while reusing canonical QS3D category tags and without mutating ProjectState/CAD on load.
- `docs/BLT-REFERENCE-UI-PARITY-PLAN-2026-08-14.md`
  - detailed screenshot inventory, current-source audit, mapping plan and native acceptance boundary.
- `scripts/preflight-blt-reference-ui-parity.py`
  - deterministic source guard for screenshot-critical labels and command mappings.

## Excluded / preserved scope

- `src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs`, `PluginEntry.cs`, `PaletteCoordinator.cs`, `WorkspacePanel.xaml.cs`, and `RightPanel.xaml.cs` were not modified; the active NETLOAD/startup claim remains authoritative for those lifecycle surfaces.
- No changes were made to `ModelHealthService`, comprehensive-health smoke, Curtain3D, release packaging/workflows, signing, or LOCAL-003 lanes.
- No proprietary BLT3D binaries, icons, assets, algorithms, or code were copied.
- QS3D intentionally keeps its own text-first Ribbon styling rather than copying proprietary reference icons.

## Validation / readback

- Claim-only reservation commit landed first as `d321660c632b2c66cf0cefe78c9c0ecea93bb198`.
- Concurrent `main` movement was detected repeatedly; the implementation was reapplied onto the latest non-overlapping main instead of force-pushing or overwriting Curtain/Source-Reconcile work.
- Final implementation commit `2bfd1bb30f265ca301b0b902bf466bf8628e3231` was pushed to `main` with a fast-forward ref update.
- GitHub readback of that SHA confirms the plan, source adapters, Workspace tree augmenter, Ribbon changes and focused preflight are all in the commit.
- GitHub Actions were not dispatched because `CI_POLICY.md` makes CI manual-only and this owner message requested source implementation/commit/push, not a CI run.
- Licensed BricsCAD V25 native UI/runtime acceptance is still a separate local evidence boundary: remote/static source work cannot prove actual host rendering, command availability by installed edition, or native undo/cancel behavior.

## Completion

Remote-safe source implementation for the screenshot-defined V25 UI/button parity lane is complete and pushed. Any exact BricsCAD V25 visual/runtime qualification remains local-only and must not be reported as PASS until exercised on the licensed host.
