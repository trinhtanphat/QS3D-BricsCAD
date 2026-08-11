# QS3D Ribbon Information Architecture — BLT3D-inspired UI plan

Date: 2026-08-11  
Scope: BricsCAD V25 ribbon presentation only  
Reference: owner-provided BLT3D desktop screenshots (1366×768-class viewport)

## 1. Objective

Improve command discoverability and visual hierarchy in the BricsCAD-hosted QS3D ribbon without creating a second application shell and without changing any semantic, persistence, selection, geometry, transaction or CAD-ownership behavior.

The screenshots are useful as a clean-room visual reference for information architecture:

- a predictable top tab strip;
- domain-oriented command clusters;
- Vietnamese-first labels;
- a dark CAD-first working area;
- high-frequency actions visible without navigating deep menus;
- separate View, Quantity and Revision surfaces.

QS3D already has substantially more implemented functionality than the captured BLT3D build. The source currently exposes View and Revision commands, plus premium WPF Workspace/RightPanel/DomainHub/ProjectTools/Revision UI. The main ribbon weakness is therefore not missing command implementations; it is density. `RibbonBootstrapper` currently creates one `RibbonPanelSource` per entire tab, so a tab with 15–25 commands renders as one long undifferentiated strip.

## 2. Screenshot review

### 2.1 Strengths worth carrying over

1. **Stable domain tabs** — project setup, BIM, recognition, drawing/modeling, view, quantity and revision are easy to scan.
2. **CAD canvas remains primary** — tools surround the model rather than replacing it with a dashboard.
3. **Left/right operational context** — active zone/floor and drawing/layer management remain near the canvas.
4. **Short command labels** — commands can be found by meaning instead of memorizing aliases.
5. **Dedicated quantity/revision tabs** — review work is separated from modeling work.

### 2.2 Problems visible in the reference build

1. **Ribbon density varies heavily.** Some tabs are crowded while `XEM` / `BẢN SỬA ĐỔI` can look empty in the installed screenshot.
2. **A single long command band has weak hierarchy.** Related actions such as navigation, sectioning, health and exports are not visually grouped.
3. **Mixed language and naming styles.** Vietnamese labels, English nouns and command-like wording compete for attention.
4. **High-risk / diagnostic actions are visually mixed with normal authoring.** Health/release checks should sit in a clearly named quality group.
5. **1366×768 is unforgiving.** Large flat groups cause horizontal compression/overflow and make users hunt for commands.

## 3. Current-source finding

`src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs` already defines these QS3D tabs:

- `KHỞI ĐẦU`
- `THIẾT LẬP DỰ ÁN`
- `TẠO MỚI`
- `MÔ HÌNH BIM`
- `NHẬN DẠNG`
- `VẼ`
- `TOOL`
- `MODELING`
- `XEM`
- `ĐỊNH LƯỢNG`
- `BẢN SỬA ĐỔI`

The source also already binds working command entry points for View (`QS3DVIEW3D`, `QS3DVIEWTOP`, `QS3DORBIT`, focus/isolate/section/zoom), Quantity/BQ/ED2/Rebar and Revision (`QS3DREVBASE`, `QS3DREVDIFF`).

Therefore an installed build that still shows blank-looking View/Revision tabs is likely behind current source or is rendering an older ribbon state. This batch does not invent substitute command logic to compensate for an old installed binary.

## 4. Implementation strategy

### 4.1 Replace “one panel per tab” with named functional panels

Introduce a presentation-only specification hierarchy:

`RibbonTabSpec -> RibbonPanelSpec -> RibbonButtonSpec`

Each tab can now create multiple native `RibbonPanelSource` instances with its own visible title.

The native command dispatcher remains unchanged:

`RibbonButton -> CommandParameter -> RibbonCommandHandler -> MdiActiveDocument.SendStringToExecute(...)`

No command is executed from a captured/stale document object.

### 4.2 Panel map

| Tab | Functional panels |
| --- | --- |
| Khởi đầu | Dự án; Điều phối; Chất lượng |
| Thiết lập dự án | Trạng thái; Template; Phạm vi |
| Tạo mới | Thiết lập; Kiến trúc; Kết cấu; Hoàn thiện 3D |
| Mô hình BIM | Phòng & hoàn thiện; Tường & vách; Kết cấu; Cửa & lỗ mở; Sinh mô hình |
| Nhận dạng | Nhận dạng; Kiểm tra |
| Vẽ | Hình học; Biến đổi; Kết nối & đo |
| Tool | Kiểm tra; Tập trung; Cắt & zoom; Bảo trì |
| Modeling | Sinh 3D; Tường & vách; Kết cấu; Cửa & host; Phòng |
| Xem | Góc nhìn; Tập trung; Mặt cắt; Điều hướng; Workspace |
| Định lượng | Khối lượng; Excel ↔ CAD; Cửa & lỗ mở; BBS; Cốt thép 3D; Health cốt thép |
| Bản sửa đổi | Bản sửa đổi; Kiểm tra; Dự án |

### 4.3 Command-preservation rule

All 103 unique command strings present in the pre-change ribbon must remain represented after regrouping. This includes native CAD commands (`_LINE`, `_MOVE`, etc.) and QS3D commands.

The implementation may improve display labels (`Workspace`, `Layer / Xref`, panel captions) but must not silently redirect a button to different business logic.

### 4.4 Stable native IDs

Ribbon button IDs include tab + panel + normalized label:

`<TAB>_<PANEL>_<BUTTON>`

This prevents collisions when the same display label exists in different functional panels.

## 5. Detailed UX decisions

### Khởi đầu

Keep the start surface minimal:

- **Dự án**: Workspace, Save.
- **Điều phối**: regenerate and primary quantity review.
- **Chất lượng**: model health and release checks.

The goal is a “get into work / verify work” split rather than a giant home toolbar.

### Thiết lập dự án

Separate state refresh/reload from reusable templates. `Layer / Xref` remains a Workspace entry because the current ribbon source did not expose a dedicated safe command for that label; this batch does not invent one.

### Tạo mới / Mô hình BIM / Modeling

Separate architectural, structural, opening/host and generated-model actions. Direct Draw remains visually prominent while existing command ownership and lifecycle rules stay untouched.

### Xem

The screenshot’s empty-looking `XEM` surface is addressed by presenting current implemented commands in five obvious groups:

- orientation;
- focus/isolation;
- section/clip;
- navigation;
- workspace/report access.

### Định lượng

Separate normal BQ/Takeoff, Excel↔CAD, opening schedules, BBS, 3D rebar authoring and rebar health. This reduces the chance of choosing a diagnostic command while looking for fabrication/quantity output.

### Bản sửa đổi

Keep the review path explicit:

1. create baseline;
2. compare;
3. run quality checks;
4. save project.

The existing `RevisionWindow` remains the detailed semantic/quantity diff UI and is intentionally not modified in this lane.

## 6. Non-goals / safety boundary

This batch does **not**:

- alter `WorkspacePanel`, `RightPanel`, `Theme.xaml` or any modeless viewer;
- create BLT-compatible proprietary behavior or reuse BLT source/assets;
- change any QS3D command implementation;
- alter semantic identity, project lifecycle, generated ownership or CAD transactions;
- add new geometry/build logic;
- change installer/release/signing;
- claim BricsCAD V25 runtime rendering evidence.

These boundaries matter because the repository has multiple active agents on neighboring product lanes.

## 7. Static regression gate

Add `scripts/preflight-ribbon-information-architecture.py`.

The preflight verifies:

- the `RibbonPanelSpec` model exists;
- `TryInitialize` iterates `tabSpec.Panels`;
- panel titles come from `panelSpec.Title`, not the tab title;
- button IDs include the panel identity;
- the legacy `spec.Buttons` single-panel loop does not return;
- all expected tabs/panel titles remain present;
- all 103 pre-existing command bindings remain present;
- enough functional panels exist to prevent accidental collapse back to one flat band.

This is a source contract, not a substitute for native BricsCAD rendering proof.

## 8. Local V25 qualification checklist

The existing LOCAL_ONLY V25 qualification lane remains authoritative. On the exact candidate SHA, verify:

1. NETLOAD/DemandLoad registers the ribbon once.
2. Reload/re-initialize remains idempotent; no duplicate tabs/panels.
3. All 11 QS3D tabs render with panel captions.
4. 1366×768 at 100% scaling: commands remain discoverable and native overflow behavior is usable.
5. 1920×1080 at 100/125/150% scaling: panel captions and Vietnamese text do not clip materially.
6. `XEM` visibly exposes 3D/Top/Orbit, focus/isolate, section/clip and zoom commands.
7. `BẢN SỬA ĐỔI` visibly exposes baseline/diff/health/release/save.
8. Representative buttons from every tab dispatch the same command as before.
9. Switching DWGs before clicking a ribbon button executes against the current `MdiActiveDocument`.
10. No source-owned project or semantic mutation occurs merely from ribbon initialization.

No `LOCAL_PASS` is inferred from static review.

## 9. Follow-up UI wave after this lane

The screenshots also motivate a broader Start Center / workflow launcher with search, favorites, recent DWGs and project summary. A separate active claim owns that new modeless surface, so this ribbon lane intentionally does not overlap it.

Once both lanes are complete and their claims are closed, a small follow-up may add a `QS3DSTART` ribbon entry if the command is then present on `main`. That wiring must be reserved separately rather than guessed in advance.

## 10. Definition of done

- grouped native ribbon panels are implemented on `main`;
- all pre-existing command bindings remain;
- the static preflight is present and source-valid;
- this plan documents the screenshot-driven IA and local render matrix;
- the agent claim is closed with actual commit SHA(s);
- runtime rendering remains explicitly local until tested in BricsCAD V25.
