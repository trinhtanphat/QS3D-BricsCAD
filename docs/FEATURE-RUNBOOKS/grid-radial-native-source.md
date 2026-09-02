# Radial Grid native source authoring — LOCAL qualification

Issue: #5291  
Lane-Key: `issue-5291`  
Command: `QS3DGRIDRADIAL`

## Evidence boundary

Hosted CI, deterministic source preflights and locked-reference V25 compilation are source evidence only. They are **not** licensed BricsCAD runtime evidence. Keep this package `LOCAL_ONLY / NO_RESULT` until the exact candidate SHA is fetched, built and exercised in a licensed BricsCAD V25 host.

The command authors primary Grid LINE rays and ARC rings; they are intentionally not tagged as derived generated output. Canonical ownership is the project-side semantic Grid element with stable id `GRIDRAD:<system-key>:<RAY|RING>:<index>`, `QS3D.GridSystem.Kind=RADIAL`, and exactly one authoritative `SourceHandles` entry.

## Preconditions

1. Fetch the exact candidate SHA recorded on #5291 / its PR. Do not qualify a nearby branch tip.
2. Build the locked-reference V25 adapter using the repository-supported local build path.
3. Open a disposable DWG with explicit resolved drawing units (`QS3DUNITS`).
4. Keep a second DWG open for multi-document isolation.
5. Record command output, before/after LINE+ARC counts, semantic ids/source handles, and saved DWG/sidecar hashes where practical.

## Matrix

### RR01 — first radial creation
Run `QS3DGRIDRADIAL` with canonical lowercase key, non-origin center, rotated first-ray direction, 6 rays / 30° step, inner radius 0 m, first ring 2 m, 4 rings / 2 m spacing.
Expected: 6 LINE rays + 4 ARC rings; 10 semantic Grid elements with stable `GRIDRAD:` ids; each owns exactly one live native source handle.

### RR02 — planner fidelity
Verify every ray starts at requested inner radius and reaches the outermost ring, every ring shares the requested center/elevation, angle/radius spacing matches requested values under DWG unit policy, and the first ray honors the picked WCS-XY direction.

### RR03 — same-key replacement / stable semantic ids
Rerun same key with changed center/direction/spacing but same counts.
Expected: old authoritative LINE/ARC sources are replaced rather than accumulated; ids persist and handles change without duplicates.

### RR04 — count shrink / grow
Rerun 6 rays + 4 rings -> 3+2 -> 8+5.
Expected: retired semantic stations/native entities disappear after shrink; new indexed ids appear after grow; matching ids persist; live sources equal ray+ring count.

### RR05 — corruption fail-closed
Erase one authoritative source manually, change one source to the wrong native type, or move it to another owner space while sidecar ownership remains.
Expected: replacement refuses before any validated old source is erased; no partial new radial system commits; semantic state remains at pre-command snapshot.

### RR06 — semantic id / cross-system collision
Occupy a desired `GRIDRAD:<key>:...` id with foreign ownership or create a same-key non-radial Grid element.
Expected: no foreign element is adopted/erased; desired-id collision fails closed before destructive mutation.

### RR07 — invalid bounds and ambiguity
Exercise blank/noncanonical keys, counts outside [1,200], coincident center/direction point, non-positive angle step, invalid radii/spacing, ray steps that normalize two rays to the same angle, and ring radii that collide/leave declared extent where host input permits.
Expected: canonical Core planner/builder rejects before native/semantic commit.

### RR08 — intersection compatibility
After RR01 run `QS3DGRIDINTERSECTIONS`, then replace the same radial key and refresh intersections.
Expected: authored LINE/ARC sources remain canonical semantic Grid inputs; existing pair-owned intersection lifecycle is reused; primary sources are never classified as derived generated output.

### RR09 — full-ring ARC native fidelity
Inspect every materialized ring entity and its geometric extents/length.
Expected: each semantic RING maps to exactly one live full-ring ARC representation matching canonical `PlanRadial` center/radius/2π sweep. If BricsCAD normalizes/rejects the adapter representation, mark `FAIL` and open a bounded native-representation defect; do not reinterpret hosted compile as runtime proof.

### RR10 — Undo/Redo
Create/replace a system, then exercise host Undo and Redo while observing native LINE/ARC and sidecar state.
Expected: record exact behavior; any native/semantic divergence blocks `LOCAL_PASS` and becomes a bounded follow-up defect.

### RR11 — save/cold-reopen + multi-DWG isolation
Save and cold reopen, verify each semantic id resolves its one source handle, then use same key in two open DWGs and replace only the active document.
Expected: cold replacement succeeds without duplicate ownership and the inactive DWG/project remains unchanged.

### RR12 — post-commit UI failure isolation
Using repository-supported local probe/debug means, make palette refresh/status fail after successful Build return.
Expected: committed radial native+semantic state remains valid; UI degradation is reported without converting success into rollback/failure.

## Acceptance recording
Record exact candidate SHA and per-case `PASS`/`FAIL`/`BLOCKED`. Only a licensed exact-SHA run may produce `LOCAL_PASS`. Hosted CI, source inspection or successful compilation remain `LOCAL_ONLY / NO_RESULT`.
