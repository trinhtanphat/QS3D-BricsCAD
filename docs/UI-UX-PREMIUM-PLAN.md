# QS3D UI/UX Premium / Professional / Luxury Plan

**Updated:** 2026-08-10 (UTC+7)  
**Product:** `trinhtanphat/QS3D-BricsCAD`  
**Scope:** BricsCAD V25 x64 plugin UI — Ribbon, palettes and modeless WPF windows hosted inside BricsCAD.

## 1. Product direction

QS3D should look like a high-end professional CAD/BIM tool, not a decorative desktop app.

The visual target is:

- **Premium:** crisp typography, strong contrast, consistent spacing, deliberate active/hover/focus states.
- **Professional:** dense enough for quantity/BIM work, predictable command hierarchy, no wasted chrome, no ambiguity between destructive and primary actions.
- **Luxury:** restrained dark neutral surfaces with one cool blue action accent and a very limited warm premium accent for branding/status emphasis only.
- **CAD-first:** the BricsCAD viewport remains visually dominant. Palettes must not compete with the drawing through gradients, glow, blur, large shadows or animation-heavy effects.
- **Vietnamese-first:** Vietnamese labels remain primary and readable at narrow palette widths; technical English is used only where it is genuinely part of a BIM/CAD term or identifier.

QS3D remains a BricsCAD-hosted plugin. This plan does not introduce a standalone shell or replace BricsCAD's native viewport.

## 2. Immediate screenshot issue — heading becomes black

The supplied runtime screenshot shows headings such as **“ĐỐI TƯỢNG ĐANG CHỌN”** rendering black/dark on a dark palette.

Root source risk:

- Workspace headings use the keyed `PanelTitle` WPF style.
- A keyed `TextBlock` style does not automatically inherit the implicit `TextBlock` style.
- If `PanelTitle` does not explicitly set `Foreground`, a host/system foreground can leak through while QS3D is embedded in a BricsCAD palette.

### P0 fix

`PanelTitle` must explicitly use `TextBrush` (`TextPrimary`) so headings stay high-contrast regardless of host defaults.

The theme preflight must guard this contract so future refactors cannot accidentally remove the explicit foreground.

## 3. Design system

### 3.1 Surface hierarchy

Use three main dark surfaces:

| Token | Role |
| --- | --- |
| `BgCanvas` | deepest workspace/background surface |
| `BgPanel` | normal palette/card surface |
| `BgElevated` | inputs, table headers and elevated controls |
| `BgHover` | hover state |
| `BgSelected` | selected row/object state |
| `BgPressed` | pressed/active command state |

Avoid pure black. Near-black neutral surfaces reduce glare while keeping CAD geometry dominant.

### 3.2 Text hierarchy

| Token | Use |
| --- | --- |
| `TextPrimary` | headings, values, normal controls |
| `TextSecondary` | captions, metadata, section labels |
| `TextDisabled` | disabled controls only |

Rules:

- Primary text must remain near-white on all dark palette surfaces.
- Section labels may be muted but cannot become low-contrast gray-on-gray.
- Disabled text should be visually distinct from read-only values.
- Read-only CAD provenance should be muted, not hidden.

### 3.3 Accent hierarchy

- **Blue (`Accent`)** — primary action, selected state, focus border.
- **Green (`Success`)** — success/healthy status only.
- **Amber (`Warning`)** — warning/review state only.
- **Red (`Danger`)** — destructive/error state only.
- **Warm premium (`Luxury`)** — rare branding/badge/divider emphasis. Do not use it for normal commands or CAD semantics.

This prevents a “luxury” look from turning into a gold-heavy theme that reduces engineering clarity.

### 3.4 Typography

Recommended compact scale:

- 9.5 px — captions/group labels.
- 10 px — table headers.
- 11 px — normal controls/body.
- 11.5–12 px — palette section titles.
- 13–14 px — modeless-window titles where needed.

Use `Segoe UI` for BricsCAD/Windows consistency. Prefer weight/contrast over oversized text.

### 3.5 Spacing and geometry

- Base spacing rhythm: **4 / 6 / 8 / 12 px**.
- Normal buttons: 24–28 px minimum height.
- Dense toolbar buttons: 22–24 px.
- Inputs: 24–28 px.
- Card radius: 3–4 px.
- Avoid excessive rounded “mobile app” styling.
- Keep separators subtle; use strong borders only for focus, selected state or structural boundaries.

## 4. Phase P0 — theme foundation and contrast safety

**Goal:** fix the screenshot bug and establish a premium baseline without changing workflows.

Deliverables:

1. Explicit white/high-contrast `PanelTitle` foreground.
2. Refined neutral dark palette and clearer border hierarchy.
3. Stronger primary/secondary/disabled text distinction.
4. Standard focus border for keyboard/text input interaction.
5. Clear hover/pressed states for buttons and selectable rows.
6. Primary and destructive buttons retain distinct semantics.
7. DataGrid/ListView headers and row spacing become more consistent.
8. Tooltips/cards receive the same visual language.
9. Static preflight prevents regression of the `PanelTitle` contrast fix.

Acceptance:

- “ĐỐI TƯỢNG ĐANG CHỌN”, “FAMILY / TYPE”, “THUỘC TÍNH”, “QUẢN LÝ BẢN VẼ”, “QUẢN LÝ LỚP” and similar headings remain legible on dark palettes.
- No keyed title style is allowed to silently inherit black host text.
- Existing command handlers, bindings and semantic workflows are untouched.

## 5. Phase P1 — Workspace palette refinement

**Goal:** make the three-pane QS3D Workspace feel like a mature BIM authoring palette.

### Left pane — model/navigation

- Stronger selected row and hover state.
- Visually separate project hierarchy from category hierarchy.
- Compact counts/badges for relevant groups without adding noise.
- Search/filter affordance for large semantic projects.
- Clear empty state when no project/semantic objects exist.
- Preserve keyboard tree navigation.

### Middle pane — Family / Type and room finish

- Make active Family visually unmistakable.
- Keep `+ Thêm`, delete and bulk actions at stable positions.
- Separate primary create action from destructive delete.
- Use compact section headers rather than large boxes.
- Make type/property selection states consistent with the left tree.
- When no compatible Family exists, show a short actionable empty state rather than a blank panel.

### Selected object area

- Title and object count should be high contrast.
- `Focus`, isolate, build, host and review actions should use consistent action hierarchy.
- Show the selected semantic category/type near the title when space permits.
- Multiple-selection state should clearly differ from single-instance editing.

### Property inspector

- Keep **Family / Type** vs **Đối tượng / Instance** scope visually explicit.
- Read-only CAD provenance gets a distinct muted surface/label.
- Editable values get visible focus state.
- Validation errors should be inline and close to the field.
- Reset/inheritance (`↺`) should have clear hover/tooltip semantics.
- Keep dense row height for engineering workflows.

## 6. Phase P2 — Right Panel: Drawing / Xref / Layer

**Goal:** turn the right palette into a production-grade control center without overloading it.

Improvements:

- Stable top action bar for attach/reload/move/detach.
- Destructive `Gỡ Xref` remains visually separated.
- Search field gets clear focus state and optional clear affordance.
- Layer visibility/lock/color indicators use consistent alignment.
- Selected-layer state must remain legible in dense lists.
- Multi-select action feedback should state how many layers are affected.
- Status footer should distinguish information, warning and failure.
- Very long layer/drawing names should truncate with tooltip, never distort the palette.

## 7. Phase P3 — modeless window consistency

Apply one visual contract to:

- Domain Hub
- Project Tools
- Schedule Hub
- BQ / Quantity Summary
- Family / Floor / Zone / Material editors
- Door/Opening schedule
- Curtain Wall
- Rebar hubs and setup
- Health / Release / Recognition / Revision windows

Standardize:

- header/title treatment;
- toolbar/action placement;
- input and filter sizing;
- DataGrid headers/row density;
- empty/loading/error states;
- footer/status area;
- primary vs secondary vs destructive actions;
- close/cancel/save semantics.

Avoid a situation where each feature looks like a separate utility.

## 8. Phase P4 — interaction and feedback polish

### State feedback

Every long or consequential operation should have an appropriate visible state:

- ready;
- dirty/unsaved semantic data;
- processing;
- success;
- warning/review required;
- failed;
- disabled/not applicable.

### Command safety

- Destructive actions require strong visual semantics, not just text.
- Do not use red for ordinary secondary buttons.
- Disabled commands should explain why through tooltip/status where practical.
- Preserve ESC/cancel and BricsCAD-native editor behavior.

### Micro-interactions

Use only lightweight state changes:

- hover;
- pressed;
- selected;
- focus;
- short status transitions.

Do **not** add blur, acrylic, animated gradients, large drop shadows or continuous motion inside BricsCAD palettes.

## 9. Phase P5 — accessibility, DPI and Vietnamese QA

Required runtime visual matrix:

- DPI: **100%, 125%, 150%, 200%**.
- Palette widths: narrow, normal, wide.
- BricsCAD dark host appearance.
- Vietnamese Unicode with long labels.
- Keyboard focus through buttons, inputs, tree/list/table controls.
- Disabled, read-only, selected, hover, warning and error states.

Checks:

- no clipped diacritics;
- no black text on dark surfaces;
- no white text on light native popup surface;
- no horizontal overflow caused by fixed labels;
- touch-sized controls are not required, but keyboard/mouse hit targets must remain reliable;
- selected rows retain text contrast.

## 10. Phase P6 — performance guardrails

Premium UI must remain fast.

Rules:

- keep ListView/DataGrid virtualization enabled;
- avoid per-row heavy visual effects;
- avoid dynamic shadows/blur;
- avoid rebuilding large item sources for simple selection changes;
- debounce only searches that genuinely need it;
- keep palette refresh independent from expensive geometry regeneration;
- do not turn UI synchronization failures into destructive CAD rollback after a valid model commit.

## 11. Phase P7 — real BricsCAD V25 visual qualification

Static XAML review is not enough. Before calling the UI production-ready, validate the exact release SHA on licensed BricsCAD V25 x64:

1. compile adapter against the installed V25 managed assemblies;
2. NETLOAD/DemandLoad;
3. open Workspace and Right Panel in a real drawing;
4. verify title/label contrast;
5. test all DPI and palette-width cases above;
6. exercise Family/Instance/property editing;
7. exercise selection/focus/isolate/build/host flows;
8. open representative modeless hubs/windows;
9. inspect popup/ComboBox/TextBox disabled/read-only behavior;
10. capture before/after screenshots for visual review;
11. test with a representative private DWG without committing that drawing.

## 12. Implementation order

Recommended order:

1. **Theme foundation + title contrast** — low-risk, global consistency.
2. **Workspace density/hierarchy** — highest daily-use value.
3. **Right Panel** — Xref/layer productivity.
4. **Shared modeless-window patterns**.
5. **Interaction/feedback states**.
6. **DPI/accessibility pass**.
7. **Runtime screenshot review and small iterative adjustments**.

Each phase should remain a small, reviewable commit and should preserve BricsCAD plugin boundaries and current command behavior.

## 13. Definition of done

The UI/UX upgrade is done only when:

- core dark theme has no host-dependent text-color leaks;
- headings and values have consistent hierarchy;
- primary/destructive/secondary actions are visually distinct;
- selected/hover/focus/disabled/read-only states are obvious;
- Workspace and Right Panel remain usable at narrow widths;
- major modeless windows share one design system;
- Vietnamese Unicode and 100–200% DPI pass;
- no meaningful palette performance regression is introduced;
- exact release SHA has real BricsCAD V25 visual/runtime evidence.

## 14. Current validation boundary

The first implementation pass can be source/static validated and committed without starting GitHub Actions. Repository Actions remain owner-controlled/manual-only.

A source/static pass is **not** the same as licensed BricsCAD V25 runtime proof. Runtime visual qualification remains a separate explicit validation step.
