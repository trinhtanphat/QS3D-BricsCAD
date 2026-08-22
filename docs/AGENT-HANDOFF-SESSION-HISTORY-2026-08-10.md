# QS3D-BricsCAD — Exhaustive session/history handoff for next agents

**Audit date:** 2026-08-10 (UTC+7)  
**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Source-of-truth branch:** `main`  
**Code history reconciled in this review through:** `659fa8f07def68ac4257ccadd78c54e77b20b802` (`fix(core): harden legacy migration family assignment and takeoff`)  
**Previous important runtime/lifecycle commit:** `db4e5dd2ae2d4cf64450be8906fc0d50b3636a3d` (`fix(runtime): preserve projects across Save As and active DWG sync`)  
**Purpose:** preserve the complete important context from the current ChatGPT development session and relevant recovered QS3D/BLT3D history so another agent can continue without rediscovering requirements, repeating mistakes, overwriting concurrent work, or making false runtime claims.

> **Concurrency note:** this repository is being modified by multiple agents. The first handoff commit was created on top of `659fa8f...`, which arrived after an earlier sync at `db4e5dd...`. That race was detected during post-commit verification and this canonical file was corrected. Do not interpret the SHA above as “forever latest main”; fetch `main` again before work and review commits newer than the handoff.

---

## 1. Review coverage and proof

This handoff was not written from a short summary or from repository docs alone.

### 1.1 Current-session coverage

The accessible current-session event/history stream was read sequentially in pages of 20 at offsets:

`0, 20, 40, 60, 80, 100, 120, 140, 160, 180, 200, 220, 240, 260, 280, 300, 320, 340, 360`.

The final page exhausted the stream and reported **0 remaining records**. Total accessible session records reviewed: **377 / 377**.

This is the auditable basis for saying **100% of the accessible current session** was reviewed. It is intentionally narrower than “100% of every chat ever in the account”: chats/history not exposed to this session, deleted conversations, or inaccessible account-wide material cannot honestly be certified.

### 1.2 Prior project-history coverage

Two additional targeted history retrievals were performed for the earlier **QS3D / BLT3D / BricsCAD V25** work. They recovered important older requirements, decisions, screenshots, source milestones, private-fixture notes, CI/runner history and branch/gate history.

### 1.3 Current-repository reconciliation

The conversation/history was reconciled against real GitHub source instead of trusting old chat claims. The audit inspected the live `main` tree and key files including:

- `AGENTS.md`;
- `CI_POLICY.md`;
- `docs/IMPLEMENTATION-STATUS.md`;
- `docs/REVIEW-2026-08-10.md`;
- `Commands.cs`;
- `ReviewCommands.cs`;
- `RuntimeProbeCommands.cs`;
- `RecognitionEngine.cs`;
- `RevisionSnapshotStore.cs`;
- current Recognition/Revision WPF windows;
- Save-As/document-lifecycle hardening (`db4e5dd...`);
- concurrent Core migration/family/takeoff/persistence hardening (`659fa8f...`).

Where an old branch/chat state disagrees with current source, **current `main` wins**. Historical branch-only work is called out separately below.

---

## 2. Owner intent that must not be lost

The target is an original, clean-room **BLT3D-like quantity takeoff / semantic BIM management plugin for BricsCAD V25**. It is a Windows desktop CAD plugin, not a web replacement for BricsCAD.

Recurring owner requirements:

1. Build for **BricsCAD V25** and make workflow/UI close to the supplied BLT3D references.
2. Keep the **native BricsCAD viewport in the center**; do not reimplement the CAD viewport merely to imitate the screenshot.
3. Use docked WPF palettes around the native viewport plus BricsCAD Ribbon/commands.
4. Buttons/functions must be real, not decorative placeholders.
5. Review the entire source carefully, fix bugs/missing behavior, and test carefully.
6. Never call a BricsCAD path runtime-tested merely because source/static/Core checks are green.
7. Prefer remote/GitHub work for Core/source; reserve a real Windows V25 environment for tasks that actually require BricsCAD.
8. Multiple agents may work concurrently; never discard another agent's newer `main` work.
9. GitHub Actions are not implied by “continue all”, commits, merges or docs; CI is owner-controlled/manual-only.
10. Continue implementing broadly, but retain clean architecture and quality gates.

---

## 3. Clean-room BLT3D boundary

Public research found an official-looking BLT Software site (`thangblt.com` / `www.thangblt.com`) describing BLT3D as a 3D quantity/BIM-oriented product, plus public references that BLT3D runs on BricsCAD and can export quantity explanations.

No public GitHub source matching BLT3D + BricsCAD was found in the research performed for this project. Therefore:

- treat BLT3D as proprietary unless a lawful reusable source/license is explicitly supplied;
- keep QS3D an **independent clean-room implementation**;
- do not copy BLT DLLs/source/icons/assets/license material;
- do not make QS3D depend on a `BLT` installation folder;
- an owner-provided BLT installation may help lawful compatibility/workflow study only;
- never request or commit license keys/secrets.

The user-supplied screenshot/workflow is sufficient as a behavioral/layout reference without proprietary code copying.

---

## 4. BricsCAD V25 technical baseline

The development baseline checked against Bricsys V25 developer documentation is:

- **.NET Framework 4.8** for managed V25 plugin code;
- Windows **x64**;
- primary managed references `BrxMgd.dll` + `TD_Mgd.dll`;
- optional `TD_MgdBrep.dll` / `TD_MgdDbConstraints.dll` only when needed;
- BricsCAD assemblies stay external (`Copy Local = False`) and are not bundled in QS3D releases;
- application contract `Teigha.Runtime.IExtensionApplication`;
- commands via `Teigha.Runtime.CommandMethodAttribute`;
- WPF docking through `Bricscad.Windows.PaletteSet` / `AddVisual`;
- exact API/runtime behavior still requires an installed real V25 host.

Keep the CAD adapter thin:

```text
BricsCAD V25
    |
QS3D.BricsCAD.V25 adapter (thin)
    |
normalized semantic model
    |
QS3D.Core / Geometry / Takeoff / Formula / Rebar / Persistence / Reporting
```

Business/domain logic should not be buried inside Ribbon or CAD command classes. This also preserves the possibility of future AutoCAD/ZWCAD/GstarCAD adapters.

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
- compact palettes so the central viewport remains useful;
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

It also contained embedded reference images. Direct priorities from that source are therefore:

- **Tường KT**;
- **HT_Phòng**;
- **Cửa / Lỗ mở**;
- **BQ → Excel**.

A private fixture was referenced:

`260808.SHOP XAY TUONG_NHA NOI TRU.dwg`

Historical inspection found it roughly 22 MiB and an older DWG format with AutoCAD/BricsCAD/ODA-era metadata. It must remain a private runtime fixture and **must not be committed to the public repository**. Same principle applies to owner private DOCX/DWG material unless explicitly changed later.

---

## 7. Initial foundation and lessons

Early clean-room foundation work established:

- `QS3D.Core` on `netstandard2.0`;
- BricsCAD adapter on `net48`, WPF, x64;
- polyline length/area metrics;
- drawing-unit normalization;
- Count/Length/Area/Volume quantity engine;
- recursive-descent formula evaluator with arithmetic, variables, parentheses and functions (`abs`, `ceil`, `floor`, `round`, `min`, `max`);
- errors for division by zero and unknown variables/functions;
- rebar notation examples including `4Ø20`, `4D20`, `Ø8a150`, `D8@150`, `2Ø18+2Ø20`, later `3x4Ø16`;
- theoretical rebar unit mass `d²/162` kg/m;
- early WPF palette shell, commands, deterministic package-free smoke runner and preflight.

Important lessons:

- A naive delimiter checker once falsely flagged `ExpressionEvaluator.cs` because it did not understand char literals; it was replaced with a lexical-aware checker. Do not reintroduce simplistic syntax heuristics.
- The early environment had no real `dotnet`/MSBuild/BricsCAD runtime, so historical static/preflight PASS does not equal plugin compile/NETLOAD PASS.
- Risky/uncertain early BricsCAD entity metadata (`ColorIndex`, `Linetype`, `Visible`) and unnecessary `CommandFlags.Redraw` usage were removed. Keep CAD API usage conservative and runtime-backed.

---

## 8. Semantic domain architecture developed in the session

The codebase evolved from a takeoff shell to an offline-first semantic project model. Keep these concepts coherent:

- Project
- Zone
- Floor
- Family / Type
- semantic Element
- source CAD handles
- generated CAD handles/geometry metadata
- instance/family properties
- deterministic derived quantities
- dirty flags and regeneration
- dependencies/host links
- revision snapshots
- health diagnostics

Source CAD geometry and generated geometry must remain distinguishable. Core business logic belongs in `QS3D.Core`; adapter code captures live CAD state and performs controlled native CAD changes.

---

## 9. Mainline concurrency and the two latest hardening batches reviewed

### 9.1 `db4e5dd...` — Save As / active-DWG lifecycle hardening

This commit moved the live project cache away from mutable document-name keys toward `Document` identity and added drawing-identity synchronization. The intent is to prevent **Save As** from orphaning the in-memory semantic project.

It also hardened:

- selection synchronization so inactive documents do not drive active UI/CAD state;
- compact left PaletteSet sizing (new target replaces an older oversized minimum width);
- preflight XAML handler coverage including `SelectedItemChanged`;
- required-file checks for project context, selection sync and palette coordination.

Future lifecycle refactors must preserve Save-As state, correct active-document behavior and multi-document isolation.

### 9.2 `659fa8f...` — legacy migration / family inheritance / takeoff hardening

This commit arrived concurrently while the handoff was being prepared and is included in this review. Key changes visible in its diff/preflight contracts include:

- v1→v2 legacy element migration supplies missing dirty state as `ElementDirtyFlags.All` and missing legacy `updatedUtc`, forcing deterministic recalculation instead of treating old quantities as clean;
- `QsdbProjectStore` validates in-memory project state **before** replacing persisted data;
- atomic replacement/recovery helper remains required;
- non-finite quantity and floor elevation/state is rejected;
- family reassignment refreshes inherited defaults while preserving deliberate instance overrides;
- legacy wall quantity paths reject non-finite dimensions/overflow;
- raw snapshot takeoff rejects negative/non-finite metrics;
- `ContinuationRegressionSmoke.cs` was added/required to cover migration dirtying, family assignment inheritance, QSDB non-finite rejection, legacy wall validation and bad snapshot metrics;
- `scripts/preflight.py` was extended so those protections cannot disappear silently.

This is why the canonical handoff records `659fa8f...` as the code-history reconciliation point rather than the earlier `db4e5dd...` sync.

---

## 10. Current implemented behavior confirmed from source/docs

### 10.1 Project / persistence

Current source/docs establish:

- BricsCAD V25 `net48/x64` adapter with external BricsCAD references;
- Project/Zone/Floor/Family/semantic Element model;
- active QSDB schema described by current status as **v2 with deterministic v1→v2 migration**;
- validated temp writes, replacement/backup strategy and project locking;
- corrupted-primary fallback to valid backup where possible;
- protected recovery state that refuses unsafe overwrite;
- persisted dirty/UTC state and validation rather than silent coercion;
- XML DTD/external resolver protection and size limits;
- post-`659fa8f` validation of non-finite state before replace;
- legacy v1 elements marked dirty so migration cannot make stale quantities look clean.

Older branch discussions mentioned other schema-version ideas. Do not revive an old “v3” claim unless current migrator/source is intentionally changed and tested.

### 10.2 Deterministic regeneration

- dependency graph / dirty propagation;
- bounded fixed-point multi-pass regeneration;
- later pass can recalculate a host dirtied after its first pass;
- explicit `QS3DREGEN`;
- BQ/BBS/Refresh regenerate dirty deterministic semantic quantities before consuming them.

### 10.3 Current principal commands (`Commands.cs`)

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

### 10.4 Current review/recognition/revision/native-3D commands (`ReviewCommands.cs`)

- `QS3DBUILD3D`
- `QS3DBBSVIEW`
- `QS3DRECOGNIZE`
- `QS3DRECOGNIZEAUTO`
- `QS3DREVBASE`
- `QS3DREVDIFF`

`QS3DBUILD3D` routes architectural walls to `WallSolidBuilder` and supported structural categories to `StructuralSolidBuilder`; current UI/status indicates native 3D focuses on Tường KT, Dầm, Sàn, Cột, Vách BTCT and Móng.

### 10.5 Tường KT / host / openings

- semantic Tường KT capture exists;
- selected plan LINE sources can produce native `Solid3d` walls;
- generated-geometry replacement has transaction/two-phase hardening;
- generated handles are separated from semantic source handles;
- stale generated handles should not erase an unrelated entity;
- Door/Opening host linking provides deterministic host quantity deduction;
- re-hosting dirties old/new dependencies appropriately.

**Not equivalent yet:** semantic opening deduction is not the same as physically boolean-subtracting an opening solid from a host solid.

### 10.6 HT_Phòng

Semantic generation exists for floor finish, waterproofing, skirting, wall finish and ceiling finish. A prior safety fix established that finish-specific untracking removes only finish semantic records and **must not erase CAD geometry**. Keep finish-only and generic untracking behavior distinct.

### 10.7 BQ / Excel

- semantic quantity aggregation/reporting;
- stable IDs used where appropriate to avoid collisions from duplicate display names;
- BQ filters / Locate / real recalculation;
- `Tính lại` regenerates rather than only reapplying UI filters;
- fallback snapshot takeoff can use live `INSUNITS` rather than hard-coded millimeters;
- unsupported/undefined units must be surfaced as an explicit fallback warning;
- real `.xlsx` output with expected header/filter/freeze behavior;
- current post-`659fa8f` Core also hardens raw snapshot metrics against negative/non-finite values.

A historical full-domain branch temporarily expanded BQ with `Thép (kg)` and changed a tested sheet range from `A1:P2` to `A1:Q2`; that caught a real stale-test regression. Inspect the **current** exporter/tests before assuming that exact historical column shape is still active.

### 10.8 Rebar / BBS

Current source/status includes:

- deterministic notation parser + validation;
- rejection of non-positive diameter/spacing/count;
- count/compound/spacing notation;
- deterministic schedule quantities including bar mark/shape/cutting length and allowances;
- lap/anchor/hook/waste concepts;
- kg/m, total length and total weight;
- semantic `Rebar*` property adapter;
- `QS3DBBS` real XLSX export;
- `QS3DBBSVIEW` modeless BBS view + Locate.

Historical experimental `RebarCsvExporter.cs` / `QS3DBBSCSV` is **not present on current `main` at this audit**. Do not advertise it unless it is deliberately reimplemented.

### 10.9 Recognition

Current `RecognitionEngine` is deterministic/rule-based:

- layer terms;
- text/block/tag metadata terms;
- entity-type compatibility;
- Vietnamese diacritic normalization (`đ`→`d`, combining marks removed);
- scoring approximately layer `+0.62`, text `+0.28`, compatible type `+0.10`;
- review when confidence is low or candidate margin is narrow;
- batch auto-accept defaults around confidence `0.92`, margin `0.15`;
- default rules for Beam, Slab, Column, StructuralWall, ArchitecturalWall, Opening, Door, Room, Foundation, Stair, Railing and Earthwork.

Current adapter exposes `QS3DRECOGNIZE` / `QS3DRECOGNIZEAUTO`; Recognition UI shows Handle, Entity, Layer, suggestion, confidence, margin, review flag/evidence and Apply/Locate actions.

Recognition remains **suggestion + review**, never silent AI authority. AI may assist later but should not become the authoritative quantity engine.

### 10.10 Revision

Current `RevisionSnapshotStore` includes:

- element snapshots with properties/quantities/source handles/floor/zone/family/category metadata;
- finite-number validation;
- XML DTD prohibition / resolver null / max size;
- temp save + backup/atomic replacement;
- backup fallback on recoverable failure;
- duplicate ID/property/quantity rejection.

Current adapter exposes `QS3DREVBASE` and `QS3DREVDIFF`; current Revision UI shows Before/After/Delta/% changes with Locate.

### 10.11 Model Health

Model Health checks important semantic/data-quality issues including structural material inheritance, malformed/overflowing rebar notation, missing rebar length/source data and model/handle consistency concerns. Continue strengthening Health before increasing auto-generation aggressiveness.

### 10.12 Xref / Layer / selection

- live Xref listing and LayerTable listing/search/show/hide;
- direct Xref reload/detach service where current source supports it;
- row selection can synchronize CAD implied selection;
- active-document filtering is important in multi-document sessions;
- `Gỡ Xref` means detach, not delete external source file;
- handle-based Locate/select;
- Save-As/document identity and active-DWG synchronization were specifically hardened in `db4e5dd...`.

### 10.13 Family assignment/inheritance

Post-`659fa8f`, family reassignment must refresh inherited family defaults **without overwriting explicit instance overrides**. This is now part of preflight/regression expectations. Any family/property-editor refactor must preserve that distinction.

### 10.14 Runtime probe

`QS3DRUNTIMEPROBE` exists. With `QS3D_RUNTIME_RESULT` set it checks x64, opens palettes, tries Ribbon initialization and writes a PASS/FAIL marker including process/host/CLR/assembly information.

The existence of this source is **not evidence that a licensed V25 run has passed**.

---

## 11. Historical branch work vs current `main`

Several full-domain features were developed on temporary integration branches. Some were later absorbed under different names/files; some did not survive exactly.

Important reconciliation:

- old `DomainExtensionsCommands.cs` is not on current `main`;
- equivalent/current review functionality now lives mainly in `ReviewCommands.cs`;
- old `QS3DSTRUCTSOLID` was superseded by current broader `QS3DBUILD3D` flow;
- `QS3DRECOGNIZE` / `QS3DRECOGNIZEAUTO` are present currently;
- `QS3DREVBASE` / `QS3DREVDIFF` are present currently;
- `QS3DBBSVIEW` is present; standard BBS XLSX remains `QS3DBBS`;
- old `QS3DBBSCSV` / `RebarCsvExporter.cs` is absent at this audit;
- old `DomainHubWindow.xaml` / `QS3DDOMAIN` experiment is absent at this audit;
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

This handoff documentation task did **not** authorize a CI dispatch.

### 12.2 Historical verified Core runs recorded by current docs

- `31341101835` — baseline Core CI: PASS;
- `31341548469` — persistence/export hardening: PASS;
- `31341704360` — hardening snapshot: PASS.

Each verifies its own commit snapshot only. Newer code is not automatically certified.

### 12.3 Other temporary/session gate IDs

Additional integration branches observed runs such as:

- `31343750300` — intermediate Core gate reported passing;
- `31343984922` — Core union gate reported PASS after fixes;
- `31343166796` — branch release-tree gate reported success;
- `31344694425` — temporary full-domain final gate observed during integration.

These are branch-history evidence. Map `head_sha` before relying on any of them for a current claim.

### 12.4 Bugs the gates actually caught

- nullable/compiler problems in recognition/rebar integration;
- old-framework nullable analysis not proving a value non-null after `IsNullOrWhiteSpace`, requiring explicit safe code;
- BQ/Excel schema test stayed `A1:P2` when an experimental branch required `A1:Q2`; test was corrected rather than weakened;
- preflight/XAML handler/required-tree guards were tightened repeatedly;
- lifecycle/preflight later added Save-As/active-document protections;
- post-`659fa8f` continuation regressions cover legacy migration dirtying, family inheritance, QSDB non-finite validation, legacy wall validation and invalid snapshot metrics.

Do not “fix CI” by globally disabling nullable, suppressing compiler problems, or weakening assertions merely to get green.

---

## 13. Gate C: real BricsCAD V25 blocker

Historical V25 workflow run `31341184031` was recorded queued because no matching self-hosted runner was assigned for labels approximately:

`[self-hosted, windows, x64, bricscad-v25]`

Therefore do **not** claim current-main completion of:

- full adapter compile against exact installed V25 assemblies;
- `NETLOAD`;
- Ribbon runtime compatibility;
- Palette docking/focus/DPI behavior;
- native Solid3d correctness on the private real DWG;
- transaction/undo behavior under real CAD failures;
- Xref runtime operations;
- visual parity at multiple DPI scales;
- exact V25.1/V25.2 API differences;
- physical geometric rebar;
- physical opening boolean subtraction;
- clean-machine installer/package qualification.

BricsCAD proprietary assemblies/private fixtures remain outside Git.

---

## 14. Runtime checklist for a local V25 agent

1. Fetch latest `main`; record exact SHA.
2. Verify installed V25 path and managed assemblies without copying them into Git.
3. Build Release x64/net48 against the exact V25 references.
4. `NETLOAD` QS3D.
5. Run `QS3DRUNTIMEPROBE` with `QS3D_RUNTIME_RESULT` and retain safe evidence.
6. Verify docked left/right QS3D palettes and native center viewport.
7. Test Windows scaling 100%, 125%, 150%, 200%.
8. Test document create/activate/close and **Save As**; project identity/state must persist correctly.
9. Test Xref selection sync, Move/Reload/Detach semantics and confirm source file is not deleted.
10. Capture Tường KT LINE in mm and at least one supported non-mm drawing; build/update 3D and verify location/width/height/offset.
11. Rebuild same source; exactly one current generated solid should remain.
12. Force invalid source/dimension; old valid geometry/project metadata must remain consistent.
13. Test `QS3DBUILD3D` for supported Dầm/Sàn/Cột/Vách BTCT/Móng source forms.
14. Test Door/Opening host linking and semantic quantity deduction.
15. Generate HT_Phòng and verify finish-only untracking never erases CAD geometry.
16. Edit family dimensions; verify inherited defaults vs explicit instance overrides; run BQ `Tính lại`.
17. Export BQ XLSX/BBS XLSX and inspect values, units, headers, filters/freeze panes.
18. Run Recognition on confident and ambiguous examples; Apply/Locate must behave correctly.
19. Capture revision baseline, change data, run `QS3DREVDIFF`, verify Before/After/Delta/Locate.
20. Run Model Health with intentionally bad/stale data.
21. Exercise undo/redo around generated native geometry where supported.
22. Only then update runtime status docs/screenshots with exact host version + SHA + evidence.

Never commit proprietary DLLs, private DWGs or sensitive customer screenshots.

---

## 15. Known gaps / future work

Still meaningful or runtime-gated:

- current-main V25 compile/NETLOAD qualification;
- exact screenshot/DPI parity;
- robust wall corners, joins, T-junctions and freeform profiles;
- physical opening booleans in generated solids;
- automatic room-boundary discovery from arbitrary wall networks;
- richer transient highlight / true zoom-to-extents behavior;
- geometric rebar placement inside BricsCAD (BBS is ahead of physical rebar geometry);
- more native structural geometry/source forms;
- performance testing on large real drawings;
- abnormal shutdown/file-lock/recovery testing;
- installer/code-signing/release qualification;
- optional licensing/update backend if productization is requested;
- broader engineering standards/rule configuration;
- full keyboard/focus/accessibility polish;
- future localization architecture if required.

Cloudflare, if added later, should be an **optional backend** for licensing/update metadata/R2 packages/etc., not the host of a Windows .NET Framework CAD plugin. GitHub remains appropriate for source/controlled CI/artifacts; QS3D itself runs inside BricsCAD.

---

## 16. Multi-agent rules

This repo moved while this very handoff was being prepared. Every agent must:

1. fetch latest `main` before work;
2. inspect recent commits relevant to its task;
3. base work on current head, not a SHA copied from chat/docs;
4. fetch/sync again immediately before commit/push;
5. if `main` moved, reapply/rebase/merge without deleting newer work;
6. inspect the final diff;
7. never force-push/reset `main` backward;
8. never silently overwrite another agent;
9. prefer focused commits;
10. verify current source before repeating any historical feature claim.

Environment division:

- local-machine agents prioritize V25 installation/build/NETLOAD/UI/screenshots/private fixtures/runner-specific behavior;
- remote agents prioritize Core/domain/persistence/reporting/tests/docs/static source review and runtime probes;
- remote agents must not claim local V25 runtime success.

Read `AGENTS.md` and `CI_POLICY.md` first.

---

## 17. Recommended continuation order

### Remote/source agents

1. Sync latest main and compare commits newer than this handoff reconciliation point.
2. Review current files for the feature; do not blindly resurrect old branch files.
3. Strengthen deterministic Core/tests/preflight without auto-CI triggers.
4. Finish domain behavior before adding more Ribbon buttons.
5. Preserve semantic/generated-handle separation and transaction safety.
6. Improve Recognition only with deterministic evidence/confidence/review behavior.
7. Improve Revision/BQ/BBS/reporting consistency/recovery.
8. Prepare small V25 probes/test workflows for the local agent.

### Local V25 agent

1. Perform Gate C first.
2. Fix actual API/runtime compile failures discovered by the host.
3. Test BLT-like docked UI and 3D workflow with the private fixture.
4. Capture safe evidence: exact SHA, host/build, probe marker, screenshots.
5. Commit only reusable/safe source/scripts/docs; keep proprietary files out.

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
- [ ] Tường KT is a core requirement.
- [ ] HT_Phòng is a core requirement.
- [ ] Cửa/Lỗ mở is a core requirement.
- [ ] BQ → real Excel is a core requirement.
- [ ] Dầm/Sàn/Cột/Vách/Móng/Đào đắp remain first-class semantic categories.
- [ ] Recognition is confidence/review based, never silent AI truth.
- [ ] Rebar/BBS deterministic; physical rebar geometry is separate.
- [ ] Revision preserves meaningful before/after quantity data + Locate.
- [ ] Model Health should expose bad semantic data.
- [ ] Save As / multi-document identity must not lose project state.
- [ ] Legacy migrated elements must not appear clean with stale quantities.
- [ ] Family reassignment preserves explicit instance overrides while refreshing inherited defaults.
- [ ] Non-finite/invalid persisted/takeoff values must be rejected.
- [ ] Generated geometry is transaction-safe and separate from source handles.
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
4. Inspect commits newer than this handoff's reconciliation point
5. Read docs/IMPLEMENTATION-STATUS.md
6. Read docs/REVIEW-2026-08-10.md
7. Read this handoff
8. Inspect current files for the specific feature
9. Decide whether the task is remote/Core-safe or requires real V25
10. Implement only after that reconciliation
```

If BricsCAD behavior cannot be proven from source, leave an explicit runtime gate instead of inventing a result.

---

## 20. Evidence ledger

### Review proof

- accessible current-session records reviewed: **377 / 377**;
- paging offsets: `0..360` in increments of 20;
- terminal read: **0 remaining**;
- targeted prior project-history retrievals: **2**;
- current GitHub source audit/reconciliation performed after history review;
- post-commit race was detected, `659fa8f...` was reviewed, and this canonical handoff was corrected rather than leaving a stale “latest main” statement.

### Important mainline commits reconciled

- `db4e5dd2ae2d4cf64450be8906fc0d50b3636a3d` — Save As / active-DWG synchronization hardening.
- `659fa8f07def68ac4257ccadd78c54e77b20b802` — legacy migration dirtying, family inheritance, persistence/non-finite/takeoff hardening + continuation regression coverage.

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
- `src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs`
- `src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs`
- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `src/QS3D.Core/Services/BulkEditService.cs`
- `src/QS3D.Core/Services/WallQuantityCalculator.cs`
- `src/QS3D.Core/Takeoff/QuantityEngine.cs`
- `src/QS3D.Core/Recognition/RecognitionEngine.cs`
- `src/QS3D.Core/Revisions/RevisionSnapshotStore.cs`
- `tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs`

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

The project is already much more than a UI mockup: current source contains semantic project data, hardened persistence/recovery/migration, fixed-point regeneration, structural categories, Tường KT/HT_Phòng/Cửa workflows, BQ/XLSX, deterministic BBS, Recognition, Revision, Model Health, Xref/layer/selection integration, native generated-geometry infrastructure, Save-As/document synchronization and continuation regressions.

The next major truth gate is a **current-main compile + NETLOAD + interactive validation on a real licensed BricsCAD V25 Windows environment**, followed by fixes based on what that host actually reports.

Until Gate C succeeds for a recorded exact SHA, keep terminology precise: **implemented/reviewed in source** is not the same as **runtime-verified in BricsCAD V25**.
