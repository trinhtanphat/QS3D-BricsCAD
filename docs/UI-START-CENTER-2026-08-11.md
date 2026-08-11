# QS3D Start Center — clean-room workflow hub

Updated: 2026-08-11 (UTC+7)

## Product boundary

`QS3DSTART` is a BricsCAD V25 modeless WPF workflow hub. It is **not** a standalone QS3D application, does not own the DWG viewport/database/editor, and does not introduce a second semantic, quantity, geometry or command engine. The visual direction is clean-room workflow familiarity only; no BLT proprietary source/assets/API are used.

The Start Center reuses the repository's existing Premium Dark `Theme.xaml` and dispatches established QS3D commands back to the current BricsCAD document.

## Implemented source behavior

### Current drawing / project dashboard

The header resolves `Application.DocumentManager.MdiActiveDocument` when the window refreshes and shows the current DWG name/path plus a read-only QS3D summary: project name, element/Family/Level/Zone counts, active Level, active Family, `ChangeVersion` and pending-save state.

Dashboard reads use `ProjectContextCoordinator.TryGetReadOnly`; they never call `GetOrCreate`. Merely opening or viewing Start Center therefore is not a project-bootstrap boundary.

### File / project actions

The UI exposes four fixed BricsCAD document actions only: `NEW`, `OPEN`, `QSAVE`, and `SAVEAS`. These are represented by a private enum/switch rather than an arbitrary native-command text box. Project operations reuse allowlisted QS3D commands such as Workspace, Project Tools and `QS3DSAVE`.

### Command launcher

`StartCenterCommandCatalog` is a hard-coded QS3D-only allowlist. Search ranks command name, Vietnamese title, group, description and keywords. Groups cover:

- Khởi đầu;
- Tạo mới / Quick Direct Draw;
- Nâng cao;
- Mô hình;
- Nhận dạng;
- Khối lượng;
- Cốt thép;
- Review & Health;
- Dự án.

The initial catalogue includes 70+ current QS3D entry points, including Family/Type, Direct Draw Quick/Advanced, Create Similar, Room/Build/Curtain, Recognition/B4D, BQ/ED2/BBS, rebar, previews, Model Health, diagnostics and project persistence.

Every launcher click resolves the **current** active document at execution time and calls only a catalogue item accepted by `StartCenterCommandCatalog.TryGet`. Arbitrary command strings are rejected.

### Quick Workflow

The first screen promotes the shortest normal authoring route: Family / Type, Wall, Beam, Column, Slab, Door, Window, Create Similar, BQ and Model Health. The Start Center does not duplicate any of those workflows; it only launches their canonical commands.

### Favorites and recent commands

Favorites and the recent-command list are per-user state. Only commands still present in the hard-coded allowlist survive load/normalization, so a modified settings file cannot turn the launcher into arbitrary command execution.

### Recent Projects / DWG

Recent project state stores only normalized rooted `.dwg` paths. It:

- canonicalizes with `Path.GetFullPath`;
- deduplicates case-insensitively for Windows;
- supports pin/unpin;
- bounds history size;
- reports missing files without deleting history automatically;
- requires a live file again before Open;
- removes/clears **history only** — never the actual DWG.

The settings format is bounded and stored under `%LOCALAPPDATA%\QS3D\BricsCAD-V25\start-center-v1.txt`. Dynamic values are Base64 encoded and saves use a temp file plus replacement/fallback pattern consistent with existing per-user UI settings.

## Multi-DWG safety

The Start Center intentionally does not retain a `Document` field. Command launch, project dashboard refresh, native file actions and recent-DWG open all resolve the active BricsCAD document again at click/refresh time. The window is a global launcher, not a document-bound mutation editor.

Project summary is read-only. Existing business commands keep ownership of their own mutation, transaction, stale-project and rollback rules.

## Keyboard / interaction

- `Ctrl+F` focuses command search.
- Double-click runs command/favorite/recent-command items.
- `Enter` runs the selected command while the command list has focus.
- `Esc` closes Start Center.

## Static regression gates

`scripts/preflight-start-center.py` is auto-discovered by `scripts/preflight-all.py`. It locks:

- `QS3DSTART` registration and modeless BricsCAD hosting;
- shared Premium Dark theme usage;
- Quick Workflow / Command Launcher / Favorites / Recent Projects / Review & Diagnostics surfaces;
- hard-coded QS3D allowlisting and a representative command set;
- click-time `MdiActiveDocument` resolution;
- non-creating `TryGetReadOnly` dashboard behavior;
- active Family / pending-save summary;
- normalized `.dwg` recent-project persistence;
- bounded state and replacement save;
- absence of `Process.Start` and `ProjectContextCoordinator.GetOrCreate` from the Start Center source lane.

`scripts/preflight-start-center-ribbon.py` separately locks discoverability: the implemented `QS3DSTART` command must appear exactly once in Ribbon source, inside `KHỞI ĐẦU` → `Dự án`, while Ribbon execution continues to resolve `MdiActiveDocument` at click time.

## Ribbon discoverability

The first Start Center implementation deliberately avoided Ribbon edits because the grouped Ribbon information-architecture lane was still active. After that lane completed, a separate conflict-safe reservation added exactly one **Start Center** button to `KHỞI ĐẦU` → `Dự án`, bound to `QS3DSTART`. No tab/panel regrouping or existing command removal is part of this follow-up.

## LOCAL_ONLY V25 qualification

Remote/source review is not BricsCAD runtime proof. Exact-candidate local qualification must verify:

1. `QS3DSTART` registration, one-window modeless reopen/activate and focus behavior;
2. the `KHỞI ĐẦU` → `Dự án` Start Center Ribbon button renders once and dispatches `QS3DSTART` to the click-time active DWG;
3. Vietnamese Unicode plus 100/125/150/200% DPI and common 1366×768 / larger desktop fit;
4. `NEW`, `OPEN`, `QSAVE`, `SAVEAS` prompt/file-dialog behavior in real V25;
5. two-DWG switching while Start Center stays open, with dashboard/command/Ribbon dispatch always following the active DWG and no project creation merely from viewing;
6. Favorites/recent commands survive BricsCAD restart;
7. recent-DWG dedupe, pin/unpin, successful open, missing-file status and Remove/Clear no-delete behavior;
8. `Ctrl+F`, `Enter`, double-click and `Esc` interaction.

Keep private paths/screenshots and proprietary runtime material out of Git; only sanitized exact-SHA evidence belongs in the local qualification record.
