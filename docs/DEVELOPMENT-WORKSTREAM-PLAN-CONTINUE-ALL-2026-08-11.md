# QS3D BricsCAD V25 — continue-all workstream rebaseline

Updated: 2026-08-11 (UTC+7)

Repository: `trinhtanphat/QS3D-BricsCAD`

Baseline observed immediately before this addendum: `e904d1f168b9ecac44d5682ed152ee1ad5f58996`

This file is a **delta/rebaseline** for `docs/DEVELOPMENT-WORKSTREAM-PLAN-2026-08-10.md`. It exists because `main` advanced by more than one hundred commits after the first workstream plan was written. Current source always wins over this snapshot. Every agent must fetch the latest `main` before editing and again immediately before integration.

## 1. Executive rebaseline

The original workstream decomposition remains valid, but several assumptions have already moved forward materially.

### Foundation contracts that are already on current `main`

The following are no longer theoretical integration blockers:

1. **DWG Save / SaveAs / Close -> `.qsdb` lifecycle**
   - `DocumentLifecycleCoordinator` now attaches native save/close handlers;
   - successful named-DWG save can persist pending semantic state;
   - close with pending semantic state uses explicit Save / Discard / Cancel behavior;
   - failed canonical save can veto close and attempt a detached recovery copy;
   - document destruction detaches persistence handlers before forgetting the project cache.

2. **Drawing-unit fail-closed contract**
   - `CadUnitService` no longer silently falls back to millimeters for unknown/unsupported `INSUNITS`;
   - drawing-unit resolution can use an explicit persisted project override;
   - quantity compatibility is checked against the effective unit contract;
   - B4D/BQ/ED2/source-reconcile paths are expected to consume the same unit truth.

3. **Semantic interchange has advanced beyond append-only planning**
   - deterministic export/validation/read/diff/preview remain the base;
   - append-only and KeepTarget slices exist;
   - remap planning now detects opaque identity/reference-shaped properties conservatively;
   - guarded **Import As New / remap append** execution exists for collision-preserving semantic import without claiming target-DWG native ownership.

4. **Semantic documentation has advanced beyond isolated tags/tables**
   - native semantic tags and authoritative native schedule tables already exist in source;
   - Core now also persists a bounded semantic documentation catalog for stable semantic View/Sheet definitions;
   - this does **not** mean BricsCAD Layout/Viewport/TitleBlock native generation is complete.

5. **Curtain per-builder atomicity continues to harden**
   - host/frame builders have stronger rollback-capable semantic/native ordering;
   - individual builder safety must not be confused with whole-command host+frame(+future panel) all-or-nothing atomicity.

## 2. PR reconciliation status

### PR #173 — project save lifecycle

Status observed during this review: **closed, not merged as that PR object**, while its core lifecycle behavior is already present on `main` through later/concurrent integration.

Planning consequence:

- do not reopen/remerge #173 blindly;
- treat WS-01 as **source baseline landed, local V25 lifecycle qualification + follow-up hardening remaining**;
- if a later audit compares #173, compare behavior/file content, not PR merged-state alone.

### PR #165 — drawing units / Proxy B4D safety

Status observed: **open draft, heavily diverged from current `main`**. The central unit-resolution contract from that branch is already visible on current `main`.

Planning consequence:

- do **not** merge #165 blindly onto a much newer main;
- first diff/reconcile any genuinely unique remaining changes;
- if no unique value remains after current-main comparison, close the stale draft rather than reintroducing old file versions;
- downstream work must use current `main` unit semantics, not the PR base SHA.

## 3. Status vocabulary for all agents

Use these states consistently:

- `LANDED_SOURCE` — source implementation is present on current `main` and has static/deterministic coverage where applicable.
- `REMOTE_DONE` — all work that can be honestly completed from source-only execution is done.
- `ACTIVE_REMOTE` — suitable for continued Core/docs/tests/source-safe implementation.
- `ACTIVE_COORDINATED` — shared domain/adapter surfaces; one primary writer at a time for hotspot files.
- `LOCAL_ONLY` — requires licensed interactive BricsCAD V25 / Windows behavior / private DWG / native API proof.
- `POLICY_REQUIRED` — blocked on owner/legal/commercial/engineering policy.
- `ENGINEERING_REQUIRED` — blocked on an explicit governing standard/revision and engineering approval.
- `LOCAL_PASS` — only valid from exact-SHA local evidence.
- `NOT_QUALIFIED` — no valid runtime/engineering qualification exists yet.

Never convert `LANDED_SOURCE` or static preflight evidence into `LOCAL_PASS`.

## 4. Open product-gap issue -> workstream map

Current repository has 12 open product-gap/local issues. They should be treated as backlog anchors, not duplicated into new parallel issue trees unless the owner explicitly wants finer tracking.

| Issue | Product gap | Primary workstreams | Current classification |
|---|---|---|---|
| #84 | Interoperability/import-export beyond XLSX/CSV/template | WS-24, WS-25 | `ACTIVE_REMOTE` + later `LOCAL_ONLY` |
| #83 | General polygon Slab/Foundation mesh | WS-18, WS-19 | Core advanced; native lifecycle `LOCAL_ONLY` |
| #82 | Real V25 UI/DPI/context-menu/Ribbon polish | WS-28, WS-29 | `LOCAL_ONLY` |
| #81 | Large-model performance qualification | WS-05, WS-30 | remote harness exists; real V25 profiling `LOCAL_ONLY` |
| #80 | Native modify/edit semantic geometry | WS-08, WS-07 | reconcile landed; grip/jig/native edit `LOCAL_ONLY` |
| #79 | Grid/reference + richer Level/elevation constraints | WS-15, WS-16 | Core/source advanced; native placement chain coordinated/local |
| #77 | Documentation layer — tags/tables/sheets/views | WS-22, WS-23 | tags/tables/source catalog advanced; Layout/Viewport local |
| #76 | Fabrication-grade rebar/detailing + structural authoring | WS-17, WS-19, WS-20 | `ENGINEERING_REQUIRED` + local runtime |
| #75 | Signing/install/update/licensing | WS-31, WS-32, WS-33 | `POLICY_REQUIRED` / `LOCAL_ONLY` |
| #74 | Direct Draw transient preview/repeated authoring | WS-06, WS-07 | `LOCAL_ONLY` for DrawJig/transient lifecycle |
| #73 | Multi-owner walls / advanced geometry | WS-09, WS-10, WS-13, WS-14 | ownership design + `LOCAL_ONLY` native proof |
| #72 | Exact V25 SHA qualification | WS-33 | `LOCAL_ONLY` canonical runtime gate |

## 5. Workstream reclassification

The original 36 workstreams remain useful. Their current recommended state is:

### Foundation / platform

- **WS-01 Project lifecycle/persistence/recovery** — `LANDED_SOURCE`, then `LOCAL_ONLY` Save/SaveAs/Close qualification and recovery UX verification.
- **WS-02 Drawing units/numeric provenance** — `LANDED_SOURCE` baseline; continue health/provenance reconciliation as `ACTIVE_COORDINATED`.
- **WS-03 Project transaction/snapshot/journal platform** — `ACTIVE_COORDINATED`; do not over-generalize until a real multi-stage native consumer needs durable recovery.
- **WS-04 Generated ownership/health evolution** — `ACTIVE_COORDINATED`; mandatory dependency for every new generated native family.
- **WS-05 Dependency/incremental regeneration** — `ACTIVE_REMOTE`; real large-model native timings remain WS-30 local.

### Authoring / modify

- **WS-06 Direct Draw common authoring engine** — `ACTIVE_COORDINATED`; refactor duplication only when it reduces risk, not for cosmetic abstraction.
- **WS-07 Direct Draw preview/repeat** — `LOCAL_ONLY`.
- **WS-08 Source Reconcile/Modify** — reconcile baseline `LANDED_SOURCE`; richer native modify/grips remain `LOCAL_ONLY`.

### Architecture / envelope

- **WS-09 L/T/X/Multi physical wall geometry** — ownership design `ACTIVE_COORDINATED`, native finalization `LOCAL_ONLY`.
- **WS-10 WallPier advanced path/profile** — Core/planner pieces may be remote; native Direct Draw/profile behavior coordinated/local.
- **WS-11 Room / HT_PHONG v2** — `ACTIVE_REMOTE` for topology/diagnostics/planning; performance/native behavior later local.
- **WS-12 Door/Opening host/boolean v2** — `ACTIVE_COORDINATED`; complex boolean and undo/runtime local.
- **WS-13 Curtain whole-command atomicity/recovery** — highest-risk `ACTIVE_COORDINATED` + `LOCAL_ONLY` proof.
- **WS-14 Curtain panel-by-panel glass** — blocked by WS-13 final recovery contract; native proof local.

### Grid / levels / structure / rebar

- **WS-15 Grid/reference system** — Core planning `REMOTE_DONE` in several slices; native system creation/constraints/association remain coordinated/local.
- **WS-16 Level native placement chain** — `ACTIVE_COORDINATED` design; final adapter/UI behavior local.
- **WS-17 Structural geometry v2** — `ACTIVE_COORDINATED`; broaden only from deterministic contracts.
- **WS-18 Polygon Slab/Foundation mesh + holes** — Core geometry significantly advanced; native loop ownership/extraction/save/reopen local.
- **WS-19 Rebar generation platform** — `ACTIVE_COORDINATED`; consolidate generated ownership/health without inferring engineering rules.
- **WS-20 Fabrication standards** — `ENGINEERING_REQUIRED` before numeric standards logic.

### Quantity / documentation / interchange

- **WS-21 BQ/ED2/reporting platform** — `ACTIVE_REMOTE/COORDINATED`; keep one authoritative quantity engine.
- **WS-22 Native semantic tags/tables v2** — broad source baseline landed; continue user-defined semantic schedule/catalog work carefully.
- **WS-23 Layout/Sheet/Viewport/TitleBlock** — semantic View/Sheet catalog foundation now exists; native Layout/Viewport implementation remains `LOCAL_ONLY`.
- **WS-24 Generic semantic interchange** — currently one of the best remote lanes; remap/import-as-new has advanced materially, but full replace/merge semantics remain open.
- **WS-25 IFC/BCF/Revit/vendor interchange** — P2; wait until WS-24 semantics are stable.

### Recognition / revision / UX / performance

- **WS-26 Recognition/B4D v2** — unit/proxy baseline advanced; keep recognition conservative and measurement-backed.
- **WS-27 Revision/Audit/Change Review** — `ACTIVE_REMOTE`; should consume semantic identity/diff instead of native handle assumptions.
- **WS-28 Workspace/Ribbon/Hub UX consolidation** — source-safe wiring possible, but real visual decisions require WS-29 evidence.
- **WS-29 Unicode/HiDPI/theme** — `LOCAL_ONLY` final qualification.
- **WS-30 Large-model performance** — Core harness `LANDED_SOURCE`; real V25 profiling `LOCAL_ONLY`.

### Commercial / release / future adapter

- **WS-31 Commercial license enforcement** — `POLICY_REQUIRED`.
- **WS-32 Authenticode/install/update trust** — source helpers advanced; operational proof `LOCAL_ONLY`.
- **WS-33 Exact-SHA V25 qualification** — `LOCAL_ONLY`; final release gate.
- **WS-34 Documentation/status reconciliation** — `ACTIVE_REMOTE`, continuous because main moves extremely quickly.
- **WS-35 Test architecture/fault injection** — `ACTIVE_REMOTE/COORDINATED`; high priority because many remaining risks are rollback failures.
- **WS-36 Future AutoCAD adapter** — P2 only after BricsCAD contract stabilizes.

## 6. Best parallel execution waves

### Wave A — source-safe remote lanes now

These can run in parallel with relatively low risk if each agent owns a narrow file/domain set:

1. **Interchange semantic execution**
   - explicit `UseSourceSemanticData` semantics;
   - catalog/element replacement ordering;
   - generated-output reset planning;
   - field/property precedence;
   - provenance-only source-handle policy;
   - generic import UX should remain separate from Core execution.

2. **Documentation semantic model**
   - catalog schema/validation/migration for semantic Views/Sheets;
   - user-defined SemanticSchedule definitions if product requirement is explicit;
   - health for dangling View/Sheet references;
   - no native Layout API guessing remotely.

3. **Dependency/performance Core**
   - targeted regeneration metrics;
   - avoid repeated O(N^2) scans;
   - bounded profiling hooks;
   - deterministic stress fixtures.

4. **Room/topology diagnostics**
   - explain why a boundary failed;
   - deterministic provenance review models;
   - large-network bounded behavior.

5. **Revision/change review**
   - semantic diff presentation models;
   - stable-ID grouped change sets;
   - generated/native output excluded as portable authority.

6. **Documentation/status reconciliation**
   - current source wins;
   - remove stale “not implemented” claims after source lands;
   - never upgrade runtime status without exact-SHA local evidence.

7. **Fault-injection test infrastructure**
   - reusable project snapshot rollback assertions;
   - post-commit UI failure isolation;
   - staged-operation failure matrices.

### Wave B — coordinated shared-platform lanes

Limit concurrent writers and reserve shared hotspot files:

1. WS-03 operation journal/result model;
2. WS-04 generated ownership v2;
3. WS-06 Direct Draw common engine refactor;
4. WS-08 batch source reconcile;
5. WS-12 opening lifecycle evolution;
6. WS-16 Level native semantic contract wiring;
7. WS-19 generated rebar platform.

These lanes must rebase/reapply onto current main frequently because they touch cross-cutting contracts.

### Wave C — local BricsCAD V25 lanes

Do not assign these to remote agents as “implementation by memory”:

1. Direct Draw DrawJig/transient/repeat;
2. MOVE/ROTATE/STRETCH/grip semantic editing;
3. Curtain whole-command recovery proof;
4. Curtain panel native solids;
5. physical L/T/X wall output;
6. Level Z-chain across native hosts/openings/curtain/rebar;
7. polygon/hole native mesh source extraction/ownership;
8. Layout/Viewport/TitleBlock native implementation;
9. UI/DPI/context-menu/Ribbon polish from screenshots;
10. large-model native profiling;
11. clean install/upgrade/uninstall;
12. exact-SHA qualification matrix.

### Wave D — owner/engineering/ops decision lanes

Do not let agents invent answers to these:

- governing reinforcement standard + exact revision;
- engineering approval process;
- SKU / seat / machine / named-user model;
- trial / expiry / grace policy;
- offline/online activation strategy;
- key rotation/revocation policy;
- repository/legal distribution model;
- production Authenticode certificate/timestamp operations.

## 7. Hotspot files — single-writer coordination recommended

Because multiple agents are actively writing `main`, these files/surfaces should be treated as temporary single-writer hotspots during a batch:

- `src/QS3D.Core/Domain/ProjectState.cs`
- `src/QS3D.Core/Domain/ProjectElement.cs`
- `src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs`
- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- `src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs`
- `src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs`
- `src/QS3D.BricsCAD.V25/Commands.cs`
- primary Ribbon/Hub command-registration surfaces
- `src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- `docs/IMPLEMENTATION-STATUS.md`
- canonical current handoff docs.

An agent working on one of these should re-fetch immediately before writing and before commit. Other agents should prefer feature-local files until the hotspot batch lands.

## 8. Integration order for high-risk work

Recommended dependency order:

```text
Persistence + Unit truth already landed
        |
        +--> Generated ownership / operation-result contracts
        |       |
        |       +--> Level native placement
        |       +--> Source Modify/Reconcile v2
        |       +--> Opening lifecycle v2
        |       +--> Rebar platform v2
        |
        +--> Curtain whole-command recovery
        |       |
        |       +--> Curtain panel native glass
        |
        +--> Wall junction-owned output

Semantic documentation catalog
        |
        +--> Native Layout/Viewport/TitleBlock (LOCAL_ONLY)

Interchange validation/read/diff
        |
        +--> Append / KeepTarget / Import-As-New
        |       |
        |       +--> UseSource/replace + generated reset
        |               |
        |               +--> Generic import UX
        |                       |
        |                       +--> IFC/BCF/Revit/vendor adapters
```

## 9. Interchange next plan — updated after current `main`

Do not restart interchange from an old “no remap” assumption. Current main has moved further.

Next safe sequence:

1. keep strict validator/typed reader authoritative;
2. maintain explicit registry of property-carried semantic references;
3. define executable `UseSourceSemanticData` separately for:
   - Zone;
   - Floor;
   - Family;
   - Element;
4. define dependency-ordered mutation of existing identities;
5. before replacing an Element, clear generated/native ownership only through canonical ownership/invalidation APIs;
6. never import source DWG Handle strings as target ownership;
7. decide whether provenance-only source references are retained separately;
8. define property/quantity precedence and whether imported calculated quantities are trusted or regenerated;
9. rollback complete semantic mutation on failure;
10. only after Core execution is stable, add one reviewed adapter UX for generic import;
11. local V25 qualification must cover Unicode paths, cancel/confirm, save/reopen, multi-DWG and post-import rebuild behavior.

IFC/Revit/BCF should remain downstream of this contract, not a parallel competing identity model.

## 10. Documentation/Sheet next plan — updated after semantic catalog landing

Separate three layers explicitly:

### Layer A — semantic definitions

Current/source-safe:

- stable semantic View IDs;
- stable semantic Sheet IDs;
- deterministic filters/reference membership;
- persisted bounded catalog;
- validation against current project identities.

### Layer B — documentation render models

Remote-safe where CAD-independent:

- schedule/table definitions;
- tag templates;
- title/field values;
- sheet-view placement plans in paper units as pure data;
- change/stale planning.

### Layer C — native BricsCAD materialization

`LOCAL_ONLY`:

- Layout creation/ownership;
- Viewport creation/scale/lock;
- title block insertion/ownership;
- paper size/plot configuration;
- MLeader associativity;
- actual TableStyle behavior;
- Model/Paper Space switching;
- save/reopen/Undo/multi-DWG behavior.

Do not call Layer A persistence “sheet generation”.

## 11. Curtain next plan — keep high-risk work serialized

Do not run separate agents simultaneously on host transaction, frame transaction and panel creation without an agreed orchestration contract.

Recommended sequence:

1. write one orchestration state machine/contract;
2. pre-plan host/frame/panel desired output before first destructive mutation;
3. choose shared native transaction or durable compensation journal;
4. add failure injection after each logical phase;
5. Health/Release Check must detect interrupted recovery state;
6. qualify host+frame all-or-nothing/recoverable behavior in V25;
7. only then add panel native output;
8. panel output gets its own canonical owner slot, stale state, fingerprint, selection and health;
9. opening regions must clip/interupt panel output deterministically;
10. save/reopen must preserve a valid completed or explicitly recoverable state.

## 12. Level native placement next plan

Treat Level as a vertical-placement platform, not a UI field.

One agent/coordinated batch should own the shared contract across:

- host bottom/top/effective height;
- opening host-relative Z;
- curtain frame/panel Z;
- rebar/tie/stirrup/mesh Z;
- Direct Draw initial semantic values;
- stale/fingerprint invalidation;
- Health/Release Check.

Do not let separate feature agents each implement their own `BottomLevelId` arithmetic.

## 13. Rebar standards next plan

Current provenance fields are useful but are not engineering compliance.

Before numeric standard-specific implementation, require explicit owner/engineer inputs:

```text
Standard code: <explicit>
Revision/year: <explicit>
Applicable material grades: <explicit>
Hook/bend/lap/anchorage source: <approved reference>
Cover/spacing rule source: <approved reference>
Shape-code convention: <explicit>
Approval owner: <engineer/process>
```

Only then implement deterministic rule modules, tests and BBS provenance. Until then, keep generic geometry standards-neutral.

## 14. Definition of done per branch

Every source branch/workstream should include, where applicable:

1. implementation;
2. deterministic Core tests or smoke coverage;
3. focused static preflight for architecture invariants only when valuable;
4. Health/Release integration for persisted/generated state;
5. documentation/status update;
6. exact local scenario handoff when runtime proof is required;
7. no automatic Actions dispatch;
8. final re-sync with `main` before commit/merge;
9. no force-push over concurrent main work.

Avoid test-token preflights that merely freeze implementation spelling without protecting a product invariant.

## 15. Production 1.0 dependency gate

Do **not** call QS3D 1.0 production-ready until the exact release SHA/package has all relevant rows genuinely green:

- source preflights;
- Core Release build/tests;
- V25 adapter build against exact installed V25 assemblies;
- NETLOAD and DemandLoad;
- DWG Save/SaveAs/Close + `.qsdb` lifecycle;
- Direct Draw/capture/source reconcile/Undo/cancel;
- wall/opening/room/curtain/structure/rebar native behavior;
- generated ownership + Health + Release Readiness;
- BQ/ED2/schedules/native documentation;
- semantic interchange supported paths;
- save/reopen and multi-DWG;
- Unicode/HiDPI;
- representative large-model performance;
- clean install/upgrade/uninstall;
- Authenticode/timestamp when distributing as signed production software;
- commercial license policy/enforcement if the sold build requires it;
- engineering qualification for any fabrication-grade reinforcement claims.

A release may intentionally ship a smaller supported surface. In that case unsupported features must be disabled/documented rather than falsely marked PASS.

## 16. Recommended immediate agent allocation

If many agents are available now, a low-conflict allocation is:

- **Agent A** — WS-24 interchange replace/UseSource policy + Core execution design.
- **Agent B** — WS-22 semantic documentation catalog validation/health/user-defined schedule model.
- **Agent C** — WS-05 dependency/performance Core harness and targeted-regeneration metrics.
- **Agent D** — WS-11 room topology diagnostics/provenance review models.
- **Agent E** — WS-27 revision/audit semantic change-review models.
- **Agent F** — WS-35 fault-injection/rollback test infrastructure.
- **Agent G** — WS-34 documentation/status reconciliation only.
- **Agent H** — PR #165/current-main unique-delta reconciliation; no blind merge.
- **Local Agent L1** — WS-33 exact-SHA baseline V25 qualification.
- **Local Agent L2** — WS-07 Direct Draw preview/repeat after confirming actual V25 API behavior.
- **Local Agent L3** — WS-13 Curtain orchestration failure-injection/atomicity.

Do not concurrently assign multiple agents to rewrite `ProjectState`, `ProjectElement`, `Commands`, generated ownership policy or release aggregation without an explicit file ownership split.

## 17. CI/release policy reminder

This planning/review work does **not** authorize GitHub Actions, self-hosted V25 workflow dispatch or release publication.

Repository policy remains:

- workflows are owner-controlled/manual-only;
- `continue all`, review, commit, push, merge or documentation updates are not CI authorization;
- a separate explicit owner request is required to run CI/build/runtime/release;
- publishing requires the release workflow's explicit release confirmation.

## 18. Bottom line

QS3D's highest-value work has shifted from adding many more commands to making the existing broad feature surface coherent under shared invariants:

```text
one project lifecycle
one drawing-unit truth
one semantic identity model
one generated-ownership policy
one vertical-placement contract
one rollback/recovery philosophy
one authoritative quantity path
one documentation identity model
one interchange identity policy
one exact-SHA runtime truth
```

Future features should plug into those contracts. Any feature that creates a second competing source of truth should be rejected even if it appears to increase short-term command parity.