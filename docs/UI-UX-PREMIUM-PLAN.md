# QS3D UI/UX Premium / Professional / Luxury Plan

**Updated:** 2026-08-11 (UTC+7)  
**Product:** `trinhtanphat/QS3D-BricsCAD`  
**Scope:** BricsCAD V25 x64 plugin UI — Ribbon, palettes and modeless WPF windows hosted inside BricsCAD.  
**Runtime reference:** owner-supplied BricsCAD screenshot from `MB MONG.dwg` on 2026-08-11.

## 1. Executive direction

QS3D must feel like a mature, high-end BIM/CAD extension rather than a collection of utility panels.

The visual target is deliberately restrained:

- **Premium** — crisp typography, high contrast, deliberate hierarchy, polished hover/focus/selected/disabled states.
- **Professional** — dense enough for engineering work, minimal wasted space, stable action placement and predictable semantics.
- **Luxury** — deep graphite/navy surfaces plus a restrained champagne accent used only for hierarchy/branding; never a gold-heavy decorative skin.
- **CAD-first** — BricsCAD's native drawing/model viewport remains visually dominant.
- **Vietnamese-first** — Vietnamese labels must stay legible at narrow palette widths and 100–200% DPI.
- **Host-safe** — critical control chrome must not depend on Windows/BricsCAD system themes that can leak light backgrounds or black text into dark QS3D surfaces.
- **Fast** — no blur, acrylic, glow, heavy shadows, animated gradients or per-row effects that make modeless palettes feel sluggish.

QS3D remains a BricsCAD-hosted plugin. This plan does not create a standalone shell or a replacement CAD viewport.

## 2. Runtime screenshot review

The supplied screenshot shows that the information architecture is already useful, but the presentation still reads as an early engineering UI.

### 2.1 Visible weaknesses

1. **Light/native control chrome inside a dark palette.** Zone/Floor ComboBoxes and some native WPF controls still appear gray/light because setters alone do not fully replace host/system templates.
2. **Weak surface separation.** Adjacent panels are close in tone, so Family/Type, selected objects, properties, Xref and layer sections blend together.
3. **Inconsistent command hierarchy.** Primary, secondary and destructive buttons are often similar in shape/weight.
4. **Scrollbars look system-default.** The bright native scrollbar is visually disconnected from the rest of the dark UI.
5. **Section hierarchy is too utilitarian.** Headings, subheadings and metadata lack a premium rhythm.
6. **Lists/tables are dense but not refined.** Header chrome and selection states need stronger consistency.
7. **The warm premium accent is underused as hierarchy.** Luxury should come from controlled typography/dividers/badges, not decorative gradients.
8. **Host-dependent foreground is dangerous.** The earlier black-heading issue proves keyed styles must explicitly own their foreground.

### 2.2 What should *not* change

- Keep the current BricsCAD viewport.
- Keep current command handlers, bindings and semantic behavior.
- Keep dense palette workflows.
- Do not add decorative buttons or fake controls.
- Do not turn all controls blue or gold.
- Do not make the palette look like a mobile/web dashboard.

## 3. Design system v2

### 3.1 Surface hierarchy

| Token | Role |
| --- | --- |
| `BgCanvas` | deepest workspace/palette background |
| `BgPanel` | normal section surface |
| `BgElevated` | cards, table regions, popup surfaces |
| `BgRaised` | headers and elevated command chrome |
| `BgInput` | TextBox/ComboBox input surface |
| `BgHover` | hover state |
| `BgSelected` | selected semantic/list row |
| `BgPressed` | active/pressed secondary action |

The palette uses graphite with a subtle navy bias. Pure black is avoided to reduce glare.

### 3.2 Border hierarchy

| Token | Role |
| --- | --- |
| `BorderWeak` | structural separators |
| `BorderStrong` | inputs/cards/header edges |
| `BorderFocus` | keyboard/mouse focus |
| `BorderLuxury` | rare champagne-accented premium surfaces |

Focus must be immediately visible without glow.

### 3.3 Text hierarchy

| Token | Role |
| --- | --- |
| `TextPrimary` | section titles, values, normal command text |
| `TextSecondary` | captions and metadata |
| `TextMuted` | tertiary context |
| `TextDisabled` | unavailable controls only |

No keyed title style may rely on host foreground inheritance.

### 3.4 Accent hierarchy

- **Blue** — primary action, focus, selection.
- **Champagne** — section/group hierarchy and rare premium badges only.
- **Green** — success/healthy.
- **Amber** — review/warning.
- **Red** — destructive/error.

The champagne accent is intentionally muted. It should make QS3D feel expensive, not ornamental.

### 3.5 Typography

- Font family: `Segoe UI` for Windows/BricsCAD compatibility.
- Body/control: 11 px.
- Caption/group label: 9.5 px.
- Table header: 10 px.
- Palette title: 12 px, semibold.
- Dense engineering labels should use weight and contrast rather than oversized text.

### 3.6 Geometry and spacing

- Base rhythm: 4 / 6 / 8 / 12 px.
- Dense buttons: 23 px minimum height.
- Normal buttons/inputs: ~25 px.
- Control/card radius: 4–5 px.
- Status pills: compact 9 px radius.
- Avoid oversized rounded corners.

## 4. P0 — shared theme foundation

**Status:** implemented in the shared `Theme.xaml` lane.

Goals:

1. Keep the explicit `PanelTitle` high-contrast foreground guard.
2. Upgrade graphite/navy palette and border hierarchy.
3. Add richer primary/secondary/muted typography tokens.
4. Add restrained champagne hierarchy resources.
5. Preserve every existing public theme resource key.
6. Add new reusable `PremiumCard`, `LuxuryCard`, `StatusPill`, `LuxuryButton` resources without forcing feature owners to use them.

Acceptance:

- Existing XAML keeps resolving all current keys.
- No black heading can be introduced through `PanelTitle`.
- The theme remains a pure presentation resource with no command/business logic.

## 5. P1 — host-independent core control chrome

**Status:** implemented in the shared theme lane; runtime visual proof remains local-only.

### ComboBox

The screenshot's light Zone/Floor controls are the clearest visual regression. The v2 theme therefore owns the full ComboBox template instead of trusting host/system chrome.

Requirements:

- dark input surface;
- dark arrow button;
- dark popup;
- blue focus/open border;
- selected/hover rows stay high contrast;
- editable ComboBoxes keep `PART_EditableTextBox` and two-way `Text` behavior;
- disabled state stays legible without looking active.

### TextBox

Requirements:

- dark custom border/template;
- visible focus border;
- explicit read-only treatment;
- disabled treatment distinct from read-only;
- no light system border leaking into a BricsCAD palette.

### CheckBox / RadioButton

Requirements:

- custom dark glyph chrome;
- blue checked/selected state;
- high-contrast glyph;
- keyboard focus;
- disabled state.

### ScrollBar

Requirements:

- narrow dark track;
- subtle graphite thumb;
- blue drag state;
- both horizontal and vertical templates;
- no bright Windows scrollbar inside QS3D lists/trees/tables.

### ToolTip

Requirements:

- dark elevated tooltip surface;
- high-contrast text;
- no system drop shadow dependency.

## 6. P2 — shared data/list chrome

**Status:** implemented in the shared theme lane; feature-specific layouts remain owned by their active claims.

- Consistent TreeView/ListView/ListBox selection and hover tones.
- Dark `GridViewColumnHeader`.
- Dark `DataGridColumnHeader`.
- Consistent DataGrid row/header density.
- Virtualization remains enabled.
- Selected rows retain primary text contrast.
- No expensive effects are introduced.

## 7. P3 — Workspace palette refinement

**Owner lane:** existing active Workspace compact-shell claim; not owned by the shared theme lane.

Target:

- clearer Zone/Floor scope card;
- cleaner model/category hierarchy;
- compact Family/Type toolbar;
- unmistakable active selection;
- stronger property scope;
- useful empty states;
- 1366×768-friendly density;
- preserve all existing real handlers and semantic boundaries.

The shared v2 theme is designed to make that lane look substantially better without requiring duplicated layout edits.

## 8. P4 — Right Panel: Drawing / Xref / Layer

**Owner lane:** existing Right Panel feature work; not owned by the shared theme lane.

Target:

- stable top action bar;
- destructive Xref removal visually separated;
- layer search/input follows the same dark control chrome;
- dark list/table header treatment;
- selected layer remains obvious;
- lock/show/hide states remain readable;
- long names truncate with tooltip rather than widening the palette.

## 9. P5 — modeless window consistency

All major modeless windows should eventually consume the same shared design language:

- Domain Hub;
- Project Tools;
- Schedule Hub;
- BQ / Quantity Summary;
- Family / Floor / Zone / Material editors;
- Room/Door/Opening schedules;
- Curtain Wall;
- Rebar hubs/setup;
- Model Health;
- Recognition / Revision / Release / Update Center.

Standardize:

- title/header hierarchy;
- primary/secondary/destructive action placement;
- filter/input sizing;
- DataGrid density;
- empty/loading/error states;
- footer/status treatment;
- close/cancel/save semantics.

Feature owners keep behavior ownership; the theme owns only shared presentation primitives.

## 10. P6 — interaction and safety semantics

Every important control state must be visually distinct:

- idle;
- hover;
- pressed;
- keyboard focus;
- selected;
- disabled;
- read-only;
- warning;
- destructive;
- successful/healthy.

Rules:

- blue is not used for every action;
- red is not used for ordinary secondary actions;
- champagne is not used for semantic warning/success;
- disabled controls must not look selectable;
- destructive actions keep explicit labels and existing confirmation/business rules;
- visual refresh failures must never retroactively corrupt a valid semantic/CAD commit.

## 11. P7 — accessibility, DPI and Vietnamese QA

Real BricsCAD qualification matrix:

- DPI: 100%, 125%, 150%, 200%;
- palette width: narrow, normal, wide;
- dark BricsCAD host;
- long Vietnamese labels and diacritics;
- keyboard navigation;
- editable/read-only/disabled ComboBox/TextBox;
- checked/unchecked/disabled CheckBox and RadioButton;
- horizontal/vertical scrolling;
- selected/hover/focus rows;
- dark ComboBox popup and ToolTip;
- modeless document switching.

Acceptance:

- no clipped Vietnamese diacritics;
- no black text on dark surfaces;
- no light native ComboBox/TextBox/ScrollBar chrome;
- no unreadable popup text;
- no focus state lost against dark backgrounds;
- no new horizontal overflow from theme chrome;
- no meaningful palette latency regression.

This is covered by the existing LOCAL_ONLY Workspace/HiDPI qualification boundary (`LOCAL-012`) and must not be promoted to runtime PASS from remote/static review.

## 12. Performance guardrails

Premium must stay fast.

- Keep DataGrid/List/Tree virtualization.
- No `DropShadowEffect`.
- No blur/acrylic.
- No animated gradients.
- No continuous animations.
- No per-row `Effect`.
- No geometry regeneration from theme state.
- No CAD command dispatch from presentation helpers.
- No semantic/project mutation from theme resources.

## 13. Static contract

`scripts/preflight-wpf-theme.py` must fail if:

- required core brushes disappear;
- premium v2 brushes disappear;
- `PanelTitle` loses explicit `TextBrush`;
- ComboBox loses its host-independent `ControlTemplate`;
- ComboBox loses `PART_Popup` or editable `PART_EditableTextBox`;
- TextBox loses `PART_ContentHost`;
- CheckBox loses custom template/glyph contract;
- ScrollBar loses vertical/horizontal templates;
- ToolTip loses dark custom template;
- heavy WPF effects (`DropShadowEffect`, `BlurEffect`) are introduced.

The preflight is source/static evidence only.

## 14. Implementation order

1. **Shared theme v2** — palette, host-independent control chrome, list/table polish.
2. **Workspace compact shell** — active parallel claim.
3. **Right Panel density/hierarchy** — active neighboring claim.
4. **Shared modeless-window pattern adoption**.
5. **Interaction/status semantics**.
6. **DPI/Vietnamese/local visual matrix**.
7. **Screenshot-driven final tuning on exact release SHA**.

## 15. Definition of done

The premium/luxury UI program is complete only when:

- core controls never fall back to bright host-system chrome;
- headings and values have consistent contrast/hierarchy;
- primary/destructive/secondary actions are visually distinct;
- selected/hover/focus/disabled/read-only states are obvious;
- Workspace and Right Panel remain usable at narrow widths;
- major modeless windows share the same design system;
- Vietnamese Unicode and 100–200% DPI are qualified;
- no meaningful rendering/performance regression is introduced;
- exact release SHA has real BricsCAD V25 visual evidence.

## 16. Current evidence boundary

This repository batch implements the **shared source-side premium theme v2** and the corresponding static guard. It does not claim a licensed BricsCAD V25 runtime visual PASS.

GitHub Actions remain manual-only under `CI_POLICY.md`; no Action is authorized merely by implementing or pushing this UI work.
