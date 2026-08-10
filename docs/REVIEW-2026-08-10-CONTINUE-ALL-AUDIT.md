# QS3D BricsCAD V25 — continue-all deep audit

Date: 2026-08-10

## Scope

This review audits the current `main` source as an integrated product rather than reviewing one feature in isolation. The priorities are correctness, project/data integrity, generated-geometry ownership, deterministic regeneration, release readiness, discoverability and documentation parity.

This document records source-level work only. It is **not** evidence that the latest `main` has compiled or run inside licensed BricsCAD V25. GitHub Actions remain owner-controlled/manual-only and were not dispatched by this review.

## Review plan

### P0 — destructive safety and project integrity

1. Verify that every generated geometry family has one unambiguous owner slot.
2. Verify destructive rebuild/erase code refuses handles claimed by source geometry or another generated family.
3. Verify newly added generated families participate in semantic selection, stale state, invalidation and health.
4. Verify project mutation APIs operate on project-owned instances rather than caller-supplied same-ID clones.
5. Verify whole-drawing recognition cannot re-ingest QS3D-generated geometry as fresh source CAD.

### P1 — deterministic model and health lifecycle

1. Verify dirty/stale propagation from Family/Instance edits and source replacement.
2. Verify opening link/re-host/unlink changes invalidate only the dependent curtain-frame overlay when appropriate.
3. Verify dedicated health, Full Health and Release Readiness cover the same generated families.
4. Verify feature preflights track architecture contracts rather than stale implementation literals.

### P1 — schedules, exports and traceability

1. Verify document-bound Schedule Hub and project editors cannot silently target another active DWG.
2. Verify Door/Opening, Room Finish, Material, Curtain, BQ and BBS schedule/export entry points are discoverable.
3. Verify BOM/release guard uses generated owner slots and live-solid checks.
4. Verify generated sample fixtures are explicitly synthetic and do not weaken the private-file guard.

### P1 — packaging and manual release

1. Verify packaging uses the V25 x64 Release output.
2. Verify command manifest is generated from source rather than maintained by hand.
3. Verify package excludes BricsCAD-owned assemblies and includes hashes/install/update helpers.
4. Verify all GitHub Actions workflows remain `workflow_dispatch` only with per-job manual-event guards.
5. Verify release publication requires explicit `confirm_release=RELEASE`.

### P2 — BLT-style workflow completeness

1. Verify main Workspace/Domain/Project/Schedule/Rebar/Curtain hubs expose implemented workflows.
2. Preserve fail-closed behavior where native V25 semantics are not proven.
3. Do not implement wall-solid union, curved curtain frames or fabrication detailing by guessing ownership/geometry semantics.

## Findings fixed in this batch

### Foundation mesh selection ownership

`GeneratedFoundationMeshHandles` was missing from semantic handle resolution. Selecting generated Foundation mesh geometry could therefore fail to resolve the semantic Foundation owner.

Fix:
- add Foundation mesh to `SemanticHandleOwnershipResolver`;
- add deterministic smoke coverage;
- update semantic-selection and Foundation preflights.

### B4D generated-source feedback loop

`QS3DB4D` used a hard-coded list of generated-handle keys and did not cover newer slab/wall/foundation mesh or curtain-frame output. A full Current Space scan could therefore treat QS3D output as source CAD and create duplicate semantic data.

Fix:
- replace the hard-coded list with shared generated-owner policy discovery;
- add `preflight-b4d-generated-exclusion.py`;
- make the Foundation gate require this contract.

### Synthetic fixture versus private-file preflight conflict

The repository intentionally contains repository-owned synthetic sample DWG/DXF files, but the generic preflight still rejected every DWG/DXF.

Fix:
- whitelist only `samples/generated/QS3D-Sample.dwg` and `samples/generated/QS3D-Sample.dxf`;
- require the sample provenance README when these fixtures exist;
- continue rejecting every other committed DWG/DXF and all DOCX/private-reference artifacts.

### Foundation-aware Release Readiness

`QS3DRELEASECHECK` did not invoke dedicated Foundation mesh health and did not include generated-rebar mode semantics.

Fix:
- add `GeneratedFoundationMeshHealthService`;
- add `GeneratedRebarModeHealthService`;
- update release-readiness preflight.

### Curtain/Foundation ownership conflict coverage

Curtain frame dedicated health and destructive erase guard were manually listing generated ownership slots. Foundation mesh exposed that this design was easy to forget when a new generated family was added.

Fix:
- include Foundation in the immediate safety repair;
- refactor curtain destructive ownership to shared `GeneratedHandleOwnershipPolicy`;
- refactor curtain dedicated health to the same policy;
- refactor tie destructive ownership to the shared rebar owner-slot policy;
- update feature preflights to validate the policy-driven architecture instead of implementation literals.

### Curtain opening relation lifecycle

Opening link/re-host/unlink can change how curtain frames must be interrupted even when the GlassWall backing host geometry is otherwise unchanged.

Fix already integrated and guarded in the current review line:
- old/new GlassWall curtain-frame overlays are marked stale on relation changes;
- backing generated wall solid is not unnecessarily marked stale;
- opening property regeneration marks linked GlassWall frames stale;
- invalidation clears opening-aware frame metadata.

### Schedule Hub discoverability

The document-bound `QS3DSCHEDULES` hub existed but was not consistently reachable from major project/workflow entry points.

Fix:
- expose Schedule Hub from Project Tools;
- expose Schedule Hub from Full Domain Hub;
- update Schedule Hub preflight to guard discoverability and command uniqueness.

### Schedule workflow manual-CI policy

The focused schedule gate is useful, but it must obey the same manual-only repository policy.

Fix:
- keep `workflow_dispatch` only and the per-job `github.event_name == 'workflow_dispatch'` guard;
- add strict `preflight-ci-manual-only.py` to the schedule gate itself.

## Architecture decisions deliberately preserved

### No automatic L/T/X wall-solid union yet

Each semantic wall currently owns its own generated host solid. Blind union of two hosts would either destroy one owner handle or create shared-handle ambiguity with opening and health workflows. The safe implemented path remains:

- analyze L/T/X/Multi junctions;
- Preview endpoint cleanup;
- fingerprint the preview;
- Apply only supported straight source-centerline adjustments;
- invalidate dependent generated geometry with ownership checks;
- rebuild later.

Physical wall-solid reconciliation remains product work until an explicit multi-owner/replacement contract is designed and V25-tested.

### No guessed curved curtain-frame adapter

Curtain backing hosts can use more general wall paths, but native mullion/transom overlay generation remains restricted to the supported LINE path. Opening-aware LINE frames are deterministic and ownership-protected. Curved/open-POLYLINE frame overlay remains runtime/product work rather than being approximated silently.

### No inferred fabrication-grade rebar detailing

Current deterministic rebar geometry does not invent code-specific hook, bend-radius or anchorage dimensions. Those require explicit configured dimensions/rules before fabrication-grade output can be claimed.

## Current source-level release checklist

Before a production release, the exact final SHA still needs:

1. aggregate source preflight;
2. Core Release build and deterministic smoke suite;
3. adapter Release/x64 compile against the exact BricsCAD V25 assemblies;
4. NETLOAD/DemandLoad in a licensed interactive V25 session;
5. private-DWG regression for save/reopen and multiple DWGs;
6. Room Auto mixed-curve topology regression;
7. wall snap/Auto Host/straight and curved opening-cut regression;
8. curtain host + opening-aware frame overlay regression;
9. column/beam/shape/tie/stirrup/slab/wall/foundation rebar regression;
10. Schedule Hub/export and traceability regression;
11. Release Readiness on representative project data;
12. Unicode/HiDPI and large-model performance regression.

## CI/CD rule

GitHub Actions are deliberately idle by default. `continue all`, review, source fixes, docs updates, commit, push or merge do **not** authorize workflow dispatch. Only an explicit owner request to run CI/build/runtime/release authorizes a manual workflow run.

When a build/release is explicitly requested, use the owner-approved manual V25 release workflow for the chosen commit/tag. Do not describe the latest source as V25 runtime-verified until that exact source has passed the licensed runtime gate.
