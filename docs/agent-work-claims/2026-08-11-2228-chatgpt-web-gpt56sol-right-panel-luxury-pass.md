# Work claim — Right Panel luxury hierarchy pass

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-right-panel-luxury-pass`
- Registered: `2026-08-11T22:28:00+07:00`
- Completed: `2026-08-11T22:37:00+07:00`
- Baseline main SHA: `ee620fdc586a48581aaa9613315aa5510bd3845b`
- Priority: P0 owner screenshot visual polish

## Delivered

The screenshot-visible `QUẢN LÝ BẢN VẼ` / `QUẢN LÝ LỚP` palette now uses the existing luxury v2 hierarchy without changing Xref/layer behavior:

- `RightBadge` now derives from the shared `StatusPill` primitive;
- toolbar bands and list surfaces derive from the shared `PremiumCard` primitive;
- both Drawing/Xref and Layer headings use a slim restrained champagne hierarchy marker;
- splitter contrast and the bottom status surface use the shared stronger/raised dark resources;
- the current Xref `Tỉ lệ` / `ScaleText` column is preserved exactly;
- every existing attach/reload/move/lock/unlock/zoom/detach/show/hide/invert/refresh action, context menu, selection handler and keyboard route remains present.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml`
- `scripts/preflight-right-panel-luxury-ui.py`
- this claim file

No `Theme.xaml`, RightPanel code-behind/partials, Workspace, Quantity, Ribbon, Core, updater/release or Xref/layer mutation implementation was changed by this lane.

## Commits / integration

- prepared presentation branch commit: `505c35bd5eb1f6e18d3a4d77cdb4a30f88f5fdd8`
- prepared guard branch commit: `a3cefeb104656f43614d9e7f8b09f3d9096ef51e`
- first PR `#504` was closed unmerged after rapid concurrent base movement made the merge window stale;
- rebased branch integration commit: `950feb9ab92000ed414b11f6be473a225f2581ab`;
- second PR `#505` was also closed unmerged after the base advanced repeatedly during merge calculation;
- conflict-safe direct `main` presentation commit: `c7849519482642af4767c6b465ff8dd16de844fb`;
- conflict-safe direct `main` guard commit: `885dd9ae43a34fb7918caf0ea9981ba0aed8f61b`.

The direct writes used the current unchanged `RightPanel.xaml` blob SHA and GitHub's contents API, so they integrated atop the then-current `main` without force-updating the branch or overwriting unrelated concurrent commits.

## Validation evidence

- Final `main` readback confirms `RightPanel.xaml` blob `8292171dc2c1cfd294907b4c8006fde4aa82b3ec`.
- Final `main` readback confirms `scripts/preflight-right-panel-luxury-ui.py` blob `0e2896968d5cae8736aa015b07756f321e2c06d6`.
- Source readback confirms `DrawingList`, `LayerSearchBox`, `LayerList`, all current click/selection/right-click/check handlers, context-menu actions, `Tỉ lệ` / `ScaleText`, layer visibility/lock/color/name bindings, premium-card/status-pill resources and restrained luxury markers remain present.
- The focused preflight is auto-discovered by the existing `preflight-*.py` convention and statically guards XML parseability, all named/handler contracts, Xref scale state and the no-heavy-effects/no-behavior-layer boundary.
- This hosted connector lane did not execute repository scripts, .NET/BricsCAD builds or a licensed V25 runtime; no such PASS is claimed.
- No GitHub Actions were dispatched or rerun.

## LOCAL_ONLY boundary

Real BricsCAD V25 visual qualification, narrow palette clipping and 100% / 125% / 150% / 200% HiDPI verification remain local-only under the existing visual qualification boundary.

## Completion

Reservation released. The screenshot-visible Right Panel luxury pass and focused regression guard are integrated on `main` while preserving the current Xref/layer behavior and scale-state winner.
