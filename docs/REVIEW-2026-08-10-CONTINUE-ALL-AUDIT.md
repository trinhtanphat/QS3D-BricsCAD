# QS3D BricsCAD V25 — continue-all deep audit

Date: 2026-08-10

## Scope

This review audits current `main` as an integrated product rather than one feature at a time. Priorities are correctness, transactional project integrity, generated-geometry ownership, deterministic regeneration, release readiness, discoverability, update/install safety and documentation parity.

This document records **source-level implementation and review**. It is not evidence that the latest `main` has compiled or run inside licensed BricsCAD V25. GitHub Actions remain owner-controlled/manual-only and were not dispatched by this review.

## Detailed review plan

### P0 — transactional data/destructive safety

1. Require one canonical generated-owner definition for classification, parsing and enumeration.
2. Reject generated output when semantic capture expects original CAD source.
3. Make semantic capture/finish synchronization rollback the complete `ProjectState` if regeneration or validation fails.
4. Require project mutation APIs to operate on actual project-owned element instances, not same-ID caller clones.
5. Make every destructive generated rebuild/erase fail closed on foreign or ambiguous ownership.
6. Prevent whole-drawing B4D recognition from re-ingesting any QS3D-generated owner handle as source.

### P1 — deterministic model and health lifecycle

1. Verify Family/Instance/source edits propagate dirty/stale state correctly.
2. Verify Door/Opening property and host-relation changes invalidate only dependent Curtain frame overlays when appropriate.
3. Verify every current generated rebar family is covered by dedicated health, Rebar Health All, Health All and Release Readiness.
4. Detect dependency cycles explicitly so regeneration cannot silently stall behind a cyclic graph.
5. Make feature/generic preflights assert architecture contracts instead of stale hard-coded implementation lists.

### P1 — schedules, exports and traceability

1. Keep modeless Schedule/Project tools bound to the DWG that opened them.
2. Keep BQ, Room Finish, Material, Curtain, Door/Opening and rebar schedule/export entry points discoverable.
3. Keep BOM/release checks bound to canonical generated-owner/live-solid data.
4. Permit only reviewed repository-owned synthetic CAD fixtures while continuing to reject private/reference DWG/DXF/DOCX artifacts.

### P1 — packaging, install/update and manual release

1. Package the V25 x64 Release output and derive command manifest directly from source `[CommandMethod]` declarations.
2. Exclude BricsCAD-owned runtime assemblies and include hashes/install/update helpers.
3. Make per-user DemandLoad installation transactional: a failed install must restore the prior install/registry state or remove a partially created one.
4. Bind updater version decisions to the signed manifest rather than an unsigned mutable sidecar/version argument; reject substitution/replay mismatches.
5. Keep all GitHub Actions `workflow_dispatch` only, with a manual-event guard on every executable job.
6. Require explicit `confirm_release=RELEASE` for publication.

### P2 — BLT-style workflow completeness

1. Surface implemented workflows through Workspace/Domain/Project/Schedule/Rebar/Curtain hubs.
2. Preserve fail-closed behavior when native V25 semantics have not been proven.
3. Do not implement multi-owner wall union, curved Curtain frames or fabrication detailing by guessing geometry/ownership rules.

## Findings fixed in this audit line

### Transactional semantic capture

Semantic capture previously had multiple mutation/regeneration steps where a failure could leave partially changed project state. The current source now snapshots/restores the complete project state around semantic capture, room-finish generation and room-finish synchronization.

The rollback snapshot restores Zone/Floor/Family/Element catalogs, quantity rules, audit events, metadata, active project selections, dirty/persistence state and project timestamp.

Generated QS3D owner handles are rejected before semantic source mutation through the canonical generated-owner policy.

### Generic wall starter-Family parity

Generic **Bóc chọn** capture can create a starter Family without going through the specialized GlassWall/WallPier commands. Those auto-created Families were missing some specialized defaults.

Fixed parity includes:
- wall axis left/right offsets;
- GlassWall `CurtainFrameDepthM`;
- WallPier `WallPierProfileMode=Rectangular`;
- WallPier `WallPierChamferM=0.02`.

A dedicated preflight now guards transactional capture + starter-Family parity.

### Canonical generated ownership

Generated ownership is now centralized in Core `GeneratedHandleOwnershipPolicy`:

- owner-slot classification;
- rebar-owner classification;
- handle splitting/parsing;
- per-element/project enumeration;
- deduplicated project-wide owner-handle collection;
- owner lookup.

Adapter code delegates to this Core policy. Semantic selection, B4D exclusion, release/BOM checks and generated destructive/health paths consume the same ownership contract instead of maintaining independent string lists.

### Foundation mesh selection ownership

Foundation mesh generated handles were initially missing from semantic selection resolution. Selecting generated Foundation mesh could fail to resolve the owning semantic Foundation.

This was fixed and later generalized further by dynamic canonical owner enumeration.

### B4D generated-source feedback loop

`QS3DB4D` originally used a hard-coded generated-handle list. Newer generated families could therefore be recognized again as source CAD.

B4D now uses canonical `CollectOwnerHandles(project)`, so classification, parsing and dedupe share the same Core policy used elsewhere. Generic and focused preflights were updated to prevent regression.

### Project mutation integrity

Zone/Family assignment and object-based Bulk Edit APIs reject foreign same-ID `ProjectElement` objects instead of mutating a caller clone and reporting false success. Floor/Zone/Family/Bulk Edit follow the same project-owned-instance invariant.

### Synthetic fixture/private-file gate conflict

The repo intentionally contains repository-owned synthetic sample CAD fixtures, but generic preflight previously rejected all DWG/DXF files.

The gate now allows only:
- `samples/generated/QS3D-Sample.dwg`;
- `samples/generated/QS3D-Sample.dxf`;

and requires sample provenance documentation. Every other committed DWG/DXF and all DOCX/private-reference artifacts remain fail-closed.

### Foundation-aware unified health/release readiness

Foundation mesh is integrated into semantic selection, stale state, invalidation, dedicated health, Rebar Health All, Full Health and Release Readiness. Release Readiness also includes generated-rebar mode semantics.

### Policy-driven dedicated health and destructive ownership

Foundation mesh exposed the risk of manual generated-slot lists. Rebar/Tie/Curtain destructive guards and the main dedicated rebar/mesh/Curtain ownership-health paths were refactored toward the shared ownership policy. Preflights were updated so architecture improvements do not create false failures from obsolete literal-token expectations.

### Curtain opening relation lifecycle

Door/Opening link, re-host and unlink operations can alter frame interruption without changing the GlassWall backing host itself.

Current behavior:
- old/new linked GlassWall frame overlays are marked stale as required;
- backing wall solid is not stale-marked unnecessarily;
- relevant opening property regeneration marks linked Curtain frames stale;
- rebuild/invalidation clears opening-aware frame metadata;
- supported LINE frames are interrupted deterministically around linked openings.

### Dependency-cycle health

Model Health now reports dependency cycles as explicit errors and Release Readiness consumes dependency health. Cyclic project graphs therefore block readiness rather than leaving regeneration as an opaque stall.

### Schedule Hub discoverability

`QS3DSCHEDULES` is exposed from Project Tools and Full Domain Hub. Its preflight guards command uniqueness, document affinity and access to BQ, Room Finish, Material, Curtain, Door/Opening and rebar schedules/exports.

### Manual schedule gate policy

`schedule-gate.yml` remains `workflow_dispatch` only, has the per-job manual-event guard and runs the strict manual-CI policy preflight when the owner explicitly dispatches it.

### Transactional installer rollback

The per-user V25 autoload installer now treats install/replace as a transaction:
- existing install state is backed up before replacement;
- registry state is preserved;
- failures restore the previous files/registry values;
- a failed first install removes partial new state.

This reduces the chance that a failed update leaves BricsCAD autoload broken.

### Signed-manifest updater version binding

Updater version selection is bound to the cryptographically verified manifest instead of trusting an unsigned separate version source. Expected-version mismatch/substitution/replay is rejected before install.

Production signing certificates/operational signing remain release work; the source verification/rollback contract is implemented.

## Architecture decisions deliberately preserved

### No automatic L/T/X wall-solid union yet

Each semantic wall owns its own generated host solid. Blind union would destroy one owner or create ambiguous shared ownership with opening/health workflows. The safe path remains:

1. analyze L/T/X/Multi junctions;
2. Preview endpoint cleanup;
3. fingerprint the preview;
4. Apply only supported source-centerline changes;
5. ownership-aware invalidate dependent outputs;
6. rebuild later.

Physical multi-owner wall-solid reconciliation remains product work until an explicit ownership/replacement contract is designed and V25-tested.

### No guessed curved Curtain-frame adapter

Supported LINE Curtain frames already perform opening-aware interruption. Remaining product work is curved/open-POLYLINE native frame generation and panel-by-panel backing glass, not LINE opening interruption.

### No inferred fabrication-grade rebar detailing

Current deterministic rebar geometry does not invent code-specific hooks, bend radii or anchorage. Those require explicit dimensions/rules before fabrication-grade output can be claimed.

## Current source-level release checklist

Before production release, the exact final SHA still needs:

1. aggregate source preflight;
2. Core Release build and deterministic smoke suite;
3. adapter Release/x64 compile against exact target BricsCAD V25 assemblies;
4. NETLOAD/DemandLoad in a licensed interactive V25 session;
5. representative private-DWG save/reopen/multi-DWG regression;
6. Room Auto mixed-curve topology regression;
7. wall snap/Auto Host/straight+curved opening-cut regression;
8. Curtain backing host + opening-aware LINE frame regression and future curved-frame product work;
9. column/beam/shape/tie/stirrup/slab/wall/Foundation rebar regression;
10. Schedule Hub/export/traceability regression;
11. Dependency/Health All/Release Readiness on representative project data;
12. installer/update rollback/signature/version-binding qualification using a signed release package;
13. Unicode/HiDPI and large-model performance regression.

## CI/CD rule

GitHub Actions are deliberately idle by default. `continue all`, review, source fixes, docs updates, commit, push or merge do **not** authorize workflow dispatch. Only an explicit owner request to run CI/build/runtime/release authorizes a manual workflow run.

When a build/release is explicitly requested, use the owner-approved manual V25 release workflow for the chosen commit/tag. Do not describe the latest source as V25 runtime-verified until that exact source has passed the licensed runtime gate.
