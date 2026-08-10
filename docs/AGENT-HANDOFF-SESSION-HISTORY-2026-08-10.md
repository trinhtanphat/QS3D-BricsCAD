# QS3D-BricsCAD — Exhaustive session/history handoff for next agents

**Audit date:** 2026-08-10 (UTC+7)  
**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Source-of-truth branch:** `main`  
**Code history reconciled in this review through:** `c987b34ce4eb1d15fd6928913571f521624ae0c7` (`fix(cad): reject erased handles in selection and health`)  
**Important preceding hardening commits reviewed:** `93539132851ac2cbc89d7050203743224b8bf967`, `9f82b2d7c5ded4b6bc749b4dc319423aa3604f76`, `7daf2595dbe318dce1ae4f39b0102a1128227a67`, `dc28dc8f69bf037709ca82a371efcb7349462b26`, `659fa8f07def68ac4257ccadd78c54e77b20b802`, `db4e5dd2ae2d4cf64450be8906fc0d50b3636a3d`  
**Purpose:** preserve the complete important context from the current ChatGPT development session and relevant recovered QS3D/BLT3D history so another agent can continue without rediscovering requirements, repeating mistakes, overwriting concurrent work, or making false runtime claims.

> **Concurrency note:** this repository is actively modified by multiple agents. During creation of this handoff, `main` advanced several times. Every concurrent code commit detected through `c987b34...` was read and incorporated into this canonical version. Do not interpret the reconciliation SHA as “forever latest main”; fetch `main` again before work and inspect commits newer than it. Commits created after this cutoff are new project history, not omitted history from this review.

---

## 1. Review coverage and proof

This handoff was not written from a short summary or repository docs alone.

### 1.1 Current-session coverage

The accessible current-session event/history stream was read sequentially in pages of 20 at offsets:

`0, 20, 40, 60, 80, 100, 120, 140, 160, 180, 200, 220, 240, 260, 280, 300, 320, 340, 360`.

The final page exhausted the stream and reported **0 remaining records**. Total accessible session records reviewed: **377 / 377**.

This is the auditable basis for saying **100% of the accessible current session** was reviewed. It is intentionally narrower than “100% of every chat ever in the account”: deleted chats, inaccessible conversations, or other account-wide material not exposed to this session cannot honestly be certified.

### 1.2 Prior project-history coverage

Two additional targeted history retrievals were performed for earlier **QS3D / BLT3D / BricsCAD V25** work. They recovered older requirements, decisions, screenshots, source milestones, private-fixture notes, CI/runner constraints and previous branch/gate history.

### 1.3 Current-repository reconciliation

Conversation/history was reconciled against real GitHub source instead of trusting old chat claims. The audit inspected the live `main` tree and key files/commits including:

- `AGENTS.md`;
- `CI_POLICY.md`;
- `docs/IMPLEMENTATION-STATUS.md`;
- `docs/REVIEW-2026-08-10.md`;
- `Commands.cs`;
- `ReviewCommands.cs`;
- `RuntimeProbeCommands.cs`;
- `RecognitionEngine.cs`;
- `RevisionSnapshotStore.cs`;
- Recognition/Revision WPF windows;
- Save-As/document-lifecycle hardening `db4e5dd...`;
- migration/family/takeoff/persistence hardening `659fa8f...`;
- Model Health required-dimension hardening `dc28dc8...`;
- finite/overflow-safe reporting hardening `7daf259...`;
- Recognition token-boundary + host-unlink safety regression `9f82b2d...`;
- Generated Solid3d ownership/liveness Health hardening `9353913...`;
- erased-handle rejection / live Entity validation `c987b34...`.

Where an old branch/chat state disagrees with current source, **current `main` wins**. Historical branch-only work is called out separately below.

---

## 2. Owner intent that must not be lost

The target is an original, clean-room **BLT3D-like quantity takeoff / semantic BIM management plugin for BricsCAD V25**. It is a Windows desktop CAD plugin, not a web replacement for BricsCAD.

Recurring owner requirements:

1. Build for **BricsCAD V25** and make workflow/UI close to the supplied BLT3D references.
2. Keep the **native BricsCAD viewport in the center**; do not reimplement the CAD viewport just to imitate the screenshot.
3. Use docked WPF palettes around the native viewport plus BricsCAD Ribbon/commands.
4. Visible functions must be real, not decorative placeholders.
5. Review the entire source carefully, fix bugs/missing behavior, and test carefully.
6. Never call a BricsCAD path runtime-tested merely because source/static/Core checks are green.
7. Prefer remote/GitHub work for Core/source and reserve a real Windows V25 environment for tasks that actually require BricsCAD.
8. Multiple agents may work concurrently; never discard another agent's newer `main` work.
9. GitHub Actions are not implied by “continue all”, commits, pushes, merges or docs; CI is owner-controlled/manual-only.
10. Continue implementing broadly, but retain clean architecture and quality gates.

---

## 3. Clean-room BLT3D boundary

Public research found an official-looking BLT Software site (`thangblt.com` / `www.thangblt.com`) describing BLT3D as a 3D quantity/BIM-oriented product, plus public references that BLT3D runs on BricsCAD and can export quantity explanations.

No public GitHub source matching BLT3D + BricsCAD was found in the research performed for this project. Therefore:

- treat BLT3D as proprietary unless lawful reusable source/license is explicitly supplied;
- keep QS3D an **independent clean-room implementation**;
- do not copy BLT DLLs/source/icons/assets/license material;
- do not make QS3D depend on a `BLT` installation folder;
- owner-provided BLT installation material may help lawful compatibility/workflow study only;
- never request or commit license keys/secrets.

The user-supplied screenshot/workflow is sufficient as a behavioral/layout reference without proprietary code copying.

---

## 4. BricsCAD V25 technical baseline

Baseline checked against Bricsys V25 developer documentation:

- **.NET Framework 4.8**;
- Windows **x64**;
- primary managed references `BrxMgd.dll` + `TD_Mgd.dll`;
- optional `TD_MgdBrep.dll` / `TD_MgdDbConstraints.dll` only when needed;
- BricsCAD assemblies stay external (`Copy Local = False`) and are not bundled;
- application contract `Teigha.Runtime.IExtensionApplication`;
- commands via `Teigha.Runtime.CommandMethodAttribute`;
- WPF docking through `Bricscad.Windows.PaletteSet` / `AddVisual`;
- exact API/runtime behavior still requires a real installed V25 host.

Keep CAD adapter thin:

```text
BricsCAD V25
    |
QS3D.BricsCAD.V25 adapter (thin)
    |
normalized semantic model
    |
QS3D.Core / Geometry / Takeoff / Formula / Rebar / Persistence / Reporting
```

Business/domain logic should not be buried inside Ribbon/CAD command classes. This also preserves future AutoCAD/ZWCAD/GstarCAD adapter possibilities.

---

## 5. BLT3D screenshot/UI contract

The supplied dark BLT3D reference had top tabs approximately:

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

### Left dock expectations

- Zone/Floor selectors.
- Tree/workspaces broadly including:
  - `Lưới Trục`
  - `HT_Phòng`
    - `Phòng`
    - `Sàn Hoàn Thiện`
    - `Chống Thấm`
    - `Chân Tường`
    - `Hoàn Thiện Tường`
    - `Trần Hoàn Thiện`
    - `Lan Can`
  - `Dầm`, `Sàn`, `Cột`, `Vách`
  - `Tường KT`
    - `Tường Gạch`
    - `Vách Kính`
    - `Trụ Tường`
  - `Cửa`
    - `Lỗ Mở Vách`
    - `Cửa Đi`
  - `Cầu Thang`, `Móng`, `Đào đắp`, `KL Tùy chỉnh`
- Family/type/element list.
- Property inspector/editor.

### Center

- **Native, fully interactive BricsCAD 2D/3D viewport**.
- Do not replace it with a custom WPF/OpenGL imitation.

### Right dock

- `Quản lý bản vẽ` / Xref workflow.
- `Quản lý lớp` / layer search/visibility workflow.

### Visual language

- dark charcoal CAD UI;
- blue primary accent;
- destructive red only where appropriate;
- compact palettes so central viewport remains useful;
- BQ/Recognition/Revision/Health/BBS dialogs share the same dark design system;
- test real DPI scaling at 100%, 125%, 150%, 200% before claiming screenshot parity.

The user also explicitly expects the 3D model to be manipulable in BricsCAD. Visual similarity alone is not completion.

---

## 6. Requirement DOCX and private DWG evidence

A requirement DOCX inspected earlier contained:

```text
HOÀN THIỆN:
BIULD CHỨC NĂNG
TƯỜNG KT
HT_PHÒNG
Cửa:
OUTPUT: xuất khối lượng sang excel
```

It also contained embedded reference images. Direct priorities from that source:

- **Tường KT**;
- **HT_Phòng**;
- **Cửa / Lỗ mở**;
- **BQ → Excel**.

Private fixture referenced:

`260808.SHOP XAY TUONG_NHA NOI TRU.dwg`

Historical inspection found it roughly 22 MiB and an older DWG format with AutoCAD/BricsCAD/ODA-era metadata. It must remain a private runtime fixture and **must not be committed to the public repository**. Same principle applies to owner private DOCX/DWG material unless explicitly changed later.

---

## 7. Initial foundation and lessons

Early clean-room foundation established:

- `QS3D.Core` on `netstandard2.0`;
- BricsCAD adapter on `net48`, WPF, x64;
- polyline length/area metrics;
- drawing-unit normalization;
- Count/Length/Area/Volume quantity engine;
- recursive-descent formula evaluator with arithmetic, variables, parentheses and functions (`abs`, `ceil`, `floor`, `round`, `min`, `max`);
- division-by-zero / unknown variable/function errors;
- rebar notation examples `4Ø20`, `4D20`, `Ø8a150`, `D8@150`, `2Ø18+2Ø20`, later `3x4Ø16`;
- theoretical rebar mass `d²/162` kg/m;
- early WPF palette shell, commands, deterministic package-free smoke runner and preflight.

Important lessons:

- A naive delimiter checker falsely flagged `ExpressionEvaluator.cs` because it did not understand char literals; lexical-aware checking replaced it. Do not reintroduce simplistic syntax heuristics.
- The early environment had no real `dotnet`/MSBuild/BricsCAD runtime, so historical static/preflight PASS does not equal plugin compile/NETLOAD PASS.
- Risky/uncertain early BricsCAD entity metadata (`ColorIndex`, `Linetype`, `Visible`) and unnecessary `CommandFlags.Redraw` usage were removed. Keep CAD API usage conservative/runtime-backed.

---

## 8. Semantic domain architecture

The codebase evolved from a takeoff shell to an offline-first semantic project model. Keep these concepts coherent:

- Project
- Zone
- Floor
- Family / Type
- semantic Element
- source CAD handles
- generated CAD handles/geometry metadata
- family + instance properties
- deterministic derived quantities
- dirty flags and regeneration
- dependencies/host links
- revision snapshots
- health diagnostics

Source CAD geometry and generated geometry must remain distinguishable. Core business logic belongs in `QS3D.Core`; adapter code captures live CAD state and performs controlled native CAD changes.

---

## 9. Mainline hardening sequence reviewed during this handoff

### 9.1 `db4e5dd...` — Save As / active-DWG lifecycle hardening

- project cache moved away from mutable document-name keys toward `Document` identity;
- drawing identity synchronizes after Save As;
- prevents Save As from orphaning in-memory semantic project state;
- selection synchronization ignores inactive documents;
- compact left PaletteSet target replaces older oversized minimum width;
- preflight XAML handler coverage includes `SelectedItemChanged`;
- required-file checks expanded for context/selection/palette code.

Future lifecycle refactors must preserve Save-As state, active-document correctness and multi-document isolation.

### 9.2 `659fa8f...` — legacy migration / family inheritance / takeoff hardening

- v1→v2 legacy elements receive missing `dirty = ElementDirtyFlags.All` and legacy `updatedUtc`, forcing deterministic recalculation rather than treating migrated quantities as clean;
- QSDB validates in-memory project state before replacing persisted data;
- atomic replacement/recovery helper remains required;
- non-finite quantity/floor state is rejected;
- family reassignment refreshes inherited defaults while preserving explicit instance overrides;
- legacy wall quantity path rejects non-finite dimensions/overflow;
- raw snapshot takeoff rejects negative/non-finite metrics;
- `ContinuationRegressionSmoke.cs` covers migration dirtying, family inheritance, QSDB non-finite rejection, legacy wall validation and bad snapshot metrics;
- preflight requires those protections.

### 9.3 `dc28dc8...` — Model Health required semantic dimensions

`ModelHealthService` gained category-specific dimension integrity checks and regression coverage:

- Architectural/Glass/Structural wall-like elements: positive finite `LengthM`, `HeightM`, `ThicknessM`;
- Beam: `LengthM`, `WidthM`, `HeightM`;
- Slab: `AreaM2`, `ThicknessM`;
- Column: `WidthM`, `HeightM`; optional `DepthM` valid if provided;
- Foundation: valid base area (`BaseAreaM2`/`AreaM2`) and thickness/height;
- Stair: `AreaM2`, `ThicknessM`;
- Railing: `LengthM`;
- Earthwork: excavation/area + `DepthM`;
- Door/WallOpening: `WidthM`, `HeightM`;
- missing required data → `MISSING_DIMENSION`;
- malformed/non-positive/non-finite data → `INVALID_DIMENSION`.

Do not weaken these checks simply to make incomplete elements appear healthy.

### 9.4 `7daf259...` — finite/overflow-safe quantity reporting

Quantity aggregation was hardened so BQ/reporting does not blindly use unchecked `+=`:

- count increments use guarded count math;
- quantity inputs are checked finite;
- accumulated totals go through `QuantityReportMath`;
- invalid/non-finite totals fail explicitly instead of poisoning reports with NaN/Infinity;
- both semantic `ProjectQuantityReportBuilder` and legacy/instance `QuantityReportBuilder` paths were hardened;
- dedicated `QuantityReportMath` + regression coverage added.

This complements `659fa8f` boundary validation: invalid values are rejected both before persistence/takeoff and during report aggregation.

### 9.5 `9f82b2d...` — Recognition token boundaries + safe host unlink

- Recognition no longer uses arbitrary substring containment for terms. Because Vietnamese `Dầm` normalizes to `dam`, text such as `DAMAGE` must not be scored as Beam merely for containing `dam`; matching uses normalized whole token/term boundaries.
- valid examples such as `KC-DAM` / `Dầm chính` still recognize Beam at high confidence;
- `HostLinkService.UnlinkOpening` validates target really is Door/WallOpening before removing host/dependency state;
- `LogicRegressionSmoke.cs` covers both behaviors.

Future Recognition changes must preserve token semantics.

### 9.6 `9353913...` — generated Solid3d ownership/liveness Health

- `CadHandleService.GetLiveSolidHandles` resolves handles and confirms current live non-erased `Solid3d` objects;
- `QS3DHEALTH` separates semantic source handles from `GeneratedSolidHandle` values;
- invalid/non-hex generated handle is an error;
- duplicate generated-handle ownership across semantic elements is an error;
- generated handle appearing in `SourceHandles` is an error;
- generated category missing/mismatched is surfaced;
- missing/non-Solid3d generated object yields `GENERATED_SOLID_MISSING`;
- Health Locate can select the generated solid for generated-geometry issues.

### 9.7 `c987b34...` — reject erased CAD handles

This commit tightened the boundary between “a handle can be parsed/resolved” and “it refers to a currently valid selectable entity”:

- `CadHandleService.Resolve` now opens resolved objects as `Entity` and rejects erased/non-Entity targets instead of considering an ObjectId sufficient;
- selection and Model Health live-handle sets therefore stop treating erased entities as live source geometry;
- generated liveness continues to verify actual `Solid3d` type, not merely any Entity;
- preflight now explicitly requires Entity-open + `!IsErased`, `GetLiveSolidHandles`/`Solid3d`, generated-health contracts and related regression hooks.

Future handle utilities must preserve this rule: **parseable ObjectId ≠ live selectable semantic source**.

---

## 10. Current implemented behavior confirmed from source/docs

### 10.1 Project / persistence

- BricsCAD V25 `net48/x64` adapter with external BricsCAD references;
- Project/Zone/Floor/Family/semantic Element model;
- active QSDB schema described by current status as **v2 with deterministic v1→v2 migration**;
- validated temp writes, replacement/backup strategy and locking;
- corrupted-primary fallback to valid backup where possible;
- protected recovery state refusing unsafe overwrite;
- dirty/UTC persistence + validation rather than silent coercion;
- XML DTD/external resolver protection and size limits;
- post-`659fa8f` non-finite state validation before replace;
- legacy v1 elements marked dirty so migration cannot make stale quantities appear clean.

Older branches discussed other schema-version ideas. Do not revive an old “v3” claim unless current migrator/source is intentionally changed/tested.

### 10.2 Deterministic regeneration

- dependency graph / dirty propagation;
- bounded fixed-point multi-pass regeneration;
- later pass can recalculate a host dirtied after its first pass;
- explicit `QS3DREGEN`;
- BQ/BBS/Refresh regenerate dirty deterministic semantic quantities before consuming them.

### 10.3 Principal commands (`Commands.cs`)

- `QS3D`, `QS3DHIDE`, `QS3DRIBBON`
- `QS3DINSPECT`
- `QS3DBQ`, `QS3DBBS`, `QS3DREGEN`
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`
- `QS3DTAKEOFF`
- `QS3DWALL`, `QS3DROOM`, `QS3DOPENING`, `QS3DDOOR`
- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`
- `QS3DFOUNDATION`, `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`
- `QS3DLINKHOST`, `QS3DFINISH`, `QS3DHEALTH`, `QS3DLOCATE`
- `QS3DRESETUI`, `QS3DSAFEMODE`, `QS3DABOUT`

### 10.4 Review / recognition / revision / native 3D (`ReviewCommands.cs`)

- `QS3DBUILD3D`
- `QS3DBBSVIEW`
- `QS3DRECOGNIZE`
- `QS3DRECOGNIZEAUTO`
- `QS3DREVBASE`
- `QS3DREVDIFF`

`QS3DBUILD3D` routes architectural walls to `WallSolidBuilder` and supported structural categories to `StructuralSolidBuilder`; current flow focuses native 3D on Tường KT, Dầm, Sàn, Cột, Vách BTCT and Móng.

### 10.5 Tường KT / host / openings / generated geometry

- semantic Tường KT capture;
- selected plan LINE → native `Solid3d` wall path;
- generated-geometry replacement transaction/two-phase hardening;
- generated handles separated from semantic source handles;
- stale generated handles should not erase unrelated entities;
- Door/Opening host linking provides deterministic host quantity deduction;
- re-hosting dirties old/new dependencies;
- unlink validates category before host-link mutation;
- Model Health checks generated Solid3d handle format, ownership, category consistency, source/generated separation and liveness;
- live source resolution rejects erased/non-Entity targets after `c987b34`.

Semantic opening deduction is not yet equivalent to physical boolean subtraction of opening solids from host solids.

### 10.6 HT_Phòng

Semantic generation exists for floor finish, waterproofing, skirting, wall finish and ceiling finish. Finish-specific untracking must remove only finish semantic records and **must not erase CAD geometry**. Keep generic and finish-only untracking distinct.

### 10.7 BQ / Excel

- semantic quantity aggregation/reporting;
- stable IDs where appropriate to prevent duplicate-name collisions;
- BQ filters / Locate / real recalculation;
- `Tính lại` regenerates rather than only reapplying filters;
- fallback snapshot takeoff can use live `INSUNITS` instead of hard-coded mm;
- unsupported/undefined units should surface explicit fallback warning;
- real `.xlsx` export with header/filter/freeze behavior;
- raw snapshot metrics reject negative/non-finite values;
- report aggregation rejects non-finite/overflowing inputs/totals.

A historical full-domain branch temporarily added `Thép (kg)` and changed a tested range from `A1:P2` to `A1:Q2`; that found a real stale-test bug. Inspect the **current** exporter/tests before assuming that experimental shape is active today.

### 10.8 Rebar / BBS

- deterministic notation parser and validation;
- rejection of non-positive diameter/spacing/count;
- count/compound/spacing notation;
- deterministic bar mark/shape/cutting length and allowances;
- lap/anchor/hook/waste concepts;
- kg/m, total length, total weight;
- semantic `Rebar*` property adapter;
- `QS3DBBS` real XLSX export;
- `QS3DBBSVIEW` modeless schedule + Locate.

Historical `RebarCsvExporter.cs` / `QS3DBBSCSV` is **not present on current `main` at this audit**. Do not advertise it unless deliberately reimplemented.

### 10.9 Recognition

Current `RecognitionEngine` is deterministic/rule-based:

- layer terms;
- text/block/tag metadata terms;
- entity-type compatibility;
- Vietnamese diacritic normalization (`đ`→`d`, combining marks removed);
- scoring approximately layer `+0.62`, text `+0.28`, compatible type `+0.10`;
- whole normalized token/term boundary matching, preventing short-term substring false positives such as `dam` inside `damage`;
- review when confidence low or top-candidate margin narrow;
- batch auto-accept defaults near confidence `0.92`, margin `0.15`;
- default rules for Beam, Slab, Column, StructuralWall, ArchitecturalWall, Opening, Door, Room, Foundation, Stair, Railing, Earthwork.

Adapter exposes `QS3DRECOGNIZE` / `QS3DRECOGNIZEAUTO`; Recognition UI shows Handle, Entity, Layer, suggestion, confidence, margin, review flag/evidence and Apply/Locate.

Recognition remains **suggestion + review**, not silent AI authority.

### 10.10 Revision

Current `RevisionSnapshotStore` includes:

- properties/quantities/source handles/floor/zone/family/category metadata;
- finite-number validation;
- XML DTD prohibition / resolver null / max size;
- temp save + backup/atomic replacement;
- backup fallback on recoverable failure;
- duplicate ID/property/quantity rejection.

Adapter exposes `QS3DREVBASE` / `QS3DREVDIFF`; Revision UI shows Before/After/Delta/% + Locate.

### 10.11 Model Health

Current Health path includes:

- source reference/handle/dependency integrity;
- structural material inheritance;
- rebar definition/distribution/length validation;
- category-specific required-dimension validation (`MISSING_DIMENSION` / `INVALID_DIMENSION`);
- generated Solid3d handle/ownership/category/source-separation/liveness validation;
- erased CAD objects rejected from the live-source set.

Do not weaken Health to make incomplete/stale semantic/native geometry look valid.

### 10.12 Xref / Layer / selection

- live Xref listing and LayerTable search/show/hide;
- direct Xref reload/detach service where current source supports it;
- row selection can synchronize implied CAD selection;
- active-document filtering matters in multi-document sessions;
- `Gỡ Xref` means detach, not delete external source file;
- handle-based Locate/select rejects erased/non-Entity targets;
- Save-As/document identity and active-DWG synchronization explicitly hardened in `db4e5dd...`.

### 10.13 Family assignment/inheritance

Family reassignment refreshes inherited family defaults **without overwriting explicit instance overrides**. This is a regression/preflight expectation.

### 10.14 Runtime probe

`QS3DRUNTIMEPROBE` exists. With `QS3D_RUNTIME_RESULT` set it checks x64, opens palettes, attempts Ribbon initialization and writes PASS/FAIL marker data including process/host/CLR/assembly information.

Source existence is **not evidence that a licensed V25 run has passed**.

---

## 11. Historical branch work vs current `main`

Several full-domain features were developed on temporary integration branches. Some were absorbed under different names/files; some did not survive exactly.

Reconciliation:

- old `DomainExtensionsCommands.cs` is not on current main;
- current review functionality mainly lives in `ReviewCommands.cs`;
- old `QS3DSTRUCTSOLID` was superseded by `QS3DBUILD3D`;
- `QS3DRECOGNIZE` / `QS3DRECOGNIZEAUTO` exist currently;
- `QS3DREVBASE` / `QS3DREVDIFF` exist currently;
- `QS3DBBSVIEW` exists; standard BBS XLSX remains `QS3DBBS`;
- old `QS3DBBSCSV` / `RebarCsvExporter.cs` absent at this audit;
- old `DomainHubWindow.xaml` / `QS3DDOMAIN` experiment absent at this audit;
- `RecognitionEngine`, durable revision storage and `StructuralSolidBuilder.cs` are present.

Never convert “implemented in a historical branch” into “available on current main” without checking files.

---

## 12. CI/compiler/regression history

### 12.1 Policy

`CI_POLICY.md` is authoritative:

- release `main` workflows are **manual-only**;
- `workflow_dispatch` only unless owner explicitly changes policy;
- no automatic `push`, `pull_request`, `schedule`, `workflow_run`, etc.;
- commit/push/merge/review/docs/“continue all” is **not** permission to run Actions;
- static/preflight PASS ≠ GitHub CI PASS;
- GitHub Core CI PASS ≠ BricsCAD runtime PASS.

This documentation task did **not** authorize CI dispatch.

### 12.2 Historical verified Core runs recorded by current docs

- `31341101835` — baseline Core CI PASS;
- `31341548469` — persistence/export hardening PASS;
- `31341704360` — hardening snapshot PASS.

Each verifies its own exact commit snapshot only.

### 12.3 Other temporary/session gate IDs

- `31343750300` — intermediate Core gate reported passing;
- `31343984922` — Core union gate reported PASS after fixes;
- `31343166796` — branch release-tree gate reported success;
- `31344694425` — temporary full-domain gate observed during integration.

These are branch-history evidence. Map `head_sha` before relying on one for a current claim.

### 12.4 Bugs the gates/reviews caught

- nullable/compiler problems in recognition/rebar integration;
- old-framework nullable analysis requiring explicit safe handling after `IsNullOrWhiteSpace`;
- BQ/Excel schema test stale at `A1:P2` when experimental branch required `A1:Q2`;
- repeated tightening of preflight/XAML handler/required-tree guards;
- Save-As/active-document lifecycle guards;
- legacy migration dirtying + family inheritance + QSDB non-finite + legacy wall/takeoff regressions;
- required semantic dimension Health regressions;
- non-finite/overflow-safe report aggregation;
- Recognition false-positive regression (`DAMAGE` must not match normalized `dam` Beam term);
- Host unlink mutation guard;
- generated Solid3d ownership/liveness/category/source-separation Health checks;
- erased CAD handles must not be accepted as live/selectable source objects.

Do not “fix CI” by disabling nullable, broad-suppressing compiler issues or weakening assertions merely to get green.

---

## 13. Gate C: real BricsCAD V25 blocker

Historical V25 workflow run `31341184031` was recorded queued because no matching self-hosted runner was assigned for labels approximately:

`[self-hosted, windows, x64, bricscad-v25]`

Therefore do **not** claim current-main completion of:

- full adapter compile against exact installed V25 assemblies;
- `NETLOAD`;
- Ribbon runtime compatibility;
- Palette docking/focus/DPI behavior;
- native Solid3d correctness on private real DWG;
- transaction/undo behavior under actual CAD failures;
- Xref runtime operations;
- visual parity at multiple DPI scales;
- exact V25.1/V25.2 API differences;
- physical geometric rebar;
- physical opening boolean subtraction;
- clean-machine installer/package qualification.

BricsCAD proprietary assemblies/private fixtures stay outside Git.

---

## 14. Runtime checklist for a local V25 agent

1. Fetch latest `main`; record exact SHA.
2. Verify installed V25 path/managed assemblies without copying them into Git.
3. Build Release x64/net48 against exact V25 references.
4. `NETLOAD` QS3D.
5. Run `QS3DRUNTIMEPROBE` with `QS3D_RUNTIME_RESULT`; retain safe evidence.
6. Verify docked left/right palettes and native center viewport.
7. Test Windows scaling 100%, 125%, 150%, 200%.
8. Test document create/activate/close and **Save As**; project state/identity must persist.
9. Test Xref selection sync, Move/Reload/Detach; source file must not be deleted.
10. Capture Tường KT LINE in mm and at least one supported non-mm drawing; build/update 3D and verify dimensions/location.
11. Rebuild same source; exactly one current generated solid should remain.
12. Delete/erase/replace generated/source objects and verify Health/Locate never treats erased handles as live, and catches generated missing/non-Solid3d/duplicate/category/source-ownership problems.
13. Force invalid source/dimension; old valid geometry/project metadata must remain consistent.
14. Test `QS3DBUILD3D` for supported Dầm/Sàn/Cột/Vách BTCT/Móng source forms.
15. Test Door/Opening host linking/unlinking and semantic deduction; non-opening unlink must fail without mutation.
16. Generate HT_Phòng; finish-only untracking must never erase CAD geometry.
17. Edit family dimensions/reassignment; verify inherited defaults vs explicit instance overrides; run BQ `Tính lại`.
18. Export BQ XLSX/BBS XLSX; inspect values, finite totals, units, headers, filters/freeze panes.
19. Run Recognition on confident/ambiguous/false-positive token cases; verify Apply/Locate.
20. Capture revision baseline, modify data, run `QS3DREVDIFF`; verify Before/After/Delta/Locate.
21. Run Model Health with missing/NaN/negative dimensions and stale/erased source/generated handles.
22. Exercise undo/redo around generated native geometry where supported.
23. Only then update runtime status docs/screenshots with exact host version + SHA + evidence.

Never commit proprietary DLLs, private DWGs or sensitive customer screenshots.

---

## 15. Known gaps / future work

Still meaningful/runtime-gated:

- current-main V25 compile/NETLOAD qualification;
- exact screenshot/DPI parity;
- robust wall corners, joins, T-junctions and freeform profiles;
- physical opening booleans;
- automatic room-boundary discovery from arbitrary wall networks;
- richer transient highlight / true zoom-to-extents behavior;
- geometric rebar placement inside BricsCAD (BBS is ahead of physical bars);
- more native structural source forms;
- performance testing on large drawings;
- abnormal shutdown/file-lock/recovery testing;
- installer/code-signing/release qualification;
- optional licensing/update backend if productization requested;
- broader engineering standards/rule configuration;
- full keyboard/focus/accessibility polish;
- future localization architecture if required.

Cloudflare, if added later, should be an **optional backend** for licensing/update metadata/R2 packages/etc., not the host of a Windows .NET Framework CAD plugin. GitHub is appropriate for source/controlled CI/artifacts; QS3D runs inside BricsCAD.

---

## 16. Multi-agent rules

This repo moved repeatedly while this handoff was prepared. Every agent must:

1. fetch latest `main` before work;
2. inspect recent commits relevant to its task;
3. base work on current head, not a SHA copied from chat/docs;
4. fetch/sync again immediately before commit/push;
5. if `main` moved, reapply/rebase/merge without deleting newer work;
6. inspect final diff;
7. never force-push/reset `main` backward;
8. never silently overwrite another agent;
9. prefer focused commits;
10. verify current source before repeating any historical feature claim.

Environment division:

- local-machine agents prioritize V25 install/build/NETLOAD/UI/screenshots/private fixtures/runner behavior;
- remote agents prioritize Core/domain/persistence/reporting/tests/docs/static review/runtime probes;
- remote agents must not claim local V25 runtime success.

Read `AGENTS.md` and `CI_POLICY.md` first.

---

## 17. Recommended continuation order

### Remote/source agents

1. Sync latest main and compare commits newer than this handoff reconciliation point.
2. Review current files; do not blindly resurrect old branch files.
3. Strengthen deterministic Core/tests/preflight without auto-CI triggers.
4. Finish domain behavior before adding Ribbon buttons.
5. Preserve source/generated-handle separation, generated ownership/liveness checks, erased-handle rejection and transaction safety.
6. Preserve Recognition token-boundary matching and confidence/review behavior.
7. Preserve host-link/unlink category guards.
8. Improve Revision/BQ/BBS/reporting consistency/recovery.
9. Keep persistence/Health/report finite-value guards intact.
10. Prepare focused V25 probes/tests for local agent.

### Local V25 agent

1. Perform Gate C first.
2. Fix actual API/runtime compile failures discovered by host.
3. Test BLT-like docked UI + 3D workflow with private fixture.
4. Capture safe evidence: exact SHA, host/build, probe marker, screenshots.
5. Commit reusable/safe source/scripts/docs only.

### After Gate C

- close visual gap to BLT3D screenshot;
- ensure every visible command has real behavior;
- finish expected 3D semantic/native geometry;
- polish Excel/BBS/revision output;
- package/sign/release.

---

## 18. “Do not lose these requirements” checklist

- [ ] BricsCAD **V25**, Windows x64, .NET Framework 4.8.
- [ ] Native BricsCAD viewport stays central.
- [ ] BLT3D-like left/right workflow and Vietnamese CAD UI.
- [ ] Clean-room; no proprietary BLT source/assets/dependency.
- [ ] No BricsCAD DLLs committed/bundled.
- [ ] No private DWG/DOCX committed.
- [ ] Tường KT is core.
- [ ] HT_Phòng is core.
- [ ] Cửa/Lỗ mở is core.
- [ ] BQ → real Excel is core.
- [ ] Dầm/Sàn/Cột/Vách/Móng/Đào đắp remain first-class.
- [ ] Recognition is confidence/review based and token-boundary safe.
- [ ] Rebar/BBS deterministic; physical rebar geometry is separate.
- [ ] Revision preserves meaningful before/after quantities + Locate.
- [ ] Model Health exposes bad semantic dimensions and stale/misowned generated geometry.
- [ ] Save As / multi-document identity must not lose project state.
- [ ] Legacy migrated elements must not appear clean with stale quantities.
- [ ] Family reassignment preserves explicit overrides while refreshing inherited defaults.
- [ ] Non-finite/invalid persisted/takeoff/report values must be rejected.
- [ ] Host unlink must not mutate non-opening elements.
- [ ] Generated geometry is transaction-safe, has one semantic owner and stays separate from source handles.
- [ ] Erased/non-Entity CAD objects must not count as live semantic source handles.
- [ ] Xref detach does not delete source file.
- [ ] Finish untracking does not erase CAD geometry.
- [ ] Undefined units are never silently presented as known units.
- [ ] Sync concurrent `main` before work and before push.
- [ ] Never force-push over another agent.
- [ ] GitHub Actions remain manual-only unless explicitly requested.
- [ ] Static/Core CI is not V25 runtime verification.
- [ ] Never claim NETLOAD/runtime/screenshot success without a real licensed V25 run.

---

## 19. Agent start protocol

```text
1. Read AGENTS.md
2. Read CI_POLICY.md
3. Fetch latest main
4. Inspect commits newer than c987b34ce4eb1d15fd6928913571f521624ae0c7
5. Read docs/IMPLEMENTATION-STATUS.md
6. Read docs/REVIEW-2026-08-10.md
7. Read this handoff
8. Inspect current files for the specific feature
9. Decide whether task is remote/Core-safe or needs real V25
10. Implement only after reconciliation
```

If BricsCAD behavior cannot be proven from source, leave an explicit runtime gate instead of inventing a result.

---

## 20. Evidence ledger

### Review proof

- accessible current-session records reviewed: **377 / 377**;
- paging offsets `0..360` in increments of 20;
- terminal read: **0 remaining**;
- targeted prior project-history retrievals: **2**;
- current GitHub source audit/reconciliation performed after history review;
- concurrent `main` races were detected by post-commit verification rather than ignored;
- every detected concurrent code commit through `c987b34...` was read and incorporated into this canonical handoff.

### Mainline hardening commits reconciled

- `db4e5dd2ae2d4cf64450be8906fc0d50b3636a3d` — Save As / active-DWG synchronization.
- `659fa8f07def68ac4257ccadd78c54e77b20b802` — migration/family inheritance/persistence/non-finite/takeoff hardening.
- `dc28dc8f69bf037709ca82a371efcb7349462b26` — category-specific Model Health dimension validation.
- `7daf2595dbe318dce1ae4f39b0102a1128227a67` — finite/overflow-safe quantity report aggregation.
- `9f82b2d7c5ded4b6bc749b4dc319423aa3604f76` — Recognition token-boundary false-positive fix + safe host unlink.
- `93539132851ac2cbc89d7050203743224b8bf967` — generated Solid3d ownership/category/liveness Health validation.
- `c987b34ce4eb1d15fd6928913571f521624ae0c7` — live Entity validation / erased-handle rejection.

### High-value current files

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
- `src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs`
- `src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs`
- `src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs`
- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `src/QS3D.Core/Services/BulkEditService.cs`
- `src/QS3D.Core/Services/WallQuantityCalculator.cs`
- `src/QS3D.Core/Services/HostLinkService.cs`
- `src/QS3D.Core/Takeoff/QuantityEngine.cs`
- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs`
- `src/QS3D.Core/Reporting/QuantityReportBuilder.cs`
- `src/QS3D.Core/Reporting/QuantityReportMath.cs`
- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `src/QS3D.Core/Recognition/RecognitionEngine.cs`
- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs`
- `tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs`
- `tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs`
- `tests/QS3D.Core.SmokeTests/LogicRegressionSmoke.cs`

### Historical run references

- `31341101835` — baseline Core PASS recorded by current docs.
- `31341548469` — persistence/export hardening PASS recorded by current docs.
- `31341704360` — hardening snapshot PASS recorded by current docs.
- `31341184031` — BricsCAD V25 Gate C historically queued/no matching runner.
- `31343750300`, `31343984922`, `31343166796`, `31344694425` — temporary/integration branch history; verify exact `head_sha` before citing.

### Historical names not to treat as current source-of-truth

- `DomainExtensionsCommands.cs`: absent on current main; current equivalent work is under `ReviewCommands.cs`.
- `DomainHubWindow.xaml` / `QS3DDOMAIN`: historical experiment, absent at this audit.
- `RebarCsvExporter.cs` / `QS3DBBSCSV`: historical experiment, absent at this audit.
- `QS3DSTRUCTSOLID`: historical command name; current source exposes `QS3DBUILD3D`.

---

## 21. Final handoff statement

The product intent is stable: build a **real BricsCAD V25 quantity/BIM workflow plugin**, visually and operationally familiar to the supplied BLT3D reference, while remaining an original clean-room implementation.

The project is already much more than a UI mockup: current source contains semantic project data, hardened persistence/recovery/migration, fixed-point regeneration, structural categories, Tường KT/HT_Phòng/Cửa workflows, BQ/XLSX, deterministic BBS, Recognition, Revision, Model Health, Xref/layer/selection integration, native generated-geometry infrastructure, Save-As/document synchronization, family-inheritance safeguards, required-dimension validation, finite-safe reporting, token-safe Recognition, host-link mutation guards, generated Solid3d ownership/liveness checks and erased-handle rejection.

The next major truth gate is a **current-main compile + NETLOAD + interactive validation on a real licensed BricsCAD V25 Windows environment**, followed by fixes based on what that host actually reports.

Until Gate C succeeds for a recorded exact SHA, keep terminology precise: **implemented/reviewed in source** is not the same as **runtime-verified in BricsCAD V25**.
