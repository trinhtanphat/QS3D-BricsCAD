# Work claim — Right Panel luxury hierarchy pass

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-right-panel-luxury-pass`
- Registered: `2026-08-11T22:28:00+07:00`
- Baseline main SHA: `ee620fdc586a48581aaa9613315aa5510bd3845b`
- Priority: P0 owner screenshot visual polish

## Goal

Upgrade the screenshot-visible `QUẢN LÝ BẢN VẼ` / `QUẢN LÝ LỚP` palette into the same premium/professional/luxury v2 language now used by Workspace and review windows, without changing any Xref/layer behavior.

## Coordination

The previous Right Panel compact-interactions lane is `COMPLETED`, and the later Xref scale-state lane was closed at `b850c4f8ca7e768b9798d2f2a95d73ce46b2d3bf`. This claim starts from the current Xref scale column/state winner and must preserve it.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml`
- `scripts/preflight-right-panel-luxury-ui.py` (new)
- this claim file for close-out

## Visual contract

- Do not edit `Theme.xaml`; reuse existing `PremiumCard`, `StatusPill`, raised graphite and restrained champagne resources.
- Refine local `RightBadge` / `RightToolbarBand` styles using shared premium primitives.
- Give Drawing/Xref and Layer sections stronger section hierarchy and premium list surfaces while keeping the palette dense at the existing 255px minimum width.
- Preserve the current Xref `Tỉ lệ` column and all `DrawingList`, `LayerSearchBox`, `LayerList` names, bindings, handlers, context menus and keyboard routing.
- Preserve every current attach/reload/move/lock/unlock/zoom/detach/show/hide/invert/refresh action; no new command path.
- No heavy effects, gradients, animations, negative-margin hacks or gold-heavy decoration.

## Excluded scope

- No RightPanel code-behind/partials, Theme, Workspace, Quantity, Ribbon, Core, updater/release, Xref behavior or layer mutation changes.
- No GitHub Actions dispatch and no BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current `main`/RightPanel before write.
- Add an auto-discovered XAML preflight that preserves all names/handlers/context actions, requires the `Tỉ lệ`/`ScaleText` winner and premium hierarchy, and rejects behavior-layer/heavy-effect tokens.
- Merge through a conflict-checked PR due to high concurrent commit rate.
- Re-read final blobs and close with exact SHAs.

## Completion condition

The screenshot-visible Right Panel is visually consistent with luxury v2, all current Xref/layer behavior and scale state are preserved, static source guard is on `main`, and the claim closes with exact evidence while runtime/HiDPI visual proof remains local-only.
