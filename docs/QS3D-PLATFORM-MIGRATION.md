# QS3D BricsCAD → QS3D Platform migration plan

**Owner decision:** 2026-08-13 (UTC+7)  
**Applies to:** `trinhtanphat/QS3D-BricsCAD`, `trinhtanphat/QS3D-Platform`, `trinhtanphat/QS3D-CAD`  
**Migration style:** incremental, parity-first, no big-bang rewrite  
**This document does not change the shipping form of this repository.**

## 1. Three-repository product architecture

The owner has explicitly created two sibling repositories so standalone CAD work no longer needs to be inferred from or forced into the BricsCAD plugin repository.

```text
                         QS3D-Platform
                    host-neutral shared layer
                  /                         \
                 /                           \
        QS3D-BricsCAD                      QS3D-CAD
        hosted product                  standalone product
              |                               |
              v                               v
       BricsCAD V25/V26              QS3D-owned desktop host
```

Repository ownership is now intentionally different:

| Repository | Product/runtime ownership | Vendor dependency policy |
| --- | --- | --- |
| `QS3D-BricsCAD` | BricsCAD V25/V26 plugin, existing customer/plugin line | may reference licensed BricsCAD assemblies externally; never redistributes them |
| `QS3D-Platform` | shared domain, geometry value objects, semantic BIM/QS, application/CAD contracts | no BricsCAD, AutoCAD, ODA, WPF or proprietary SDK dependency |
| `QS3D-CAD` | standalone Windows CAD/BIM/QS desktop product | production CAD/DWG/render SDK lives behind isolated adapters and must be legally licensed |

The standalone requirement is therefore **reopened at the QS3D product-family level**, not by converting `QS3D-BricsCAD` into an executable.

## 2. Current bootstrap baselines

At the time this plan was created:

- `QS3D-Platform` has a vendor-neutral bootstrap with shared contracts targeted to `netstandard2.0`, plus a `net8.0` in-memory adapter/smoke lane.
- `QS3D-CAD` has a standalone application/command/document bootstrap and pins Platform by exact Git submodule revision.
- this repository still owns the established `QS3D.Core` implementation and the V25/V26 BricsCAD adapters; no production feature is considered migrated merely because a similarly named Platform type exists.

**Current BricsCAD source remains authoritative until a specific migration slice has parity evidence and the BricsCAD consumer is switched deliberately.**

## 3. Hard migration invariants

### 3.1 BricsCAD remains a product, not a temporary compatibility hack

`QS3D-BricsCAD` remains supported as a hosted plugin for customers who use BricsCAD. Standalone progress must not silently degrade V25/V26 behavior, packaging, update identity or qualification discipline.

### 3.2 Shared Platform must remain consumable by V25

BricsCAD V25 is `net48`. Therefore shared Platform libraries intended for both products must remain compatible with `net48` consumption, currently by targeting `netstandard2.0` unless a later documented multi-target strategy replaces it.

Do not introduce a shared public contract that requires only .NET 8 and then claim V25 can consume it.

### 3.3 Vendor types never cross into Platform

Platform public or private implementation must not reference or expose:

- `BrxMgd` / BricsCAD managed types;
- Teigha/BricsCAD runtime `ObjectId`, `Database`, `Transaction`, `Editor`, `Solid3d`, `Document` or UI types;
- AutoCAD proprietary types;
- ODA proprietary/native types;
- WPF window/control classes.

A vendor concept that must be shared is represented by a QS3D-owned value type or interface.

### 3.4 No big-bang `QS3D.Core` move

Do not delete `src/QS3D.Core` and point the plugin at a newly copied Platform tree in one change. Each domain is migrated only after its observable contract is understood and regression parity exists.

### 3.5 Runtime evidence never transfers between products

- Platform deterministic tests are not BricsCAD runtime evidence.
- BricsCAD V25 PASS is not V26 PASS.
- BricsCAD PASS is not standalone native-DWG PASS.
- in-memory standalone PASS is not ODA/native drawing PASS.

### 3.6 Data authority may not change accidentally

The current plugin source-of-truth contract remains DWG source geometry plus `.qsdb` semantic/project metadata until a separately qualified migration intentionally changes it. A future standalone `.qs3d` container does not retroactively change the plugin's persisted authority.

## 4. Migration classification vocabulary

Every existing source surface is classified as one of:

- **MOVE** — host-neutral behavior belongs in Platform after parity tests.
- **ADAPT** — QS3D interface/contract belongs in Platform; BricsCAD implementation remains here.
- **KEEP** — BricsCAD-specific product code remains entirely here.
- **REWRITE** — semantics are reusable but the implementation is too host-coupled to copy safely.
- **SPLIT** — one source area contains both portable and host-specific responsibilities.
- **DEFER** — do not migrate until a required contract/kernel/runtime capability exists.

Classification describes ownership direction; it does not authorize an unreviewed source move.

## 5. `QS3D.Core` migration matrix

The current Core tree is broad. Use the following category-level direction, then classify individual files before moving them.

| Current Core area | Direction | Platform target | Notes |
| --- | --- | --- | --- |
| `Domain/` | **MOVE** | `QS3D.Platform.Domain` | project/floor/zone/family/element identity and invariants are shared product concepts |
| `Geometry/` | **SPLIT/MOVE** | `QS3D.Platform.Geometry` | pure numeric/value/algorithm code moves; any native CAD solid/entity implementation stays adapter-side |
| `Measurement/` | **MOVE** | Quantity/measurement layer | normalized finite measurements and unit policy are host-neutral |
| `Formulas/` | **MOVE** | Quantity/rules | deterministic formula parsing/evaluation belongs in Platform |
| `Cost/` | **MOVE** | Quantity/cost | cost rules and projections are product-neutral when they do not call host APIs |
| `Mapping/` | **MOVE** | Mapping/application domain | semantic/layer/category mapping policy is reusable; host layer enumeration is adapter work |
| `Model/` | **MOVE/SPLIT** | Domain/application | pure project models move; host runtime wrappers do not |
| `Diagnostics/` | **MOVE/SPLIT** | Diagnostics | health rules move; native drawing probes become adapter inputs |
| `Audit/` | **MOVE/SPLIT** | Diagnostics/audit | deterministic findings move; BricsCAD UI/command presentation stays here |
| `Export/` | **SPLIT** | Reporting/export models | report models/CSV/XLSX projections may move; native BricsCAD table/entity emission stays here |
| `Documentation/` | **SPLIT** | Reporting/documentation contracts | semantic document models move; native CAD table/annotation writing is adapter-side |
| `Features/` | **SPLIT** | Domain/application feature services | deterministic planners/rules move; selection/transaction/entity creation remains host implementation |
| `Extensions/` | **REVIEW** | appropriate Platform layer | move only utilities that are genuinely vendor-neutral and contract-worthy |
| `Licensing/` | **SPLIT** | optional shared entitlement contracts | generic entitlement states may be shared; BricsCAD-specific host/package/update licensing remains product-specific |

The presence of a directory in the table is not evidence that every file in it is portable.

## 6. BricsCAD host surface classification

The following families normally remain `KEEP` or `ADAPT` in this repository:

### KEEP — BricsCAD product/runtime

- `PluginEntry` and `IExtensionApplication` lifecycle;
- `NETLOAD` / DemandLoad integration;
- V25/V26 assembly references and host-major build projects;
- Ribbon creation and BricsCAD palette lifecycle;
- WPF windows whose ownership/lifecycle is tied to BricsCAD documents;
- BricsCAD `DocumentLock` and native database transactions;
- native selection/editor prompts and transient graphics;
- `ObjectId` resolution and runtime entity opening;
- native `Solid3d` creation/boolean operations;
- BricsCAD layer/block/xref/layout/table APIs;
- registry/install/uninstall/package/update integration;
- V25/V26 runtime probes and local qualification scripts;
- host-major release manifests and package identity.

### ADAPT — Platform contract + BricsCAD implementation

Candidate adapter boundaries include:

- document manager/current drawing;
- drawing identity;
- entity handle lookup;
- database read/write transaction;
- selection sets;
- editor prompts/messages;
- layers;
- blocks;
- xrefs;
- layouts/plot;
- geometry interrogation;
- native solid generation;
- highlight/reveal/focus;
- undo/history capability;
- host units/UCS conversions.

The BricsCAD adapter translates native host objects at the boundary; it must not leak them into Platform.

## 7. Semantic identity migration

Identity is correctness-critical and must be migrated before broad feature code.

Required shared concepts:

- `ProjectId`;
- `FloorId`;
- `ZoneId`;
- `FamilyId`;
- `ElementId`;
- drawing identity;
- canonical CAD handle;
- source/generated CAD reference;
- provenance/ownership role.

Rules to preserve:

1. persisted CAD references use drawing identity + stable handle, not runtime object pointers;
2. equivalent hexadecimal handles normalize to one identity where that is the established product contract;
3. blank/corrupt IDs fail closed before mutation;
4. source and generated ownership remain distinguishable;
5. migration must preserve current `.qsdb` canonicalization and stale/dirty semantics.

## 8. Geometry and measurement migration

Create a strict separation:

```text
native BricsCAD geometry
        |
        v
BricsCAD adapter normalization
        |
        v
Platform Point/Vector/Bounds/measurement facts
        |
        v
semantic/quantity rules
```

Portable code may own:

- finite numeric guards;
- points/vectors/bounds;
- unit normalization;
- polygon/polyline algorithms that need no native object;
- tolerance policy;
- deterministic measurement formulas.

Adapter code owns:

- reading native curve/solid data;
- native intersection/boolean calls when a CAD kernel is required;
- tessellation/native topology extraction;
- writing entities/solids back into BricsCAD.

Do not create a misleading Platform `Solid3d` clone merely to mirror a vendor API.

## 9. Quantity/reporting migration

Target pipeline:

```text
BricsCAD native entity/semantic source
        -> adapter-normalized measurement facts
        -> Platform semantic quantity facts
        -> Platform deterministic rules
        -> Platform quantity results
        -> Platform schedule/report projection
        -> BricsCAD UI/native-table/export adapter
```

This preserves the strongest part of the existing QS3D architecture: quantities can be deterministic and testable without pretending that host geometry or UI is portable.

Migration gates include:

- exact quantity values on synthetic fixtures before/after migration;
- canonical source/element traceability preserved;
- dirty/freshness behavior preserved;
- no report/export file becomes the source of truth;
- BricsCAD locate/reveal still resolves the same source references.

## 10. Persistence migration

Persistence should migrate late enough that shared identity/domain contracts are stable.

### Plugin compatibility requirements

- current `.qsdb` files must remain loadable under the documented compatibility policy;
- bounded XML/input handling must not regress;
- current-schema validation remains fail closed;
- atomic publication/backup/recovery remains intact;
- stale-session/concurrent-save protection remains intact;
- dirty/persistence-stamp behavior remains observable-equivalent.

### Shared Platform direction

Move schema-neutral project state, validation rules, migration contracts and serialization-independent models first. Move concrete `.qsdb` serialization only when tests prove compatibility across the exact historical/current fixture matrix.

A future standalone `.qs3d` package is a separate persistence adapter/container and must not force destructive conversion of plugin projects.

## 11. Dependency/regeneration migration

Target Platform ownership:

- semantic dependency graph;
- dirty-state propagation;
- regeneration planning;
- deterministic regeneration inputs/results;
- model-health implications of stale/broken dependencies.

BricsCAD keeps:

- native mutation transaction;
- native entity generation;
- native rollback;
- document ownership/locking;
- host event subscriptions.

A Platform planner may say *what* should regenerate; the BricsCAD adapter owns *how* native objects are changed.

## 12. UI migration policy

Do **not** move BricsCAD WPF/XAML wholesale into Platform.

Shared UI concepts should be view models/contracts only when they are genuinely product-neutral. Concrete standalone UI belongs in `QS3D-CAD`; concrete hosted palette/window integration remains here.

A visual workflow may look similar across products while still having two host-specific shells.

## 13. Package/dependency consumption strategy

### Bootstrap stage

`QS3D-CAD` may pin `QS3D-Platform` by exact Git submodule SHA while APIs are evolving quickly.

### BricsCAD migration stage

Do not make the mature plugin depend on a floating sibling `main`. For each integration slice, consume an exact reviewed Platform version/commit. The preferred production direction is a versioned package/artifact once public contracts stabilize.

Every BricsCAD release note/qualification record should eventually identify:

- Platform package/version;
- Platform source SHA;
- BricsCAD source SHA;
- target host major;
- native runtime evidence for that exact combination.

## 14. Migration phases

### M0 — boundary and inventory

- document three-repository ownership;
- create per-file migration inventory;
- lock framework compatibility;
- establish package/version strategy;
- add cross-repository provenance rules.

### M1 — foundational value types

Migrate with parity tests:

- IDs;
- canonical handles;
- finite numeric helpers;
- units;
- points/vectors/bounds;
- generic result/diagnostic primitives.

No user-visible feature should depend on duplicate identity implementations longer than necessary.

### M2 — project/semantic domain

Migrate:

- project;
- floor/level;
- zone;
- family/type;
- semantic element;
- source/generated references;
- core relationships/invariants.

Keep `.qsdb` compatibility and existing plugin commands working throughout.

### M3 — quantity/formulas/cost/report projections

Migrate deterministic engines and add before/after golden fixtures. Keep native table/UI/selection presentation in the BricsCAD adapter.

### M4 — persistence/diagnostics/dependency/regeneration

Migrate shared rules only after domain/identity stability. Require schema and dirty-state regression suites before deleting legacy Core implementations.

### M5 — BricsCAD adapter conformance

Implement Platform CAD contracts against V25/V26 shared host source where safe. The adapter must prove:

- transaction commit/rollback;
- stable handle resolution;
- selection identity;
- document ownership;
- undo/history behavior;
- layer/block/xref/layout semantics for claimed capabilities.

### M6 — duplicate retirement

Delete old `QS3D.Core` implementations only after:

1. consumer has switched to Platform;
2. deterministic parity tests pass;
3. package/version is pinned;
4. BricsCAD runtime gates affected by the change are queued/executed as required;
5. rollback path is understood.

## 15. Mandatory per-slice workflow

For every migration slice:

1. refresh all relevant repository heads;
2. reserve work under the applicable repository coordination policy;
3. identify exact source contract and counterexamples;
4. add/confirm regression tests on the established implementation;
5. implement the Platform equivalent without vendor dependencies;
6. run Platform deterministic tests;
7. publish/pin the exact Platform version/SHA;
8. adapt the BricsCAD consumer without changing unrelated behavior;
9. run BricsCAD source/preflight/Core regression gates;
10. if native/runtime behavior changes, update the canonical LOCAL_ONLY queue rather than manufacturing a remote PASS;
11. compare observable results before deleting duplicate code;
12. close claims with exact SHAs/evidence.

A copy-paste that merely compiles is not a migration.

## 16. Cross-host parity suite

Build a reusable synthetic fixture suite that can eventually run against:

- current/legacy Core implementation;
- Platform pure implementation;
- BricsCAD adapter normalized outputs;
- standalone CAD adapter normalized outputs.

Priority parity cases:

- canonical handle aliases;
- floor/zone/family identity;
- direct scalar dirty-state detection;
- source/generated ownership;
- host links/openings;
- room boundaries/finishes;
- quantity rules;
- extreme finite numeric coordinates;
- schedules/BQ traceability;
- save/reload semantic state;
- malformed persisted data fail-closed behavior.

## 17. Framework compatibility gates

Until V25 support is retired by an explicit product decision:

- shared Platform contracts intended for BricsCAD must compile for `netstandard2.0` or another framework consumable by `net48`;
- V26/standalone may use .NET 8 in their host-specific projects;
- Platform APIs must not require a .NET 8-only type in public signatures used by V25;
- framework-specific optimizations belong behind adapters or multi-targeted implementations.

Framework compatibility is an API-design constraint, not a build-script afterthought.

## 18. Legal/SDK boundary

The sibling standalone effort does not authorize copying BricsCAD runtime binaries or proprietary implementation into Platform/CAD.

- BricsCAD assemblies remain external licensed host references for this product only.
- Platform remains clean-room/vendor-neutral.
- Standalone production DWG/rendering SDK binaries live behind the chosen legally licensed adapter and are not committed unless redistribution terms explicitly permit the chosen packaging method.
- customer/private drawings remain excluded from public/source regression fixtures.

## 19. Immediate migration backlog

Suggested migration work items, each independently reservable:

- **PBR-001:** generate exact per-file `QS3D.Core` MOVE/SPLIT/KEEP inventory.
- **PBR-002:** parity-map identity/canonical handle/unit policies to Platform.
- **PBR-003:** migrate project/floor/zone/family/element domain with golden tests.
- **PBR-004:** migrate normalized measurement and quantity-rule contracts.
- **PBR-005:** migrate formula/cost/report projections.
- **PBR-006:** design `.qsdb` compatibility facade over shared Platform project state.
- **PBR-007:** define Platform dependency/regeneration interfaces and BricsCAD execution adapter.
- **PBR-008:** implement BricsCAD document/database/transaction/selection contract adapter.
- **PBR-009:** package/version Platform for deterministic BricsCAD consumption.
- **PBR-010:** create cross-product semantic parity fixture suite.

These items should be split further whenever an existing ACTIVE/BLOCKED claim overlaps a feature.

## 20. Completion criteria for migration

Migration is complete only when:

- production BricsCAD no longer duplicates shared domain/quantity/persistence behavior that Platform owns;
- V25 and V26 consume a pinned/released Platform contract compatible with their target frameworks;
- standalone CAD consumes the same semantic/quantity contracts;
- host adapters contain vendor-specific database/editor/UI/geometry implementation only;
- cross-host parity suites prove shared semantics;
- `.qsdb` compatibility and BricsCAD release/runtime gates remain intact;
- standalone native-DWG qualification is independently complete;
- no repository needs a proprietary sibling SDK binary to run its host-neutral tests.

Until those conditions are met, the migration is **incremental and partially complete by design**. This is safer than treating a repository split as proof that the mature BricsCAD implementation has already moved.