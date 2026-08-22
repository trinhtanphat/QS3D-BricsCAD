# QS3D-BricsCAD repository audit and implementation plan — 2026-08-11

Repository: `trinhtanphat/QS3D-BricsCAD`  
Baseline observed for this audit: `556f463e4ddbb3d8782fb3376c6aeee12c18e08c`  
Default/integration branch: `main`

> `main` is being changed by multiple agents in parallel. This document is a planning snapshot, not a branch lock. `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md`, current ACTIVE claims, and the newest source always win over older planning text.

## 1. Mandatory multi-agent operating model

QS3D now has enough concurrent development that coordination is part of correctness.

Before substantive code/test/config/asset work, every agent MUST:

1. fetch/re-read newest `main` and inspect recent commits;
2. read `AGENTS.md` and `docs/AGENT-WORK-REGISTRATION.md`;
3. inspect ACTIVE records in `docs/agent-work-claims/`;
4. create/update a Markdown claim under `docs/agent-work-claims/` containing agent identity, task, exact reserved paths, planned changes, validation and risks;
5. **commit and push that claim to `origin/main` before implementation**;
6. verify the claim is visible on `main`;
7. work only inside the reserved scope and do not overwrite another ACTIVE claim;
8. re-read newest `main` before integration;
9. update the claim to `COMPLETE` or `BLOCKED`, including commit SHA(s), validation and handoff notes, then push the status update.

Claims should be narrow enough that unrelated agents can continue in parallel. Broad wildcard reservations are allowed only when the task genuinely owns that subsystem. A remote agent must not repeatedly retry LOCAL_ONLY work; `docs/LOCAL-AGENT-INBOX.md` is the single live queue and `DO_NOT_RETRY_REMOTE` boundary for those scenarios.

### Planning-drift correction

Older planning documents may suggest per-workstream feature branches. Current repository policy in `AGENTS.md` requires coordinated direct work on `main` with a registration commit first. Agents must follow the current policy, not stale branch suggestions in older snapshots.

## 2. Current architecture assessment

QS3D is a mature BricsCAD-hosted BIM/QS plugin rather than a simple quantity prototype.

### Strong areas already present

- CAD-independent `QS3D.Core` domain, geometry, services, persistence, reporting, revisions, takeoff and diagnostics.
- BricsCAD V25 adapter/UI layer with semantic authoring, modeless workspace/palettes and native transaction boundaries.
- Project/Zone/Floor/Family/Element semantic model and generated-geometry ownership concepts.
- `.qsdb` persistence, backup/atomic-save hardening, project lifecycle checks, audit/revision paths and stale-state guards.
- Direct Draw / Create Similar / semantic editing workflows that are increasingly reducing command and drafting steps.
- Room/finish, wall/opening/curtain, structure/rebar, source capture/reconcile and BQ/ED2/reporting workflows.
- Large deterministic Core smoke suite plus many source preflight gates.
- Explicit local V25 qualification queue with exact-SHA evidence rules.

### Main remaining maturity risks

The highest risks are cross-cutting consistency rather than absence of basic features:

- semantic identity and duplicate ownership safety;
- atomicity across semantic + native CAD mutation;
- canonical project binding after cache/reload/modeless delays;
- generated-source ownership and stale/rebuild behavior;
- consistent Level/Zone/Family identity across authoring and reporting;
- local BricsCAD V25 UX/runtime proof for source-safe changes;
- documentation drift caused by fast parallel development;
- release/install/signing qualification and evidence freshness.

## 3. Audit findings selected now

### P0 — legacy quantity reporting can double-count duplicate semantic identity

`ProjectQuantityReportBuilder` already rejects duplicate `ProjectElement.Id` values. The legacy `QuantityReportBuilder.Group(IEnumerable<ElementInstance>)` does not: repeated identity is accumulated as if it represented separate physical elements. This can silently double count quantities and provenance.

Action in this batch:

- fail closed on case-insensitive duplicate `ElementInstance.Id`;
- add regression coverage in `ProjectQuantitySmoke`;
- preserve deterministic first-seen order and existing homogeneous grouping behavior.

Acceptance:

- two distinct `ElementInstance` objects carrying identities `A` and `a` are rejected;
- repeating the exact same object is rejected;
- valid distinct identities still group exactly as before.

### P0 — multi-agent registration must remain mandatory

The canonical registration protocol already exists and is stronger than a casual note convention. The important update is to make every new planning/audit explicitly point to that protocol and to eliminate stale instructions that encourage agents to ignore current direct-main coordination.

Acceptance:

- every substantive agent can be mapped to a committed claim before its implementation commit;
- reservations name exact files/scope and completion SHAs;
- overlapping ACTIVE claims are resolved before editing rather than after merge conflict.

## 4. Prioritized development workstreams

### WS-01 — Semantic mutation atomicity and rollback

Priority: **P0**  
Execution: source-safe Core work + LOCAL_ONLY native proof where CAD transactions are involved.

Goals:

- no mutate-then-throw path may leave partially changed in-memory semantic state;
- centralize snapshot/rollback helpers rather than feature-specific ad-hoc recovery;
- preserve project change-version/audit semantics on failed writes;
- distinguish semantic rollback from native CAD rollback failure.

Acceptance:

- fault-injection smoke tests prove before/after semantic equivalence;
- native adapters fail closed when a CAD transaction throws;
- no stale reference is used after project replacement/reload.

Coordination: an ACTIVE agent currently owns Core MEP + QuickCreate/QuickRemove atomicity; other agents must not overlap it.

### WS-02 — Canonical project lifecycle and sidecar authority

Priority: **P0**

Goals:

- every true write binds the canonical existing project after cache forget/reload;
- read-only inspection/export cannot bootstrap a replacement project;
- sidecar revision/fingerprint changes invalidate stale plans and modeless callbacks;
- save/reopen/Save As behavior remains deterministic across multiple DWGs.

Acceptance:

- exact-SHA local lifecycle matrix passes;
- absent/corrupt/replaced sidecar paths fail closed without hidden project creation;
- detached read/export state never becomes a later write target.

### WS-03 — Semantic identity, ownership and duplicate detection

Priority: **P0**

Goals:

- consistent case-insensitive canonical IDs for Project/Floor/Zone/Family/Element/generated ownership;
- duplicate semantic IDs fail closed at import, persistence, report and regeneration boundaries;
- one generated handle has one unambiguous owner/slot unless the schema explicitly allows otherwise;
- SourceHandles are normalized/deduplicated without losing reverse-locate provenance.

Acceptance:

- duplicate identity cannot silently multiply BQ/ED2 totals;
- Health reports ambiguous ownership before destructive regeneration;
- persistence/import roundtrips preserve canonical identity.

### WS-04 — Direct Draw / Create Similar / active-family authoring UX

Priority: **P0/P1**

Goals:

- keep reducing `LINE -> capture -> build` style multi-command workflows;
- make active-family support explicit and fail closed for unsupported categories;
- Create Similar must copy semantic intent without stale project/family references;
- consolidate quick/advanced draw dispatch while retaining category-specific geometry policy;
- maintain current premium/professional UI direction without hiding safety status.

Acceptance:

- common Wall/Beam/Column/Slab authoring is one coherent guided workflow;
- cancel/empty selection is side-effect free;
- active-family and Create Similar flows are deterministic after document switch/reload.

### WS-05 — Workspace / modeless UI lifecycle and multi-selection editing

Priority: **P0/P1**

Goals:

- document-bound windows never write into a stale/replaced project;
- selection inspection stays read-only;
- multi-selection property editing clearly represents same/mixed/unavailable values;
- foreground/background refresh does not mutate semantic state;
- dark-theme contrast, disabled/hover/focus states remain readable.

Acceptance:

- document switch/close/reopen cannot leak editor state between DWGs;
- mixed-value writes are atomic and category-safe;
- local V25 UI smoke proves no stale modeless callbacks.

### WS-06 — Reporting, BQ/ED2, schedules and provenance

Priority: **P0/P1**

Goals:

- one quantity truth across regenerated semantics, BQ, ED2, schedules, CSV/XLSX and revision review;
- every row retains semantic IDs, source handles, drawing/project identity and applicable material/density provenance;
- non-finite/overflow/duplicate identity fail closed;
- selection-scoped reports validate IDs before calculating;
- grouped rows never merge incompatible Zone/material/density semantics.

Acceptance:

- valid grouped/detail totals reconcile exactly;
- invalid identity/numeric inputs cannot produce plausible output;
- output can be traced back to semantic element and CAD source.

### WS-07 — Room/finish lifecycle and regenerated-source safety

Priority: **P0/P1**

Goals:

- generated finishes remain owned by the correct Room/project;
- cross-scope finishes are excluded from quantities correctly;
- floor/wall/ceiling/waterproofing finish tri-state editing does not create partial state;
- auto-regeneration does not operate on stale semantic references.

Acceptance:

- delete/rename/reassign Room scenarios leave no orphan generated finish semantics;
- schedule and BQ agree after regeneration;
- local runtime proves cancel/reload/absent-sidecar behavior.

### WS-08 — Rebar and engineering-grade fabrication output

Priority: **P1**

Goals:

- preserve exact generated ownership/replacement semantics;
- strengthen shape/tie/stirrup distribution numerical guards;
- ensure fabrication output carries enough provenance to distinguish derived approximation from qualified engineering input;
- avoid source changes that imply design-code certification without approved policy/evidence.

Acceptance:

- deterministic Core regressions for all shape/count/spacing edge cases;
- no orphan/duplicate generated rebar ownership;
- engineering qualification gates remain explicit.

### WS-09 — Interchange/import conflict policy

Priority: **P1**

Goals:

- keep append/merge/use-source/keep-target decisions previewable before mutation;
- stale preview plans must be rejected after project/sidecar change;
- imported identity and dependency references are canonicalized before write;
- collision handling remains deterministic and atomic.

Acceptance:

- failed/stale/conflicting import leaves target project unchanged;
- provenance records source identity and applied policy;
- local confirmation path cannot mutate a replacement project.

### WS-10 — Generated geometry ownership + Health/Diagnostics

Priority: **P1**

Goals:

- common owner-slot policy for solids, rebar, annotations, tables and future artifacts;
- Health recognizes stale/missing/duplicate generated outputs without destructive guessing;
- regeneration replaces only outputs owned by the intended semantic owner;
- diagnostics expose actionable repair paths.

Acceptance:

- no cross-owner deletion;
- duplicate handles/owners are surfaced before rebuild;
- repair is idempotent.

### WS-11 — Persistence/revision/audit integrity

Priority: **P1**

Goals:

- all persisted lifecycle fields roundtrip exactly;
- timestamps use canonical UTC semantics;
- revision snapshots are immutable and compare canonical identities;
- backup/fallback writes cannot silently regress to older state.

Acceptance:

- roundtrip + corruption + timestamp + backup smoke suites remain deterministic;
- revision comparison distinguishes identity changes from display-name changes correctly.

### WS-12 — Performance and scale

Priority: **P1/P2**

Goals:

- profile large element/ownership/dependency/report workloads;
- eliminate repeated O(N²) scans where stable indexes are safe;
- keep UI virtualized and avoid expensive semantic mutation on selection refresh;
- define representative project-size performance budgets.

Acceptance:

- PerfHarness records reproducible baselines;
- large-model operations remain cancellable/readable and do not weaken correctness to gain speed.

### WS-13 — Build, release, installer and signing

Priority: **P0 for release**, otherwise P1

Goals:

- reproducible exact-V25 adapter build;
- release artifacts tied to exact source SHA and version metadata;
- installer/autoload/update manifest remain deterministic;
- code signing/private credentials stay outside the repo;
- manual CI/release policy is documented clearly if GitHub Actions are intentionally restricted.

Acceptance:

- clean source build + Core smoke + preflights;
- local V25 NETLOAD/DemandLoad/runtime matrix;
- signed/package hash evidence for release candidate.

### WS-14 — Documentation/status drift control

Priority: **P1**

Goals:

- current docs never instruct an obsolete branch/CI/runtime workflow as authoritative;
- architecture/status docs distinguish implemented, source-verified, LOCAL_ONLY pending and production-qualified states;
- old audit plans remain historical snapshots and link to newer authority.

Acceptance:

- no roadmap item claims current PR/branch blockers that no longer exist without a dated snapshot warning;
- every LOCAL_ONLY blocker is represented once in `docs/LOCAL-AGENT-INBOX.md`.

## 5. Validation pyramid

### Tier A — remote/source-safe, required for every applicable batch

- re-read newest target file SHAs before write;
- compile/static syntax where the available environment supports it;
- focused Core smoke regression;
- applicable `scripts/preflight-*.py` gates;
- source-contract review for lifecycle/ownership/rollback rules.

### Tier B — repository integration

- aggregate Core smoke suite;
- aggregate preflight discovery;
- commit status/workflow metadata when available;
- verify no overlapping claim was overwritten and final commit is on `main`.

### Tier C — LOCAL_ONLY BricsCAD V25

Use `docs/LOCAL-AGENT-INBOX.md`. Typical proof includes:

- exact V25 build/load/command registration;
- document switch/save/reopen/Save As lifecycle;
- modeless UI focus/selection/contrast behavior;
- real DWG transaction rollback and generated-geometry ownership;
- package/install/autoload/signing checks.

Remote agents must never label Tier A/B evidence as `LOCAL_PASS`.

## 6. Definition of done for future agents

A work item is complete only when:

- the claim was committed before substantive implementation;
- scope did not overlap an ACTIVE reservation without explicit handoff;
- source behavior has a regression/preflight where practical;
- failure and cancel paths are side-effect safe;
- current `main` was re-read before push;
- source-safe validation results are recorded accurately;
- any missing V25/private-DWG/UI proof is updated in the canonical local inbox instead of retried remotely;
- the claim is closed with actual commit SHA(s) and handoff notes.

## 7. Immediate sequence after this planning commit

1. Harden legacy `QuantityReportBuilder` duplicate identity handling.
2. Extend `ProjectQuantitySmoke` with duplicate-identity regressions.
3. Re-read concurrent `main` and ensure those two reserved files did not change.
4. Push implementation to `main` without force.
5. Inspect commit status and close this agent claim with exact SHAs and validation notes.
6. Leave all other workstreams available for other registered agents instead of serializing the whole project behind one broad claim.
