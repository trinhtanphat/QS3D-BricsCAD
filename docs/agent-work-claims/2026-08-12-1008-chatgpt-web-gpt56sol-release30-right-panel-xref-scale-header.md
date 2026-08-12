# Work claim — release #30 Right Panel Xref scale header preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-right-panel-xref-scale-header`
- Registered: `2026-08-12T10:08:00+07:00`
- Baseline main SHA: `8a4ce7d10b62fb687af89f5fe5616c437438f3d3`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reports one Right Panel luxury UI failure because the Xref scale column header was clarified from `Tỉ lệ` to `Tỉ lệ Xref` while the ScaleText binding and all Xref workflows remain present.

## Reserved scope

Reconcile only `scripts/preflight-right-panel-luxury-ui.py` with the current Xref scale header. Preserve RightPanel XAML/code-behind unchanged.

## Canonical evidence

- RightPanel.xaml retains DrawingList, Xref action handlers, context/keyboard wiring and premium shared theme hierarchy.
- The Xref state columns retain Name, LockState, InstanceText and `DisplayMemberBinding="{Binding ScaleText}"`.
- The visible scale header is now the more explicit `Header="Tỉ lệ Xref"`; the old exact `Header="Tỉ lệ"` no longer exists.
- Layer live-state bindings and no-heavy-effects/no-project-mutation presentation constraints remain unchanged.

## Expected surfaces

- `scripts/preflight-right-panel-luxury-ui.py`
- this claim file for close-out

## Excluded scope

- No XAML/code-behind edits, no visual redesign, no Xref behavior changes.
- No weakening of ScaleText binding, actions, layer state, shared theme or forbidden presentation/mutation checks.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Replace only `Header="Tỉ lệ"` with `Header="Tỉ lệ Xref"` in the Xref display contract.
- Retain `DisplayMemberBinding="{Binding ScaleText}"` and every other visual/wiring/state/forbidden assertion unchanged.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active Right Panel luxury/Xref-scale reservation.

## Completion condition

The Right Panel luxury gate follows the current explicit Xref scale header while retaining binding/workflow/theme safety checks, is pushed to `main`, and this claim is closed with exact evidence.
