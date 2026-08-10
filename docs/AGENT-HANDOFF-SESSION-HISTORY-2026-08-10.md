# QS3D-BricsCAD — Exhaustive session/history handoff for next agents

**Audit date:** 2026-08-10 (UTC+7)  
**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Source-of-truth branch:** `main`  
**`main` snapshot immediately before this handoff commit:** `db4e5dd2ae2d4cf64450be8906fc0d50b3636a3d` (`fix(runtime): preserve projects across Save As and active DWG sync`)  
**Purpose:** preserve the complete important context from the current ChatGPT development session and the relevant recovered QS3D/BLT3D history so another agent can continue without rediscovering requirements, repeating mistakes, overwriting concurrent work, or making false runtime claims.

---

## 1. Review coverage and proof

This handoff was not written from a short conversational summary alone.

### 1.1 Current-session coverage

The accessible session event/history stream was read sequentially in pages of 20 records at offsets:

`0, 20, 40, 60, 80, 100, 120, 140, 160, 180, 200, 220, 240, 260, 280, 300, 320, 340, 360`.

The final page exhausted the stream and reported **0 remaining records**. Total accessible session records reviewed: **377 / 377**.

That is the evidence behind the statement “100% of the accessible current session was reviewed”. It does **not** mean that an agent can prove review of chats that the platform did not expose to this session, deleted chats, or arbitrary account-wide history that was never surfaced.

### 1.2 Prior-history coverage

In addition to the 377/377 current-session records, two targeted project-history retrievals were performed for the earlier **QS3D / BLT3D / BricsCAD V25** work. They recovered the important older decisions, source milestones, runner/CI constraints, screenshot requirements, private fixture information and previous branch/gate history described below.

### 1.3 Current GitHub source audit

The history was then reconciled against the actual repository instead of assuming old chat claims were still true. The audit included:

- latest `main` branch SHA;
- recursive current repository tree;
- `AGENTS.md`;
- `CI_POLICY.md`;
- `docs/IMPLEMENTATION-STATUS.md`;
- `docs/REVIEW-2026-08-10.md`;
- current `Commands.cs`;
- current `ReviewCommands.cs`;
- current `RuntimeProbeCommands.cs`;
- current `RecognitionEngine.cs`;
- current `RevisionSnapshotStore.cs`;
- current Recognition and Revision WPF windows;
- the latest Save-As/document-lifecycle hardening commit.

Where an old chat/branch state differs from current `main`, **current `main` wins** and the discrepancy is documented rather than silently merged into one story.

---

## 2. Owner intent that must not be lost

The product is an original, clean-room **BLT3D-like quantity takeoff / semantic BIM management plugin for BricsCAD V25**. It is a Windows desktop CAD plugin, not a web replacement for BricsCAD.

The recurring owner requirements are:

1. Build for **BricsCAD V25** and make the workflow/UI close to the supplied BLT3D reference images.
2. Keep the **native BricsCAD viewport in the center**. Do not build a custom CAD renderer just to imitate the screenshot.
3. Use docked WPF palettes around the native viewport plus a BricsCAD Ribbon/commands.
4. Implement real functions, not decorative/mock buttons.
5. Review the entire source carefully, fix bugs and missing behavior, then test carefully.
6. Do not claim something is tested in BricsCAD merely because C# source/static checks look correct.
7. The local computer is weak; use remote/GitHub work for source/core tasks when possible and reserve a real Windows/BricsCAD environment for genuine V25 runtime work.
8. Multiple agents may work at once. Never discard another agent's newer `main` work.
9. Do **not** run GitHub Actions merely because code/docs were changed. CI is owner-controlled/manual-only.
10. Continue implementing broadly (“continue all / implement all”), but keep clean technical boundaries and do not bypass quality gates.

---

## 3. Clean-room / BLT3D source boundary

Public research during this work found an official-looking BLT Software site (`thangblt.com` / `www.thangblt.com`) describing BLT3D as a 3D quantity/BIM-oriented solution, and public social references saying BLT3D runs on BricsCAD and can export quantity explanations.

No public GitHub repository/source code matching BLT3D + BricsCAD was found in the searches performed during this project. Therefore the safe/default engineering assumption is:

- BLT3D is proprietary unless the owner later supplies source with a license that explicitly permits reuse;
- QS3D must remain an **independent clean-room implementation**;
- do not copy BLT DLLs, proprietary source, icons/assets, license material or binary internals into this repo;
- do not make QS3D depend on an installed `BLT` folder;
- an owner-provided BLT installation may be inspected only for lawful compatibility/workflow understanding, not as a binary/source dependency;
- never request or commit license keys/secrets.

The screenshot/workflow can be emulated without copying proprietary source/assets.

---

## 4. BricsCAD V25 technical baseline

The V25 baseline used throughout the work was checked against Bricsys developer documentation:

- target managed runtime: **.NET Framework 4.8**;
- Visual Studio 2019+ is appropriate for V25 .NET development;
- primary managed references: `BrxMgd.dll` and `TD_Mgd.dll`;
- optional APIs can include `TD_MgdBrep.dll` and `TD_MgdDbConstraints.dll` when actually needed;
- BricsCAD assemblies should remain external references (`Copy Local = False` / not bundled with QS3D releases);
- plugin application contract is `Teigha.Runtime.IExtensionApplication`;
- commands use `Teigha.Runtime.CommandMethodAttribute`;
- `Bricscad.Windows.PaletteSet` and `AddVisual` support the WPF palette architecture;
- plugin target is **Windows x64**;
- exact V25 API/runtime compatibility still requires a real V25 installation.

Current architecture should continue to keep the BricsCAD adapter thin and place deterministic business/domain logic in `QS3D.Core`.

Reference architecture:

```text
BricsCAD V25
    |
QS3D.BricsCAD.V25 adapter (thin)
    |
normalized semantic model
    |
QS3D.Core / Geometry / Takeoff / Formula / Rebar / Persistence / Reporting
```

This keeps future AutoCAD/ZWCAD/GstarCAD adapters possible and reduces CAD-API coupling.

---

## 5. BLT3D screenshot / UI contract

The supplied BLT3D reference showed a dark CAD workspace with these top tabs (Vietnamese wording is important):

- `KHỞI ĐẦU`
- `THIẾT LẬP DỰ ÁN`
- `MÔ HÌNH BIM`
- `NHẬN DẠNG`
- `VẼ`
- `TOOL`
- `MODELING`
- `XEM`
- `ĐỊNH LƯỢNG`
- `BẢN SỬA ĐỔI`

The visual/workflow structure to retain:

### Left dock

- Zone / Floor selectors.
- Semantic model tree including approximately:
  - `Lưới Trục`
  - `HT_Phòng`
    - `Phòng`
    - `Sàn Hoàn Thiện`
    - `Chống Thấm`
    - `Chân Tường`
    - `Hoàn Thiện Tường`
    - `Trần Hoàn Thiện`
    - `Lan Can`
  - `Dầm`
  - `Sàn`
  - `Cột`
  - `Vách`
  - `Tường KT`
    - `Tường Gạch`
    - `Vách Kính`
    - `Trụ Tường`
  - `Cửa`
    - `Lỗ Mở Vách`
    - `Cửa Đi`
  - `Cầu Thang`
  - `Móng`
  - `Đào đắp`
  - `KL Tùy chỉnh`
- Family/type/element list.
- Property inspector/editor.

### Center

- **Native BricsCAD viewport**, fully interactive 2D/3D.
- Do not replace it with a WPF/OpenGL imitation.

### Right dock

- `Quản lý bản vẽ` / Xref-related workflow.
- `Quản lý lớp` / layer visibility/search.

### Visual language

- dark charcoal CAD theme;
- blue accent for primary actions;
- red/destructive styling only where appropriate;
- compact dock widths so the CAD viewport keeps usable space;
- secondary WPF windows (BQ, Recognition, Revision, Health, BBS) should share the same theme, including DataGrid headers/rows/cells, ComboBox popup and tooltips;
- test Windows scaling 100%, 125%, 150%, 200% on the real runtime before claiming screenshot parity.

The user asked for an interface close to the reference **and** a manipulable 3D model. “Looks similar” is not sufficient if commands are placeholders.

---

## 6. Requirement document and private DWG evidence

A requirement DOCX previously inspected contained the core wording:

```text
HOÀN THIỆN:
BIULD CHỨC NĂNG
TƯỜNG KT
HT_PHÒNG
Cửa:
OUTPUT: xuất khối lượng sang excel
```

It also contained embedded reference images.

The implementation priorities inferred directly from that material were therefore:

- `Tường KT`;
- `HT_Phòng`;
- `Cửa` / openings;
- quantity output to Excel.

A private drawing fixture was also referenced:

`260808.SHOP XAY TUONG_NHA NOI TRU.dwg`

Historical inspection identified it as roughly 22 MiB and an older DWG format carrying AutoCAD/BricsCAD/ODA-era metadata. It is a **private runtime fixture** and must not be committed to this public repository. A local V25 runner may use it for runtime validation only.

Likewise, private DOCX/DWG artifacts supplied by the owner are not release assets unless the owner explicitly changes that policy.

---

## 7. Initial foundation and important early implementation facts

The first clean-room foundation was originally assembled under a local working folder like `QS3D-BricsCAD-V25` and included the solution/core/adapter/smoke/preflight structure that later became the GitHub project.

Important early core behavior included:

- `QS3D.Core` targeting `netstandard2.0`;
- BricsCAD adapter targeting `net48`, WPF, x64;
- polyline length/area metrics;
- drawing-unit conversions for quantity normalization;
- quantity engine for Count / Length / Area / Volume;
- recursive-descent formula evaluator with `+ - * /`, parentheses, variables and functions such as `abs`, `ceil`, `floor`, `round`, `min`, `max`;
- division-by-zero / unknown variable/function rejection;
- rebar notation support for examples such as:
  - `4Ø20`
  - `4D20`
  - `Ø8a150`
  - `D8@150`
  - `2Ø18+2Ø20`
  - later `3x4Ø16`;
- theoretical steel mass formula `d² / 162` kg/m;
- early palette/command shell and package-free deterministic smoke runner.

Important quality lesson from the initial phase: a simplistic C# delimiter checker falsely flagged `ExpressionEvaluator.cs` because it did not understand character literals. It was replaced by a lexical checker that skips strings, verbatim strings, char literals and comments. Do not reintroduce naive syntax checks that create false positives.

The early container had no real `dotnet`/MSBuild/BricsCAD installation. Therefore historical “preflight/lexical PASS” must never be rewritten as “V25 plugin compiled and NETLOAD passed”.

Early potentially risky BricsCAD entity metadata access (`ColorIndex`, `Linetype`, `Visible`) was removed to reduce compile risk, and an unnecessary `CommandFlags.Redraw` dependency was removed. Preserve the general principle: only use V25 APIs that are justified and runtime-test them later.

---

## 8. Domain/model architecture developed through the session

The project evolved from a takeoff shell into a semantic project model. The major concepts that should remain coherent are:

- Project
- Zone
- Floor
- Family / Type
- semantic Element
- source CAD handles
- generated CAD handles/geometry metadata
- element properties
- deterministic derived quantities
- dirty flags / regeneration
- dependencies/host links
- revision snapshots
- health diagnostics

Business logic belongs in Core; the adapter should capture live CAD state, invoke Core and write controlled CAD changes.

The persistence design is offline-first and sidecar-based rather than depending on an always-online backend.

---

## 9. Current `main` source-of-truth at handoff time

Immediately before creating this handoff, `main` was:

`db4e5dd2ae2d4cf64450be8906fc0d50b3636a3d`

with message:

`fix(runtime): preserve projects across Save As and active DWG sync`

This is newer than several SHAs discussed earlier in the session. Agents must **not** start from those older chat SHAs.

The immediately preceding mainline history also included storage/revision/report hardening and BLT-workspace/Xref/quantity fixes. The repository is actively changing, so sync again before every code change and before every push.

### 9.1 Latest Save As / document identity hardening

The latest inspected commit moved live project caching away from mutable document-name keys toward `Document` identity and added drawing-identity synchronization. This is intended to stop Save As from orphaning the in-memory semantic project.

The same hardening also strengthened:

- active-document-only selection synchronization;
- compact left palette minimum width (current preflight guards the newer target rather than the older oversized width);
- XAML handler coverage in preflight, including `SelectedItemChanged`;
- required-file coverage for project context, selection sync and palette coordination.

Any future document-lifecycle refactor must preserve Save-As behavior and multi-document isolation.

---

## 10. Current implemented behavior confirmed from repository source/docs

The following is current-source functionality, not merely an old plan:

### Project/persistence

- BricsCAD V25 `net48/x64` adapter with external BricsCAD references.
- Project/Zone/Floor/Family/semantic Element model.
- QSDB schema and deterministic migration path documented by current source/status.
- validated temp save and replacement/backup strategy;
- project locking / recovery behavior;
- corrupted-primary fallback to valid backup where possible;
- protected recovery state that refuses to overwrite damaged project data when recovery is unsafe;
- persistence of dirty flags and UTC update state;
- rejection of invalid persisted numeric/timestamp state rather than silently coercing it;
- XML hardening against DTD/external resolution and oversized input.

**Important:** older branch discussions mentioned different schema-version ideas. Current `docs/IMPLEMENTATION-STATUS.md` is the source of truth and describes the active QSDB schema as **v2 with v1 → v2 migration**. Do not resurrect an old “v3” claim without checking source/migrators first.

### Deterministic regeneration

- dependency graph / dirty propagation;
- bounded fixed-point/multi-pass regeneration;
- host changes can dirty dependent quantities and be resolved in a later pass;
- explicit `QS3DREGEN`;
- BQ/BBS/Refresh regenerate deterministic dirty semantic quantities before consuming them.

### Semantic capture/categories

Current `Commands.cs` contains real commands for:

- `QS3D`
- `QS3DHIDE`
- `QS3DRIBBON`
- `QS3DINSPECT`
- `QS3DBQ`
- `QS3DBBS`
- `QS3DREGEN`
- `QS3DSAVE`
- `QS3DRELOAD`
- `QS3DREFRESH`
- `QS3DTAKEOFF`
- `QS3DWALL`
- `QS3DROOM`
- `QS3DOPENING`
- `QS3DDOOR`
- `QS3DBEAM`
- `QS3DSLAB`
- `QS3DCOLUMN`
- `QS3DSTRUCTWALL`
- `QS3DFOUNDATION`
- `QS3DSTAIR`
- `QS3DRAILING`
- `QS3DEARTHWORK`
- `QS3DLINKHOST`
- `QS3DFINISH`
- `QS3DHEALTH`
- `QS3DLOCATE`
- `QS3DRESETUI`
- `QS3DSAFEMODE`
- `QS3DABOUT`

### Review / recognition / revision / 3D commands

The current implementation uses `ReviewCommands.cs` rather than the older experimental `DomainExtensionsCommands.cs`. Current commands include:

- `QS3DBUILD3D`
- `QS3DBBSVIEW`
- `QS3DRECOGNIZE`
- `QS3DRECOGNIZEAUTO`
- `QS3DREVBASE`
- `QS3DREVDIFF`

`QS3DBUILD3D` currently routes architectural walls to `WallSolidBuilder` and supported structural categories to `StructuralSolidBuilder`; source comments/status indicate native 3D support focuses on Tường KT, Dầm, Sàn, Cột, Vách BTCT and Móng.

### Tường KT / host/opening behavior

- semantic Tường KT capture is implemented;
- selected plan LINE sources can produce native `Solid3d` walls;
- generated-geometry replacement has been hardened toward two-phase/transaction-safe updates;
- generated geometry is tracked separately from semantic source handles;
- stale generated handles should not cause unrelated entity deletion;
- Door/Opening can be linked to a host wall/vách for deterministic quantity deduction;
- re-hosting should dirty old and new hosts correctly.

Physical boolean subtraction of opening solids from generated host solids is still runtime/future work; semantic quantity deduction is not equivalent to a physical CAD boolean.

### HT_Phòng

Semantic generation exists for room finishes such as:

- floor finish;
- waterproofing;
- skirting;
- wall finish;
- ceiling finish.

A previous safety fix established that “remove finish tracking” must remove only finish semantic records and **must not erase CAD geometry**. Generic untracking and finish-specific untracking should remain distinct concepts.

### BQ / Excel

- semantic quantity report aggregation exists;
- rows group by stable IDs rather than only display names where appropriate;
- quantity window supports filtering/Locate and recalculation;
- `Tính lại` performs real regeneration rather than simply reapplying UI filters;
- fallback takeoff can use live drawing INSUNITS instead of blindly assuming millimeters;
- unsupported/undefined INSUNITS fallback should be explicit in status text;
- real `.xlsx` export exists;
- header freeze and AutoFilter are expected;
- area columns have been corrected/hardened in the exporter.

A historical full-domain branch added a `Thép (kg)` BQ column and changed an Excel range expectation from `A1:P2` to `A1:Q2`; compiler/smoke gates caught the stale expectation. When modifying current BQ schema, inspect the current exporter/tests rather than assuming that old branch shape is still present.

### Rebar / BBS

Current source/status includes:

- deterministic notation parser with validation;
- rejection of non-positive diameter/spacing/count;
- count, compound and spacing notation;
- deterministic BBS schedule calculations including mark/shape/cutting length and allowances;
- lap/anchor/hook/waste concepts in the schedule path;
- kg/m, total length and total weight;
- semantic `Rebar*` property adapter;
- `QS3DBBS` real XLSX export;
- `QS3DBBSVIEW` modeless schedule UI + Locate path.

The historical experimental `RebarCsvExporter.cs` / `QS3DBBSCSV` is **not present on current `main` at this audit**. Do not tell users it is available unless it is reimplemented and verified.

### Recognition

Current `RecognitionEngine` is present on `main` and is deterministic/rule-based, not LLM-authoritative.

Current behavior includes:

- layer terms;
- text/block/tag metadata terms;
- entity-type compatibility;
- Vietnamese text normalization / diacritic removal;
- scoring roughly weighted as layer `+0.62`, text `+0.28`, compatible type `+0.10`;
- review requirement when confidence is low or the top two candidates are too close;
- batch auto-accept default around `0.92` confidence with `0.15` minimum margin;
- rules for Beam, Slab, Column, StructuralWall, ArchitecturalWall, Opening, Door, Room, Foundation, Stair, Railing and Earthwork.

Current `ReviewCommands.cs` exposes `QS3DRECOGNIZE` and `QS3DRECOGNIZEAUTO`, and the current `RecognitionWindow.xaml` presents Handle, Entity, Layer, suggested category, confidence, margin, review flag and evidence with Apply/Locate operations.

Recognition should remain **suggestion + review**, especially for ambiguous cases. AI may later assist suggestions but must not silently become the authoritative quantity calculator.

### Revision

Current source includes a durable `RevisionSnapshotStore` with:

- baseline/current element snapshots;
- properties;
- quantities;
- source handles;
- floor/zone/family/category metadata;
- finite-number checks;
- XML DTD prohibition;
- max file size guard;
- temp write + backup/atomic replacement strategy;
- backup fallback on recoverable data failure;
- duplicate ID/property/quantity rejection.

Current `ReviewCommands.cs` exposes `QS3DREVBASE` and `QS3DREVDIFF`. Current `RevisionWindow` shows per-quantity Before/After/Delta/% changes and supports Locate.

### Model Health

Current health path checks important semantic/data quality problems including structural material inheritance, rebar definition validity and missing lengths, stale/missing handles and other model consistency concerns described by source/status. Continue extending this before adding “magic” automation that can silently generate bad takeoff data.

### Xref / Layer / selection

- live Xref listing;
- LayerTable listing/search/show/hide;
- direct Xref service for targeted reload/detach rather than command-string emulation where current source supports it;
- row selection can synchronize implied CAD selection;
- active-document filtering matters in a multi-document session;
- `Gỡ Xref` must detach the reference, not imply deletion of the external source file;
- Locate/selection uses CAD handles;
- latest main contains extra lifecycle/selection protections for active DWG and Save As.

### Runtime probe

`QS3DRUNTIMEPROBE` exists. It requires `QS3D_RUNTIME_RESULT` and writes a PASS/FAIL marker containing process/host/CLR/assembly/ribbon/palette information. It checks x64 and attempts real palette/ribbon initialization.

A probe source existing in Git is **not** evidence that it has successfully run on a licensed V25 machine.

---

## 11. Historical branch work vs current `main` — do not confuse them

Several full-domain features were developed/tested on temporary integration branches during this session. Some were later absorbed under different names/files; some were not retained exactly.

Important reconciliation:

- Historical `DomainExtensionsCommands.cs` is **not present** on current `main`.
- The relevant current adapter functionality now lives largely in `ReviewCommands.cs`.
- Historical `QS3DSTRUCTSOLID` became/was superseded by the current broader `QS3DBUILD3D` flow.
- Current recognition commands exist as `QS3DRECOGNIZE` / `QS3DRECOGNIZEAUTO`.
- Current revision commands exist as `QS3DREVBASE` / `QS3DREVDIFF`.
- Current BBS UI exists as `QS3DBBSVIEW`; standard XLSX export remains `QS3DBBS`.
- Historical `QS3DBBSCSV` / `RebarCsvExporter.cs` is absent at this audit.
- A historical `DomainHubWindow.xaml` / `QS3DDOMAIN` experiment is absent on current `main` at this audit. Do not assume it survived integration.
- `RecognitionEngine` and durable revision storage are present in current Core.
- `StructuralSolidBuilder.cs` is present in the current adapter.

This distinction is essential: a feature can be “implemented in an old branch during the chat” without being “available in current `main`”. Every next agent must inspect current source before describing functionality.

---

## 12. CI / compiler / regression history

### 12.1 Repository policy

`CI_POLICY.md` is authoritative:

- workflows on release `main` are **manual-only**;
- use `workflow_dispatch` only;
- do not add automatic `push`, `pull_request`, `schedule`, `workflow_run`, etc. without explicit owner instruction;
- a commit, push, merge, code fix, review, docs update or “continue all” is **not** permission to run Actions;
- static/local/preflight success is not equivalent to GitHub CI success;
- GitHub CI success is not equivalent to BricsCAD runtime success.

This documentation handoff task did **not** authorize CI and must not trigger CI.

### 12.2 Historical verified main/core runs recorded by current docs

Current `docs/IMPLEMENTATION-STATUS.md` records these historical Core CI passes:

- `31341101835` — baseline Core CI: PASS;
- `31341548469` — persistence/export hardening: PASS;
- `31341704360` — hardening snapshot: PASS.

Those runs verify their respective commit snapshots only. Newer source committed after them must not be called CI-verified unless an explicitly authorized run is completed for that newer SHA.

### 12.3 Other session branch gates

During temporary full-domain integration work, additional runs/IDs were observed, including examples such as:

- `31343750300` (a Core gate reported passing at one intermediate stage);
- `31343984922` (Core union gate reported PASS after schema/regression fixes);
- `31343166796` (a release-tree gate reported successful for its branch snapshot);
- `31344694425` (temporary full-domain final-gate branch run observed queued/in progress during that integration sequence).

These are **branch/session history**, not a blanket certification of today’s `main`. Before using one as evidence, map the run's `head_sha` to the exact code being discussed.

Temporary push-trigger workflows used for isolated gates were meant to exist only on temporary CI branches, not the release tree.

### 12.4 Real compiler/regression bugs caught during integration

The gate work was useful because it found real issues rather than merely producing green badges:

- nullable/compiler problems around recognition/rebar code;
- an old-framework annotation issue where `string.IsNullOrWhiteSpace` did not let the compiler prove a value non-null before `.Trim()`, requiring explicit null-safe code;
- BQ schema expansion caused an XLSX smoke expectation to remain `A1:P2` when the tested branch required `A1:Q2`; the regression was corrected instead of weakening the test;
- XAML/preflight required-file and handler coverage was repeatedly tightened;
- later document lifecycle/preflight was strengthened for Save As and active document sync.

Do not “fix CI” by disabling nullable, suppressing compiler errors broadly, or weakening assertions just to get green.

---

## 13. Gate C / real BricsCAD V25 blocker

The major remaining truth boundary is the real V25 runtime.

The V25 integration workflow historically recorded run `31341184031` as queued because there was no matching self-hosted runner with labels approximately:

`[self-hosted, windows, x64, bricscad-v25]`

Therefore do **not** claim all of the following have completed unless a newer real runner/session proves them:

- full adapter build against the exact installed V25 managed assemblies;
- `NETLOAD` success;
- Ribbon runtime compatibility on the installed V25 build;
- Palette docking/focus/DPI behavior;
- native `Solid3d` generation correctness in the supplied real DWG;
- transaction/undo behavior under real CAD failure cases;
- Xref reload/detach/move/selection behavior in a live drawing;
- screenshot parity at multiple Windows DPI values;
- V25.1/V25.2 API differences;
- real physical rebar geometry;
- physical opening boolean subtraction;
- installer/package runtime behavior on a clean V25 machine.

The repository contains runner/install/runtime-probe scripts to help perform this later. BricsCAD binaries must remain external and private.

---

## 14. Runtime checklist for a local/Windows V25 agent

A local agent with a licensed V25 installation should prioritize runtime-only work rather than ordinary Markdown/source cleanup.

Minimum acceptance sequence:

1. Sync latest `main` and record exact SHA.
2. Confirm BricsCAD V25 path and managed assemblies; keep them outside Git.
3. Build Release x64/net48 against the exact installed references.
4. `NETLOAD` QS3D into V25.
5. Run `QS3DRUNTIMEPROBE` with `QS3D_RUNTIME_RESULT` configured and retain safe evidence.
6. Open the main QS3D palettes; verify left/right docking and native center viewport.
7. Test at Windows 100%, 125%, 150%, 200% scaling for clipping and usable viewport space.
8. Exercise document create/activate/close and **Save As** to verify project identity/state is preserved and the active drawing is used.
9. Attach an Xref; select it in the right panel; verify CAD selection, move/reload/detach semantics and that external source files are not deleted.
10. Capture a Tường KT LINE in a millimeter drawing and at least one supported non-mm drawing; build/update 3D; check dimensions/position/offset.
11. Rebuild the same source and confirm exactly one current generated solid remains.
12. Force an invalid source/dimension and verify old valid generated geometry + semantic metadata are not corrupted.
13. Test Dầm/Sàn/Cột/Vách BTCT/Móng through `QS3DBUILD3D` using supported source geometry.
14. Test Opening/Door host linking and quantity deduction.
15. Generate `HT_Phòng`; verify finish-only untracking never erases CAD geometry.
16. Edit family dimensions; run BQ `Tính lại`; verify regenerated quantities update without reopening.
17. Export BQ XLSX and BBS XLSX; inspect values, headers, filters, freeze panes and units.
18. Run Recognition and review ambiguous/confident cases; verify Apply and Locate.
19. Capture revision baseline, modify semantic quantities/properties and verify `QS3DREVDIFF` field/quantity changes + Locate.
20. Run Model Health on intentionally malformed/stale examples.
21. Verify undo/redo around native generated geometry where supported.
22. Only after those tests, capture screenshots and update runtime status docs with exact host/build/version and evidence.

Do not commit proprietary BricsCAD DLLs, private DWGs or screenshots containing sensitive customer information.

---

## 15. Known gaps / future work that still matter

Even with the current broad source base, the following remain meaningful product gaps or runtime-gated work:

- actual V25 compile/NETLOAD qualification on the current latest SHA;
- exact screenshot parity and DPI polish;
- robust wall corners, joins, T-junctions and freeform profiles;
- physical boolean openings in generated solids;
- automatic room-boundary discovery from arbitrary wall networks;
- richer transient highlight / zoom-to-extents behavior beyond implied selection;
- geometric rebar placement inside BricsCAD (BBS is much further along than physical bars);
- deeper structural native geometry beyond currently supported source forms;
- more complete drawing/revision visualization workflows;
- packaging/installer/code-signing/release qualification;
- optional commercial licensing/update backend if/when productization is requested;
- broader standards/engineering rule configuration and validation;
- performance testing on large real drawings and many semantic elements;
- crash/recovery testing under abnormal BricsCAD shutdown/file locks;
- comprehensive accessibility/keyboard/focus behavior in modeless WPF windows;
- full localization strategy if the product later needs more than Vietnamese-first UI.

Cloudflare, if introduced later, should be an **optional backend** for things such as licensing/update manifests/customer metadata/R2 packages, not the runtime host for a Windows .NET Framework CAD plugin. GitHub is appropriate for source/CI/artifacts; the actual plugin runs in BricsCAD.

---

## 16. Multi-agent rules that must be followed

This repository is actively modified by multiple agents. During this very review, `main` advanced while the handoff was being prepared. Therefore every next agent must:

1. fetch latest `main` before starting;
2. inspect recent commits/files relevant to its task;
3. work from the current head, not a SHA copied from this handoff;
4. sync `main` again immediately before committing/pushing;
5. if `main` moved, reapply/rebase/merge its intended patch without deleting newer work;
6. inspect the final diff;
7. never force-push/reset `main` backward;
8. never silently overwrite another agent's implementation;
9. keep commits focused where practical;
10. do not convert branch-history claims into current-source claims without checking files.

For environment division:

- local-machine agents: prioritize V25 install/build/NETLOAD/UI/screenshots/private fixtures/runner-specific failures;
- remote agents: prioritize Core/domain/persistence/reporting/tests/docs/source review and preparing runtime probes;
- remote agents must not claim local runtime success.

Read `AGENTS.md` and `CI_POLICY.md` before changing source or workflows.

---

## 17. Recommended continuation order

### Remote/source agents

1. Sync current `main` and compare against this handoff.
2. Run source review around the feature being changed; do not blindly port old branch files.
3. Strengthen deterministic Core tests/preflight without adding automatic CI triggers.
4. Finish domain logic before adding more Ribbon buttons.
5. Preserve semantic source/generated-handle separation and transaction safety.
6. Improve Recognition rules/review UX only with explicit deterministic evidence and confidence behavior.
7. Improve Revision/BQ/BBS/reporting consistency and recovery guarantees.
8. Prepare small V25 runtime probes/test drawings/scripts for the local agent.

### Local V25 agent

1. Perform Gate C first.
2. Fix actual API/runtime compile errors discovered by the real host.
3. Test BLT-like docked UI and 3D workflows with the private fixture.
4. Capture concrete evidence (host version, exact SHA, probe marker, screenshots safe to share).
5. Feed only reusable/safe code/scripts/docs back into Git; keep proprietary/runtime files out.

### Product polish after Gate C

- close the visual gap to the reference screenshot;
- ensure every visible command has real behavior;
- complete 3D semantics/geometry where users expect manipulable BIM-like objects;
- refine Excel/BBS/revision output;
- then package/sign/release.

---

## 18. “Do not lose these requirements” checklist

- [ ] BricsCAD **V25**, Windows x64, .NET Framework 4.8.
- [ ] Native BricsCAD viewport stays central.
- [ ] BLT3D-like left/right workflow and Vietnamese CAD-style UI.
- [ ] Clean-room; no proprietary BLT dependency/assets/source.
- [ ] No BricsCAD DLLs committed/bundled.
- [ ] No private DWG/DOCX committed.
- [ ] Tường KT is a core requirement.
- [ ] HT_Phòng is a core requirement.
- [ ] Cửa/Lỗ mở is a core requirement.
- [ ] BQ → real Excel output is a core requirement.
- [ ] Dầm/Sàn/Cột/Vách/Móng/Đào đắp and structural quantities remain first-class categories.
- [ ] Recognition must be confidence/review based, not silent AI truth.
- [ ] Rebar/BBS stays deterministic; physical rebar geometry is separate work.
- [ ] Revision must preserve meaningful before/after quantity data and Locate.
- [ ] Model Health should block/flag bad semantic data rather than hiding it.
- [ ] Save As / multi-document identity must not lose semantic project state.
- [ ] Generated geometry must be transaction-safe and separate from source handles.
- [ ] Xref detach must not delete the source file.
- [ ] Finish untracking must not erase CAD geometry.
- [ ] Undefined units must never be silently presented as known units.
- [ ] Sync concurrent `main` before work and before push.
- [ ] Never force-push over another agent.
- [ ] GitHub Actions remain manual-only unless the owner explicitly requests a run.
- [ ] Static/Core CI is not V25 runtime verification.
- [ ] Do not claim NETLOAD/runtime/screenshot success until a real licensed V25 session proves it.

---

## 19. Agent start protocol

When another agent receives this file, the safest first actions are:

```text
1. Read AGENTS.md
2. Read CI_POLICY.md
3. Fetch latest main
4. Read docs/IMPLEMENTATION-STATUS.md
5. Read docs/REVIEW-2026-08-10.md
6. Read this handoff
7. Inspect current files for the specific feature
8. Compare recent main commits after the handoff snapshot SHA
9. Decide whether task is Core/remote-safe or requires real V25
10. Implement only after that reconciliation
```

If the task requires BricsCAD API behavior that cannot be proven from source, leave an explicit runtime gate instead of inventing a result.

---

## 20. Evidence ledger / notable IDs and paths

### Session review proof

- accessible session records reviewed: **377 / 377**;
- paging offsets: `0..360` in increments of 20;
- final page: `0 remaining`;
- targeted project-history retrievals: **2**;
- current GitHub source audit performed after history retrieval.

### Current main snapshot before this documentation commit

- `db4e5dd2ae2d4cf64450be8906fc0d50b3636a3d`
- `fix(runtime): preserve projects across Save As and active DWG sync`

### Current high-value files

- `AGENTS.md`
- `CI_POLICY.md`
- `docs/IMPLEMENTATION-STATUS.md`
- `docs/REVIEW-2026-08-10.md`
- `docs/UI-SPEC.md`
- `docs/V25-RUNNER.md`
- `scripts/preflight.py`
- `scripts/install-bricscad-v25.ps1`
- `scripts/test-bricscad-v25-runtime.ps1`
- `src/QS3D.BricsCAD.V25/Commands.cs`
- `src/QS3D.BricsCAD.V25/ReviewCommands.cs`
- `src/QS3D.BricsCAD.V25/RuntimeProbeCommands.cs`
- `src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs`
- `src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs`
- `src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs`
- `src/QS3D.Core/Recognition/RecognitionEngine.cs`
- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs`

### Historical CI/run references

- `31341101835` — baseline Core PASS (recorded by current docs)
- `31341548469` — persistence/export hardening PASS (recorded by current docs)
- `31341704360` — hardening snapshot PASS (recorded by current docs)
- `31341184031` — BricsCAD V25 Gate C historically queued/no matching runner
- `31343750300`, `31343984922`, `31343166796`, `31344694425` — additional temporary/integration branch gate history; verify exact head SHA before citing as evidence

### Important historical names that are not current source-of-truth

- old `DomainExtensionsCommands.cs`: not present on current main at this audit;
- old `DomainHubWindow.xaml` / `QS3DDOMAIN`: not present on current main at this audit;
- old `RebarCsvExporter.cs` / `QS3DBBSCSV`: not present on current main at this audit;
- old `QS3DSTRUCTSOLID`: current source instead exposes `QS3DBUILD3D` in `ReviewCommands.cs`.

---

## 21. Final handoff statement

The important product intent is stable: build a **real BricsCAD V25 quantity/BIM workflow plugin**, visually and operationally familiar to the provided BLT3D reference, while remaining a clean-room original implementation. The project is already far beyond a UI mockup: it has semantic project data, persistence/recovery, deterministic regeneration, structural categories, Tường KT/HT_Phòng/Cửa workflows, BQ/XLSX, BBS, Recognition, Revision, Health, Xref/layer and native-geometry infrastructure.

The next major truth gate is not another speculative refactor; it is **current-main compile + NETLOAD + interactive runtime validation on a real licensed BricsCAD V25 Windows environment**, followed by fixes based on what that host actually reports.

Until that Gate C succeeds on a recorded current SHA, keep the wording precise: **implemented/reviewed in source** is not the same as **runtime-verified in BricsCAD V25**.
