# Rectangular Grid native source authoring — LOCAL qualification

Issue: #5280  
Lane-Key: `issue-5280`  
Command: `QS3DGRIDRECT`

## Evidence boundary

Hosted CI, deterministic source preflights and locked-reference V25 compilation are source evidence only. They are **not** licensed BricsCAD runtime evidence. Keep this package `LOCAL_ONLY / NO_RESULT` until the exact candidate SHA is fetched, built and exercised in a licensed BricsCAD V25 host.

The command authors primary Grid LINE sources; they are intentionally not tagged as derived generated output. Canonical ownership is the project-side semantic Grid element with stable id `GRIDRECT:<system-key>:<U|V>:<index>` and exactly one authoritative `SourceHandles` entry.

## Preconditions

1. Fetch the exact candidate SHA recorded on #5280 / its PR. Do not qualify a nearby branch tip.
2. Build the locked-reference V25 adapter using the repository-supported local build path.
3. Open a disposable DWG with explicit resolved drawing units (`QS3DUNITS`).
4. Keep a second DWG open for the multi-document isolation case.
5. Record command line output, before/after LINE counts, semantic Grid ids/source handles and saved DWG/sidecar hashes where practical.

## Matrix

### GR01 — first rectangular creation

Run `QS3DGRIDRECT` with a canonical lowercase system key, origin, rotated positive-U direction, U count 4 / spacing 3.0 m and V count 3 / spacing 4.0 m.

Expected: 7 native LINEs in current space; 7 semantic Grid elements with stable `GRIDRECT:` ids; each semantic element owns exactly one live LINE source handle; no derived generated-output marker is attached to those primary sources.

### GR02 — rotated planner fidelity

Repeat in a clean DWG at a non-axis-aligned U direction.

Expected: U/V families are orthogonal, station spacing matches requested metres under the DWG unit policy, all LINEs remain at the selected origin elevation, and no axis is silently projected back to world X/Y.

### GR03 — same-key replacement / stable semantic ids

Rerun the same system key with the same counts but different origin/direction/spacing.

Expected: old authoritative LINEs are replaced rather than accumulated; semantic ids remain identical; source handles change to the new authoritative LINEs; no duplicate semantic id/source handle appears.

### GR04 — count shrink / grow

Rerun the same key 4x3 -> 2x2 -> 5x4.

Expected: retired semantic stations and their native LINEs disappear after shrink; new stable indexed ids appear after grow; matching station ids persist; total live sources equal U+V after each commit.

### GR05 — corruption fail-closed

Before rerun, erase one authoritative source manually or move one source into another owner space while leaving the sidecar ownership intact.

Expected: replacement is refused before any validated old source is erased; no partial new system is committed; semantic state remains at its pre-command snapshot.

### GR06 — semantic id collision fail-closed

Create a foreign semantic element that occupies one desired `GRIDRECT:<key>:...` id without the same system ownership, then rerun.

Expected: command refuses the operation before destructive mutation.

### GR07 — invalid input bounds

Exercise blank/noncanonical/uppercase/spaced system keys, U/V count below 2 or above 200, zero/negative/non-finite spacing where host input permits, and a U direction point coincident with origin.

Expected: fail closed; no native or semantic mutation.

### GR08 — intersection-marker compatibility

After GR01 run `QS3DGRIDINTERSECTIONS`, then rerun the same Grid key with changed geometry and refresh intersections again.

Expected: authored Grid LINEs are accepted as canonical sources; pair-owned marker lifecycle remains the existing implementation; refresh does not classify primary authored Grid lines as derived outputs.

### GR09 — Undo/Redo

Create or replace one system, then exercise host Undo and Redo while observing DWG native LINEs and project sidecar state.

Expected: record the exact behavior. Do not claim transactional Undo coupling unless observed; any divergence is a blocker for `LOCAL_PASS` and must become a bounded follow-up defect.

### GR10 — save / cold reopen

Save DWG and project, close BricsCAD, reopen, verify all semantic ids resolve their one authoritative source handle, then rerun same key.

Expected: cold-reopen replacement succeeds with no duplicate ownership.

### GR11 — multi-DWG isolation

Create the same system key in two open DWGs with different geometry. Switch active documents and rerun one side.

Expected: only the active document/project mutates; the other DWG and sidecar remain byte/semantic unchanged.

### GR12 — post-commit UI failure isolation

Using the repository-supported local probe/debug mechanism, make palette refresh/status fail after a successful Build return.

Expected: committed native + semantic Grid remains valid; command reports UI sync degradation without converting the successful geometry/semantic commit into rollback/failure.

## Acceptance recording

Record exact candidate SHA and per-case `PASS`/`FAIL`/`BLOCKED`. Only a licensed exact-SHA run may produce `LOCAL_PASS`. Hosted CI, source inspection or successful compilation must remain `LOCAL_ONLY / NO_RESULT`.