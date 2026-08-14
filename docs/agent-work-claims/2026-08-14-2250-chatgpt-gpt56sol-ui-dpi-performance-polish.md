# Work claim — UI/DPI/performance production polish

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260814-ui-production-polish`
- Registered: `2026-08-14T22:50:00+07:00`
- Baseline main SHA: `f778b9200f149cc5a4e342f1da0416b95ec628fb`
- Implementation branch: `agent/chatgpt-gpt56sol/ui-dpi-performance-polish-20260814`
- Priority: owner requested the UI/DPI/performance production-polish assessment to move from ~80% to complete remote-safe coverage.

## Reserved scope

Finish systemic WPF production polish that can be proven from source: shared DPI/pixel-alignment defaults, virtualization/recycling and scroll behavior for large item controls, responsive shared styling, and regression/preflight coverage that prevents those contracts from drifting. Keep behavior additive and centralized rather than performing speculative per-window redesigns.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/Theme.xaml`
- selected `src/QS3D.BricsCAD.V25/UI/*.xaml` only when an existing local style overrides the shared production contract and a narrow compatibility patch is required
- a focused `scripts/preflight-ui-production-polish.py` regression guard
- aggregate preflight wiring only if the repository already has an established feature-preflight registration surface that must include the new guard

## Excluded scope

- Grid V25 UI planner surfaces reserved by the current #79 lane, commercial signing/package integrity, interchange FieldMerge, and other active claim surfaces.
- Semantic/business behavior, CAD model mutation, geometry, persistence schemas, Ribbon feature additions, or unrelated visual redesign.
- Licensed BricsCAD runtime, Windows multi-monitor DPI transitions, GPU/render timings, native host responsiveness measurements, installer/signing, and screenshot acceptance; these remain `LOCAL_ONLY` and must not be represented as remote PASS evidence.
- No implementation source/test/script commit directly to `main`.

## Validation plan

- Audit current shared theme and representative high-volume panels/windows before changing source.
- Centralize layout rounding/device-pixel/text-display defaults where WPF inheritance/style semantics make that safe.
- Ensure large `DataGrid`, `ListBox`/`ListView`, and `TreeView` surfaces use UI virtualization with recycling and logical scrolling unless a specific control intentionally opts out.
- Add a deterministic source guard covering the shared contract and rejecting obvious regressions such as disabling virtualization on production list surfaces.
- Inspect branch diff against refreshed `main`; use repository CI/manual workflow evidence when available, and record `LOCAL_ONLY` for host-only DPI/performance acceptance.

## Coordination

Fresh main/recent-commit/branch checks found prior responsive polish already integrated via #1008 and a current Grid V25 UI planner claim. This lane therefore reserves only the shared production-polish contract and avoids the active Grid feature surface. Current commercial signing and FieldMerge claims are non-overlapping. If a concurrent claim begins writing `UI/Theme.xaml` or the same preflight path, this lane stops and reconciles before further source writes.

## Completion condition

Remote-safe WPF production defaults are centralized, representative high-volume item controls inherit or explicitly preserve virtualization/recycling/logical scrolling, a regression guard protects the contract, implementation is committed/pushed on the declared agent branch and integrated according to repository policy. Native BricsCAD/Windows DPI transitions and real host performance remain separate `LOCAL_ONLY` acceptance evidence.