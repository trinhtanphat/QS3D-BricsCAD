# Work claim — Quantity Settings runtime rule resolution

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-runtime-rules`
- Registered: `2026-08-11T20:58:00+07:00`
- Completed: `2026-08-11T21:34:00+07:00`
- Baseline main SHA: `21515fcb529fbce6712e3ce4968a27d8f65430f6`
- Priority: P1 — continue the owner-requested Setup & Rules feature beyond UI/persistence with a safe Core-side effective-rule lookup contract.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityCalculationRuleSet.cs` (new)
- `scripts/preflight-quantity-calculation-rule-set.py` (new)
- this claim file for close-out

## Contract

- Build an immutable/defensive runtime snapshot over `QuantityCalculationSettings` with deterministic category and directed intersection lookup.
- Native QS3D category codes resolve exactly first.
- Legacy BLT compatibility fallback is limited to exact category-label equivalences already established in the existing Quantity Settings UI; ambiguous BLT categories remain integer-code-only until an explicit mapping contract exists.
- Directed intersection lookup preserves source -> target direction and never mirrors, synthesizes or mutates a missing pair.
- Unknown imported category codes remain valid for exact integer-code lookup.
- Invalid settings fail closed through the existing `NormalizeAndValidate()` contract.

## Excluded scope

- No edits to `QuantitySettingsWindow.xaml` / `.xaml.cs`; the concurrent intersection-browser lane owns those files.
- No edits to `QuantitySettingsStore.cs`; the local V25 build-fix lane owns it.
- No changes to formwork/intersection geometry arithmetic, `StructuralRegenerator`, `ProjectQuantityReportBuilder`, Ribbon, shared theme, Workspace/RightPanel, updater/release or GitHub Actions.
- No claim that BLT intersection subtraction semantics can be executed without the required CAD contact/intersection geometry pipeline.

## Implementation evidence

- `e6ad80883b509c356ca4697f24a41c14e8be1f2e` — initial Core effective-rule resolver.
- `47aed6f3337698efd226385a8f06d0a0c65ffbd7` — tightened BLT fallback to exact native/compatibility label matches only; removed inferred Beam/Slab/ArchitecturalWall aliases.
- `8a0e87504d523451cec130b5e007586aee2b4517` — focused static rule-resolution preflight.
- `e2b651f3214c2c7533e48b8ac72e4c2f8773acd9` — preflight hardened to reject inferred BLT aliases.
- Final source was re-fetched from `main`; writes remained confined to the registered files and no GitHub Actions were dispatched.

## Remaining boundary

- The supplied BLT JSON itself contains numeric category codes only. The existing QS3D UI establishes exact same-label native compatibility only for Phòng, Sàn hoàn thiện, Chân tường, Hoàn thiện tường, Lan can, Cột and Vách BTCT. Codes such as Dầm HCN, Giằng tường, Lanh tô, Sàn đặc, Đường dốc and Tường gạch are intentionally not cast onto broader QS3D enum categories without an explicit owner/reference mapping.
- Applying the five directed subtraction flags to real CAD contact/intersection geometry remains a separate engine lane; this claim provides the deterministic runtime lookup boundary but does not invent unavailable geometry semantics.

## Completion condition

- COMPLETE: Core consumers now have a defensive deterministic effective-rule resolver for native and explicitly compatible imported rules, unknown integer codes remain exact-addressable, directed pairs stay directional, missing rules are not synthesized, and the focused regression preflight is present.
