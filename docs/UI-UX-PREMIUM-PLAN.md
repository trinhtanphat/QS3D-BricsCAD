# QS3D UI/UX Premium / Professional / Luxury Plan

**Updated:** 2026-08-12 (UTC+7)  
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

The supplied screenshot showed that the information architecture was useful, but presentation still read as an early engineering UI.

### 2.1 Original visible weaknesses

1. **Light/native control chrome inside a dark palette.** Zone/Floor ComboBoxes and some native WPF controls could appear gray/light because setters alone did not fully replace host/system templates.
2. **Weak surface separation.** Adjacent panels were close in tone, so Family/Type, selected objects, properties, Xref and layer sections blended together.
3. **Inconsistent command hierarchy.** Primary, secondary and destructive buttons were often similar in shape/weight.
4. **Scrollbars looked system-default.** Bright native scrollbars were visually disconnected from the rest of the dark UI.
5. **Section hierarchy was too utilitarian.** Headings, subheadings and metadata lacked a premium rhythm.
6. **Lists/tables were dense but not refined.** Header chrome and selection states needed stronger consistency.
7. **The warm premium accent was underused as hierarchy.** Luxury should come from controlled typography/dividers/badges, not decorative gradients.
8. **Host-dependent foreground was dangerous.** The earlier black-heading issue proved keyed styles must explicitly own their foreground.

### 2.2 What must not change

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

**Status:** `REMOTE_DONE` source-side.

Implemented outcomes:

1. Explicit `PanelTitle` high-contrast foreground guard.
2. Graphite/navy palette and border hierarchy.
3. Primary/secondary/muted typography tokens.
4. Restrained champagne hierarchy resources.
5. Existing public theme resource keys preserved.
6. Reusable `PremiumCard`, `LuxuryCard`, `StatusPill`, `LuxuryButton` resources.

Acceptance remains:

- Existing XAML resolves all current keys.
- No black heading can be introduced through `PanelTitle`.
- The theme remains a pure presentation resource with no command/business logic.

## 5. P1 — host-independent core control chrome

**Status:** `REMOTE_DONE` source-side; runtime visual proof remains `LOCAL_ONLY`.

### ComboBox

The screenshot's light Zone/Floor controls were the clearest visual regression. Theme v2 owns the full ComboBox template instead of trusting host/system chrome.

Requirements implemented at source level:

- dark input surface and arrow button;
- dark popup;
- blue focus/open border;
- high-contrast selected/hover rows;
- editable ComboBoxes retain `PART_EditableTextBox` and two-way `Text` behavior;
- disabled state remains legible without looking active.

### TextBox

- dark custom border/template;
- visible focus border;
- explicit read-only treatment;
- disabled treatment distinct from read-only;
- no light system border dependency.

### CheckBox / RadioButton

- custom dark glyph chrome;
- blue checked/selected state;
- high-contrast glyph;
- keyboard focus;
- disabled state.

### ScrollBar

- narrow dark track;
- subtle graphite thumb;
- blue drag state;
- horizontal and vertical templates;
- no bright Windows scrollbar dependency inside QS3D lists/trees/tables.

### ToolTip

- dark elevated tooltip surface;
- high-contrast text;
- no system drop-shadow dependency.

## 6. P2 — shared data/list chrome

**Status:** `REMOTE_DONE` source-side and adopted across the premium UI pass.

- Consistent TreeView/ListView/ListBox selection and hover tones.
- Dark `GridViewColumnHeader` and `DataGridColumnHeader`.
- Consistent DataGrid row/header density.
- Virtualization remains enabled.
- Selected rows retain primary text contrast.
- No expensive effects are introduced.

## 7. P3 — Workspace palette refinement

**Status:** `REMOTE_DONE` source-side. The earlier compact-shell reservation is completed; this is no longer an active neighboring UI lane.

Implemented outcomes include:

- clearer Zone/Floor scope card and model/category hierarchy;
- compact Family/Type toolbar and stronger property/selection hierarchy;
- dense three-pane engineering layout without changing command handlers or semantic boundaries;
- responsive compact header behavior that prevents badge/action collisions and preserves full command handlers/tooltips;
- narrow-width `MÔ HÌNH`/refresh space reservation and ellipsis-safe title behavior;
- explicit dark context-menu treatment and shared contrast/focus contracts;
- centralized palette minimums and compact host sizing while preserving the native BricsCAD viewport boundary.

The source-side premium Workspace pass is complete. Remaining visual proof is the real-host DPI/palette-width matrix in the canonical LOCAL_ONLY queue.

## 8. P4 — Right Panel: Drawing / Xref / Layer

**Status:** `REMOTE_DONE` source-side. The earlier Right Panel feature reservation is completed; this is no longer an active neighboring UI lane.

Implemented outcomes include:

- stable premium drawing/Xref and layer hierarchy using shared cards/status primitives;
- destructive Xref detach visually separated from normal actions;
- dark layer search/input and list/table chrome;
- obvious selected-layer state;
- readable lock/show/hide status and live native layer/Xref state;
- long-name truncation with tooltip;
- cached layer filtering, filtered/total result feedback and stale-selection cleanup;
- preserved current Xref `Tỉ lệ` / `ScaleText` surface and existing handlers/context menus/keyboard routing.

Focused static regression guards preserve the presentation-only boundary and critical Xref/layer bindings/handlers.

## 9. P5 — modeless window consistency

**Status:** `REMOTE_DONE` broad source pass.

Major modeless windows currently present under `src/QS3D.BricsCAD.V25/UI` consume the shared premium design language, including:

- Domain Hub;
- Project Tools;
- Schedule Hub;
- BQ / Quantity Summary;
- Family / Floor / Zone / Material editors;
- Room/Door/Opening schedules;
- Curtain Wall;
- Rebar hubs/setup;
- Model Health and Audit Log;
- Recognition and Revision;
- other current modeless engineering/review windows guarded by `preflight-ui-premium-layout.py`.

Standardized source-side patterns include:

- title/header hierarchy;
- primary/secondary/destructive action placement;
- filter/input sizing;
- DataGrid density;
- empty/loading/error/status treatment where applicable;
- footer/status treatment;
- close/cancel/save semantics without changing feature behavior.

Feature behavior ownership remains outside the presentation system. Real BricsCAD rendering/HiDPI qualification remains local-only.

## 10. P6 — interaction and safety semantics

**Status:** `REMOTE_DONE` shared source foundation; real-host interaction proof remains local-only.

Every important control state must remain visually distinct:

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

**Status:** `LOCAL_ONLY / PENDING_LOCAL`; remote agents must not promote this to PASS from source/static evidence.

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

This work is already represented in the canonical local queue and must not be rediscovered as remote backlog.

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

`scripts/preflight-wpf-theme.py` and the premium layout/feature-specific preflights must keep failing closed if the source loses core presentation contracts, including:

- required core/premium brushes;
- explicit `PanelTitle` / `TextBrush` contrast;
- host-independent ComboBox/TextBox/CheckBox/ScrollBar/ToolTip templates and named parts;
- dark data/list headers and premium primitives;
- critical Workspace/RightPanel handlers/bindings owned by their focused guards;
- modeless-window Theme adoption;
- no heavy WPF effects (`DropShadowEffect`, `BlurEffect`) or presentation-to-business-logic leakage.

These preflights are source/static evidence only.

## 14. Implementation status/order

1. **Shared theme v2** — `REMOTE_DONE`.
2. **Workspace compact shell / responsive refinement** — `REMOTE_DONE`.
3. **Right Panel density/hierarchy** — `REMOTE_DONE`.
4. **Shared modeless-window pattern adoption** — `REMOTE_DONE` broad source pass.
5. **Interaction/status semantics** — `REMOTE_DONE` shared source foundation.
6. **DPI/Vietnamese/local visual matrix** — `LOCAL_ONLY / PENDING_LOCAL`.
7. **Screenshot-driven final tuning on an exact release SHA** — `LOCAL_ONLY`, after real-host evidence.

Do not create another broad remote cosmetic rewrite merely because steps 6–7 are pending; those steps require the real BricsCAD/Windows environment.

## 15. Definition of done

The **remote source-side premium program** is complete when the shared theme, Workspace, Right Panel, major modeless windows and static contracts remain present on current `main`. That source-side condition is currently met.

The **production visual qualification** is complete only when:

- core controls do not fall back to bright host-system chrome in real BricsCAD;
- headings and values retain contrast/hierarchy;
- primary/destructive/secondary actions are visually distinct;
- selected/hover/focus/disabled/read-only states are obvious;
- Workspace and Right Panel remain usable at narrow widths;
- Vietnamese Unicode and 100–200% DPI are qualified;
- no meaningful rendering/performance regression is observed;
- the exact release SHA has real BricsCAD V25 visual evidence.

Remote/static review cannot satisfy those real-host conditions.

## 16. Current evidence boundary

Current `main` contains the broad source-side premium UI program: shared theme v2, responsive Workspace refinement, luxury Right Panel hierarchy, modeless-window consistency, diagnostics/revision polish and focused static guards. The earlier plan text that described Workspace and Right Panel as still-active neighboring UI lanes is obsolete and has been reconciled here.

This document does **not** claim licensed BricsCAD V25 runtime/HiDPI visual PASS. That evidence remains `LOCAL_ONLY` under the canonical local queue.

GitHub Actions remain manual-only under `CI_POLICY.md`; no Action is authorized merely by implementing, documenting or pushing UI work.
