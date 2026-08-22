# QS3D — remaining local / engineering qualification gates

Updated: 2026-08-10 (UTC+7)

This file is a **mandatory handoff for work that a source-only agent must not pretend to finish**. Read it together with `docs/LOCAL-V25-QUALIFICATION.md` and the current `main` source.

The owner requirement is to continue source implementation aggressively, but to leave exact local instructions whenever completion requires real BricsCAD V25, a private DWG, Windows-native behavior, an approved engineering standard, or production signing material.

## 0. Rules before doing any item

1. Fetch/pull the latest `main` immediately before editing.
2. Inspect recent commits; multiple agents can push concurrently.
3. Never overwrite a stale blob, reset `main` backwards or force-push over another agent.
4. Run `python scripts/preflight-ci-manual-only.py` before any release-related handoff.
5. **Do not dispatch GitHub Actions** unless the repository owner separately and explicitly asks for a build/release run.
6. Never commit BricsCAD proprietary DLLs, private/customer DWGs, signing private keys, raw certificates containing secrets, machine credentials or unsanitized runtime evidence.
7. Local runtime evidence belongs under the gitignored `artifacts/` tree unless a sanitized text summary is intentionally committed.

## 1. Source state that must not be reimplemented blindly

Before starting the remaining tasks, verify current `main`, but treat these as the expected baseline:

- WallPier open/bulged-POLYLINE path-profile support already exists in Core + V25 adapter.
- Curtain frame generation already supports guarded LINE and open/bulged WCS-XY POLYLINE paths.
- Curtain panel topology already exists in Core through `CurtainWallDetailPlanner.Panels`; the open gap is native panel output/ownership/atomic replacement, not grid planning from scratch.
- Generated CAD ownership is centralized by `QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy`; new generated slots named `Generated*Handle` or `Generated*Handles` participate automatically in generic ownership discovery.
- Host/opening/rebar builders have received cross-layer atomicity hardening. Do not regress those boundaries.
- `QS3DRELEASECHECK` aggregates semantic, generated, live-CAD, stale, rebar and BOM health.
- Rebar fabrication qualification now has an **opt-in, standards-neutral evidence gate** through `RebarFabricationQualificationHealthService`. It checks declared provenance/approval consistency only; it does not and must not claim engineering compliance by itself.
- `docs/LOCAL-V25-QUALIFICATION.md` is the canonical exact-SHA Windows/V25 execution runbook.

## 2. P0 local task — Curtain panel-by-panel native glass

### Why source-only completion is unsafe

Core already describes panel cells, but correct V25 native panel solids depend on real `Solid3d` behavior, path orientation, bulged-polyline tessellation, opening clipping, transaction replacement, unit conversion and boolean/tolerance behavior. Adding a third independent native commit after host + frame would increase the existing partial-commit risk.

### Required architecture

Do **not** create panel solids as an unrelated best-effort transaction.

Target contract:

- semantic host remains the `GlassWall` element;
- native panel outputs use an explicit owner slot such as `GeneratedCurtainPanelHandles`;
- add matching stale-state/fingerprint support instead of leaving panel handles invisible to `ProjectElement.MarkGeneratedGeometryStale()`;
- panel outputs must resolve through canonical generated ownership and selection/locate/cleanup paths;
- host + frame + panel replacement must be preplanned and committed as one atomic CAD replacement boundary, or use an equally strong recoverable transaction design proven on V25;
- old owned output may be replaced; foreign/ambiguous output must fail closed and must never be erased;
- cap panel/object counts before native mutation to avoid runaway DWG generation;
- opening/door regions hosted on the GlassWall must interrupt or clip panel generation deterministically; do not leave full glass panels through an opening while only the host backing solid is cut.

### Primary source surfaces

Inspect current versions before editing:

- `src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs`
- `src/QS3D.Core/Geometry/CurtainFrameOpeningPlanner.cs`
- `src/QS3D.Core/Geometry/CurtainWallOpeningFramePlanner.cs`
- `src/QS3D.Core/Domain/ProjectElement.cs`
- `src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs`
- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- `src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameLiveFingerprint.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameLiveStateService.cs`
- `src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs`

### Required deterministic source tests/preflight

Add tests/preflight for at least:

- one LINE curtain with multiple cells;
- one open straight-segment POLYLINE curtain;
- one bulged POLYLINE curtain;
- panel count bound before any mutation;
- opening removes/splits the affected panel region;
- stale semantic change invalidates panel output;
- duplicate/foreign panel ownership blocks replacement;
- forced frame/panel failure leaves no half-updated semantic/native ownership contract;
- Release Check sees missing/stale/duplicate panel output.

### Required real V25 proof

On the same exact SHA and built DLL:

1. build a LINE GlassWall with frames + panel solids;
2. repeat with an open POLYLINE;
3. repeat with a bulged POLYLINE;
4. add Door/WallOpening and rebuild;
5. verify no panel crosses the physical opening;
6. edit dimensions/grid parameters and rebuild;
7. force one panel creation/replacement failure and verify previous valid native state remains intact;
8. save/reopen and run `QS3DHEALTHALL` + `QS3DRELEASECHECK`;
9. select a generated panel and confirm owner/Family/Locate behavior resolves to the correct semantic GlassWall.

PASS requires screenshots/logs locally plus a sanitized exact-SHA result summary. Source-only review is not PASS.

## 3. P0 local task — physical L/T/X wall junction geometry

### Non-negotiable ownership rule

**Do not Boolean-union the existing semantic wall `GeneratedSolidHandle` objects together.** A destructive union can consume one wall solid, create shared ownership ambiguity, break opening host semantics, break regeneration/replacement and make health/selection unsafe.

### Required architecture

Use an ownership-safe junction representation. Preferred direction:

- preserve each semantic wall and its generated host solid;
- let `WallJunctionPlanner` / `WallJunctionAdjustmentPlanner` decide junction topology and source-centerline adjustment;
- create a dedicated junction-owned infill/composite output rather than assigning one physical solid to multiple wall owners;
- if a new semantic junction element is introduced, give it stable identity, dependency links to participating wall IDs and generated ownership; if a separate auxiliary ownership record is used instead, it must still be persisted, health-checked and replaceable without ambiguity;
- expose generated junction output through generic ownership, cleanup, selection/locate and B4D/source-capture exclusion;
- invalidate/rebuild a junction when any participating wall source/profile/elevation/thickness changes;
- reject mixed project/drawing ownership and incompatible vertical ranges before mutation;
- opening/door host ownership stays attached to the original wall, never to an ambiguous union artifact.

### Primary source surfaces

- `src/QS3D.Core/Geometry/WallJunctionPlanner.cs`
- `src/QS3D.Core/Geometry/WallJunctionAdjustmentPlanner.cs`
- `src/QS3D.BricsCAD.V25/WallJunctionCommands.cs`
- `src/QS3D.BricsCAD.V25/WallJunctionSnapCommands.cs`
- current wall native builders and generated ownership/health services.

### Required source tests

Cover at least:

- L junction, T junction and X junction;
- two, three and four owners respectively;
- differing wall thicknesses;
- incompatible elevation/height fails before CAD mutation;
- regeneration after one source moves;
- removal/untrack of a participating wall invalidates or removes junction output safely;
- ambiguous/foreign generated junction ownership fails closed;
- Door/Opening on one participant remains owned by that wall after junction rebuild.

### Required real V25 proof

Run L/T/X cases with representative tolerances and thicknesses in millimetre and metre drawings. Inspect seams/intersections visually and through native solid validity. Force a boolean/solid creation failure and verify no original wall solid is consumed or left half-replaced.

## 4. P0 engineering task — standard-specific fabrication-grade rebar

### Current source contract

The repository now has an opt-in evidence gate:

Project metadata:

- `QS3D.RebarFabrication.RequireQualification`
- `QS3D.RebarFabrication.StandardCode`
- `QS3D.RebarFabrication.DetailingRevision`

Generated-rebar element properties:

- `RebarFabricationStatus` — must be `Approved` when qualification is required;
- `RebarFabricationStandardCode` — must bind to the project standard;
- `RebarFabricationDetailingRevision` — must bind to the project revision.

When qualification is enabled, `QS3DRELEASECHECK` must block missing output, missing standard/revision, unapproved output or standard/revision mismatch.

Run:

```text
python scripts/preflight-rebar-fabrication-qualification.py
```

This is **not** an automatic engineering-code compliance claim.

### What an engineering/local agent must supply

The project owner/engineer must choose the exact governing standard and revision before implementation of numeric fabrication rules, for example a specific TCVN/ACI/BS revision. Do not infer one from locale or project name.

For the selected standard, implement only values/rules backed by an approved engineering source, including whichever are applicable:

- minimum bend diameters/radii by bar grade/diameter;
- hook type/angle/tail dimensions;
- lap splice rules;
- anchorage/development rules;
- cover constraints;
- bar spacing/clear-distance constraints;
- shape-code mapping/BBS conventions;
- tolerances and rounding.

Store provenance (standard code + exact revision) with the generated/detailing data. Do not hard-code a generic value and label it TCVN/ACI/BS compliant.

### Acceptance

- deterministic unit tests for every encoded rule and boundary;
- one approved reference calculation per rule family;
- BBS/detailing export carries the same standard/revision provenance;
- `QS3DRELEASECHECK` blocks a mixed-standard or stale-revision model;
- local V25 geometry matches explicit semantic fabrication dimensions;
- a qualified engineer signs off the rule set outside the software before customer fabrication use.

## 5. P0 production task — signing / installer / updater qualification

Source already contains signing/tag/version/update hardening. Remaining production proof requires real operational material.

Local/release engineer must validate:

- approved Authenticode certificate subject/thumbprint;
- private-key custody outside Git/repository artifacts;
- trusted timestamp authority and successful timestamp verification;
- assembly/package version exactly matches intended tag/manifest;
- finalized signed package hash matches published metadata;
- clean Windows install, DemandLoad, upgrade, rollback and uninstall;
- updater rejects relabeled/replayed/mismatched signed payloads;
- `SECURELOAD` is not weakened to make installation pass.

Never commit the private key or customer signing credentials. A source-only agent may improve validators/scripts but may not mark production signing PASS.

## 6. Exact-SHA local qualification matrix

Always start from `docs/LOCAL-V25-QUALIFICATION.md` and run its exact-SHA runner. The final sanitized handoff for an intended customer build must include:

```text
Exact SHA: <40-char SHA>
Windows x64: PASS/FAIL
BricsCAD V25 build/edition: <value>
Core build/tests/preflights: PASS/FAIL
V25 adapter build: PASS/FAIL
NETLOAD: PASS/FAIL
DemandLoad: PASS/FAIL
Direct Draw: PASS/FAIL
Door/Opening booleans: PASS/FAIL
Room/HT_PHONG: PASS/FAIL
Curtain host+frame: PASS/FAIL
Curtain panel-by-panel: PASS/FAIL/NOT IMPLEMENTED
Wall L/T/X physical junction: PASS/FAIL/NOT IMPLEMENTED
Rebar geometry/atomicity: PASS/FAIL
Rebar governing standard + revision: <explicit value or NOT QUALIFIED>
Rebar fabrication qualification: PASS/FAIL/NOT QUALIFIED
Save/reopen + multi-DWG: PASS/FAIL
Unicode/HiDPI: PASS/FAIL
Private-DWG regression: PASS/FAIL
Clean install/upgrade/uninstall: PASS/FAIL
Authenticode + timestamp: PASS/FAIL/NOT SIGNED
Known blockers: <sanitized list>
```

Never change `FAIL`, `NOT IMPLEMENTED` or `NOT QUALIFIED` to PASS based only on source review.

## 7. Definition of done for these remaining gates

These items are complete only when all are true:

- source architecture preserves semantic/generated ownership and fail-closed behavior;
- deterministic Core/static regression coverage exists;
- aggregate preflight remains green;
- exact-head V25 build succeeds against installed V25 assemblies;
- interactive V25 scenarios above are green on the same SHA;
- save/reopen/multi-DWG behavior is green;
- engineering-standard claims are backed by an explicit approved standard/revision and source;
- production signing claims are backed by actual certificate/timestamp/package evidence;
- documentation reflects only proved support;
- no proprietary/private artifacts are committed;
- GitHub Actions remain manual-only until the owner explicitly requests a run.
