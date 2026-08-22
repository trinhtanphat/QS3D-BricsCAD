# Work claim — BLT-reference UI parity registration audit

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ui-parity-followup`
- Registered: `2026-08-14T12:37:00+07:00`
- Baseline main SHA: `2b9e7371b3886d23636b0ab5b1a247f3a5faaa53`
- Scope amended: `2026-08-14T12:42:00+07:00` from refreshed `main` `181c8151793870c3f739a1464697f89d991f855a`
- Priority: owner requested `continue all` plus a full session/repository review after the screenshot-parity implementation.

## Reserved scope

Audit the just-landed clean-room BLT-reference V25 UI parity implementation for integration gaps that can be proven from repository source. Fix the concrete presentation-registration defect where `ReferenceWorkspaceTreeAugmenter.EnsureRegistered()` has no caller, so the screenshot-detail tree augmentation can remain dead code even though its source exists.

Also strengthen the focused parity preflight/documentation so future refactors must keep the registration path reachable.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ReferenceWorkspaceTreeAugmenter.cs`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ReferenceTreeRegistration.cs` — new presentation-only partial used to make registration reachable from `WorkspacePanel` type initialization without editing an active startup/lifecycle file
- `scripts/preflight-blt-reference-ui-parity.py`
- `docs/BLT-REFERENCE-UI-PARITY-PLAN-2026-08-14.md`
- this claim file for close-out

`WorkspacePanel.CompactShell.cs` was inspected read-only to find the existing static presentation initialization point but is no longer an implementation write surface. The new partial keeps this follow-up isolated from existing compact-shell behavior and from the active startup claim.

## Excluded scope

- No edits to `WorkspacePanel.xaml.cs`, `WorkspacePanel.CompactShell.cs`, `RightPanel.xaml.cs`, `PluginEntry.cs`, `PaletteCoordinator.cs`, `DocumentLifecycleCoordinator.cs`, or `RibbonInitializationCoordinator.cs`; those lifecycle/startup/presentation surfaces are preserved.
- No Source Reconcile/#1005, Curtain/#1106/#1125, Model Health, release workflow, signing, or LOCAL_ONLY runtime work.
- No proprietary BLT binaries/assets/code and no pixel-for-pixel copying.
- No GitHub Actions dispatch; CI remains manual-only unless separately requested.

## Validation plan

- Re-read current `main` immediately before implementation and preserve concurrent commits.
- Verify the tree augmenter has exactly one reachable, idempotent registration path through `WorkspacePanel` type initialization.
- Extend the focused source guard to reject an orphaned `EnsureRegistered()` implementation.
- Read back the final pushed source and keep licensed BricsCAD V25 rendering/click/HiDPI acceptance classified as local-only.

## Coordination

This lane is a narrow follow-up to completed commit `2bfd1bb30f265ca301b0b902bf466bf8628e3231`. It does not take ownership of the active NETLOAD/startup lifecycle claim or the active Source Reconcile/Curtain issue lanes. The new partial file avoids modifying the startup claim's reserved files.

## Completion condition

Registration is source-reachable and guarded on current `main`, the plan records the integration correction, the coherent follow-up is pushed without overwriting concurrent work, and the claim is marked `COMPLETED` with exact SHA evidence.
