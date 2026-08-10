# QS3D Continue-All Project Logic — 2026-08-10

## Objective

Advance QS3D toward a production-grade BricsCAD V25 hosted BIM/QS workflow without weakening the product boundary or pretending remote source work has qualified licensed BricsCAD runtime behavior.

The product logic is organized around five invariants: semantic identity, explicit references, CAD ownership, atomic mutation, and runtime qualification boundaries.

## 1. Semantic identity is authoritative

Zone, Floor, Family and Element IDs are project-domain identities. Commands must not silently reinterpret collisions.

Current import modes deliberately have different contracts:

- Append-only: only identities absent from target are accepted.
- KeepTarget: same-ID target identities remain authoritative; new source identities may append.
- UseSource Element: same-ID Element portable semantic state can replace target semantic state while target CAD ownership is preserved/invalidation is explicit.
- UseSource Catalog: same-ID Zone/Floor/Family portable semantic state can replace the target catalog state under the guarded catalog policy.
- Remapped Copy: every source identity is copied to a deterministic namespace and target identities are never replaced.

These are separate policies, not UI labels for the same mutation.

## 2. References are typed, not string search/replace

Canonical references are explicit fields (`FamilyId`, `FloorId`, `ZoneId`, `DependsOn`). Portable references inside property dictionaries require a finite registered policy. Current vertical Level keys are `BottomLevelId` and `TopLevelId`.

Any future feature that introduces another semantic-ID-bearing property must register it before remapped-copy/federation can rewrite it. Unknown reference-looking fields fail closed when they resolve to source identities.

This avoids a dangerous class of corruption where an arbitrary note, mark, material code or other string happens to equal an Element/Family/Floor/Zone ID.

## 3. CAD ownership never crosses files implicitly

Portable semantic interchange is not native DWG ownership interchange.

Source handles, generated-output handles, native object ownership metadata and source drawing fingerprints must never become target ownership merely because semantic JSON was imported. Replacement paths preserve/invalidate target-owned CAD through their explicit services; remapped-copy discards incoming ownership and marks imported Elements dirty.

Native generation remains a separate explicit operation.

## 4. Mutation must be previewable and atomic

For high-impact operations the preferred shape is:

`strict validate -> read-only plan/preview -> explicit user policy/confirmation -> snapshot/transaction -> mutation -> post-validate -> audit -> explicit rebuild/save`

Core operations use `ProjectStateSnapshot` or equivalent source-safe rollback. Native adapter operations that touch DWG entities additionally require the existing CAD transaction/ownership safeguards.

The semantic project persistence lifecycle is now coupled to the successful DWG save/close workflow in source, but exact BricsCAD V25 save/reopen behavior still requires local qualification.

## 5. Dirty/generated state is a safety signal

Semantic changes that can affect generated geometry must not leave old geometry looking authoritative. New or replaced imported Elements are dirty or generated-output closure is explicitly invalidated. Regeneration is a separate action and can use the newer Core dry-run/guarded apply contracts where appropriate.

## Feature plan and present state

### A. Project durability — source implemented

- semantic change/persistence stamp lifecycle;
- `.qsdb` sidecar persistence coupled to successful DWG lifecycle source hooks;
- close Save/Discard/Cancel/recovery source behavior;
- atomic project snapshots and save hardening.

Remaining: exact-SHA V25 runtime qualification, crash/host-specific save edge cases and sanitized local evidence.

### B. Interchange and federation — active source focus

Implemented pipeline:

`export -> strict validate -> typed immutable read -> semantic diff -> collision preview/resolution -> append/KeepTarget/UseSource mutation -> remapped isolated copy`

The new remapped-copy path adds a project-federation primitive without pretending to be a live link. It is the safest source-resolvable next step because it does not require native handle rebinding.

Remaining future work must be separate and explicit:

- persisted import-instance manifest if product requirements need long-lived federation instances;
- refresh/re-import of an existing remapped instance only after a three-way/source-version policy is designed;
- per-instance visibility/grouping in the Project Browser;
- richer portable formats (IFC/BCF/Revit adapters) only with their own format/ownership contracts.

### C. Project Browser / property workspace — source improving

Current main already contains deterministic semantic project-tree planning and a fail-closed multi-selection property inspector. Next UI work should consume these Core contracts rather than recalculate semantic truth inside WPF.

Potential source-safe additions:

- filter/search model over the deterministic tree;
- explicit selection sets and saved views as semantic metadata;
- batch edit preview before apply;
- federated-copy grouping by import namespace/provenance.

Native selection highlighting and BricsCAD grip/transient behavior remain runtime-sensitive.

### D. Documentation / schedules — strong source implementation

Current native semantic documentation includes the generic semantic Table plus specialized Door/Opening, Room Finish, Material Usage, BQ and BBS Tables. Remaining work should prioritize a bounded formatting policy and source-side schedule definition model before native Layout/Sheet automation.

MLeader placement, associative leaders, TableStyle behavior, Layout/Viewport/title-block workflows and PaperSpace scale behavior need licensed V25 runtime qualification.

### E. Direct Draw / edit parity — native-local boundary

Semantic authoring commands exist and source can continue to improve transaction guards, preview models and canonical edit logic. Full BLT-like DrawJig/transient/repeat UX, native grips and host interaction cannot be honestly qualified remotely.

### F. Geometry/rebar — engineering/runtime boundary

Core geometry, grids, slab-hole mesh, wall/rebar schedule logic and ownership health have advanced substantially. Standards-specific rebar detailing/fabrication remains dependent on owner/engineer standards, and native Boolean/Solid3d/mesh behavior requires local V25 evidence.

## Implementation batch in this document

This batch implements the Remapped Copy / Federated Copy primitive:

1. `ProjectInterchangeSemanticReferencePolicy` — finite semantic-property reference registry.
2. `ProjectInterchangeRemapCopyImporter` — deterministic mapping plan and atomic mutation.
3. `QS3DINTERCHANGEREMAP` — guarded file read, namespace entry, preview and explicit confirmation.
4. smoke/static source guards.
5. `docs/INTERCHANGE-REMAP-COPY.md`.
6. Project Tools surfacing where the current UI source can be updated without overwriting concurrent work.

## Non-goals for this batch

- no workflow dispatch or Release;
- no fake BricsCAD runtime pass;
- no live external links;
- no source CAD handle rebinding;
- no automatic `.qsdb` save after import;
- no IFC/Revit compatibility claim;
- no standards-specific engineering approval.

## Completion rule

A source-safe item is complete when code, fail-closed logic, smoke/static guards and truthful documentation are on `main` without overwriting concurrent changes. A LOCAL_ONLY item is complete only when an authorized local owner qualifies the exact SHA under the repository's V25 qualification policy.
