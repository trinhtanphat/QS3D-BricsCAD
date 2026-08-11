# Work claim — Quantity Settings intersection rule browser

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-intersection-browser`
- Registered: `2026-08-11T21:25:00+07:00`
- Completed: `2026-08-11T21:31:00+07:00`
- Baseline main SHA: `19a40ff629122a0e2258c3a7a066a945e380a033`
- Priority: P1 — direct screenshot-parity continuation of the owner-requested Setup & Rules workflow.

## Implemented

- `9c82be8ea5594475eed9609a3b6148de53096b20` — replaced the flat all-rules Intersection Rules grid with a compact three-pane browser: Cấu kiện chính, Cấu kiện tham chiếu, and one selected directed-rule editor. The right pane exposes the five existing persisted subtraction flags and a reverse-rule summary/navigation surface.
- `f84ea990b8ca4727c318a6b432c27368ba1b85be` — added selector/browser behavior. Selector choices are rebuilt from the union of category-rule codes plus every intersection source/target code, so imported unknown compatibility codes remain selectable. A selected pair resolves only an existing exact `SourceCode -> TargetCode` row; missing pairs are displayed unavailable and are never synthesized. Reverse navigation swaps selectors only when the real reverse row exists.
- `af2fb0874ad338f717451a4b21d9f4a40c49ef04` / `62c7276ff1e2c82ca9a761c2228b90caf2caf97c` — added and hardened `scripts/preflight-quantity-settings-intersection-browser.py`, including XAML well-formedness, three-pane wiring, directed lookup ordering, no silent rule creation, reverse navigation, and full-matrix persistence guards.

## Preserved contracts

- `BuildSettingsFromView()` still serializes every `IntersectionRows` entry, not only the selected row; import/export/reset therefore keep the complete matrix payload.
- The browser does not mirror or copy A -> B values into B -> A. Both directions continue to be independent persisted rules.
- No Core quantity arithmetic, intersection geometry, default values, schema fields, category-code mapping, `QuantitySettingsStore.cs`, Ribbon, shared Theme, Workspace/RightPanel, updater/release or Direct Draw source was changed.
- No engineering subtraction semantics were inferred from the screenshots.

## Validation

- Re-fetched current `QuantitySettingsWindow.xaml` after implementation and confirmed the three-pane source/reference/editor structure, selected-rule checkboxes, reverse summary/navigation, and removal of the flat `ItemsSource={Binding IntersectionRows}` grid from this tab.
- Re-fetched current `QuantitySettingsWindow.xaml.cs` and confirmed union selector construction, exact directed `SingleOrDefault` row resolution, explicit missing-pair refusal, reverse-row lookup/swap, and complete `IntersectionRows.Select(...).ToList()` persistence.
- Re-fetched the focused preflight after its final hardening; it is auto-discovered by `scripts/preflight-all.py` and also parses the XAML as XML before checking the source contract.
- GitHub exposes no combined status checks for the final preflight commit. No GitHub Actions workflow was dispatched.

## Coordination / LOCAL_ONLY

- The separate active local Quantity Settings V25 build-fix lane owns only `QuantitySettingsStore.cs` plus its recovery gate and explicitly excludes UI behavior; this lane did not touch those files.
- A later Core runtime-rule-resolution claim explicitly excludes this UI browser and is non-overlapping.
- Exact BricsCAD V25 WPF rendering, keyboard/mouse interaction and DPI qualification remain part of the existing local UI/runtime qualification queue; no duplicate LOCAL inbox item and no remote runtime PASS were created.

## Completion evidence

- Setup & Rules now presents the requested source/reference pair interaction without hiding or discarding the full directed rule matrix.
- Current source/test tip for this lane: `62c7276ff1e2c82ca9a761c2228b90caf2caf97c`; subsequent concurrent main commits were preserved and no force push was used.
