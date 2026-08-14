# Work claim — BLT-reference UI parity registration audit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ui-parity-followup`
- Registered: `2026-08-14T12:37:00+07:00`
- Baseline main SHA: `2b9e7371b3886d23636b0ab5b1a247f3a5faaa53`
- Scope amended: `2026-08-14T12:42:00+07:00` from refreshed `main` `181c8151793870c3f739a1464697f89d991f855a`
- Scope amended again after repository/session review: `docs/COMMANDS.md` is missing every screenshot-facing command added in `2bfd1bb...`, so command-reference synchronization is included before close-out.
- Priority: owner requested `continue all` plus a full session/repository review after the screenshot-parity implementation.

## Reserved scope

Audit the just-landed clean-room BLT-reference V25 UI parity implementation for integration gaps that can be proven from repository source. Fix the concrete presentation-registration defect where `ReferenceWorkspaceTreeAugmenter.EnsureRegistered()` had no caller, and synchronize the canonical command reference with the new VẼ/IFC entry points.

Also strengthen the focused parity preflight/documentation so future refactors must keep the registration path reachable.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ReferenceTreeRegistration.cs`
- `scripts/preflight-blt-reference-ui-parity.py`
- `docs/BLT-REFERENCE-UI-PARITY-PLAN-2026-08-14.md`
- `docs/COMMANDS.md` — document the screenshot-facing VẼ/IFC commands now present in source
- this claim file for close-out

`WorkspacePanel.CompactShell.cs` was inspected read-only to find the existing static presentation initialization point but is not an implementation write surface. The new partial keeps this follow-up isolated from existing compact-shell behavior and from the active startup claim.

## Excluded scope

- No edits to `WorkspacePanel.xaml.cs`, `WorkspacePanel.CompactShell.cs`, `RightPanel.xaml.cs`, `PluginEntry.cs`, `PaletteCoordinator.cs`, `DocumentLifecycleCoordinator.cs`, or `RibbonInitializationCoordinator.cs`.
- No Source Reconcile/#1005, Curtain/#1106/#1125, Model Health, release workflow, signing, or LOCAL_ONLY runtime work.
- No proprietary BLT binaries/assets/code and no pixel-for-pixel copying.
- No GitHub Actions dispatch; CI remains manual-only unless separately requested.

## Validation plan

- Re-read current `main` immediately before implementation and preserve concurrent commits.
- Verify the tree augmenter has exactly one reachable, idempotent registration path through `WorkspacePanel` type initialization.
- Extend the focused source guard to reject an orphaned `EnsureRegistered()` implementation.
- Verify `docs/COMMANDS.md` names every screenshot-facing QS3D command and states which entries delegate to native BricsCAD workflows.
- Read back the final pushed source and keep licensed BricsCAD V25 rendering/click/HiDPI acceptance classified as local-only.

## Coordination

This lane is a narrow follow-up to completed commit `2bfd1bb30f265ca301b0b902bf466bf8628e3231`. It does not take ownership of the active NETLOAD/startup lifecycle claim or the active Source Reconcile/Curtain issue lanes.

## Completion condition

Registration is source-reachable and guarded on current `main`, command documentation matches the new UI surface, the plan records the integration correction, the coherent follow-up is pushed without overwriting concurrent work, and the claim is marked `COMPLETED` with exact SHA evidence.
