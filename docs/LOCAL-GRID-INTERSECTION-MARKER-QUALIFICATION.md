# Grid pair-owned native intersection marker qualification

## Status

- Repository source carrier: Issue #3771 (`Lane-Key: issue-3771`).
- Exact source/guard candidate: `707ba4f2991e6ab47a81d9de80a32c19e55fca79`; a later descendant is acceptable only when its additional changes are handoff documentation and repository-required reconciliation that preserves this source/guard tree.
- Remote/source status: `SOURCE_READY` only after the exact current candidate passes repository CI.
- Licensed runtime status: `PENDING_LOCAL`. Hosted/static CI is not BricsCAD runtime evidence.

## Purpose

Qualify the native BricsCAD lifecycle for deterministic Grid intersection markers whose ownership belongs to the canonical Grid pair, not to either Grid independently. The source contract uses compact `GIP1:` pair tokens and `GIX1:` occurrence-owner tokens, materializes native `Circle` entities, persists `QS3D_GRID_INTERSECTION` XData, replaces only complete owned marker sets, and fails closed for malformed, duplicate, foreign-project, stale or ambiguous ownership.

## Prerequisites

1. Clean checkout of the exact intended SHA. Record it before building.
2. Licensed BricsCAD V25 x64 for the V25 run and licensed BricsCAD V26 x64 with the matching V26 assembly for the parity run.
3. Matching QS3D plugin/Core binaries built from the exact candidate. Record host version, assembly identity and SHA-256 without exposing private install paths.
4. Disposable test drawings only. Do not mutate customer/private drawings or commit raw DWGs, paths, project IDs or Handle lists.
5. Start with zero unrelated BricsCAD processes. Preserve and restore any test-owned loader/profile state used by the existing local qualification harness.

## Narrow command matrix

Use simple semantic Grid sources backed by live native LINE/ARC entities. Include at least one LINE×LINE intersection and one representative ARC pair. Keep all sources coplanar in WCS XY for the positive cases.

### P01 — create and deterministic ownership

1. In disposable drawing A, create/capture at least two canonical Grid sources with one valid intersection.
2. Run `QS3DGRIDINTERSECTIONS`.
3. Verify exactly one native Circle is created at the planned XY/elevation and carries `QS3D_GRID_INTERSECTION` XData version `1`.
4. Verify the marker records one canonical `GIP1:` pair token and one canonical `GIX1:` owner token for occurrence `0`, plus both canonical Grid IDs and finite invariant coordinates.
5. Repeat refresh without changing geometry. The owned set must be replaced deterministically with no duplicate live owner token and no unmarked/foreign object deletion.

### P02 — selected-pair scope and pre-bind cancellation

1. Add a third Grid so drawing A has at least two independent Grid pairs.
2. Change geometry for one selected Grid only.
3. Run `QS3DGRIDINTERSECTIONSSEL` using that Grid.
4. Verify only marker pairs touching the selected Grid are eligible for replacement; unrelated Grid-pair markers remain untouched.
5. Cancel/empty/invalid selection must return before canonical mutation binding/cache creation and without native marker mutation.
6. Replace/reload the project between the detached selection preview and canonical bind; the command must reject ProjectId/ChangeVersion drift and require rerun.
7. `QS3DGRIDINTERSECTIONHEALTH` must remain read-only and must not canonical-bind/cache a project solely to inspect marker health.

### P03 — fail-closed ownership corruption

On separate restored copies, inject one condition at a time into the test-owned marker set:

- remove one expected marker entity;
- duplicate a live owner token;
- corrupt XData version/field count;
- alter the persisted pair token or owner token so it no longer matches the two Grid IDs and occurrence;
- change the persisted project identity to a foreign project;
- make one source Grid stale/erased/generated or otherwise non-authoritative;
- create same-pair duplicate/near-duplicate intersections inside the source tolerance where owner occurrence would be ambiguous.

Run refresh and `QS3DGRIDINTERSECTIONHEALTH`. Every unsafe refresh must fail before destructive erase/append, preserve the surviving old marker/native set, and leave another drawing untouched. Health should emit the corresponding bounded missing/stale/ownership-invalid condition without claiming repair.

### P04 — geometry refresh, Undo/Redo, save/reopen

1. Restore a healthy marker set.
2. Move/edit one authoritative Grid so the planned intersection changes while remaining valid and coplanar.
3. Refresh and verify the old owned marker is erased exactly once and the replacement appears at the new planned point with the same canonical pair identity contract.
4. Native Undo must restore the coherent prior marker state; Redo must restore the coherent replacement state.
5. Save, close and cold reopen. Re-run health and verify the persisted marker ownership/position remains coherent.

### P05 — document and owner-space affinity

1. Open disposable drawings A and B with distinct QS3D projects.
2. Prepare valid Grid pairs in both drawings.
3. While a command prepared for A is pending, activate B where the command boundary permits a document switch test. Mutation must refuse rather than write B or stale A state.
4. Reactivate A and run a clean refresh. B must remain byte/native/semantic unchanged.
5. A pair whose source entities reside in incompatible native owner spaces or elevations must fail closed before marker materialization.

### P06 — V26 parity

Repeat the bounded positive create/refresh, one corruption refusal, selected-scope, save/cold-reopen and multi-DWG isolation cells in licensed V26 using the matching V26 build. V25 results do not qualify V26.

## Expected result

- Pair ownership is deterministic and compact (`GIP1:`/`GIX1:`), independent of Grid input order.
- Marker replacement is exact-set and transactionally fail-closed: unsafe/corrupt/foreign/stale ownership never causes partial erase or partial append.
- Generated QS3D output is never accepted as an authoritative Grid source.
- Selected refresh touches only pairs containing selected canonical Grid IDs and does not bind/cache on cancel/invalid selection.
- Health is read-only and detects missing, stale-geometry, stale-owner and ownership-invalid states without silently mutating them.
- Undo/Redo, save/cold-reopen and active-document switching preserve project/document affinity.
- No cross-DWG mutation and no foreign/unmarked entity deletion occurs.

## Cleanup

Close all disposable documents according to the scenario, remove test sidecars/copies/private scripts and restore any test-owned loader/profile changes. Verify zero test-owned BricsCAD processes remain. Keep raw runtime captures under ignored local artifact paths only.

## Minimum sanitized evidence

Record:

- exact tested Git SHA;
- BricsCAD V25/V26 versions and x64/runtime identity;
- plugin/Core identity and SHA-256;
- aggregate Grid/pair/marker counts, never raw Handle lists;
- P01-P06 pass/fail booleans and a bounded failure code/stage when applicable;
- proof of compact `GIP1:`/`GIX1:` ownership consistency without publishing private Grid IDs;
- cancel/pre-bind and project-drift refusal result;
- before/after object-count and position summaries for replacement and corruption-refusal cells;
- Undo/Redo and save/cold-reopen outcomes;
- drawing A/B isolation result;
- cleanup/process-residue result.

`LOCAL_PASS` may be recorded only after these checks actually execute in the compatible licensed host on the exact SHA. Until then the disposition is `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
