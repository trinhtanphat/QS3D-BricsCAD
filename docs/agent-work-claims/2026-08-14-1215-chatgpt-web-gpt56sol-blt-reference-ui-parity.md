# Work claim — BLT-reference clean-room UI/button parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ui-parity`
- Registered: `2026-08-14T12:15:00+07:00`
- Baseline main SHA: `8fb008400aaed7581b349a66dd9496f1ce4f5a78`
- Priority: owner supplied four BLT3D reference screenshots and explicitly requested a detailed plan plus source implementation/commit/push on `main`.

## Reserved scope

Perform a clean-room source audit against the owner-provided screenshots and close the remote-safe V25 Ribbon/command-surface gaps without copying proprietary code/assets. Preserve the existing BricsCAD-hosted product boundary and existing QS3D semantic workflows.

The implementation lane is limited to screenshot-facing Ribbon information architecture/button wiring plus thin command adapters needed for screenshot buttons that are not currently exposed. Existing Workspace/RightPanel surfaces are audited for parity but are not rewritten when they already provide the requested behavior.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs`
- a new V25 screenshot/reference command-adapter source file if required
- `docs/BLT-REFERENCE-UI-PARITY-PLAN-2026-08-14.md`
- a focused static/preflight guard under `scripts/` if useful
- `docs/LOCAL-AGENT-INBOX.md` only if the source change creates a new exact native V25 acceptance scenario not already covered

## Excluded scope

- `src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs`, `PluginEntry.cs`, `PaletteCoordinator.cs`, `WorkspacePanel.xaml.cs`, and `RightPanel.xaml.cs` remain owned by the active NETLOAD/startup claim and will not be modified here.
- No changes to `ModelHealthService`, comprehensive-health smoke, Curtain3D, release packaging/workflows, signing, or LOCAL-003 lanes.
- No proprietary BLT3D binaries, icons, assets, algorithms, or code will be copied.
- No claim of licensed BricsCAD runtime PASS from remote/static evidence.

## Validation plan

- Re-read current `main` immediately before writes and avoid overlapping concurrent commits.
- Ensure every new Ribbon button maps either to an existing QS3D command or to an explicit thin BricsCAD-native command adapter with clear behavior.
- Add a deterministic source guard for screenshot-critical VẼ/IFC labels and command mappings where practical.
- Perform source readback after push; leave GitHub Actions undispatched because the owner did not explicitly request CI in this message.
- Record native V25 verification as pending/local-only where source/static evidence cannot prove host UI behavior.

## Coordination

The active NETLOAD claim `docs/agent-work-claims/2026-08-13-1720-chatgpt-netload-existing-project-hang.md` owns Ribbon startup scheduling/coordinator and Workspace/RightPanel load lifecycle. This claim intentionally modifies only `QuickWorkflowRibbonAugmenter.cs` inside the Ribbon subsystem and does not change initialization timing or startup lifecycle contracts. Other active health/Curtain/release/concurrency-handoff claims are outside this UI lane.

## Completion condition

A coherent source/docs/static-guard batch is pushed to current `main`, the claim is closed with exact implementation SHA/readback, and any required licensed/native V25 UI acceptance remains explicitly classified rather than reported as a remote PASS.
