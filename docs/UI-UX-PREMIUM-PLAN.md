# QS3D UI/UX Premium / Professional / Luxury Plan v2

**Updated:** 2026-08-11 (UTC+7)  
**Product:** `trinhtanphat/QS3D-BricsCAD`  
**Scope:** BricsCAD V25 x64 plugin UI hosted inside BricsCAD — Ribbon, Workspace/Right Panel palettes and modeless WPF windows.  
**Primary evidence:** owner-supplied real BricsCAD screenshots on 2026-08-11.

## 1. Executive target

QS3D should read visually as a serious high-end BIM/QS production plugin: dense, calm, precise and expensive-looking without becoming decorative.

The visual language is:

- **Premium:** crisp typography, predictable hierarchy, polished control chrome and deliberate state feedback.
- **Professional:** engineering density first; no oversized cards, excessive radius, glow, blur or animation.
- **Luxury:** graphite/navy surfaces, restrained champagne highlights and clean spacing; luxury comes from restraint, not gold everywhere.
- **CAD-first:** BricsCAD's native viewport remains the visual center. QS3D palettes support the drawing instead of competing with it.
- **Vietnamese-first:** Vietnamese labels and diacritics must remain readable at narrow palette widths and 100–200% DPI.
- **Host-independent:** Windows/BricsCAD theme defaults must not leak white popup/hover/selected backgrounds or black text into dark QS3D controls.

QS3D remains a BricsCAD plugin. This plan does not introduce a standalone shell or a replacement CAD viewport.

## 2. Screenshot findings

### P0-A — host theme leakage

The supplied screenshots expose state-color defects around drop-downs and selectable controls:

- ComboBox popup/hover/selected surfaces can become white or light.
- Native system ComboBox arrow/button chrome can break the dark palette.
- Text inputs, check boxes and scroll bars can inherit host/system rendering that does not match QS3D.
- Keyed text styles can inherit an unsuitable foreground if they do not explicitly own `Foreground`.

**Required source contract:** controls whose default WPF chrome can inherit Windows/BricsCAD colors must use explicit QS3D templates for all important visual states.

### P0-B — Workspace element collision

The second screenshot shows an actual layout defect around compact Workspace header/action content: labels and actions can visually collide/overlap at the current palette width.

The original `Workspace compact BLT-style shell polish` lane is already completed on `main`. This Premium v2 batch owns the newly demonstrated narrow-header collision guard while preserving that completed compact-shell behavior. Its source acceptance contract is:

- header/action rows use layout containers (`Grid`/`DockPanel`) rather than visual overlay tricks;
- text and action controls have stable spacing and do not share the same visual cell unintentionally;
- fixed-width labels must not consume action space at narrow widths;
- no negative margins or absolute positioning to “make it fit”;
- long Vietnamese labels trim/wrap intentionally rather than paint over adjacent controls;
- the palette remains usable at 1366×768-class density;
- scroll fallback is allowed where horizontal compression would otherwise cause overlap.

The shared theme must not try to hide structural overlap with smaller fonts or opacity.

## 3. Design tokens

### 3.1 Surface hierarchy

| Token | Role |
| --- | --- |
| `BgCanvas` | deepest workspace/window surface |
| `BgPanel` | normal panel/card surface |
| `BgElevated` | table headers and elevated blocks |
| `BgControl` | normal input/drop-down surface |
| `BgHover` | hover state |
| `BgSelected` | selected semantic row/item |
| `BgPressed` | active/pressed state |

Rules:

- never use pure black;
- selected state must remain clearly different from hover;
- popup surfaces must use the same dark hierarchy as the parent palette;
- no white/light fallback background is acceptable in dark mode.

### 3.2 Text hierarchy

| Token | Role |
| --- | --- |
| `TextPrimary` | normal values, headings and active controls |
| `TextSecondary` | captions, metadata, helper labels |
| `TextDisabled` | disabled state only |

Rules:

- keyed heading styles explicitly set foreground;
- disabled is visually distinct from read-only;
- read-only values remain legible;
- selected rows never rely on system highlight text colors.

### 3.3 Accent hierarchy

- **Blue** — primary action, focus, semantic selection.
- **Champagne** — restrained premium detail for section hierarchy/focus nuance.
- **Green** — success/healthy only.
- **Amber** — review/warning only.
- **Red** — destructive/error only.

No status color may be reused as decoration.

## 4. Typography and density

Use `Segoe UI`.

Recommended scale:

- 9.5 px — captions / compact section labels;
- 10 px — table headers;
- 11 px — body and normal controls;
- 11.5–12 px — palette section titles;
- 13–14 px — selected modeless-window title surfaces where space permits.

Spacing rhythm:

- 4 / 6 / 8 / 12 px;
- normal controls 24–28 px high;
- dense actions 22–24 px high;
- radius 2–4 px;
- borders 1 px;
- avoid large shadows and visual effects.

## 5. P0 implementation — Premium Theme v2

### 5.1 Shared palette refresh

Move the shared dark system toward graphite/navy:

- deeper `BgCanvas`;
- slightly separated panel/control/elevated surfaces;
- stronger but restrained border hierarchy;
- brighter primary text;
- calmer secondary text;
- champagne only in limited hierarchy accents.

### 5.2 Host-independent ComboBox

Provide a full `ComboBox` template with:

- explicit dark control border/background;
- explicit dark arrow-button template;
- explicit `PART_Popup`;
- explicit dark popup border/background;
- retained `PART_EditableTextBox`;
- dark mouse-over state;
- dark selected state;
- explicit keyboard focus border;
- disabled state;
- no dependency on system highlight colors.

Provide a full `ComboBoxItem` template so highlighted/selected rows cannot become white.

### 5.3 Host-independent TextBox

Provide explicit `TextBox` chrome with `PART_ContentHost`:

- control background;
- hover surface;
- focus border;
- selection brush;
- read-only treatment;
- disabled treatment.

### 5.4 Host-independent CheckBox

Provide explicit checkbox box/check/indeterminate rendering:

- dark unchecked state;
- blue checked state;
- focused/hovered border;
- disabled state;
- no Windows theme white box.

### 5.5 Host-independent ScrollBar

Provide slim dark vertical/horizontal templates:

- transparent page tracks;
- dark thumb;
- brighter hover thumb;
- blue dragging thumb;
- no system arrow/page chrome.

### 5.6 Selectable collections

Keep selected/hover foreground/background explicit for:

- `ComboBoxItem`;
- `ListBoxItem`;
- `ListViewItem`;
- `TreeViewItem`;
- `DataGridRow`;
- `DataGridCell`.

Do not replace `ListViewItem` with a generic content template that would break `GridView` column presentation.

### 5.7 Buttons

Use three clear levels:

- neutral toolbar/secondary;
- blue primary;
- red destructive.

Primary hover must remain primary-blue rather than turning into a generic dark hover.

## 6. P1 — Workspace palette

The compact-shell baseline is already completed on `main`; this Premium v2 batch adds only the owner-demonstrated `MÔ HÌNH` / `Làm mới` collision guard and keeps the rest of the Workspace behavior unchanged.

### Layout

- collision-free compact headers/actions;
- stable Zone/Floor selectors;
- clear model/category hierarchy;
- Family/Type action area with fixed action hierarchy;
- selected-object actions grouped by purpose;
- property inspector keeps Family/Instance scope obvious;
- narrow-width fallback must never overlap controls.

### Visual hierarchy

- project scope / model / Family / property areas read as four distinct work zones;
- selected rows and active Family are unmistakable;
- headings use primary text;
- small section metadata uses muted/champagne sparingly;
- action labels never overlay titles.

## 7. P2 — Right Panel

- stable drawing/Xref action hierarchy;
- destructive detach separated from attach/reload/move;
- layer search with clear focus state;
- layer visibility/lock state aligned;
- selected rows remain high contrast;
- long names trim with tooltip;
- footer distinguishes info/warning/error.

## 8. P3 — modeless window consistency

Apply the same design contract to:

- Domain Hub;
- Project Tools;
- Schedule Hub;
- BQ / Quantity Summary;
- Family/Floor/Zone/Material editors;
- Door/Opening schedules;
- Curtain/Rebar tools;
- Health/Recognition/Revision/Release windows.

Standardize:

- title/header;
- toolbar placement;
- input sizing;
- table header/row density;
- empty states;
- error/status surface;
- primary/secondary/destructive buttons.

## 9. P4 — interaction feedback

Every consequential operation should expose an appropriate state:

- ready;
- processing;
- success;
- warning/review;
- failed;
- disabled/not applicable;
- stale/reload required where applicable.

Rules:

- do not add fake buttons or decorative controls that look actionable;
- disabled actions should explain the reason where practical;
- preserve ESC/cancel and native BricsCAD editor behavior;
- use lightweight state changes only.

## 10. P5 — accessibility and DPI

Runtime matrix:

- 100%, 125%, 150%, 200% DPI;
- narrow, normal and wide palette widths;
- Vietnamese Unicode;
- keyboard focus;
- mouse hover;
- selected;
- disabled;
- read-only;
- warning/error states.

Acceptance:

- no clipped diacritics;
- no black text on dark surfaces;
- no white popup/selected/hover surfaces;
- no overlapping labels/buttons;
- no hidden command text;
- no fixed width that forces collision at ordinary palette sizes.

## 11. P6 — performance guardrails

Premium must stay fast:

- keep `ListView`/`DataGrid` virtualization;
- no blur/acrylic;
- no drop shadows on repeated rows;
- no animated gradients;
- no continuous animation;
- do not rebuild large item sources for a visual-only state;
- do not tie palette refresh to geometry regeneration.

## 12. Source/static gates

`scripts/preflight-wpf-theme.py` must enforce:

- well-formed `Theme.xaml`;
- colors are not used where a Brush is required;
- required graphite/navy/premium brushes exist;
- `PanelTitle` explicitly owns high-contrast `Foreground`;
- host-independent `ComboBox`, `ComboBoxItem`, `TextBox`, `CheckBox`, `ScrollBar` styles exist;
- ComboBox popup/editable parts are retained;
- TextBox content host is retained;
- dark hover/selected/focus resources are explicit;
- no `SystemColors`, blur or drop-shadow theme dependency.

The Workspace collision fix has its own active feature-specific preflight and must not be hidden inside the global theme gate.

## 13. Local BricsCAD V25 qualification

Static source review cannot prove native WPF rendering.

The existing local UI qualification boundary must verify the exact implementation SHA with real BricsCAD V25:

1. load QS3D into BricsCAD;
2. open Workspace and Right Panel;
3. exercise every ComboBox open/hover/selected/disabled state;
4. exercise TextBox focus/read-only/disabled;
5. exercise CheckBox checked/unchecked/indeterminate if present;
6. exercise vertical/horizontal scroll bars;
7. verify selected List/Tree/DataGrid rows;
8. verify the Workspace collision screenshot scenario;
9. test 100–200% DPI and narrow/normal/wide palette widths;
10. capture sanitized before/after screenshots.

Do not promote source/static evidence to `LOCAL_PASS`.

## 14. Definition of done

UI/UX Premium v2 is complete when:

- shared host-dependent white/black leaks are removed from source;
- ComboBox popup/hover/selected states remain dark by explicit template;
- primary/destructive/neutral action states are distinct;
- Workspace structural overlap is fixed by the Premium v2 collision guard without changing Workspace handlers;
- Vietnamese labels remain readable at practical widths;
- major modeless windows can consume the same shared design system;
- no heavy visual-effect performance regression is introduced;
- exact V25 runtime evidence passes the local visual matrix.

## 15. Implementation order

1. shared Premium Theme v2 + static guard;
2. Workspace narrow-header overlap fix layered on the completed compact-shell baseline;
3. Right Panel polish;
4. modeless-window consistency;
5. accessibility/DPI pass;
6. exact V25 visual qualification;
7. only then small screenshot-driven refinements.

GitHub Actions remain owner-controlled/manual-only. A UI source commit does not authorize Actions or a release.
