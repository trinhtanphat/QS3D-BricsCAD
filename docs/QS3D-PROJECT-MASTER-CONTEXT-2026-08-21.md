# QS3D PROJECT MASTER CONTEXT — CHAT, REQUIREMENTS, IMPLEMENTATION & GITHUB HISTORY

**Project:** `trinhtanphat/QS3D-BricsCAD`  
**Master note date:** 2026-08-21 (UTC+7)  
**Purpose:** durable handoff / project knowledge base for future ChatGPT or agent sessions  
**Canonical task for this note:** Issue `#3355`, Lane-Key `issue-3355`  
**Status of this document:** repository context snapshot; current source and current GitHub state always win over stale historical wording

---

## 0. How to use this file

This file consolidates the project knowledge accumulated across recent QS3D conversations, especially the 2026-08-20 planning/requirements session and the 2026-08-21 implementation/CI/integration sessions.

Use it to avoid re-deriving the same requirements and decisions from scratch when a chat context window resets.

Important precedence:

1. current repository source;
2. current `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `CI_POLICY.md`, `docs/PRODUCT-BOUNDARY.md` and the other active governance documents;
3. current GitHub Issues/PRs/branches/checks;
4. this master context;
5. older chat wording / older patches / old handoffs.

This is a **knowledge and handoff document**, not proof of a licensed BricsCAD runtime PASS.

---

# 1. Executive project summary

QS3D is being developed as a BIM/QS workflow centered on fast quantity work inside BricsCAD.

The user's recurring product goal is:

> A QS engineer should be able to model or capture building elements, see and inspect the 3D model, calculate quantities, detect problematic geometry/quantity conditions, export to Excel, and trace a quantity/Excel row back to the exact CAD/BIM object with as few manual steps as practical (target: roughly 1–2 clicks for common review actions).

The major capability groups repeatedly requested are:

- project / floor / level / zone / family / type management;
- authoring or capturing BIM-like structural/architectural elements;
- native 3D geometry in BricsCAD;
- object identity and provenance;
- select / zoom / highlight / isolate / locate;
- clash / intersection / overlap review;
- quantity takeoff;
- explainable quantities and formwork quantities;
- readable Excel export;
- reverse trace `Excel → CAD`;
- Model / Quantity / Excel bidirectional traceability;
- duplicate / stale / missing quantity detection;
- save / reopen / recalculate stability;
- low-click workflow for a QS engineer.

A major theme of the implementation work is **reuse of one semantic/model/quantity truth**, not creation of parallel geometry or quantity engines.

---

# 2. Current product boundary — authoritative over historical wording

Historical conversation wording mentioned BIM3D, BricsCAD, AutoCAD, BLT3D, “app” / “giống BLT”, and possible standalone behavior.

The current repository decision is more specific.

## 2.1 `QS3D-BricsCAD` is a BricsCAD-hosted plugin

This repository ships a **Windows x64 BricsCAD plugin**, not a standalone QS3D CAD executable.

Host-specific assemblies:

- BricsCAD V25: `QS3D.BricsCAD.V25.dll`, `net48`;
- BricsCAD V26: `QS3D.BricsCAD.V26.dll`, `net8.0-windows`;
- shared `QS3D.Core`: currently `netstandard2.0`.

BricsCAD owns the native DWG database, editor/document lifecycle, viewport, selection, transactions and native CAD/3D APIs.

QS3D contributes commands, Ribbon, palettes / modeless WPF windows, semantic/project data, modeling/capture orchestration, quantity/reporting, recognition, guarded generated geometry workflows and plugin packaging/update logic.

## 2.2 Product-family split

```text
QS3D-Platform
vendor-neutral shared/domain layer
        |
        +-----------------------+
        |                       |
QS3D-BricsCAD              QS3D-CAD
BricsCAD plugin            standalone CAD/BIM/QS product
```

Therefore:

- “BLT-like” in this repo means **clean-room workflow/UX familiarity**, not “copy BLT” and not “ship QS3D.exe”;
- a future standalone desktop shell belongs to `QS3D-CAD`;
- shared vendor-neutral domain logic may progressively belong to `QS3D-Platform`;
- V25 binaries must never be relabeled as V26 binaries.

## 2.3 AutoCAD wording

AutoCAD appeared in the early product exploration, but the current shipping boundary of this repository is BricsCAD V25/V26. Any future AutoCAD support must be treated as a separate explicit requirement/adapter/product decision rather than assumed from old chat text.

---

# 3. Requirement-management workflow agreed in the original planning session

The original planning conversation established a strict analysis workflow so implementation does not outrun the actual business requirement.

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

The user is the final approver of important Problem / Requirement / Solution decisions.

Useful status labels:

- `[ĐÃ XÁC NHẬN]`
- `[SUY LUẬN]`
- `[CHƯA RÕ]`
- `[ĐỀ XUẤT]`
- `[MÂU THUẪN]`
- `[RỦI RO]`

Historical rule:

> Do not silently convert a proposed Requirement or Solution into a final product baseline merely because ChatGPT suggested it.

When a business requirement changes substantially, represent it as a Change Request rather than silently rewriting old requirements.

A recovery instruction was defined:

> `STOP implementation. Quay lại Requirement Mode.`

This means: restate what is confirmed, what is inferred, what is still unclear, and what has actually been approved before continuing.

---

# 4. Original Requirement Map

```text
QS3D / BIM Quantity System
│
├── A. MODEL
│   ├── Load / author / capture model
│   ├── Object identification
│   ├── Properties
│   └── Object mapping
│
├── B. 3D VIEW / REVIEW
│   ├── Select
│   ├── Zoom
│   ├── Highlight
│   ├── Isolate
│   ├── Hide
│   └── Live visualization
│
├── C. QUANTITY
│   ├── Quantity takeoff
│   ├── Rules
│   ├── Grouping
│   ├── Aggregation
│   ├── Explanation
│   └── Quantity → model trace
│
├── D. CHECK / QA
│   ├── Clash
│   ├── Overlap
│   ├── Duplicate quantity
│   ├── Missing quantity
│   └── Highlight error/source
│
├── E. EXCEL
│   ├── Export
│   ├── Import / read
│   ├── Sync / provenance
│   └── Excel → Model
│
├── F. QS WORKFLOW
│   ├── 1–2 click common review actions
│   ├── Filter
│   ├── Search
│   ├── Review
│   └── Audit trail
│
└── G. PLATFORM / HOST
    ├── BricsCAD V25/V26
    ├── Shared Core / Platform contracts
    ├── Performance
    ├── Persistence
    └── Runtime qualification
```

Current product-boundary documents determine which repository owns each capability.

---

# 5. Main user questions / requests and resulting answers or decisions

This is a summarized Q&A ledger rather than a word-for-word transcript.

## Q1 — “Look at the QS3D-BricsCAD source: does it already meet my BIM3D/QS requirements?”

### User intent

Evaluate the current repository against model authoring, 3D display, location, clash/intersection, overlap, highlight, Excel, reverse trace to model and optimized QS workflow.

### Answer / decision

The repository already contained substantial building blocks, so the direction is **harden and connect existing capabilities**, not rewrite the system.

Current-main truth documented by the active BIM3D-QS program includes:

- Project / Zone / Floor(Level) / Family / Type / semantic Element model;
- Direct Draw and guarded authoring/build paths;
- native BricsCAD `Solid3d` generation and ownership tracking;
- quantity/BQ commands and schedules;
- Excel/XLSX/CSV export paths with provenance in supported flows;
- locate/highlight/focus/isolate;
- persistence / regenerate / revision infrastructure.

The remaining work is mainly workflow completeness, deterministic contracts, customer-facing Excel, explainable quantities/formwork, native modify/edit, broader legacy/interoperability, runtime qualification and UX reduction of manual steps.

## Q2 — “How should I ask ChatGPT so it understands the full requirement before coding?”

### Answer / decision

Use a Problem → Requirement → Solution → Approval process.

For every important requirement capture:

- current problem;
- target user;
- input;
- user action;
- expected output;
- acceptance criteria;
- edge cases;
- dependencies;
- confirmed/unclear/proposed state.

A requirement should trace through:

```text
Requirement
→ Solution
→ Feature
→ Task
→ Test
```

Do not accept plans that are only generic “Phase 1 / Phase 2” lists with no traceability.

## Q3 — “Can QS3D be made to behave like the existing BLT3D EXE when the original source is gone?”

### Answer / decision

Yes for **clean-room business workflow and UX parity**, but not by copying proprietary implementation/assets.

Allowed direction:

- infer visible workflow, data contracts and user operations;
- reimplement behavior independently in QS3D;
- preserve QS3D's own semantic, geometry and quantity engines;
- use BLT/BLT3D as a familiarity/reference target.

Do not copy proprietary BLT source/assets, embed proprietary binaries as an implementation dependency, or guess unsupported legacy schemas.

## Q4 — “Add a QS3D menu for Excel export and Excel → CAD.”

### Historical early patch

An early patch changed the quantity tab/menu wording to `QS3D`, renamed the visible export action to `Xuất Excel`, and added `Excel → CAD`.

That early patch still used older command identities such as `QS3DED2` / `QS3DEXCELLOCATE`.

### Current answer / evolution

The later customer Excel workflow moved the visible compact quantity Ribbon to:

- `Xuất Excel` → `QS3DEXCEL`;
- `Excel → CAD` → `QS3DEXCELTRACE`.

Legacy commands remain compatibility surfaces where applicable, but are not the primary customer-facing buttons in the compact workflow.

## Q5 — “Export Excel and trace back to the model.”

### Answer / implementation direction

This became:

```text
3D / Quantity
→ Xuất Excel
→ workbook with provenance
→ select/read workbook trace
→ Excel → CAD
→ locate/select/highlight/zoom source object(s)
```

The customer workbook projection reuses the existing quantity engine.

Workbook contract includes:

- `DGKL`;
- `COP_PHA`;
- `CHI_TIET`;
- hidden `TRACE_MODEL`.

Trace provenance includes:

- semantic Element ID;
- canonical CAD Handle;
- drawing fingerprint;
- trace key / integrity binding.

Malformed/tampered identity is rejected instead of guessed.

## Q6 — “Can a quantity row be traced back to the model in one or two clicks?”

### Answer / decision

Yes, this is a core QS UX goal.

```text
Quantity / Excel row
→ provenance
→ CAD Handle / Element ID
→ live resolution
→ Select / Zoom / Highlight / Isolate
```

Grouped quantities must preserve the relationship between every Element ID and its source CAD Handle rather than storing only a lossy aggregate.

## Q7 — “Fix laggy logs when NETLOADing the BricsCAD plugin.”

### Current knowledge

This performance/problem statement was explicitly raised.

It should be treated as a plugin initialization/logging lifecycle defect, but this master note does **not** have sufficient exact current-source evidence to claim a specific final NETLOAD logging fix was landed.

Future sessions must inspect current startup/logging code and Git history before stating that this item is complete.

## Q8 — “Review the full chat, put the work into planning, implement, commit/push, check CI, then merge main.”

### Answer / repository lifecycle decision

```text
read current main + rules
→ find/reuse leaf Issue
→ Lane-Key = issue-N
→ one canonical agent branch
→ implementation/docs
→ commit + push
→ exact-head branch CI
→ one canonical PR
→ protected preflight + core on current candidate
→ strict freshness / mergeability
→ merge PR to main when authorized/standing same-task rule applies
→ refresh main
```

No direct contents write to `main`. No stale green CI. No duplicate carrier because a branch is inconvenient/red/behind.

## Q9 — “CI says quantity topbar/ribbon parity preflights fail; fix it.”

### Root cause

The new customer Ribbon intentionally used:

- `Xuất Excel / QS3DEXCEL`;
- `Excel → CAD / QS3DEXCELTRACE`;

while two legacy preflight guards still expected:

- `Xuất .blte2 / QS3DED2`;
- `Đối chiếu Cũ/Mới / QS3DREVDIFF`.

### Fix

Updated the two guard scripts to check the current visible customer commands while separately preserving compatibility guards for legacy command registration/behavior.

Important commits:

- `a34f4813099bb64aaf014f96d20337f9302aab2c`
- `90870cdb8c5dcf5192006c513102911d95602a0d`

## Q10 — “Continue fixing CI until green.”

### Important Excel-lane CI defects fixed

- nullable exporter fingerprint declaration;
- nullable ZIP entry lookup;
- nullable test helper return;
- V25 scope resolution around explicit “all scope” behavior;
- Excel text-cell maximum length;
- oversized `TRACE_MODEL` regression.

Notable session commits included:

- `cf4f97a02a3fc925168969ff960da7bbbb3578eb`
- `48d65f19f76967603ae566b38f8a7597a8c92df1`
- `ec19cc031374eacf283f3cd1a9400861e12bf12c`
- `b737713e7603a8f98c463a0f25a3f981dfc98c3d`
- `ee8fd86dd7cb400eb9e41917900283a0b9146cfb`

## Q11 — “Fix the clash-boundary PR CI and merge.”

### Root cause

A regression smoke used:

```csharp
input.Reverse().ToArray()
```

On the targeted API surface, overload resolution hit an in-place `Reverse()` returning `void`.

### Fix

```csharp
Enumerable.Reverse(input).ToArray()
```

Fix commit:

`442c24a50005645303af8e5f458731352da88054`

The later protected run passed deterministic smoke and V25 compile.

## Q12 — “Keep going through all PRs/issues/branches.”

### Answer / governance constraint

“Continue all” means continue the lifecycle of **authorized/canonical work**, not blindly mutate or close every open Issue.

Never steal another active canonical carrier, merge a LOCAL_ONLY lane from source-only evidence, close an umbrella/product gap merely to reduce the count, merge stale/red/unreviewed PRs, or create replacement PRs only to escape CI.

## Q13 — “Create one MD file with all knowledge/questions/answers, commit it to main, and give me the file.”

### Current task

This document is the result.

Canonical tracking:

- Issue `#3355`
- Lane-Key `issue-3355`
- branch `agent/interactive-20260821-0935-g56sol-context/issue-3355-project-master-context`

Repo policy has no docs-only direct-main exception, so it must land through a PR with required protected checks.

---

# 6. BIM3D-QS customer golden path

The active umbrella program is Issue `#3142`.

The current customer-first product priority is **BIM3D + quantity export**, not 4D/5D expansion.

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
→ Định vị / Diễn giải
→ Xuất Excel
```

P0 categories include:

- ArchitecturalWall;
- Beam;
- Column;
- Slab;
- StructuralWall;
- Foundation;
- Door / WallOpening for deductions/traceability.

P0 quantity truth includes fields only where meaningful:

- count;
- length;
- gross/net area;
- gross/net volume;
- opening/deduction evidence;
- material/effective material;
- Floor/Zone/Family/category;
- unit conversion;
- source Handle;
- semantic Element ID;
- drawing fingerprint.

Deferred from this P0 customer milestone:

- 4D scheduling;
- 5D cost/rates/claims;
- Primavera/MS Project/ERP;
- generalized IFC/RVT round-trip completeness;
- broad AI automation;
- full MEP/fabrication parity;
- standalone CAD behavior in this repository.

---

# 7. Customer Excel / reverse-trace implementation knowledge

## 7.1 Canonical customer Excel lane

Historical lane:

- Issue `#3296`
- PR `#3299`
- branch `agent/chatgpt-gpt56sol/customer-excel-trace-3296`

Key files recorded in that PR:

- `docs/BIM3D-QS-CUSTOMER-EXCEL-TRACE-PLAN-3296.md`
- `scripts/preflight-customer-excel-trace.py`
- `scripts/preflight-quantity-topbar-reference.py`
- `scripts/preflight-ribbon-quantity-reference-parity.py`
- `src/QS3D.BricsCAD.V25/CustomerExcelCommands.cs`
- `src/QS3D.BricsCAD.V25/Ribbon/QuantityReferenceRibbonAugmenter.cs`
- `src/QS3D.BricsCAD.V25/Services/ExcelLocateResolutionService.cs`
- `src/QS3D.Core/Export/QsCustomerWorkbookExporter.cs`
- `src/QS3D.Core/Export/QsCustomerWorkbookTraceReader.cs`
- focused Core smoke tests.

## 7.2 Provenance/integrity rules

1. grouped `Count` must match grouped Element IDs;
2. CAD Handles are validated as canonical unsigned 64-bit identities;
3. trace key is recomputed/verified;
4. malformed/tampered trace provenance is rejected;
5. aggregate/details scope stays coherent;
6. generated Excel text cells obey the Excel text limit;
7. grouped Element ID ↔ Handle relationships remain unambiguous.

The core customer promise is not just “export a spreadsheet”; it is:

```text
report row
↔ semantic source
↔ original/current drawing
↔ CAD Handle
↔ model object
```

---

# 8. Explainable quantities and formwork

The user repeatedly identified formwork / quantity completeness as important BLT-like workflow gaps.

Fresh `main` snapshot while creating this note:

`6d7bd0870eabb4d6e42da8184924cdc81a7fb3e9`

Latest verified commit message:

`feat(quantity): add explainable formwork engine (#3350)`

The commit states that the Core formwork calculation contract works over host-measured faces, reuses existing quantity rules/deduction gates, produces deterministic per-face trace and `FormworkM2`, and passed protected `preflight + core` on its exact candidate.

This is important progress toward the earlier formwork gap, but source/Core evidence is still not identical to customer qualification on arbitrary historical/private DWGs.

---

# 9. BLT / BLT3D clean-room knowledge

BLT3D matters because the user wants QS3D to feel operationally familiar for modeling, quantity, formwork, Excel, reverse review and efficient QS workflow.

The old source is unavailable, so use independently observable/public behavior and independently understood engineering/data contracts.

Do not copy proprietary source/assets, make the EXE an implementation dependency, or manufacture unsupported category mappings.

Fresh open PR snapshot includes:

- Issue `#3352`
- PR `#3354`
- `feat: import legacy BLT3D objects without redraw`

Its stated goal is to open old BLT3D-authored DWGs without redraw and recognize supported legacy structural objects into the existing QS3D semantic/quantity pipeline.

It keeps source objects read-only/non-destructive and treats unsupported/ambiguous legacy schemas as fail-closed.

Full historical schema/category mapping and real proxy behavior still require a real historical BLT3D DWG in licensed BricsCAD.

---

# 10. Important CI / bug-fix timeline

This is not a complete Git history.

## 10.1 Earlier integration batch

PR `#3295` was recorded as merged earlier.

Recorded merge SHA:

`db7cc6f15a828d166731cee8011dd5289e948422`

## 10.2 Customer Excel lane — `#3296 / #3299`

Major stages:

- Ribbon/customer workbook implementation;
- stale legacy parity guards caused preflight failures;
- guards updated in `a34f4813...` and `90870cdb...`;
- Core nullable compilation defects fixed;
- workbook cell-size/tamper/integrity regressions added;
- V25 command scope behavior fixed;
- stale/cancelled runs discarded whenever the head changed.

Final protected evidence recorded:

- run `#32434789970`
- `preflight = SUCCESS`
- `core = SUCCESS`
- deterministic smoke = SUCCESS
- BricsCAD V25 plugin compile = SUCCESS

Merge:

`99dc024faafa4becc1a89fa61a894f69fba8aa49`

Issue `#3296` auto-closed/completed.

## 10.3 Floating tool bound — `#3303 / #3307`

Recorded protected run:

`#32434196978`

Recorded landed SHA:

`a8dbee08bd8dd0a6241c23cd47e02f485d528a13`

Issue `#3303` closed/completed.

## 10.4 Clash regression boundary — `#3310 / #3312`

Fix commit:

`442c24a50005645303af8e5f458731352da88054`

Final protected evidence:

- run `#32435254406`
- `preflight = SUCCESS`
- `core = SUCCESS`
- deterministic smoke = SUCCESS
- V25 plugin compile = SUCCESS

Recorded merge:

`c80405e4cd1e0530b16acf1e98d580ef4e76cd0c`

Issue `#3310` closed/completed.

## 10.5 Later remote bug sweep integration — `#3338`

Tracking Issue `#3337`, integration PR `#3338`, branch `integration/20260821-remote-bug-sweep`.

The integration PR accumulated many source-safe fixes including Takeoff handle canonicality, estimating provenance coherence, grouped workbook provenance, audit bounds and other deterministic hardening.

Fresh snapshot when this note was created:

- PR `#3338` is **OPEN**
- `merged = false`
- `mergeable = true`
- displayed head SHA `5667e27b137afdf8d668b9bd6f915e3bd21d2138`
- displayed base SHA `5446d92dfb1216e4c7f064d7803a15b7dfe30dde`

Current `main` has advanced beyond that base, so old green/failure conclusions must not be reused.

---

# 11. Current open-PR snapshot at note creation

Current `main`:

`6d7bd0870eabb4d6e42da8184924cdc81a7fb3e9`

| PR | Scope | Target | Important note |
|---|---|---|---|
| `#3354` | legacy BLT3D objects without redraw | `main` | clean-room adapter; real legacy DWG remains PENDING_LOCAL |
| `#3353` | XLSX shared-string support in customer trace reader | `main` | trace/provenance checks preserved |
| `#3346` | repository-professionalism input hardening | `main` | governance/script lane |
| `#3345` | AuditTrail aggregate text payload bound | `integration/20260821-remote-bug-sweep` | integration leaf |
| `#3344` | floating interaction-surface state bound | `integration/20260821-remote-bug-sweep` | integration leaf |
| `#3338` | remote bug-sweep combined integration | `main` | still open; refresh against newer main |

The existence of these PRs does not imply this documentation lane owns or may rewrite them.

---

# 12. Important active/open program and product-gap Issues

- `#3142` — BIM3D-QS customer-first modeling → quantity export MVP. Umbrella only; each concrete child has its own leaf Issue.
- `#72` — licensed V25 exact-SHA qualification. Remote/static CI cannot substitute for this.
- `#74` — Direct Draw transient preview / repeated authoring. Product/runtime UX gap.
- `#73` — advanced/multi-owner wall geometry ownership. Native materialization remains runtime-sensitive.
- `#84` — broader interoperability/import-export. Substantial semantic work exists but full round-trip/native coverage is incomplete.

Do not close these merely because related source code exists.

---

# 13. Repository collaboration rules — condensed operational contract

## 13.1 `main` is PR-only

No direct contents write, direct ref update, force push or equivalent task write to `main`, including docs/Markdown.

## 13.2 Same-task standing merge lifecycle

For normal owner-requested work, once the current canonical PR is current, fresh, mergeable and required checks are green, the same-task owner session should proactively merge unless the user explicitly opted out.

## 13.3 Required protected checks

- `preflight`
- `core`

Strict freshness applies. Older green SHA/cancelled/stale runs do not count.

## 13.4 Lane identity

```text
Issue #N
Lane-Key: issue-N
Branch: agent/<unique-owner-token>/issue-N-<scope>
PR: allocated later
```

Never guess Issue/PR numbers or reuse an umbrella number as the child implementation identity.

## 13.5 One canonical carrier

One semantic task → one active owner, one canonical branch, one canonical PR.

A red/behind/inconvenient branch is not a reason to create a competing carrier.

## 13.6 Concurrent work

If another canonical owner pushes the same file while a session is editing: do not overwrite; refresh; preserve the winning implementation; continue only under ownership/coordination rules.

## 13.7 Branch and PR CI

`agent/**` and `integration/**` receive automatic branch CI.

PRs receive protected candidate validation.

A known red exact-head branch failure must be investigated on the same carrier.

## 13.8 Multi-agent integration

Use `integration/<batch-id>` where appropriate and validate the combined tree.

---

# 14. CI evidence classes — do not confuse them

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

- branch CI green proves one exact branch SHA;
- PR CI green proves one current PR candidate;
- merged main proves landing;
- cloud/release CI proves release/build/package checks for an exact SHA;
- licensed runtime PASS requires actual host execution;
- private-DWG/customer acceptance requires the specified fixtures/scenarios.

---

# 15. Runtime qualification boundary

Remote agents can prove source contracts, deterministic Core tests, static preflights, build correctness, compile compatibility and package/source integrity.

Remote agents cannot honestly prove without execution:

- licensed BricsCAD `NETLOAD`;
- DemandLoad on a real workstation;
- real WPF/Ribbon/palette interaction;
- native editor/command lifecycle;
- real Undo/Redo;
- save/reopen on representative/private DWGs;
- proxy behavior of historical BLT3D objects;
- DPI/multi-monitor behavior;
- signing credentials;
- clean-machine deployment.

Use `PENDING_LOCAL` / `LOCAL_ONLY` where appropriate.

---

# 16. Historical early Excel menu patch vs current source

A prior session produced `QS3D-Excel-Menu.patch`.

Historical intent:

- rename quantity tab to `QS3D`;
- change visible `.blte2` wording to `Xuất Excel`;
- add `Excel → CAD`.

It is useful as requirement history but is **not current implementation truth**.

Later customer Excel work evolved routing to `QS3DEXCEL` and `QS3DEXCELTRACE`, with older commands retained as compatibility behavior rather than primary customer buttons.

---

# 17. Quantity / engineering data expectations

A sample quantity discussion used fields such as:

- element;
- type;
- floor;
- length;
- width;
- height;
- concrete volume `BT m³`;
- formwork `VK m²`.

These values must come from authoritative geometry/quantity rules with units and provenance.

Never compute nice-looking rows from bounding-box guesses when exact semantic/native evidence is required.

For historical/proxy/legacy content, lack of authoritative evidence should result in blocked/unsupported/auditable output rather than fabricated BT/VK.

---

# 18. Architecture principles inferred from the work

## One semantic truth

Do not create separate parallel models for CAD, quantity, Excel and trace-back. Maintain explicit mappings.

## Provenance is a first-class domain concern

Element ID, CAD Handle, drawing fingerprint, trace key and generated ownership must be canonical, validated and fail-closed.

## Determinism over convenience

Repeated hardening themes:

- reject padded/noncanonical identity;
- validate collection counts before iteration/order;
- bound inputs;
- reject malformed package/XML data;
- do not normalize semantic IDs into ambiguous aliases;
- no silent truncation.

## Host isolation

Keep Core/domain logic testable without BricsCAD references. Keep native types behind host adapters.

## Compatibility without UI clutter

Legacy commands can remain callable/guarded while the customer Ribbon presents a simpler modern workflow.

---

# 19. What is already strong vs still a gap

## Strong / substantial source capability

- project/floor/zone/family semantic model;
- multiple Direct Draw/capture/build paths;
- native Solid3d ownership;
- quantity/BQ infrastructure;
- Excel/XLSX/CSV infrastructure;
- provenance and reverse trace;
- locate/highlight/isolate;
- deterministic Core testing;
- strict CI governance;
- explainable formwork engine on current main;
- extensive fail-closed hardening.

## Still active / incomplete / environment-dependent

- exact customer runtime qualification on licensed BricsCAD;
- full historical BLT3D DWG schema/proxy interoperability;
- repeated/transient Direct Draw UX;
- rich native modify/edit flow;
- advanced multi-owner/native geometry;
- broader interoperability formats/round-trip completeness;
- some V25/V26 parity/runtime surfaces;
- private-DWG acceptance;
- current open integration/leaf PRs;
- future AutoCAD support if explicitly required;
- standalone desktop behavior, which belongs to `QS3D-CAD`.

---

# 20. Recommended next-session startup checklist

1. read this document;
2. read current `AGENTS.md`;
3. read current `docs/MAIN-WRITE-AUTHORIZATION.md`;
4. read current `docs/PRODUCT-BOUNDARY.md`;
5. read current `CI_POLICY.md`;
6. fetch current `main` and record exact SHA;
7. search relevant open Issues/PRs;
8. check Lane-Key ownership;
9. inspect current source for the feature;
10. treat old implementation details as history unless current source confirms them;
11. never create a duplicate carrier;
12. continue remote-safe work only on the canonical remote lane;
13. hand off licensed/private-fixture scenarios instead of claiming PASS;
14. follow full PR/check/freshness lifecycle to `MERGED_MAIN` when same-task policy authorizes it.

---

# 21. Compact master context for a future chat

> Project: `trinhtanphat/QS3D-BricsCAD`.
>
> QS3D-BricsCAD is a BricsCAD V25/V26 hosted plugin, not a standalone EXE. Standalone belongs to `QS3D-CAD`; vendor-neutral shared/domain work belongs progressively to `QS3D-Platform`.
>
> Product goal: optimize a BIM3D/QS engineer workflow: author/capture model → native 3D → quantity → explain/locate/highlight → Excel → Excel-to-CAD reverse trace, with deterministic provenance and common review actions around 1–2 clicks.
>
> Historical BA workflow: Problem P-xxx → Requirement R-xxx → Solution S-xxx → User Approval → Gap Analysis → Architecture → Plan → Task → Code → Test.
>
> BLT/BLT3D is clean-room workflow/UX reference only.
>
> Customer Excel uses `QS3DEXCEL` and `QS3DEXCELTRACE` with Element ID + CAD Handle + drawing fingerprint + trace integrity.
>
> Completed major lane: #3296/#3299 customer workbook + reverse trace; run #32434789970 green; merge `99dc024faafa4becc1a89fa61a894f69fba8aa49`.
>
> Completed major lane: #3310/#3312 clash-boundary smoke; run #32435254406 green; recorded merge `c80405e4cd1e0530b16acf1e98d580ef4e76cd0c`.
>
> Current-main snapshot while preparing this note: `6d7bd0870eabb4d6e42da8184924cdc81a7fb3e9`, latest commit adds explainable formwork.
>
> At note creation #3338, #3344, #3345, #3346, #3353 and #3354 were open; always refresh live state.
>
> Repo rules: main PR-only; one Issue/Lane-Key/branch/PR per task; new leaf Lane-Key = `issue-N`; automatic branch CI; protected merge requires current `preflight + core` SUCCESS, strict freshness and mergeability; no stale green; no false licensed runtime PASS.
>
> Runtime qualification remains separate, especially Issue #72.

---

# 22. Source / provenance of this master note

Compiled from:

- prior `QS3D_CHAT_SESSION_CONTEXT_2026-08-20.md`;
- subsequent QS3D conversations on 2026-08-20 and 2026-08-21;
- current repository governance/product-boundary documents;
- active BIM3D-QS program Issue `#3142`;
- relevant GitHub PR/Issue/CI/commit evidence observed during implementation;
- fresh `main` and open-PR snapshot taken while creating Issue `#3355`.

Do not treat a recorded historical PR/run SHA as proof that the same branch is still current. Refresh GitHub before acting.

---

# 23. Canonical status of this note task

- Issue: `#3355`
- Lane-Key: `issue-3355`
- Owner/session: `interactive-20260821-0935-g56sol-context`
- Branch: `agent/interactive-20260821-0935-g56sol-context/issue-3355-project-master-context`
- Intended file: `docs/QS3D-PROJECT-MASTER-CONTEXT-2026-08-21.md`
- Scope: docs-only master context
- Main-write method: protected PR only
- Merge condition: exact-current `preflight + core` SUCCESS + strict freshness + mergeable PR
- Release impact: ordinary docs-only note; no licensed runtime/release claim

---

**END OF MASTER CONTEXT**
