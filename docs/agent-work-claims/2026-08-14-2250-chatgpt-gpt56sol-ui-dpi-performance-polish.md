# Work claim — UI/DPI/performance production polish

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260814-ui-production-polish`
- Registered: `2026-08-14T22:50:00+07:00`
- Completed: `2026-08-14T23:09:00+07:00`
- Baseline main SHA: `f778b9200f149cc5a4e342f1da0416b95ec628fb`
- Implementation branch: `agent/chatgpt-gpt56sol/ui-dpi-performance-polish-20260814`
- Integration PR: `#1372`
- Integration merge SHA: `16666e914d8ea291466c6263a8a16433828cb3ed`
- Priority: owner requested the UI/DPI/performance production-polish assessment to move from ~80% to complete remote-safe coverage.

## Reserved scope

Finish systemic WPF production polish that can be proven from source: shared DPI/pixel-alignment defaults, virtualization/recycling and scroll behavior for large item controls, responsive shared styling, and regression/preflight coverage that prevents those contracts from drifting. Keep behavior additive and centralized rather than performing speculative per-window redesigns.

## Implemented

- Added `src/QS3D.BricsCAD.V25/UI/ProductionUiPolish.cs` and registered it from `PluginEntry.Initialize()` before host UI coordination starts.
- DPI/layout/text defaults (`UseLayoutRounding`, `SnapsToDevicePixels`, display text formatting) are filled only when the dependency-property value source is `Default`; explicit local values, styles, bindings, templates, animations, and existing product-specific choices remain authoritative.
- The guard is restricted to the outermost QS3D `Window`/`UserControl`. It scans only the QS3D visual tree and does not register global `DataGrid`/`ListBox`/`TreeView` class handlers against BricsCAD-owned UI.
- `DataGrid` receives safe default row/column virtualization, logical scrolling, item virtualization, recycling, and virtualization while grouping.
- `ListBox`/`ListView` receive logical scrolling, item virtualization, recycling, and grouping virtualization; `TreeView` receives logical scrolling, item virtualization, and recycling.
- Added `scripts/preflight-ui-production-polish.py`; existing `scripts/preflight-all.py` discovers it automatically through its `preflight-*.py` contract.
- The preflight preserves the existing shared `Theme.xaml` DPI/virtualization contract, verifies runtime registration and host isolation, and rejects explicit XAML regressions that disable item/DataGrid virtualization. It intentionally does not reject `CanContentScroll=False` globally because physical scrolling is valid for non-item outer scroll surfaces.

## Integration evidence

- Branch source commits include `039e60dd09910268f3089db234e70180f38226e9`, `e45312b6e2bb9373c64957404c28e78efc3c95f8`, `af81748b9614c0830611bfcb0c8630fa87d244e2`, `6ec80a3a0e1580d38dec08b27ee99144b4d31dc8`, `c623997b007e0dfd77984251537baad94b035d47`, and `3d804060e79b6962114e4b95232af92cc770a379`.
- Concurrent `main` work was reconciled without force-push via merge commit `7fa062f0c871226680c290cba4ab05450cde8bd9`, using then-current `main` as the primary parent and the implementation branch as the second parent.
- PR `#1372` was confirmed mergeable after reconciliation and merged into `main` at `16666e914d8ea291466c6263a8a16433828cb3ed`.
- Post-merge read-back confirmed `ProductionUiPolish.cs` is present on `main` with blob `bda034d6702c902dc6f4a487173a0ee489718321`.
- Repository CI policy is manual-only. No CI PASS is claimed for this lane because the available connector did not expose a new workflow-dispatch action, and the local container cannot resolve GitHub for an independent checkout. This limitation does not get rewritten as a pass.

## Excluded / LOCAL_ONLY acceptance

- Grid V25 UI planner surfaces reserved by the separate active lane, commercial signing/package integrity, interchange FieldMerge, and other active claim surfaces were not modified.
- Semantic/business behavior, CAD model mutation, geometry, persistence schemas, Ribbon feature additions, and unrelated visual redesign were not modified.
- Licensed BricsCAD runtime, Windows multi-monitor DPI transitions at 100/125/150/200%, GPU/render timings, native host responsiveness measurements, installer/signing, and screenshot acceptance remain `LOCAL_ONLY`. They must be validated on a licensed BricsCAD V25 Windows host and are not represented as remote PASS evidence.

## Completion condition

Remote-safe WPF production defaults are centralized, high-volume item-control virtualization/recycling/logical-scrolling defaults are hardened without overriding intentional local choices, regression coverage is integrated, and the implementation is merged through the declared agent branch and PR according to repository policy. Native BricsCAD/Windows DPI transitions and real host performance remain separate `LOCAL_ONLY` acceptance evidence.