# QS3D-BricsCAD — local agent continue-all handoff

**Updated:** 2026-08-10 (UTC+7)  
**Observed main before this handoff write:** `1e9f3666f8fb7d5e57e7143d021c032d83816472`

> `main` is moving concurrently. The SHA above is context only. Fetch the newest `main` immediately before any edit/build/qualification and record the exact tested SHA in evidence.

This document collects work that cannot be honestly marked complete from remote/static source review alone. It supplements, not replaces:

- `docs/LOCAL-V25-QUALIFICATION.md`
- `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`
- `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`
- `docs/DOCUMENTATION-LAYER.md`
- `docs/INTERCHANGE-JSON.md`

## 0. Rules

1. Work from a clean checkout of the exact SHA being qualified.
2. Never commit proprietary BricsCAD DLLs, certificates/private keys, private customer DWGs or raw customer evidence.
3. Do not weaken `SECURELOAD` or signing verification to make a test pass.
4. Do not force-push/reset `main`; multiple agents may be writing concurrently.
5. GitHub Actions are manual-only. A `continue all` instruction does **not** authorize workflow dispatch or release publication.
6. Source-implemented/static-guarded is not equivalent to compiled/NETLOAD/runtime-certified.

## 1. First local build gate

On Windows with the required .NET tooling and licensed BricsCAD V25 x64 installation, run the canonical local qualification runner from the newest `main` and pass the actual installed V25 directory:

```powershell
.\scripts\run-local-v25-qualification.ps1 `
  -BricsCadDir "C:\Program Files\Bricsys\BricsCAD V25 en_US" `
  -Profile "QS3D-V25-TEST"
```

Use the real local BricsCAD V25 directory; the path above is only the common example already used by the canonical qualification runbook. `-BricsCadDir` is mandatory. Do not copy BricsCAD managed assemblies into the repository.

`scripts/preflight-all.py` auto-discovers every `scripts/preflight-*.py` gate, so the current aggregate qualification already includes the Interchange JSON, polygon scanline and semantic-tag gates. They can still be run individually when diagnosing a failure:

```powershell
python .\scripts\preflight-interchange-json.py
python .\scripts\preflight-polygon-scanline.py
python .\scripts\preflight-semantic-tags.py
```

The Core smoke suite must include:

- `PolygonScanlineClipperSmoke.Run()`
- `SemanticTagRendererSmoke.Run()`
- `ProjectInterchangeJsonSmoke.Run()`

Do not mark any of these PASS merely because the source tokens exist. Record the real build/smoke process exit code.

## 2. Interchange JSON — V25 command qualification

Source now exposes:

```text
QS3DINTERCHANGEJSON
```

Required local proof:

- command registers uniquely after NETLOAD and DemandLoad;
- Save dialog opens/returns safely;
- cancel produces no file and no project mutation;
- Unicode Vietnamese drawing/output paths work;
- overwrite succeeds through the temp/replace boundary;
- representative project export is valid JSON and is stable for unchanged project state;
- semantic element IDs, Family/Floor/Zone references, dependencies and quantities are present;
- source DWG handles are explicitly drawing-local provenance;
- generated solid/rebar/mesh/curtain/physical-cut owner handles are absent;
- no importer/round-trip claim is shown to users.

The format remains read-only. Do not add import by deserializing directly into a live project.

## 3. Documentation layer — native V25 implementation

Core now has `SemanticTagRenderer`; native annotation does not yet have runtime proof.

### 3.1 Semantic MText/MLeader tags

Implement only against the installed V25 managed API and qualify:

- resolve one semantic owner through canonical source/generated ownership;
- rendered label comes from `SemanticTagRenderer` rather than copied decorative text;
- persist stable semantic owner ID + template identity on generated annotation;
- create a dedicated generated annotation ownership slot/registry; do not reuse `GeneratedSolidHandle`;
- transactional create/update/replacement;
- user/foreign annotation is never erased by ambiguous ownership;
- semantic/property/quantity change invalidates or refreshes tag deterministically;
- deleting/untracking an owner cannot leave a valid-looking orphan tag;
- Model Space and Paper Space are explicit and preserved across save/reopen;
- Unicode, long text, layer/style, scale, rotation, leader and viewport behavior are checked in V25.

### 3.2 DWG tables

Use existing QS3D schedule models as the data source. Do not reimplement quantities in the adapter.

Start with one schedule kind and prove:

- stable generated table ownership;
- deterministic columns/units/order;
- bounded rows/cells before mutation;
- Unicode Vietnamese;
- refresh/replacement after project changes;
- no deletion of user tables;
- Model/Paper Space contract;
- save/reopen and multi-DWG behavior.

### 3.3 Sheets / Layouts / Viewports

Do not guess BricsCAD API signatures. Establish stable QS3D sheet/view IDs, ownership, create/update/rename/delete rules, viewport scale/lock behavior and user-layout protection in the real V25 host first.

## 4. General polygon Slab/Foundation mesh

Core now contains `PolygonScanlineClipper` for bounded simple convex/concave footprint clipping. This is only a geometric primitive.

Still required before enabling polygon mesh:

1. implement/test a deterministic inward **bar-centerline cover offset** for sloped and concave edges;
2. define hole/opening loops and clipping semantics;
3. reject self-intersections, invalid orientation/zero-area loops and impossible cover offsets before mutation;
4. cap polygon vertices, scan segments, bars per face and total generated native objects;
5. reuse current independent X/Y diameter/spacing/count and top/bottom face semantics;
6. preserve canonical generated mesh ownership/stale/health/replacement behavior;
7. make adapter generation cross-layer atomic with project rollback before CAD commit;
8. validate rectangle compatibility against the existing `RectangularSlabMeshPlanner`/Foundation path;
9. test concave footprints, holes, narrow necks, sloped edges, very large coordinates and near-tolerance geometry;
10. qualify generated geometry in licensed V25 and save/reopen.

Do **not** call scanline clipping alone cover-compliant reinforcement.

## 5. Bottom/Top Level native Z-chain

Core Level semantics and health/release checks exist, but native UI assignment is intentionally gated.

Before exposing Bottom/Top Level controls, migrate the dependent vertical-placement chain coherently to the shared `ElementVerticalPlacementService` contract:

- wall/GlassWall/WallPier/StructuralWall host solids;
- open-polyline/path host builders;
- Beam/Column/Slab/Foundation where the same semantics apply;
- Door/WallOpening host matching and physical cutters;
- Curtain LINE/path frames and future panels;
- longitudinal/tie/stirrup/slab/wall/foundation rebar generated Z placement;
- Direct Draw source-relative compatibility;
- quantities/fingerprints/stale invalidation where height/elevation changes;
- save/reopen and project Floor/Level mutation.

Required compatibility matrix:

- no Level refs = existing legacy geometry exactly;
- Bottom Level only = absolute bottom + legacy height;
- Bottom + Top = effective height from top-bottom;
- Top without Bottom = fail closed;
- missing/renamed/deleted Level = health/release blocker;
- Level edit invalidates all affected native/dependent outputs;
- opening/rebar/curtain remains spatially aligned with host.

Do not expose assignment UI until this chain is coherent.

## 6. Source reconcile / Modify workflow

Source now provides ownership-safe reconcile and:

```text
QS3DSYNCSOURCE
```

Required local V25 proof:

- source LINE/POLYLINE edit then reconcile updates semantic measurements;
- generated host/rebar/curtain dependents are invalidated/removed safely;
- selecting generated output instead of authoritative source fails closed;
- unknown/ambiguous source ownership fails closed;
- forced failure before CAD commit restores project state and generated geometry;
- undo/redo behavior is understood and documented;
- active-document switch/multi-DWG does not mutate the wrong project;
- save/reopen keeps reconciled semantic state.

Do not compose `QS3DSYNCSOURCE` + `QS3DBUILD3D` and call the result atomic unless a real shared transaction/recovery contract is implemented.

## 7. Curtain wall remaining P0

### 7.1 Whole-command atomicity/recovery

`QS3DCURTAIN3D` now wraps the canonical nested host/frame builder transactions in one outer native transaction and restores a command-level semantic snapshot when that outer transaction does not commit. The old `PARTIAL COMMIT` source contract is retired.

LOCAL_ONLY completion still requires failure injection after every logical phase on the exact built SHA, followed by health and save/reopen checks proving no half-host/half-frame state survives.

### 7.2 Native panel-by-panel glass

Use existing `CurtainWallDetailPlanner.Panels` as semantic geometry input. Required design:

- dedicated `GeneratedCurtainPanelHandles`-style canonical ownership;
- count/fingerprint/stale/health/release semantics;
- bounded panel count before mutation;
- LINE + guarded straight/bulged path mapping;
- opening/door interruption or clipping that cannot put glass through openings;
- foreign/ambiguous owner fail-closed behavior;
- atomic/recoverable host + frame + panel replacement;
- select/locate/cleanup integration;
- real V25 save/reopen geometry proof.

## 8. Physical L/T/X wall junction output

Current wall-junction analysis/snap planning must not be confused with safe multi-owner physical Solid3d reconciliation.

Preferred local/source design remains:

- preserve original wall ownership;
- create a dedicated junction-owned infill/composite output with stable owner identity and dependency list, or an explicit semantic junction record;
- do not boolean-union and then ambiguously reassign one solid to multiple walls;
- invalidate junction output when any owner source/profile/thickness/elevation changes;
- keep Door/Opening host ownership on the original wall;
- reject mixed project/drawing/incompatible vertical ranges before native mutation;
- generated junction output must be distinguishable from BQ/rebar/detail outputs.

Validate L/T/X, 2/3/4 owners, different thicknesses, removal/rebuild and opening-host retention in V25.

## 9. Direct Draw preview/repeated mode

Source P0/P1 authoring is implemented, but richer transient preview/repeat authoring is local-runtime work.

Only implement after confirming V25 DrawJig/transient/editor lifecycle:

- ESC/cancel leaves no persistent preview or semantic/CAD residue;
- thickness/profile preview follows active planar UCS;
- repeated mode reuses only safe last values/active Family;
- document switch cancels safely;
- no second geometry/Family model is introduced;
- final source + semantic + native commit remains atomic;
- UI/palette refresh errors remain non-destructive.

## 10. Signing / installer / updater / licensing

Production trust remains local/infrastructure work.

Signing:

- use Windows certificate store/approved certificate only;
- HTTPS timestamp;
- sign -> verify signer/timestamp -> finalize manifest/hashes/ZIP;
- clean install/upgrade/rollback/uninstall on a customer-like machine;
- never commit PFX/password/private key.

Licensing:

Core verification primitives are not enough to invent commercial enforcement. Owner/product inputs are still required for SKU, seat model, machine/user/org binding, trial/grace, activation/offline policy, key rotation/revocation, replacement policy and command whitelist. Do not lock the adapter using guessed policy.

## 11. Performance / UI matrix

After functional qualification, measure before optimizing:

- large element/family/property counts;
- DependencyGraph/regeneration;
- room boundary graph and Auto Room;
- wall junction analysis/snap planning;
- Auto Host candidate matching;
- Curtain grids/frames/panels;
- BQ/schedules/BBS/Excel/Interchange JSON;
- generated ownership registry and Health All / Release Check;
- rebar/mesh generation limits;
- 100/125/150/200% DPI and narrow/normal/wide palettes.

Record hardware, BricsCAD build, project size and exact SHA with every timing. Do not optimize from synthetic guesses alone.

## 12. Close-out template

```text
Exact QS3D SHA: <40-char SHA>
Windows: <edition/build>
BricsCAD V25: <edition/build>
.NET/MSBuild: <version>
Core Release build: PASS/FAIL
Core smoke: PASS/FAIL
Adapter exact-V25 build: PASS/FAIL
NETLOAD: PASS/FAIL
DemandLoad: PASS/FAIL
New source preflights: PASS/FAIL
Interchange JSON runtime: PASS/FAIL
Source reconcile runtime: PASS/FAIL
Level native chain: PASS/FAIL/NOT IMPLEMENTED
Documentation native layer: PASS/FAIL/NOT IMPLEMENTED
Polygon mesh native: PASS/FAIL/NOT IMPLEMENTED
Curtain atomicity/panels: PASS/FAIL/NOT IMPLEMENTED
L/T/X physical junction: PASS/FAIL/NOT IMPLEMENTED
Direct Draw transient/repeat: PASS/FAIL/NOT IMPLEMENTED
Signing/install/update: PASS/FAIL/NOT AUTHORIZED
Licensing provider: PASS/FAIL/POLICY REQUIRED
Private DWG regression: PASS/FAIL
Unicode/HiDPI: PASS/FAIL
Large-model performance: PASS/FAIL
Known blockers: <sanitized list>
```

Only after the relevant rows are genuinely PASS should documentation/release status be upgraded from source-implemented to runtime-qualified.
