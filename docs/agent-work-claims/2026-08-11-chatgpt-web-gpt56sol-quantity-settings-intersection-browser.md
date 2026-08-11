# Work claim — Quantity Settings intersection rule browser

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-intersection-browser`
- Registered: `2026-08-11T21:25:00+07:00`
- Baseline main SHA: `19a40ff629122a0e2258c3a7a066a945e380a033`
- Priority: P1 — direct screenshot-parity continuation of the owner-requested Setup & Rules workflow.

## Reserved scope

- Replace the current 784-row-style flat Intersection Rules grid with a compact three-pane directed-rule browser matching the supplied workflow: primary component selector, reference component selector, and one editable selected directed rule.
- Keep the full imported/native intersection matrix in memory and persistence; the browser edits exactly the selected existing source->target row rather than dropping/filtering unselected rules.
- Expose the reverse target->source rule as a read-only summary plus an explicit "view reverse" navigation action, without inferring or changing engineering subtraction semantics.
- Preserve unknown compatibility category codes and templates whose rule category codes are not present in the native enum/category-rule list.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs`
- `scripts/preflight-quantity-settings-intersection-browser.py`
- this claim file for close-out

## Excluded scope

- No edits to `QuantitySettingsStore.cs` or its recovery preflight; the active local V25 build-compatibility claim owns those files.
- No changes to Core quantity arithmetic, intersection geometry, default rule values, schema fields, category-code mapping, Ribbon, shared `Theme.xaml`, Workspace/RightPanel, updater/release or Direct Draw.
- No claim that BLT-compatible numeric rules are production-engine semantics; this is UI/navigation over the existing persisted directed rule payload only.
- No GitHub Actions dispatch.

## Functional contract

- Selector choices are the union of category-rule codes and all intersection source/target codes, so unknown imported compatibility codes remain addressable.
- Source and target selection resolves at most one existing directed row; a missing pair is displayed as unavailable and is not silently created.
- Editing the detail checkboxes mutates only that selected row object; `BuildSettingsFromView()` continues serializing every `IntersectionRows` entry.
- Reverse-rule display uses the actual existing reverse row and navigation swaps selectors; it does not mirror/copy values between A->B and B->A.
- Template import/reset refreshes the browser choices and selects a deterministic first available pair.

## Validation plan

- Re-fetch current `main` and both UI files before implementation; preserve concurrent winners.
- Add a focused auto-discovered static preflight requiring union selector construction, exact directed-row lookup, no missing-pair creation, reverse navigation, and full `IntersectionRows` persistence.
- Re-fetch final source/current main and source-review for no arithmetic/engine changes; do not dispatch Actions.

## Coordination

- The active Quantity Settings V25 build-fix lane is limited to `QuantitySettingsStore.cs` plus its recovery gate and explicitly excludes UI behavior, so these UI files are non-overlapping.
- The active premium theme lane owns shared `Theme.xaml`; this window keeps its existing local styles and does not alter shared resources.

## Completion condition

- Setup & Rules has a compact primary/reference/directed-rule interaction matching the supplied screenshot at source level, preserves all rule payloads/unknown codes, is regression-guarded, and this claim is marked `COMPLETED` with exact implementation evidence.
