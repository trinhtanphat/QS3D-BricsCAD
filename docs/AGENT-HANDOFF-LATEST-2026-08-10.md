# QS3D-BricsCAD — canonical latest agent handoff

**Audit/update date:** 2026-08-10 (UTC+7)  
**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Branch:** `main`  
**Repository/source reconciliation cutoff for this edition:** `ec34455` (`feat(opening): add safe polyline host cut planner`) plus rebased B4D/ED2/Handle commit `00f53d4` and the DWG-identity/generated-ownership hardening documented below.
**Historical exhaustive session handoff:** `docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md`  
**Status of this file:** **canonical for current source status and continuation**. The older session-history handoff is retained as the detailed historical audit trail, but any source-status statement in that older file that conflicts with this file or newer `main` is superseded.

> This repository is actively modified by multiple agents. Always fetch `main` and inspect commits newer than the cutoff above before changing code. A newer commit is new project history, not proof that this handoff omitted an older session event.

---

## 1. What was reviewed and how completeness is bounded

This edition reconciles three bodies of evidence:

1. **Current ChatGPT session:** the accessible session event stream was previously read sequentially in pages at offsets `0, 20, 40, ... 360`, reaching **377 / 377 accessible records** and a terminal result of **0 remaining**.
2. **Earlier project history:** two targeted history retrievals were performed for the previous QS3D / BLT3D / BricsCAD V25 work, recovering requirements, UI references, source milestones, runner/CI history and private-fixture notes.
3. **Live GitHub source:** after the session-history audit, `main` was repeatedly re-read because other agents continued committing. This update reconciles the source through `fc59fccc...`, including the full-domain merge, schema v3/rules/templates/audit work, BBS CSV, automatic room-boundary discovery, V25 DemandLoad packaging, release CI, semantic-overflow hardening, 3D source guards and deterministic rule dependencies.

Therefore **“100% reviewed” means 100% of the session/history material that was actually exposed to this conversation plus the repository history explicitly reconciled here**. It does not honestly certify deleted chats, inaccessible account-wide history, or future commits created after this cutoff.

---

## 2. Product goal and non-negotiable owner intent

QS3D is an original, clean-room **BLT3D-like quantity takeoff / semantic BIM workflow plugin for BricsCAD V25**.

The product contract repeatedly established in the session is:

- target **BricsCAD V25**, Windows x64;
- use the **native BricsCAD viewport as the central 2D/3D canvas**;
- use native BricsCAD commands/Ribbon plus docked WPF palettes around that viewport;
- make the workflow and layout familiar to the supplied BLT3D screenshots while improving polish and maintainability;
- implement real behavior behind visible buttons; do not ship decorative/mock actions as if complete;
- keep deterministic business logic in Core and BricsCAD-specific operations in the adapter;
- do not call source/static/Core success “runtime verified in BricsCAD”;
- multiple agents may work at once; never reset/force-push away another agent's work;
- GitHub Actions remain owner-controlled/manual-only unless the owner explicitly requests a run;
- private customer DWGs/DOCX and proprietary BricsCAD/BLT binaries stay outside the public repository.

---

## 3. Clean-room and proprietary boundaries

BLT3D is treated as proprietary unless a lawful reusable license/source is explicitly supplied later.

Rules:

- no BLT3D DLL/source decompilation-derived implementation, copied icons/assets or license material;
- no dependency on a local `BLT` installation folder;
- no BricsCAD-owned DLLs committed or packaged as QS3D payload;
- owner-provided screenshots may be used as UI/workflow references;
- owner-provided private DWGs can be runtime fixtures but not public repository assets;
- never request or commit license keys/secrets.

The project must remain an independent implementation even when the workflow resembles BLT3D.

---

## 4. BricsCAD V25 technical baseline

The established V25 managed-plugin baseline is:

- adapter target: **.NET Framework 4.8**, Windows x64;
- Core target: `netstandard2.0`;
- primary external BricsCAD managed references: `BrxMgd.dll` and `TD_Mgd.dll`;
- optional V25 APIs only when needed and verified;
- BricsCAD references remain external / `Copy Local = False`;
- application entry uses `Teigha.Runtime.IExtensionApplication`;
- commands use `CommandMethod`;
- WPF palettes use BricsCAD `PaletteSet`/visual hosting;
- exact API compatibility still requires compile/NETLOAD on the installed V25 build.

Architecture boundary:

```text
BricsCAD V25
    |
QS3D.BricsCAD.V25      <- thin CAD/runtime adapter
    |
semantic normalized state
    |
QS3D.Core              <- domain, quantities, rules, persistence, reporting,
                           recognition, revision, templates, diagnostics, rebar
```

Do not move deterministic quantity/business rules into UI code-behind merely for convenience.

---

## 5. UI/UX target from the supplied BLT3D reference

The required overall workspace remains:

```text
QS3D / BricsCAD Ribbon
KHỞI ĐẦU | THIẾT LẬP DỰ ÁN | MÔ HÌNH BIM | NHẬN DẠNG | VẼ | TOOL |
MODELING | XEM | ĐỊNH LƯỢNG | BẢN SỬA ĐỔI

Left dock                 Native BricsCAD viewport            Right dock
Zone / Floor              interactive 2D + 3D                 Drawing/Xref
Semantic tree                                                   Layer manager
Family / element list
Property editor
```

Important semantic tree/workflow names from the reference and planning:

- `Lưới Trục`;
- `HT_Phòng` → Phòng, Sàn Hoàn Thiện, Chống Thấm, Chân Tường, Hoàn Thiện Tường, Trần Hoàn Thiện, Lan Can;
- Dầm, Sàn, Cột, Vách;
- `Tường KT` → Tường Gạch, Vách Kính, Trụ Tường;
- `Cửa` → Lỗ Mở Vách, Cửa Đi;
- Cầu Thang, Móng, Đào đắp, KL Tùy chỉnh.

Visual contract:

- dark charcoal CAD UI;
- compact palettes so the viewport retains meaningful space;
- consistent blue accent and selected/hover/focus states;
- BQ/BBS/Recognition/Revision/Health/Audit/Domain Hub windows share the design system;
- real visual acceptance must include Windows 100/125/150/200% DPI on a V25 runtime;
- the generated mockup/reference image is a target, not evidence of a runtime screenshot.

---

## 6. Requirement DOCX and private DWG that shaped the project

The owner requirement DOCX contained the key priorities:

```text
HOÀN THIỆN:
BIULD CHỨC NĂNG
TƯỜNG KT
HT_PHÒNG
Cửa:
OUTPUT: xuất khối lượng sang excel
```

Those remain the architectural core even though the project now covers broader structural/full-domain workflows.

Private regression fixture referenced in the session:

`260808.SHOP XAY TUONG_NHA NOI TRU.dwg`

It is for local/V25 regression only and must not be committed to this public repository.

---

## 7. Current source-of-truth: major implemented capabilities

This section reflects current `main` through the cutoff, not the older branch snapshots.

### 7.1 Project model, persistence and schema

Current source has:

- Project / Zone / Floor / Family / semantic Element state;
- active Zone/Floor/Family context and data-driven property editing;
- family property propagation to member elements with derived quantities dirtied;
- multi-DWG live cache keyed by `Document` identity rather than mutable drawing filename;
- Save As drawing identity synchronization;
- live DWG identity based on BricsCAD `Database.FingerprintGuid`, with a same-path legacy migration and fail-closed rejection of copied/mismatched `.qsdb` Handle identities;
- **QSDB schema v3**, with deterministic **v1 → v2 → v3** migration;
- persisted `QuantityRule` definitions and audit provenance;
- persisted dirty flags and UTC update state;
- validated temp writes and atomic replacement where supported;
- `.bak` recovery and protected failure state rather than silent destructive overwrite;
- single-writer project lock;
- file-size and XML DTD/external-entity protection;
- validation of malformed/non-finite persisted data.

**Correction versus the older handoff:** schema v2 is no longer the current state; source status is v3.

### 7.2 Regeneration and quantity math

Current regeneration model includes:

- dependency graph and dirty propagation;
- bounded fixed-point regeneration;
- semantic regeneration preserves `Geometry` dirty state for native-solid categories; a successful committed CAD builder is the only path that clears that flag;
- explicit `QS3DREGEN`;
- BQ/BBS/Refresh regenerate deterministic dirty quantities before consuming them;
- guarded `QuantityMath` for finite/non-negative multiply/add/subtract/divide/hypotenuse/clamp operations;
- semantic/structural regeneration now stages calculations so an overflow throws without partially replacing the element's prior quantity map;
- smoke regressions explicitly verify wall, finish, beam, stair and earthwork overflow cases retain the pre-existing sentinel state instead of partially mutating quantities.

### 7.3 Project QuantityRules

Quantity rules are first-class project data and persist in `.qsdb`.

The rule engine now:

- can use numeric Family properties, instance properties and current quantities as variables;
- records rule provenance/version metadata;
- discovers variable references from expressions;
- orders dependent managed outputs deterministically rather than relying on list/ID order;
- removes stale managed outputs safely;
- detects circular rule dependencies and fails atomically rather than leaving partially recalculated managed outputs.

Example intended pattern:

```text
AdjustedVolume = NetVolumeM3 * 2
FinalCost      = AdjustedVolume * 100
```

`FinalCost` must resolve after `AdjustedVolume` regardless of rule insertion order; cycles are errors.

### 7.4 Semantic capture and architecture

Current capture/workflow includes:

- Room;
- Tường KT / ArchitecturalWall;
- Opening;
- Door;
- Dầm / Beam;
- Sàn / Slab;
- Cột / Column;
- Vách BTCT / StructuralWall;
- Foundation;
- Stair;
- Railing;
- Earthwork;
- generic/custom takeoff.

Quick Takeoff supports Count/Length/Area/Volume with live BricsCAD `INSUNITS` conversion instead of silently assuming millimeters.

### 7.5 HT_Phòng

Semantic generation exists for:

- floor finish;
- waterproofing;
- skirting;
- wall finish;
- ceiling finish.

Safety invariant retained from earlier review: **finish untracking must not erase CAD geometry**.

### 7.6 Door/Opening host links

Host linking supports deterministic opening/door deduction and audit events.

Safety invariants:

- only Door/WallOpening elements may be linked/unlinked as openings;
- re-hosting dirties both old and new host dependencies as needed;
- unlinking a non-opening element must fail without mutating `HostWallId` or dependencies;
- semantic deduction is not the same as physical boolean subtraction of a native solid.

### 7.7 Automatic room-boundary discovery — now implemented for planar straight networks

`QS3DROOMAUTO` is now source-implemented.

Core `RoomBoundaryEngine` currently:

- consumes straight planar boundary segments;
- validates finite/non-degenerate input;
- limits input to 5,000 source segments and 20,000 subdivided edges;
- splits intersections and T-junctions;
- snaps/deduplicates endpoints by tolerance;
- constructs a graph;
- removes dangling bridge edges from bounded-face discovery;
- traverses bounded faces;
- computes stable boundary keys, area and perimeter;
- retains source IDs as boundary evidence;
- has deterministic regression coverage.

Adapter commands read selected LINE/POLYLINE networks and create Room semantics without pretending that the discovered room owns unrelated wall source handles.

**Correction versus the older handoff:** automatic room-boundary discovery is no longer wholly future work. Remaining runtime/product work includes curved/bulged boundaries, private-DWG proof, very large network performance and arbitrary real-world topology hardening.

### 7.8 Native 3D source paths

`QS3DBUILD3D` is the current broad native-3D command.

Source-level paths cover:

- Tường KT;
- Beam;
- Slab;
- Column;
- StructuralWall;
- Foundation;
- Stair footprint mass;
- Railing line prism;
- Earthwork footprint mass extruded downward.

Important safety work now present:

- source/generated handles are distinct;
- generated geometry replacement uses guarded/two-phase behavior;
- generated entities receive versioned QS3D XData ownership (`ProjectId`, `ElementId`, category); erase/replacement and physical opening boolean modification require a matching live marker;
- health validates generated Solid3d ownership/liveness/category;
- erased/non-Entity source handles are not considered live;
- 3D builders reject ambiguous semantic ownership of one CAD source;
- one semantic element selected through multiple source objects is rejected rather than generating ambiguous duplicates;
- expected source geometry type is explicit: LINE where a line prism is required, closed POLYLINE where a footprint extrusion is required;
- non-finite/invalid geometry inputs and dimensions fail instead of creating corrupted solids.

These are still **source paths**, not a claim of V25 runtime success.

### 7.9 BQ / quantity reporting / Excel

Current BQ behavior includes:

- stable Floor/Family ID grouping where appropriate;
- filtering and Locate;
- real recalculation callback;
- deterministic semantic regeneration before report consumption;
- persisted visible-column preferences in project metadata;
- XLSX export;
- finite/overflow-safe report accumulation rather than unchecked `+=`;
- filters/freeze/header behavior covered by deterministic source/tests.
- exported aggregate rows carry stable QS3D Element IDs and hexadecimal CAD handles;
- `QS3DED2` aliases the BQ/export workflow;
- `QS3DEXCELLOCATE` reads a QS3D export row or the supplied legacy BLT hidden `$<decimal handle>` convention and selects/zooms the corresponding live CAD entities;
- derived room-finish rows resolve handles transitively through their source-room dependency without duplicating semantic handle ownership.

### 7.10 Rebar / BBS

Current BBS is broader than the old handoff stated:

- notation parser with validation;
- guarded rebar arithmetic;
- lazy length fallback where applicable;
- aggregate schedule total validation;
- bar mark/shape/cutting-length and allowance concepts;
- theoretical kg/m, total length and total weight;
- `QS3DBBS` → XLSX;
- `QS3DBBSVIEW` → review/Locate UI;
- **`QS3DBBSCSV` → UTF-8 CSV**.

The CSV exporter now includes safeguards for:

- spreadsheet formula injection;
- control characters;
- malformed/non-finite row values;
- atomic output replacement rather than leaving a partially-written final CSV.

**Correction versus the older handoff:** `RebarCsvExporter.cs` and `QS3DBBSCSV` are now present on current `main`.

### 7.11 Recognition

Recognition remains deterministic/rule-based and review-oriented, not LLM-authoritative.

Current behavior includes:

- layer terms;
- block/text/tag metadata;
- entity-type compatibility;
- Vietnamese normalization;
- token/term boundary matching so `DAMAGE` is not mistaken for normalized `Dầm`/`dam`;
- confidence + top-candidate margin;
- `QS3DRECOGNIZE` review UI;
- `QS3DRECOGNIZEAUTO` high-confidence application;
- invalid confidence/margin and ambiguous mappings rejected;
- semantic category collision protection;
- **project/company layer mappings can override fallback heuristics deterministically**.
- `QS3DB4D` performs a whole-Current-Space scan, excludes QS3D-generated solids, reads Polyline/Region/Hatch/Solid3d metrics and auto-applies only high-confidence results while leaving ambiguous results in review.
- rescanning an already tracked object preserves its assigned Family/Floor/Zone instead of silently moving it to the active context.

### 7.12 Templates and company standards

`.qstemplate` import/export is now implemented.

A template can contain:

- Families;
- QuantityRules;
- recognition layer mappings;
- visible BQ columns;
- generic Family material/classification properties.

Template safety/behavior includes:

- size and DTD/external-entity guards;
- validated temp write + backup replacement;
- project validation before application;
- in-use Family category changes rejected when unsafe;
- duplicate rule-output conflicts rejected;
- projected recognition mappings validated before mutation;
- inherited Family defaults propagate without overwriting deliberate instance overrides;
- affected elements are dirtied and regenerated;
- audit provenance is recorded;
- failed/destructive imports have rollback/confirmation safeguards;
- import deliberately does not silently auto-save the `.qsdb` before review.

### 7.13 Audit and Domain Hub UI

The previous experimental status changed: current `main` now includes:

- `QS3DDOMAIN`;
- `DomainHubWindow`;
- `AuditCommands` / audit-log review UI;
- persisted project audit provenance.

**Correction versus the older handoff:** Domain Hub is no longer absent/historical-only.

### 7.14 Revision

Current revision path includes:

- `.qsrev` snapshot persistence;
- `QS3DREVBASE`;
- `QS3DREVDIFF`;
- Before/After/Delta/% UI + Locate;
- finite/overflow-safe revision arithmetic;
- duplicate revision element IDs rejected;
- finite-safe summary accumulation.

### 7.15 Model Health

Health currently checks multiple layers of integrity:

- required semantic dimensions by category;
- material inheritance where applicable;
- rebar definitions/lengths;
- host/dependency consistency;
- source handle liveness;
- generated Solid3d handle format;
- generated ownership uniqueness;
- generated category consistency;
- generated handle incorrectly mixed into `SourceHandles`;
- generated Solid3d liveness/type;
- erased/non-Entity CAD handles excluded from live source sets.

Health should remain a blocker/diagnostic layer rather than being weakened to make incomplete data appear healthy.

### 7.16 Xref / Layer / selection / document lifecycle

Current adapter includes:

- live Xref list;
- LayerTable list/search/show/hide;
- selection inspection;
- handle-based Locate/select;
- active-document selection filtering;
- Save As project identity synchronization;
- direct Xref reload/detach where supported;
- detach semantics must never be confused with deleting the external Xref source file.

### 7.17 V25 release packaging and DemandLoad — source/CI implemented, runtime still gated

Current source now contains release tooling that did not exist in the first handoff:

- `scripts/package-v25.ps1`;
- V25 installer/uninstaller scripts;
- generated command manifest from current `CommandMethod` declarations;
- release metadata and SHA-256 payload hashes;
- package exclusion of BricsCAD-owned runtime assemblies;
- per-user V25 DemandLoad registration;
- default OnCommand behavior and optional OnStartup path;
- payload hash verification;
- optional Authenticode enforcement;
- staged replacement;
- `-WhatIf`/confirmation semantics;
- safe uninstall;
- no deliberate lowering of BricsCAD security settings.

The runtime probe was also hardened so it verifies **actual palette visibility**, not merely successful command dispatch.

This is release-tool **source + GitHub-hosted validation**, not proof that a licensed V25 machine has successfully DemandLoaded/NETLOADed the newest plugin.

---

## 8. Important recent source-hardening chronology after the old handoff cutoff

The older exhaustive handoff reconciled only through `c987b34...`. Since then, major mainline work included:

- `1b8e88a...` — persist QuantityRules, audit, templates and recognition mappings;
- `284e8a6...` / `d8ef708...` — BBS arithmetic and aggregate-total hardening;
- `9e2fd98...` — atomic/inherited-safe template and rule workflows;
- `53dd144...` — merged full-domain quantities, 3D hub and BBS CSV while preserving concurrent hardening;
- `9cefc14...` — reject ambiguous recognition mappings and invalid confidence;
- `e5dfe9f...` — template rollback/confirmation safety;
- `b074f87...` — audit UI and completed Domain Hub workflows;
- `08f4401...` — atomic, row-validated BBS CSV;
- `b418a8e...` — runtime probe verifies actual palette visibility;
- `1de86b2...` — merged verified V25 DemandLoad/package tooling;
- `69de9d6...` — semantic quantity regeneration finite/overflow-safe and atomic;
- `16da418...` — automatic bounded-room discovery from planar CAD networks;
- `9d6a894...` — 3D source ownership/geometry input guards;
- `fc59fcc...` — deterministic QuantityRule dependency ordering and cycle-atomic behavior.

Several of these were merged from concurrent branches. Preserve the union; do not cherry-pick an older branch wholesale over current `main`.

---

## 9. Current command reference that another agent should know

Workspace/project:

- `QS3D`
- `QS3DDOMAIN`
- `QS3DSAVE`
- `QS3DRELOAD`
- `QS3DREFRESH`
- `QS3DREGEN`
- `QS3DHEALTH`

Semantic capture:

- `QS3DROOM`
- `QS3DROOMAUTO`
- `QS3DWALL`
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
- `QS3DFINISH`
- `QS3DLINKHOST`

Native 3D / viewport:

- `QS3DBUILD3D`
- `QS3DVIEW3D`
- `QS3DVIEWTOP`
- `QS3DORBIT`
- `QS3DZOOMSELECTED`
- `QS3DZOOMALL`

Quantity/rebar/recognition/revision:

- `QS3DB4D`
- `QS3DBQ`
- `QS3DED2`
- `QS3DEXCELLOCATE`
- `QS3DBBS`
- `QS3DBBSVIEW`
- `QS3DBBSCSV`
- `QS3DRECOGNIZE`
- `QS3DRECOGNIZEAUTO`
- `QS3DREVBASE`
- `QS3DREVDIFF`

See `docs/COMMANDS.md` and current `CommandMethod` declarations rather than relying only on this list if `main` has advanced.

---

## 10. CI and verification evidence

### 10.1 Policy remains manual-only

Current release workflows still use `workflow_dispatch` only.

A source edit, commit, merge, review, “continue all” or documentation handoff is **not** permission to dispatch GitHub Actions.

### 10.2 Important successful GitHub-hosted runs

Historical branch/core integration evidence includes:

- `31343984922` — Core union gate: completed **success**; used during full-domain integration and caught real compiler/schema/test regressions before the union proceeded.

Newer release-tree evidence recorded in current status and independently checked includes:

- `31346731964` — **success**; V25 DemandLoad release-tooling validation branch; generic preflight + full-domain/release guard + PowerShell syntax + Core Release + deterministic smoke suite.
- `31346906413` — **success**; integrated V25 DemandLoad release tree after Audit/Template UI work; repeated generic/full-domain/release preflight, PowerShell parsing, Core Release and complete deterministic smoke suite.

These prove their exact repository/Core/release-script snapshots only.

### 10.3 Gate C remains a separate runtime truth boundary

Historical V25 integration run:

- `31341184031` — queued/no matching self-hosted `[self-hosted, windows, x64, bricscad-v25]` runner at the time it was attempted.

Therefore GitHub-hosted success does **not** prove current V25 adapter compilation/NETLOAD/runtime.

### 10.4 Local-machine evidence for the B4D/ED2 integration

- exact installed BricsCAD **V25.2.10** managed references compiled the Release/x64 adapter with **0 warnings / 0 errors**;
- the complete Core smoke executable passed three consecutive runs after moving suite registration out of a module initializer that could deadlock parallel tests before `Main`;
- generic and full-domain/release preflights passed without dispatching GitHub Actions;
- read-only `DGKL.xlsx` checks resolved row 5 decimal handles `12510,12512` to `30DE,30E0` and row 6 to `30DF,30E1`;
- the verified local package contains the plugin/Core DLLs, command manifest, SHA-256 manifest and DemandLoad install/uninstall scripts, while excluding BricsCAD-owned DLLs.

This is compile/static/file-format evidence. It is not a claim that the newest DLL has been NETLOADed into the currently open interactive BricsCAD session.

---

## 11. What is still runtime-gated or genuinely incomplete

Do not mark these complete from source review alone:

- newest plugin compile against exact installed V25 `BrxMgd.dll` / `TD_Mgd.dll`;
- real DemandLoad install/uninstall and `NETLOAD`;
- Ribbon/palette/Domain Hub/BBS/Recognition/Template/Revision/Audit commands on V25.1/V25.2;
- private DWG regression;
- exact BLT-like UI fidelity and Vietnamese Unicode/HiDPI at 100/125/150/200%;
- robust wall polyline corners, joins, T-junctions and freeform profiles;
- physical opening/door boolean subtraction from host solids;
- automatic room detection for curved/bulged boundaries and performance proof on very large real networks;
- physical/geometric rebar placement tied to BBS;
- richer transient highlight/isolate/section-box UX;
- production Authenticode signing and signed updater;
- optional commercial licensing/team/cloud backend.

Cloudflare, if introduced later, should remain an optional backend for licensing/update/team metadata or package delivery; it is not the runtime host for a Windows .NET Framework BricsCAD plugin.

---

## 12. Local V25 runtime acceptance checklist

A Windows agent with licensed BricsCAD V25 should do this before any release claim:

1. fetch latest `main` and record exact SHA;
2. verify real V25 directory and managed references without copying them into Git;
3. build Core and V25 adapter Release/x64 against that exact installation;
4. install using the DemandLoad tooling in a controlled test profile and also validate direct `NETLOAD` recovery path;
5. run `QS3DRUNTIMEPROBE` and preserve safe evidence;
6. verify actual left/right palette visibility and Ribbon initialization;
7. test 100/125/150/200% Windows DPI;
8. test multi-document create/open/activate/Save As/close and project-sidecar identity;
9. test Xref select/reload/detach without deleting source files;
10. test Tường KT native build and rebuild exactly once from valid LINE source;
11. test Beam/StructuralWall/Railing LINE paths and Slab/Column/Foundation/Stair/Earthwork closed-POLYLINE paths;
12. verify ambiguous source ownership, erased source, wrong source type, non-finite dimension and failed-build cases do not corrupt prior generated geometry;
13. run `QS3DROOMAUTO` on simple rooms, T-junction networks, dangling edges and the private fixture; compare area/perimeter;
14. test Door/Opening link/re-link/unlink and semantic deduction;
15. generate HT_Phòng and prove finish untracking never erases CAD geometry;
16. edit Family/default/instance values and verify inheritance rules;
17. run QuantityRules with dependent outputs and verify order independence/cycle rejection;
18. BQ recalc + XLSX; inspect finite totals and visible-column persistence;
19. Run `QS3DB4D` on the private drawing, review ambiguous results and verify a rescan does not reassign existing Family/Floor/Zone context;
20. Run `QS3DED2`, export handles, then use `QS3DEXCELLOCATE` on both the new export and a copied legacy BLT workbook row;
21. BBS VIEW/XLSX/CSV; inspect formula-injection and malformed-row behavior;
22. Recognition review/auto with company layer mappings, ambiguous layers and false-positive token cases;
23. Template export/import, destructive confirmation, failed-import rollback and no implicit `.qsdb` save;
24. revision baseline/diff and finite-safe delta/% math;
25. Model Health on missing dimensions, stale source/generated handles, duplicate ownership and generated-category mismatch;
26. audit-log review;
27. package ZIP/hashes/command manifest/install/uninstall on a clean V25 test profile;
28. capture runtime screenshots only after all above relevant UI paths actually load.

---

## 13. Multi-agent integration rules — critical

This project repeatedly had `main` move while another branch was under review. Continue using the safe pattern:

```text
fetch latest main
→ inspect concurrent commits
→ keep their work
→ apply/merge the new feature onto that latest tree
→ resolve overlap by preserving the stronger behavior from both sides
→ review final diff
→ test the exact integrated tree when authorized
→ commit/push without force
```

Never use:

```text
old feature branch
→ force/reset main
```

Important nuance from the session: a successful temporary CI-gate branch may **diverge** from later `main` because the gate branch contains temporary CI-only commits/workflow triggers. The correct integration goal is to preserve the validated feature/fixes on latest `main`, not to make every temporary gate commit an ancestor of release `main`.

Temporary `*-temp.yml` or push-trigger gate workflows must not be left in the release tree. Current release workflows remain manual-only.

---

## 14. Corrections to the older exhaustive handoff

Agents must not repeat these old statements as current facts:

- **Old:** QSDB current schema is v2.  
  **Current:** schema is **v3**, migration v1→v2→v3.

- **Old:** `DomainHubWindow` / `QS3DDOMAIN` is a historical experiment absent on main.  
  **Current:** Full Domain Hub is present and documented.

- **Old:** `RebarCsvExporter.cs` / `QS3DBBSCSV` is absent.  
  **Current:** BBS CSV is present and has additional safety hardening.

- **Old:** automatic room-boundary discovery is wholly future work.  
  **Current:** deterministic straight-planar network discovery and `QS3DROOMAUTO` are implemented; curved/bulged/large-real-world proof remains future/runtime-gated.

- **Old:** packaging/installer is future only.  
  **Current:** V25 package + per-user DemandLoad install/uninstall source exists and has GitHub-hosted release-script validation; actual licensed V25 install/runtime remains unverified.

- **Old:** project templates/company recognition mappings are only planning.  
  **Current:** `.qstemplate`, project QuantityRules/audit provenance and layer mappings exist in source.

The older handoff remains valuable for chronology, screenshots, early architecture decisions, private fixture notes and detailed historical audit. Use this latest file for current implementation status.

---

## 15. High-value current files for the next agent

Read these before modifying related behavior:

```text
AGENTS.md
CI_POLICY.md
docs/AGENT-HANDOFF-LATEST-2026-08-10.md
docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md
docs/IMPLEMENTATION-STATUS.md
docs/PLAN.md
docs/COMMANDS.md
docs/UI-SPEC.md
docs/V25-INSTALL.md
docs/V25-RUNNER.md

scripts/preflight.py
scripts/preflight-full-domain.py
scripts/package-v25.ps1
scripts/install-v25-autoload.ps1
scripts/uninstall-v25-autoload.ps1
scripts/test-bricscad-v25-runtime.ps1

src/QS3D.BricsCAD.V25/Commands.cs
src/QS3D.BricsCAD.V25/ReviewCommands.cs
src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs
src/QS3D.BricsCAD.V25/DomainHubCommands.cs
src/QS3D.BricsCAD.V25/TemplateCommands.cs
src/QS3D.BricsCAD.V25/AuditCommands.cs
src/QS3D.BricsCAD.V25/BbsCsvCommands.cs
src/QS3D.BricsCAD.V25/RuntimeProbeCommands.cs
src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs
src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs
src/QS3D.BricsCAD.V25/Cad/CadGeometryGuard.cs
src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs
src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs
src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs
src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs
src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs

src/QS3D.Core/Domain/ProjectState.cs
src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs
src/QS3D.Core/Persistence/QsdbProjectStore.cs
src/QS3D.Core/Persistence/ProjectStateSnapshot.cs
src/QS3D.Core/Rules/QuantityRuleEngine.cs
src/QS3D.Core/Formulas/ExpressionEvaluator.cs
src/QS3D.Core/Templates/TemplateProfileStore.cs
src/QS3D.Core/Recognition/RecognitionEngine.cs
src/QS3D.Core/Recognition/ProjectRecognitionService.cs
src/QS3D.Core/Geometry/RoomBoundaryEngine.cs
src/QS3D.Core/Services/QuantityMath.cs
src/QS3D.Core/Services/SemanticRegenerators.cs
src/QS3D.Core/Services/StructuralRegenerator.cs
src/QS3D.Core/Services/HostLinkService.cs
src/QS3D.Core/Diagnostics/ModelHealthService.cs
src/QS3D.Core/Rebar/RebarSchedule.cs
src/QS3D.Core/Export/RebarCsvExporter.cs
src/QS3D.Core/Revisions/RevisionService.cs
src/QS3D.Core/Revisions/RevisionSnapshotStore.cs
src/QS3D.Core/Audit/AuditTrail.cs

Tests especially worth preserving:
tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs
tests/QS3D.Core.SmokeTests/WorkflowSafetySmoke.cs
tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/LogicRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/RoomBoundaryRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/SemanticOverflowSmoke.cs
```

---

## 16. Agent start protocol

Every new coding agent should:

1. read `AGENTS.md` and `CI_POLICY.md`;
2. fetch latest `main`;
3. compare latest `main` with this handoff cutoff `fc59fccc5d116b28758ddcbc77bdf10217f71f21`;
4. read this latest handoff;
5. read the older session-history handoff only when deeper chronology/early evidence is needed;
6. read current `IMPLEMENTATION-STATUS`, `PLAN` and `COMMANDS`;
7. inspect current source for the exact feature being changed;
8. decide whether the task is Core/repository-safe or requires real V25;
9. implement without overwriting concurrent work;
10. before push, fetch `main` again and reconcile any new commits;
11. do not run Actions unless the owner explicitly asks;
12. never claim V25 runtime success without real licensed-host evidence.

---

## 17. Evidence ledger

Session/history evidence retained from the prior audit:

- accessible current-session stream reviewed: **377 / 377**;
- pagination reached terminal **0 remaining**;
- targeted earlier project-history retrievals: **2**.

Historical integration evidence:

- `31343984922` — Core union gate: **success**.

Newer release validation evidence:

- `31346731964` — V25 DemandLoad release tooling: **success**.
- `31346906413` — integrated V25 DemandLoad release tree: **success**.

Runtime blocker/history:

- `31341184031` — historical V25 self-hosted integration attempt remained queued because the matching `bricscad-v25` runner was unavailable.

Current reconciliation cutoff for this document:

- `ec34455` — safe polyline-host opening cuts plus the preceding wall-junction/rebar-layout/Room Auto/TKT/UI batch;
- `00f53d4` — B4D whole-space scan and ED2 Excel/Handle round-trip rebased onto that mainline.

The older handoff cutoff `c987b34...` is an ancestor of later `main`; subsequent full-domain/release work was layered/merged on top rather than using a destructive reset.

---

## 18. Final continuation statement

QS3D is no longer merely a UI shell or early quantity prototype. Current source contains a broad semantic project model, schema-v3 persistence, recovery, deterministic regeneration/rules, Tường KT/HT_Phòng/Cửa, structural categories, native 3D source paths, B4D-style whole-drawing recognition, BQ/ED2 XLSX with reverse Handle lookup, BBS XLSX/CSV, Recognition, company layer mappings, templates, Revision, Model Health, automatic planar/curved-polyline room discovery, Audit, Domain Hub, Xref/Layer/selection integration and V25 release/DemandLoad tooling.

The largest remaining truth boundary is **real BricsCAD V25 execution on the newest exact source SHA**. Until that happens, terminology must stay precise:

**source-implemented / GitHub-hosted-Core-verified ≠ compiled/NETLOAD/runtime-verified in licensed BricsCAD V25.**
