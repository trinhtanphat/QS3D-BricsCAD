# QS3D PROJECT MASTER CONTEXT — CHAT, REQUIREMENTS, IMPLEMENTATION, CI & GITHUB HISTORY

**Project:** `trinhtanphat/QS3D-BricsCAD`  
**Updated through:** 2026-08-22 (UTC+7)  
**Purpose:** canonical durable handoff / project knowledge base for future ChatGPT and agent sessions  
**Refresh task:** Issue `#3557`, Lane-Key `issue-3557`  
**Authoring baseline:** `main@6432dbd209b6ebde8282852eaf0603028bc3d84b`  
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

This note excludes private IP/VPS details, passwords, activation credentials, proprietary source/binaries, private DWGs, unsanitized runtime artifacts and hidden reasoning.

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

Historical AutoCAD wording does not mean this repo currently ships AutoCAD support. “BLT-like” / “giống BLT” means **clean-room workflow/UX familiarity** only; never copy proprietary BLT/BLT3D source/resources or make proprietary binaries an implementation dependency.

---

# 3. Requirements workflow agreed with the user

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

Useful labels: `[ĐÃ XÁC NHẬN]`, `[SUY LUẬN]`, `[CHƯA RÕ]`, `[ĐỀ XUẤT]`, `[MÂU THUẪN]`, `[RỦI RO]`.

Requirement traceability:

```text
Requirement → Solution → Feature → Task → Test
```

Do not silently convert an assistant proposal into an approved product baseline. Recovery phrase: `STOP implementation. Quay lại Requirement Mode.`

---

# 4. Requirement map

```text
MODEL → author/capture → identity/properties/mapping
3D REVIEW → select/zoom/highlight/isolate
QUANTITY → rules/grouping/aggregation/explanation/model trace
CHECK/QA → clash/overlap/duplicate/missing/highlight source
EXCEL → export/read/provenance/Excel→Model
QS WORKFLOW → low-click/filter/search/review/audit
PLATFORM/HOST → V25/V26/Core/persistence/performance/runtime qualification
```

---

# 5. Main user questions and answers/decisions

This is a summarized Q&A ledger, not a word-for-word transcript.

## Q1 — Does existing source already meet BIM3D/QS needs?

**Answer:** it already contained major building blocks. Harden/connect existing semantic, generated geometry, quantity, export, provenance and review capabilities instead of rewriting from zero.

## Q2 — How should requirements be communicated before coding?

Use Problem → Requirement → Solution → Approval. Record user, input, action, output, acceptance, edge cases, dependencies and confirmation state; require traceability to implementation/tests.

## Q3 — Can QS3D behave like BLT3D without original source?

Yes for clean-room workflow and independently observable behavior. No copying proprietary source/assets/internal implementation. Preserve QS3D’s own semantic, geometry and quantity engines.

## Q4 — Add QS3D Excel menu and Excel → CAD.

Customer-facing compact Ribbon evolved to:

- `Xuất Excel` → `QS3DEXCEL`;
- `Excel → CAD` → `QS3DEXCELTRACE`.

Legacy commands such as `QS3DED2` / `QS3DREVDIFF` may remain callable/guarded without cluttering the primary customer Ribbon.

## Q5 — Export Excel and trace back to the model.

```text
3D / Quantity
→ Xuất Excel
→ workbook with provenance
→ TRACE_MODEL
→ Excel → CAD
→ validate live identity
→ select / highlight / zoom source object(s)
```

The workbook projection reuses existing quantity truth rather than introducing a second engine.

## Q6 — Can a quantity row locate the model in 1–2 clicks?

Yes. Use Element ID + CAD Handle + drawing fingerprint + integrity evidence, validate live state, then Select / Zoom / Highlight / Isolate. Grouped rows must retain complete Element-ID ↔ Handle mapping.

## Q7 — Fix laggy logs during `NETLOAD`.

This is a real runtime/performance concern but chat history alone is not sufficient proof of a final fix. Inspect current startup/logging code and validate in the licensed host before claiming completion.

## Q8 — Review everything, implement, commit/push, fix CI and merge main.

Repository-safe lifecycle:

```text
read current main + rules
→ collision check
→ one Issue/Lane-Key
→ one canonical branch
→ implement/docs
→ push + exact-head branch CI
→ one canonical PR
→ protected preflight + core
→ strict freshness + mergeability
→ expected-head merge
→ refresh main
```

No direct task write to `main`.

## Q9 — Quantity topbar/ribbon parity preflights fail; fix them.

New Ribbon correctly used `QS3DEXCEL` / `QS3DEXCELTRACE`; two legacy guards still expected `QS3DED2` / `QS3DREVDIFF` as visible buttons. Fix the guard expectations while separately retaining legacy command compatibility guards.

Key commits:

- `a34f4813099bb64aaf014f96d20337f9302aab2c`
- `90870cdb8c5dcf5192006c513102911d95602a0d`

## Q10 — Continue fixing CI until green.

Follow-on Excel-lane defects included nullable annotations, nullable ZIP lookup, nullable smoke helper, V25 scope handling, Excel text-cell limit and oversized `TRACE_MODEL` regression.

Recorded commits:

- `cf4f97a02a3fc925168969ff960da7bbbb3578eb`
- `48d65f19f76967603ae566b38f8a7597a8c92df1`
- `ec19cc031374eacf283f3cd1a9400861e12bf12c`
- `b737713e7603a8f98c463a0f25a3f981dfc98c3d`
- `ee8fd86dd7cb400eb9e41917900283a0b9146cfb`

Exact current head wins. Cancelled CI after a newer push is stale evidence, not a code failure.

## Q11 — Fix clash-boundary CI.

`input.Reverse().ToArray()` bound to an in-place `Reverse()` returning `void`, causing `CS0023`. Correct fixture: `Enumerable.Reverse(input).ToArray()`. Fix commit: `442c24a50005645303af8e5f458731352da88054`.

## Q12 — Continue all PRs/issues/branches and merge main.

“Continue all” means continue authorized/canonical lifecycle work. It does not justify stealing unrelated active carriers, using stale green CI, closing LOCAL_ONLY gaps without evidence or fabricating completion.

## Q13 — Can BricsCAD/license-server setup help local-only builds?

Treat activation/licensing as private environment configuration, not repository source. Automation may consume an already authorized licensed environment but must never commit credentials, activation secrets, proprietary DLLs or implement bypass behavior.

## Q14 — BLTFAMILYFIX runtime experiment.

A diagnostic showed an in-memory family dictionary could be initialized without modifying the DLL on disk; restart rolls back RAM-only state. This is useful process-local diagnostic evidence, not a durable QS3D implementation or permission to commit proprietary BLT internals.

## Q15 — Put all project knowledge/Q&A in one Markdown file on main.

Current refresh is Issue `#3557`, Lane-Key `issue-3557`. Repo policy has no docs-only direct-main exception, so this note lands via branch/PR and protected `preflight + core`.

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
Tạo mới / Capture → tham số chính → 3D → Khối lượng → Định vị/Diễn giải → Xuất Excel
```

P0 categories repeatedly identified: ArchitecturalWall, Beam, Column, Slab, StructuralWall, Foundation, Door / WallOpening.

P0 quantity truth should expose only meaningful fields such as count, length, gross/net area, gross/net volume, opening/deduction evidence, effective material, Floor/Zone/Family/category, units and source provenance.

4D/5D, ERP/Primavera/MS Project, generalized IFC/RVT completeness, broad AI automation and standalone CAD behavior are not the first BIM3D/QS critical path.

---

# 7. Customer Excel / reverse trace

Historical canonical lane:

- Issue `#3296`
- PR `#3299`
- branch `agent/chatgpt-gpt56sol/customer-excel-trace-3296`

Workbook sheets: `DGKL`, `COP_PHA`, `CHI_TIET`, hidden `TRACE_MODEL`.

Integrity contract:

1. grouped Count matches grouped Element IDs;
2. Handles are canonical unsigned 64-bit identity text;
3. drawing fingerprint binds trace to intended drawing;
4. TRACE_KEY is recomputed/verified;
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

Merge: `99dc024faafa4becc1a89fa61a894f69fba8aa49`. Issue `#3296` closed/completed.

---

# 8. Other major CI/fix outcomes

## Earlier integration batch

PR `#3295`, recorded merge `db7cc6f15a828d166731cee8011dd5289e948422`, integrated many earlier source-safe PRs including #3078, #3235, #3106, #3011, #2966, #3045, #3029, #3012, #3000, #2902, #2929, #2912, #2896, #2878, #2886 and a clean transplant of #2871.

## Floating tool work-area bound

Issue `#3303`, PR `#3307`, protected run `#32434196978` green, landed SHA `a8dbee08bd8dd0a6241c23cd47e02f485d528a13`.

## Clash regression boundary

Issue `#3310`, PR `#3312`, fix `442c24a50005645303af8e5f458731352da88054`, protected run `#32435254406` green, merge `c80405e4cd1e0530b16acf1e98d580ef4e76cd0c`.

## Remote bug sweep integration

Tracking Issue `#3337`, PR `#3338`, branch `integration/20260821-remote-bug-sweep`.

It assembled source-safe hardening for Takeoff identity, FeatureFlags names, preview-review bounds, Start Center bookkeeping, aggregate-preflight environment hardening, diagnostic/count contracts, curtain/frame bounds, revision identity, workflow scanner hardening, estimating provenance, grouped workbook provenance and audit payload bounds.

Historical nuance: intermediate CI included red source-guard runs. Those are stale diagnostics after landing.

Live verification on 2026-08-22:

- PR `#3338` closed and `merged = true`;
- final integration head `8f490d4581330607e8a8b7c8878b3069870574ab`;
- merge commit `ab9ef0fe761ce9ea243576b295359c304e5e33b4`.

Do not treat old run `#32437200807` as a current blocker after this merge.

---

# 9. 2026-08-22 repository evolution

Authoring baseline for this refresh:

`main@6432dbd209b6ebde8282852eaf0603028bc3d84b`

That commit merged PR `#3549`: `fix(updater): preserve registered DemandLoad mode`.

Therefore old open-PR tables and run status from the 2026-08-21 note are historical only. Future agents must refresh live state before acting.

Active product themes visible on 2026-08-22 include BIM3D/QS production-pilot coordination (`#3468`), licensed V25 qualification (`#72`), V26 qualification/build parity (`#1462`, `#3550`, `#3553`), Direct Draw transient/repeated authoring (`#74`), native modify/edit (`#80`), interoperability (`#84`), coordination persistence/relink and continued fail-closed hardening. This documentation lane does not own those carriers.

---

# 10. Explainable quantity/formwork and engineering truth

Formwork/quantity explanation was repeatedly identified as a major BLT-like gap. Later source work added an explainable formwork engine using host-measured faces with deterministic trace.

Source/Core tests can prove deterministic contracts but not arbitrary/private DWG geometry or licensed interactive UX.

Engineering quantities must come from authoritative semantic/native geometry and unit rules. Do not generate plausible-looking BT/VK values from bounding-box guesses when exact evidence is required.

---

# 11. Clean-room BLT / legacy rules

Allowed: observe user-visible behavior, infer independent business workflows/data contracts and reimplement independently inside QS3D architecture.

Forbidden: copy proprietary source/resources, commit proprietary binaries, rely on undocumented internals as authority or invent unsupported legacy mappings.

Historical/private BLT DWG/proxy behavior remains a licensed/private-fixture qualification boundary where applicable.

---

# 12. Runtime qualification boundary

Remote/source agents can prove source contracts, deterministic Core tests, preflight guards, build/compile compatibility and package/source integrity.

They cannot honestly infer without execution: licensed `NETLOAD`, real DemandLoad startup, Ribbon/WPF/palette interaction, native editor lifecycle, Undo/Redo, SaveAs+cold reopen, multi-DWG, DPI/multi-monitor, historical proxy behavior, signing/trust, clean-machine install/update/uninstall or private-DWG/customer acceptance.

Use `LOCAL_ONLY` / `PENDING_LOCAL` appropriately. Managed-reference V25 compile is not licensed interactive runtime PASS.

---

# 13. Repository collaboration / Git lifecycle

Current policy at authoring baseline:

- `main` is direct-write read-only; no docs-only exception;
- one owner prompt → one task carrier by default;
- normal Lane-Key = `issue-N`;
- if an equivalent active carrier exists: `DUPLICATE_CARRIER / NO MUTATION`;
- a red current-carrier CI triggers diagnose/fix/push/recheck on the same carrier;
- protected merge requires current `preflight + core`, strict freshness, collision/review cleanliness, mergeability and expected-head match;
- ordinary docs still receive branch/PR CI and protected `preflight + core`;
- normal owner-task endpoint is `MERGED_MAIN` unless explicitly opted out or truly blocked.

---

# 14. CI evidence classes

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

A 409/stale-blob conflict is a concurrency signal: refresh and preserve newer canonical work. A cancelled run after a newer head appears is stale, not a product failure.

---

# 15. Repeated hardening lessons

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
- avoid queued command re-entry when it can lose PICKFIRST/document affinity.

---

# 16. Strong source capability vs remaining gaps

**Strong/substantial:** project/floor/zone/family semantics, author/capture paths, generated ownership, quantity/BQ, XLSX/CSV/reporting, provenance/reverse trace, locate/highlight/isolate, deterministic Core smoke, strict CI governance, explainable/formwork source contracts and extensive fail-closed hardening.

**Still active/incomplete/environment-dependent:** full licensed V25/V26 customer qualification, Direct Draw transient/repeated UX, richer native edit lifecycle, advanced multi-owner geometry, broader interoperability, historical BLT proxy/schema coverage, private-DWG acceptance, V26 release parity and selected coordination persistence/relink/product-pilot work.

---

# 17. Operational questions intentionally excluded from repo secrets

VPS/RDP troubleshooting is unrelated infrastructure and private IP/network details should not be copied into this project knowledge base.

BricsCAD activation/license-server material is private environment configuration. Document capability boundaries/handoffs only; never commit secrets, credentials, proprietary license artifacts or bypass mechanisms.

---

# 18. Current master-note refresh task

- Issue: `#3557`
- Lane-Key: `issue-3557`
- owner/session: ChatGPT GPT-5.6 Sol / current owner prompt
- branch: `agent/chatgpt-gpt56sol/project-master-context-3557`
- baseline: `6432dbd209b6ebde8282852eaf0603028bc3d84b`
- file: `docs/QS3D-PROJECT-MASTER-CONTEXT-2026-08-21.md`
- scope: `ORDINARY_DOCS`
- production/source changes: none
- merge path: protected PR only
- required checks: current `preflight + core`
- runtime claim: none.

Prior context provenance: `QS3D_CHAT_SESSION_CONTEXT_2026-08-20.md` and prior canonical master-note task Issue `#3355` / PR `#3357`. This refresh updates the same canonical repo file rather than creating a competing master document.

---

# 19. Future-session startup checklist

1. Read current `AGENTS.md` and `docs/AGENT-RUNTIME-CONTRACT.md`.
2. Read current main-write, CI, product, registration/collision/lifecycle rules.
3. Resolve current `origin/main` to exact SHA.
4. Check only enough live Issues/PRs/claims to avoid collision.
5. Reuse canonical carrier if Lane-Key already exists.
6. Inspect current source; treat this file as context/history.
7. Fix red CI on the same carrier.
8. Never use stale green evidence after head/base changes.
9. Never direct-write or force-update `main`.
10. Merge same-task PR only after fresh protected gates and expected-head verification.
11. Do not claim licensed runtime PASS from hosted CI.
12. Keep private DWGs, network details, activation/license secrets and proprietary artifacts out of Git.

---

# 20. Compact handoff

> QS3D-BricsCAD is a BricsCAD V25/V26 hosted BIM/QS plugin. Customer goal: author/capture → native 3D → quantity → explain/locate/highlight → Excel → Excel-to-CAD reverse trace with deterministic provenance and low-click review.
>
> Requirements workflow: Problem → Requirement → Solution → User Approval → Gap → Architecture → Plan → Task → Code → Test.
>
> BLT/BLT3D is clean-room workflow/UX reference only.
>
> Customer Excel uses `QS3DEXCEL` / `QS3DEXCELTRACE`, `DGKL`, `COP_PHA`, `CHI_TIET`, hidden `TRACE_MODEL`, Element ID + CAD Handle + drawing fingerprint + integrity validation.
>
> Major Excel lane #3296/#3299: run #32434789970 green; merge `99dc024faafa4becc1a89fa61a894f69fba8aa49`.
>
> Clash lane #3310/#3312: run #32435254406 green; merge `c80405e4cd1e0530b16acf1e98d580ef4e76cd0c`.
>
> Remote bug sweep #3337/#3338: live verified merged at `ab9ef0fe761ce9ea243576b295359c304e5e33b4`; old red run #32437200807 is stale historical evidence.
>
> 2026-08-22 refresh authoring baseline: `main@6432dbd209b6ebde8282852eaf0603028bc3d84b`.
>
> Repo lifecycle: direct-main forbidden; one canonical carrier; exact-head CI; protected current-candidate `preflight + core`; strict freshness; expected-head merge; no stale green; no false runtime PASS.

---

# 21. Provenance and truthfulness

Compiled from the prior 2026-08-20 chat/session context, prior canonical master context under #3355/#3357, subsequent 2026-08-21/22 QS3D conversations, current repository governance, and user-visible GitHub Issue/PR/CI/commit evidence.

This file intentionally does **not** contain private chain-of-thought. It records conclusions, decisions, evidence and user-facing rationale needed for handoff.

Do not treat any recorded old PR/run/head SHA as current without refreshing GitHub first.

---

**END OF MASTER CONTEXT**