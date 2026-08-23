# QS3D PROJECT MASTER CONTEXT — CHAT, REQUIREMENTS, IMPLEMENTATION, CI & GITHUB HISTORY

**Project:** `trinhtanphat/QS3D-BricsCAD`  
**Updated through:** 2026-08-23 (UTC+7)  
**Purpose:** canonical durable handoff / project knowledge base for future ChatGPT and agent sessions  
**Refresh task:** Issue `#3583`, Lane-Key `issue-3583`  
**Authoring baseline:** `main@227653b249e601961dab85bed12c7ce9a746ceb9`  
**Canonical repo path:** `docs/QS3D-PROJECT-MASTER-CONTEXT-2026-08-21.md`

> This is a summarized project record, not a verbatim transcript and not hidden chain-of-thought. Current source, current governance and live GitHub state always override stale historical snapshots.

---

## 0. How to use this note

Use this file after a chat/session reset so the next agent does not need to rediscover the project from scratch.

Precedence:

1. current repository source at current `main`;
2. current `AGENTS.md`, `docs/AGENT-RUNTIME-CONTRACT.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `CI_POLICY.md`, product/registration/collision/lifecycle policies;
3. current GitHub Issues, PRs, branches and exact-head CI;
4. this master context;
5. older chat wording, old branches, old handoffs and stale CI.

This note records user-facing facts, conclusions, requirements, decisions, observed diagnostics and repository evidence. It intentionally excludes passwords, activation credentials, private IP addresses, proprietary source/binary contents, private DWGs, unsanitized runtime artifacts and hidden reasoning.

---

# 1. Executive product summary

QS3D is a BIM/QS workflow hosted inside BricsCAD. The recurring customer goal is:

> A QS engineer should be able to author/capture building elements, review native 3D, calculate explainable quantities, detect coordination/quantity problems, export a readable Excel workbook and trace a quantity/Excel row back to the exact model object with as few manual steps as practical.

Repeated capability groups:

- Project / Floor(Level) / Zone / Family / Type management;
- semantic BIM-like Elements backed by BricsCAD-native geometry;
- select / locate / zoom / highlight / isolate;
- clash / intersection / overlap / duplicate review;
- quantity takeoff and explainable quantity/formwork;
- XLSX/CSV/report export;
- `Excel → CAD` reverse trace;
- stable Element ID / CAD Handle / drawing provenance;
- save/reopen/regenerate determinism;
- fail-closed handling of stale, malformed, ambiguous or unsupported data;
- low-click QS review workflow.

A recurring architectural rule is **one semantic/model/quantity truth**. Do not create parallel geometry, quantity or identity engines merely to implement a UI/export feature.

---

# 2. Product boundary

`QS3D-BricsCAD` is a **Windows x64 BricsCAD-hosted plugin**, not a standalone CAD desktop executable.

Host assemblies:

- V25: `QS3D.BricsCAD.V25.dll`, `net48`;
- V26: `QS3D.BricsCAD.V26.dll`, `net8.0-windows`;
- shared Core: vendor-neutral domain logic.

BricsCAD owns DWG database/editor/document lifecycle, viewport, transactions, native selection and native CAD geometry. QS3D owns commands, Ribbon/palettes, semantic/project data, modeling/capture orchestration, quantity/reporting, recognition, provenance and guarded generated-geometry workflows.

Product-family direction:

```text
QS3D-Platform
vendor-neutral contracts/domain
        |
        +-------------------------+
        |                         |
QS3D-BricsCAD                QS3D-CAD
BricsCAD plugin              standalone CAD/BIM/QS product
```

Historical AutoCAD wording does not mean this repo currently ships AutoCAD support.

“BLT-like”, “BLT-style”, “giống BLT” or “BLT3D-familiar” means **clean-room workflow/UX familiarity only**. Never copy proprietary BLT/BLT3D source/resources, never commit proprietary binaries as implementation dependencies, and never use activation/licensing workarounds as product logic.

---

# 3. Requirements workflow agreed with the user

The preferred requirements workflow is:

```text
Problem P-xxx
    ↓
Requirement R-xxx
    ↓
Solution S-xxx
    ↓
User Approval
    ↓
Gap Analysis
    ↓
Architecture
    ↓
Development Plan
    ↓
Task
    ↓
Code
    ↓
Test / Acceptance
```

Useful labels:

- `[ĐÃ XÁC NHẬN]`
- `[SUY LUẬN]`
- `[CHƯA RÕ]`
- `[ĐỀ XUẤT]`
- `[MÂU THUẪN]`
- `[RỦI RO]`

Requirement traceability:

```text
Requirement → Solution → Feature → Task → Test
```

Do not silently convert an assistant proposal into an approved product baseline.

Recovery phrase when implementation drifts away from requirements:

`STOP implementation. Quay lại Requirement Mode.`

---

# 4. Requirement map

```text
MODEL
  → author/capture
  → identity/properties/mapping
  → save/reopen/regenerate

3D REVIEW
  → select
  → locate
  → zoom
  → highlight
  → isolate

QUANTITY
  → engineering rules
  → grouping
  → aggregation
  → explanation
  → trace to model

CHECK / QA
  → clash
  → overlap
  → duplicate
  → missing/inconsistent data
  → highlight source

EXCEL
  → export
  → read/import
  → provenance
  → integrity validation
  → Excel → Model

QS WORKFLOW
  → low-click UX
  → filter/search
  → review
  → audit

PLATFORM / HOST
  → Core
  → BricsCAD V25
  → BricsCAD V26
  → persistence
  → performance
  → runtime qualification
```

---

# 5. Main user questions, requests and answers/decisions

This is a summarized Q&A ledger, not a word-for-word transcript.

## Q1 — Does existing QS3D source already meet BIM3D/QS needs?

**Answer:** the repository already contained major building blocks. The preferred direction is to harden and connect existing semantic, generated-geometry, quantity, export, provenance and review capabilities instead of rewriting from zero.

## Q2 — How should requirements be communicated before coding?

**Answer:** use Problem → Requirement → Solution → Approval. Record user, input, action, output, acceptance, edge cases, dependencies and confirmation state; require traceability to implementation/tests.

## Q3 — Can QS3D behave like BLT3D without original source?

**Answer:** yes for independently observed workflow and UX. No copying proprietary source/assets/internal implementation. Preserve QS3D’s own semantic, geometry and quantity engines.

## Q4 — Add QS3D Excel menu and Excel → CAD.

Customer-facing compact Ribbon evolved to:

- `Xuất Excel` → `QS3DEXCEL`;
- `Excel → CAD` → `QS3DEXCELTRACE`.

Legacy commands such as `QS3DED2` / `QS3DREVDIFF` may remain callable and guarded without cluttering the primary customer Ribbon.

## Q5 — Export Excel and trace a row back to the model.

Agreed customer flow:

```text
3D / Quantity
→ Xuất Excel
→ workbook with provenance
→ TRACE_MODEL
→ Excel → CAD
→ validate live identity
→ select / highlight / zoom source object(s)
```

The workbook projection reuses existing quantity truth rather than introducing a second quantity engine.

## Q6 — Can a quantity row locate the model in 1–2 clicks?

**Answer:** yes. Use Element ID + CAD Handle + drawing fingerprint + integrity evidence, validate against live state, then Select / Zoom / Highlight / Isolate. Grouped rows must retain complete Element-ID ↔ Handle mapping.

## Q7 — Fix laggy logs during `NETLOAD`.

**Answer:** this is a real runtime/performance concern, but chat history alone is not enough to claim a durable fix. Inspect current startup/logging code and validate in the licensed host before closing the runtime concern.

## Q8 — Review everything, implement, commit/push, fix CI and merge main.

Repository-safe lifecycle:

```text
read current main + rules
→ collision check
→ one Issue/Lane-Key
→ one canonical branch
→ implement/docs
→ commit + push
→ exact-head branch CI
→ one canonical PR
→ protected preflight + core
→ strict freshness + mergeability
→ expected-head merge
→ refresh main
```

No direct task write to `main`.

## Q9 — Quantity topbar/ribbon parity preflights fail; fix them.

Root cause: the new Ribbon correctly used `QS3DEXCEL` / `QS3DEXCELTRACE`, while two older guards still expected `QS3DED2` / `QS3DREVDIFF` as visible buttons.

Correct remediation: align visible-button guard expectations with the customer Excel workflow while keeping legacy command registration/behavior compatibility guards separately.

Key commits:

- `a34f4813099bb64aaf014f96d20337f9302aab2c`
- `90870cdb8c5dcf5192006c513102911d95602a0d`

## Q10 — Continue fixing CI until green.

Follow-on Excel-lane defects included:

- nullable exporter fingerprint annotation;
- nullable ZIP entry lookup;
- nullable smoke helper;
- V25 command scope representation;
- Excel 32,767-character text-cell limit;
- oversized `TRACE_MODEL` regression;
- exact current-head/stale-run handling.

Recorded commits include:

- `cf4f97a02a3fc925168969ff960da7bbbb3578eb`
- `48d65f19f76967603ae566b38f8a7597a8c92df1`
- `ec19cc031374eacf283f3cd1a9400861e12bf12c`
- `b737713e7603a8f98c463a0f25a3f981dfc98c3d`
- `ee8fd86dd7cb400eb9e41917900283a0b9146cfb`

Rule learned: exact current head wins. A cancelled CI run after a newer push is stale evidence, not itself a product failure.

## Q11 — Fix clash-boundary CI.

Failure: `input.Reverse().ToArray()` bound to an in-place `Reverse()` returning `void`, causing `CS0023`.

Correct fixture: `Enumerable.Reverse(input).ToArray()`.

Fix commit:

- `442c24a50005645303af8e5f458731352da88054`

## Q12 — Continue all PRs/issues/branches and merge main.

“Continue all” means continue authorized/canonical lifecycle work. It does not justify:

- stealing unrelated active carriers;
- using stale green CI;
- creating duplicate semantic lanes;
- closing LOCAL_ONLY gaps without evidence;
- direct-writing `main`;
- force-updating branch protection;
- fabricating completion.

## Q13 — Can the BricsCAD/license-server setup be used for local-only builds/tests?

**Answer:** an already authorized licensed environment can be used for legitimate local qualification, but activation/licensing material is private environment configuration, not repository source. Never commit credentials, activation secrets, proprietary licensing binaries or bypass logic.

User-provided environment note: licensing behavior for one BricsCAD major should not be assumed to qualify another major. V25/V26 qualification remains exact-host/exact-SHA evidence.

## Q14 — What did `BLTFAMILYFIX` prove?

A diagnostic showed that an in-memory family dictionary could be initialized without modifying the DLL on disk. Restart removes that RAM-only state.

This proved a process-local diagnostic fact only. It did **not** fix the proprietary BLT plugin on disk and does not become QS3D implementation truth.

## Q15 — Put all project knowledge/Q&A in one Markdown file and land it on main.

The canonical file is this file. Earlier master-context refreshes were #3355/#3357 and #3557/#3558. The current refresh is #3583.

Repo policy has no docs-only direct-main exception, so the file must land through a dedicated branch/PR and protected `preflight + core`.

## Q16 — Can a VPS that exposes only RDP port 33890 still use Git?

General answer: inbound RDP exposure and outbound Git connectivity are different things. Git over HTTPS typically needs outbound TCP 443; SSH-based Git normally needs outbound TCP 22 unless an alternate transport is configured. Having only an inbound RDP port open does not by itself prevent outbound Git.

Do not store the user’s private VPS IP/range in this project note.

## Q17 — A VPS became unreachable after a command + restart. What principle applies?

Treat restart/network/firewall/RDP changes as high-risk operational work. Preserve a recovery path before changing firewall/routing/RDP service settings. Do not infer that a Git-related command caused the outage unless logs/configuration prove it.

## Q18 — Can Windows 11 be installed on an older i5-5200U machine without USB?

User workstation context recorded in chat:

- CPU reported by `wmic`: Intel Core i5-5200U @ 2.20 GHz;
- machine initially had only `C:` visible;
- no USB installation media was available;
- the user considered shrinking `C:`/creating another partition and using installation files locally;
- later installation progress appeared successful.

General decision: unsupported-hardware Windows 11 installation may require compatibility checks/workarounds and carries update/support risk. Do not treat a screenshot or disk-space number alone as proof that every old file is gone; verify the actual partition/install choice and Windows.old/data state.

## Q19 — After a clean/reinstall, are chipset/drivers needed?

General answer: Windows Update usually supplies many drivers, but check Device Manager and OEM/platform drivers for chipset, graphics, Wi-Fi/LAN, audio and storage where needed. Do not install random driver packs when hardware is already healthy.

## Q20 — Why are Windows power-mode choices such as “Best performance” missing?

This is workstation/OS configuration, not QS3D source. Possible causes include Windows version/build, power-plan policy, legacy hardware/driver support, OEM power management, Modern Standby capability or current plan restrictions. Diagnose from actual `powercfg`/Settings state rather than assuming the UI should always expose the same choices.

---

# 6. BIM3D-QS customer golden path

Umbrella: Issue `#3142`.

```text
Project / Floor / Family
→ author/capture 3D
→ verify semantic/native model
→ calculate quantities
→ review / locate / explain
→ export workbook
→ save / reopen
→ recalculate deterministically
```

User-facing shorthand:

```text
Tạo mới / Capture
→ tham số chính
→ 3D
→ Khối lượng
→ Định vị/Diễn giải
→ Xuất Excel
```

P0 categories repeatedly identified:

- ArchitecturalWall
- Beam
- Column
- Slab
- StructuralWall
- Foundation
- Door / WallOpening

P0 quantity truth should expose meaningful fields such as:

- count;
- length;
- gross/net area;
- gross/net volume;
- opening/deduction evidence;
- effective material;
- Floor/Zone/Family/category;
- units;
- source provenance.

4D/5D, ERP/Primavera/MS Project, generalized IFC/RVT completeness, broad AI automation and standalone CAD behavior are not the first BIM3D/QS critical path.

---

# 7. Customer Excel / reverse trace

Historical canonical lane:

- Issue `#3296`
- PR `#3299`
- branch `agent/chatgpt-gpt56sol/customer-excel-trace-3296`

Workbook sheets:

- `DGKL`
- `COP_PHA`
- `CHI_TIET`
- hidden `TRACE_MODEL`

Integrity contract:

1. grouped Count matches grouped Element IDs;
2. Handles are canonical unsigned 64-bit identity text;
3. drawing fingerprint binds trace to the intended drawing;
4. `TRACE_KEY` is recomputed/verified;
5. malformed/tampered provenance is rejected;
6. aggregate/detail scope stays coherent;
7. Excel text cells obey the 32,767-character bound;
8. grouped Element-ID ↔ Handle mapping stays unambiguous.

Protected final evidence:

- run `#32434789970`
- `preflight = SUCCESS`
- `core = SUCCESS`
- deterministic smoke = SUCCESS
- BricsCAD V25 plugin compile = SUCCESS

Merge:

- `99dc024faafa4becc1a89fa61a894f69fba8aa49`

Issue `#3296` closed/completed.

Important boundary: this hosted CI evidence did not, by itself, prove licensed interactive BricsCAD runtime behavior.

---

# 8. Other major CI/fix outcomes

## 8.1 Earlier integration batch

PR `#3295`, recorded merge:

- `db7cc6f15a828d166731cee8011dd5289e948422`

It integrated many earlier source-safe PRs including #3078, #3235, #3106, #3011, #2966, #3045, #3029, #3012, #3000, #2902, #2929, #2912, #2896, #2878, #2886 and a clean transplant of #2871.

## 8.2 Floating tool work-area bound

- Issue `#3303`
- PR `#3307`
- protected run `#32434196978` green
- landed SHA `a8dbee08bd8dd0a6241c23cd47e02f485d528a13`

## 8.3 Clash regression boundary

- Issue `#3310`
- PR `#3312`
- fix `442c24a50005645303af8e5f458731352da88054`
- protected run `#32435254406` green
- merge `c80405e4cd1e0530b16acf1e98d580ef4e76cd0c`

## 8.4 Remote bug sweep integration

Tracking Issue `#3337`, PR `#3338`, branch `integration/20260821-remote-bug-sweep`.

The batch assembled source-safe hardening for:

- Takeoff identity;
- FeatureFlags names;
- preview-review bounds;
- Start Center bookkeeping;
- aggregate-preflight environment hardening;
- diagnostic/count contracts;
- curtain/frame bounds;
- revision identity;
- workflow scanner hardening;
- estimating provenance;
- grouped workbook provenance;
- audit payload bounds.

Historical nuance: intermediate CI included red source-guard runs. Those are stale diagnostics after landing.

Verified historical landing:

- final integration head `8f490d4581330607e8a8b7c8878b3069870574ab`;
- merge commit `ab9ef0fe761ce9ea243576b295359c304e5e33b4`.

Do not treat old run `#32437200807` as a current blocker after that merge.

---

# 9. BLT3D debugging context — observed behavior only

This section records user-visible diagnostics from the BLT3D troubleshooting sessions. It is **not** QS3D source truth and must not be used to copy proprietary implementation.

## 9.1 Goal

The user’s stated goal was to diagnose/fix BLT3D behavior itself for testing, not to move BLT3D proprietary code into QS3D.

Primary defect discussed:

> Draw a wall with an opening; formwork on the top horizontal face above the opening is missing.

A second recurring symptom was quantity output staying at zero.

## 9.2 Runtime/environment observations

Observed/user-reported items included:

- BricsCAD with .NET runtime `v4.0.30319`;
- `APPLOAD` used for DLL loading;
- files/packages referenced during diagnostics included `blt3D.dll`, `blt3D.pdb`, `BltColumnWrapper.dll` and temporary diagnostic/patch DLLs;
- DLL Unblock was checked during troubleshooting;
- some load attempts produced “module expected to contain an assembly manifest” or other load/reset-family errors;
- later wrapper load showed the BLT panel.

No proprietary binary should be committed into QS3D based on these tests.

## 9.3 Wall creation behavior observed

User-reported interaction:

- two points only did not generate the wall;
- three collinear points such as start → middle → end, then Enter, did generate a wall;
- shorthand observation: “click click click → Enter” worked.

This is an observed UI behavior, not an architectural requirement unless separately approved.

## 9.4 Property/edit behavior

Commands/settings used:

- `DBLCLKEDIT=1`
- `PICKFIRST=1`
- double-click/property-panel testing
- `B4D`
- diagnostic commands such as `BLTENTITY`, `BLTHANDLE`, `BLTFAMILY`, `BLTFAMILYFIXSTATUS`, `BLTFAMILYFIX`

## 9.5 Quantity symptom

Repeated user observation:

- generated wall existed;
- `B4D` returned quantity/formwork values of zero in tested cases.

This remained diagnostic evidence, not a verified final fix.

## 9.6 `BLTENTITY`

Observed diagnostic result included:

- selected entity was a `Solid3d`;
- non-zero raw volume was reported;
- clone/explode operations produced Regions;
- extents were available.

Interpretation: the CAD solid itself existed and had geometric volume; the zero-quantity symptom was therefore not simply “no geometry exists”.

## 9.7 `BLTHANDLE`

Observed at different points:

- one lookup succeeded and returned a valid ObjectId;
- another diagnostic later reported `link_CShap not found`.

Interpretation: object identity/association or expected metadata/linkage was a plausible failure surface.

## 9.8 `BLTFAMILY` / `BLTFAMILYFIX`

Observed:

- `NameConverter` came from `blt3D` assembly;
- `s_listSemilerName` was initially `<null>`;
- `BLTFAMILYFIX` initialized a `Dictionary<String, blt_family>` in RAM;
- no DLL on disk was modified;
- restart would roll the in-memory change back.

Interpretation: a missing process-local family dictionary was demonstrated as one runtime condition. It did not prove the complete root cause of all B4D/formwork failures.

## 9.9 Errors observed

Messages seen included variants of:

- `Lỗi cai_danh_sach_family_hien_huu`
- `Lỗi reset_danh_sach_family_hien_huu`
- `General modeling failure`

These are historical diagnostic clues only.

## 9.10 Clean-room boundary

Allowed for QS3D:

- learn from observable user workflows;
- model independent business rules;
- implement equivalent customer outcomes in QS3D’s own architecture.

Not allowed:

- copy BLT/BLT3D proprietary source;
- commit proprietary binaries/resources;
- depend on undocumented proprietary internals;
- treat a temporary memory patch as a product implementation.

---

# 10. Explainable quantity/formwork and engineering truth

Formwork/quantity explanation was repeatedly identified as a major BLT-like customer gap.

Engineering quantities must come from authoritative semantic/native geometry and unit rules. Do not generate plausible-looking concrete/formwork values from bounding-box guesses when exact evidence is required.

Source/Core tests can prove deterministic contracts, but not arbitrary/private DWG geometry or licensed interactive UX.

For openings, deductions and formwork faces, preserve an explainable trace:

```text
source element
→ measured geometry / recognized topology
→ rule / deduction
→ quantity component
→ aggregate row
→ model trace
```

---

# 11. Clean-room BLT / legacy rules

Allowed:

- observe user-visible behavior;
- infer independent business workflows/data contracts;
- reimplement independently inside QS3D architecture;
- build clean-room compatibility tests from public/authorized observations.

Forbidden:

- copy proprietary source/resources;
- commit proprietary binaries;
- rely on undocumented internals as repository authority;
- invent unsupported legacy mappings;
- implement license bypass/activation circumvention.

Historical/private BLT DWG/proxy behavior remains a licensed/private-fixture qualification boundary where applicable.

---

# 12. Windows/workstation context

These are support notes from user conversations, not QS3D product requirements.

## 12.1 Older CPU / Windows 11

User-reported CPU:

`Intel(R) Core(TM) i5-5200U CPU @ 2.20GHz`

This is older than Microsoft’s normal supported Windows 11 CPU list. Installation/upgrade may therefore involve compatibility exceptions and should be treated as unsupported-hardware risk unless Microsoft/OEM support status changes.

## 12.2 No USB / only C: drive

User described:

- no USB installation media;
- only `C:` visible initially;
- desire to install using local installation files/partitioning.

Safe principle:

- back up important data;
- distinguish ISO/media files from actual partitions;
- do not format/delete the only data partition unless the intended clean-install outcome is explicit;
- verify actual disk/partition choices before assuming old data is gone.

## 12.3 Driver check after install

Recommended verification:

- run Windows Update;
- inspect Device Manager for unknown/problem devices;
- install OEM/platform chipset, graphics, network, audio or storage drivers only when needed;
- avoid untrusted “driver pack” utilities.

## 12.4 Power mode

Missing “Best performance” UI options can depend on Windows build, power plan, OEM firmware/drivers and platform capability. Use actual `powercfg` output/Settings state to diagnose rather than assuming every machine exposes identical choices.

---

# 13. VPS/network/Git operational context

These notes intentionally omit private IP addresses.

## 13.1 RDP port vs Git connectivity

A VPS exposing an inbound RDP port does not tell whether outbound Git works.

Typical requirements:

- Git HTTPS: outbound TCP 443;
- Git SSH: outbound TCP 22 unless alternate SSH-over-443 or another approved transport is configured;
- DNS resolution must work;
- local firewall/proxy/TLS policy must permit the chosen transport.

## 13.2 Restart/inaccessibility incident

User reported a VPS becoming unreachable after a command and restart.

Operational lesson:

- before firewall/RDP/network changes, preserve an out-of-band recovery path if available;
- record current rules/configuration;
- make reversible, narrow changes;
- validate RDP service/listener/firewall/routing before reboot;
- do not attribute the outage to a specific prior command without evidence.

## 13.3 Repository boundary

Do not commit:

- private IP inventories;
- passwords;
- RDP credentials;
- VPN/proxy secrets;
- private SSH keys;
- cloud credentials.

---

# 14. BricsCAD license/build-server context

User discussed using a server/activation setup so local agents could run BricsCAD-related checks and build local-only branches.

Project rule:

- an already authorized licensed environment can be used for legitimate testing/builds;
- license server configuration is environment infrastructure, not application source;
- do not commit secrets, activation files, proprietary licensing binaries or machine-specific credentials;
- do not implement activation bypass logic;
- V25/V26 runtime qualification must be performed with the matching host major and exact source SHA.

A clean hosted compile against managed/pinned BricsCAD references is not the same as licensed interactive `NETLOAD`/DemandLoad qualification.

---

# 15. Runtime qualification boundary

Remote/source agents can prove:

- source contracts;
- deterministic Core tests;
- preflight guards;
- build/compile compatibility;
- package/source integrity.

They cannot honestly infer without execution:

- licensed `NETLOAD`;
- real DemandLoad startup;
- Ribbon/WPF/palette interaction;
- native editor/document lifecycle;
- Undo/Redo;
- SaveAs + cold reopen;
- multi-DWG behavior;
- DPI/multi-monitor behavior;
- historical proxy behavior;
- signing/trust;
- clean-machine install/update/uninstall;
- private-DWG/customer acceptance.

Use `LOCAL_ONLY`, `PENDING_LOCAL`, `LOCAL_PASS`, `LOCAL_PARTIAL` precisely.

Managed-reference V25 compile is not licensed interactive runtime PASS.

---

# 16. Current licensed Wall Snap P02 finding

Snapshot at authoring baseline `main@227653b249e601961dab85bed12c7ce9a746ceb9`.

Latest main commit merged sanitized LOCAL-007 P02 evidence through PR `#3602`.

The local P02 qualification issue is:

- `#3599` — `[LOCAL-007 P02] qualify V25 Wall Snap preview/apply lifecycle`
- state at snapshot: open
- current classification: `LOCAL_PARTIAL / PENDING_REMOTE`

Two remote source defects were discovered by licensed BricsCAD V25.2.10 execution on exact candidate `b6cd726ef76c5fc0c9c044d5823b341004c912cd`.

## 16.1 Issue #3600 — Preview revision self-invalidation

Observed behavior:

- `QS3DWALLSNAPPREVIEW` produced the intended one-edit plan;
- project revision advanced more than the existing reserved headroom;
- persisted preview change-version ended behind final project `ChangeVersion`;
- unchanged immediate `QS3DWALLSNAPAPPLY` rejected its own fresh Preview as stale.

Required remote fix:

- make Preview metadata publication revision-atomic or account for all bounded mutations;
- persist preview version equal to final Preview project version;
- ensure same-project/same-source/same-plan Preview → Apply succeeds;
- add source-safe regression around the real bound metadata dictionary.

Do not mark LOCAL_PASS until an exact-SHA licensed rerun succeeds.

## 16.2 Issue #3601 — replacement project remains cached after prompt drift

Observed behavior:

- sidecar/project changed while Apply selection was active;
- Apply correctly failed closed with no CAD/semantic/sidecar mutation;
- however the replacement project remained in the project cache after refusal.

Required remote fix:

- on ProjectId/ChangeVersion mismatch after final mutation bind, forget newly bound replacement cache;
- preserve non-creating absent-sidecar behavior;
- add deterministic source-safe regression asserting no cached replacement remains.

Again, exact-SHA licensed rerun is required afterward.

---

# 17. Repository collaboration / Git lifecycle

Current policy principles:

- `main` is direct-write read-only for normal task work;
- there is no docs-only direct-main exception;
- one semantic task uses one canonical Lane-Key/carrier;
- normal Lane-Key is `issue-N`;
- if an equivalent active carrier exists: `DUPLICATE_CARRIER / NO MUTATION`;
- a red current-carrier CI triggers diagnose/fix/push/recheck on the same carrier;
- branch CI validates an exact branch SHA;
- protected PR CI validates the current PR candidate;
- merge requires current `preflight + core`, strict freshness, collision/review cleanliness, mergeability and expected-head match;
- ordinary docs still receive branch/PR CI;
- normal owner-task endpoint is `MERGED_MAIN` unless explicitly opted out or truly blocked;
- `continue all` does not authorize unrelated bulk merges unless the owner explicitly establishes integration scope.

---

# 18. CI evidence classes and concurrency rules

```text
edited
!= committed
!= pushed
!= branch CI green
!= PR current/green
!= merged to main
!= exact-main release
!= licensed runtime PASS
```

Key concurrency lessons:

- a `409` stale-blob write conflict usually means another canonical owner pushed first;
- refresh instead of overwriting;
- a cancelled run after a newer head appears is stale evidence, not a product failure;
- never use an older green SHA as evidence for a changed head;
- do not close/recreate a canonical PR merely to make branch-CI timestamps look prettier;
- exact current candidate wins.

---

# 19. Repeated hardening lessons

- reject noncanonical/padded identity instead of silently aliasing;
- reject raw control characters before Trim can erase them;
- validate known collection counts against traversal counts;
- bound collections, strings, packages and input sizes;
- reject malformed Unicode/XML/package data fail-closed;
- preserve Element-ID ↔ Handle provenance;
- distinguish optional empty state from malformed non-empty identity;
- recompute integrity keys rather than trusting persisted/imported text;
- avoid silent truncation;
- preserve exact numeric predicates where binary64 cancellation changes topology;
- keep Core host-independent;
- keep native objects inside bounded document/transaction lifetimes;
- avoid queued command re-entry when it can lose PICKFIRST/document affinity;
- source/static guards must be backed by deterministic tests where feasible;
- local runtime findings can reveal defects that static guards miss.

---

# 20. Strong source capability vs remaining gaps

**Strong/substantial:**

- project/floor/zone/family semantics;
- author/capture paths;
- generated ownership;
- quantity/BQ;
- XLSX/CSV/reporting;
- provenance/reverse trace;
- locate/highlight/isolate;
- deterministic Core smoke;
- CI governance;
- explainable/formwork source contracts;
- extensive fail-closed hardening.

**Still active/incomplete/environment-dependent:**

- full licensed V25/V26 customer qualification;
- current Wall Snap Preview/Apply defects #3600/#3601;
- Direct Draw transient/repeated UX;
- richer native edit lifecycle;
- advanced multi-owner geometry;
- broader interoperability;
- historical BLT proxy/schema coverage;
- private-DWG acceptance;
- V26 release/runtime parity;
- selected coordination persistence/relink/product-pilot work.

---

# 21. Historical GitHub lifecycle outcomes

Important historical evidence:

## Customer Excel

- Issue #3296
- PR #3299
- protected run #32434789970 green
- merge `99dc024faafa4becc1a89fa61a894f69fba8aa49`

## Floating tool bound

- Issue #3303
- PR #3307
- run #32434196978 green
- landed `a8dbee08bd8dd0a6241c23cd47e02f485d528a13`

## Clash boundary regression

- Issue #3310
- PR #3312
- run #32435254406 green
- merge `c80405e4cd1e0530b16acf1e98d580ef4e76cd0c`

## Remote bug sweep

- Issue #3337
- PR #3338
- merge `ab9ef0fe761ce9ea243576b295359c304e5e33b4`

## Master context provenance

- Issue #3355 / PR #3357 — initial canonical master-context creation;
- Issue #3557 / PR #3558 — refresh through 2026-08-22;
- Issue #3583 — current refresh task for the same canonical file.

Old CI/run/head data in this file is historical evidence only. Refresh GitHub before acting.

---

# 22. Current repository snapshot

Snapshot time: 2026-08-23 (UTC+7).

At the moment this refresh lane was registered:

- `main = 227653b249e601961dab85bed12c7ce9a746ceb9`;
- latest main merge: PR #3602, sanitized LOCAL-007 P02 Wall Snap qualification evidence;
- open PR search returned `0`;
- #3599 remained open for local Wall Snap P02 rerun;
- #3600 remained open for Preview revision accounting;
- #3601 remained open for replacement-project cache cleanup.

This snapshot is intentionally time-bounded. Concurrent agents may advance `main` immediately after it is written.

---

# 23. Current master-note refresh task

- Issue: `#3583`
- Lane-Key: `issue-3583`
- owner/session: `chatgpt-gpt56sol-20260822-master-note1`
- branch: `agent/chatgpt-gpt56sol-20260822-master-note1/issue-3583-project-knowledge-note`
- starting baseline: `227653b249e601961dab85bed12c7ce9a746ceb9`
- file: `docs/QS3D-PROJECT-MASTER-CONTEXT-2026-08-21.md`
- scope: `ORDINARY_DOCS`
- production/source changes: none
- merge path: protected PR only
- required checks: current `preflight + core`
- runtime claim: none

This refresh updates the existing canonical repo file rather than creating a competing master document.

---

# 24. Future-session startup checklist

1. Read current `AGENTS.md`.
2. Read current `docs/AGENT-RUNTIME-CONTRACT.md`.
3. Read current main-write, CI, product, registration/collision/lifecycle rules.
4. Resolve current `origin/main` to exact SHA.
5. Check relevant live Issues/PRs/claims to avoid collision.
6. Reuse the canonical carrier if the Lane-Key already exists.
7. Inspect current source; treat this file as context/history.
8. Fix red CI on the same carrier.
9. Never use stale green evidence after head/base changes.
10. Never direct-write or force-update `main`.
11. Merge same-task PR only after fresh protected gates and expected-head verification.
12. Do not claim licensed runtime PASS from hosted CI.
13. Keep private DWGs, network details, activation/license secrets and proprietary artifacts out of Git.
14. If a local runtime finding creates a source-safe defect, hand it to a remote/source lane and rerun local qualification only on the new exact SHA.

---

# 25. Compact handoff

> QS3D-BricsCAD is a BricsCAD V25/V26 hosted BIM/QS plugin.
>
> Customer goal: author/capture → native 3D → quantity → explain/locate/highlight → Excel → Excel-to-CAD reverse trace with deterministic provenance and low-click review.
>
> Requirements workflow: Problem → Requirement → Solution → User Approval → Gap → Architecture → Plan → Task → Code → Test.
>
> BLT/BLT3D is a clean-room workflow/UX reference only. Historical BLT diagnostics showed real Solid3d geometry, B4D zero-quantity symptoms, identity/family-link issues and a process-local `s_listSemilerName` initialization experiment; none of that authorizes copying proprietary internals.
>
> Customer Excel uses `QS3DEXCEL` / `QS3DEXCELTRACE`, `DGKL`, `COP_PHA`, `CHI_TIET`, hidden `TRACE_MODEL`, Element ID + CAD Handle + drawing fingerprint + integrity validation.
>
> Excel lane #3296/#3299: run #32434789970 green; merge `99dc024faafa4becc1a89fa61a894f69fba8aa49`.
>
> Clash lane #3310/#3312: run #32435254406 green; merge `c80405e4cd1e0530b16acf1e98d580ef4e76cd0c`.
>
> Remote bug sweep #3337/#3338: merged at `ab9ef0fe761ce9ea243576b295359c304e5e33b4`.
>
> Current refresh baseline: `main@227653b249e601961dab85bed12c7ce9a746ceb9`.
>
> Current runtime tail: #3599 `LOCAL_PARTIAL / PENDING_REMOTE`; #3600 and #3601 require remote source fixes followed by exact-SHA licensed rerun.
>
> Repo lifecycle: direct-main forbidden; one canonical carrier; exact-head CI; protected current-candidate `preflight + core`; strict freshness; expected-head merge; no stale green; no false runtime PASS.

---

# 26. Provenance and truthfulness

Compiled from:

- prior QS3D chat/session context;
- prior canonical master context under #3355/#3357;
- refresh #3557/#3558;
- subsequent 2026-08-21/22/23 user-visible conversations;
- current repository governance;
- user-visible GitHub Issue/PR/CI/commit evidence;
- user-reported BLT3D, Windows and VPS troubleshooting observations.

This file intentionally does **not** contain private chain-of-thought.

It records conclusions, decisions, observed diagnostics, evidence and user-facing rationale needed for handoff.

Do not treat any recorded old PR/run/head SHA as current without refreshing GitHub first.

---

**END OF MASTER CONTEXT**
