# Work claim — LOCAL-014 Plan-to-3D P01 quick-path probe

- Status: `COMPLETED`
- Agent: `codex-local-root` (`/root`, local Windows + licensed BricsCAD V25)
- Registered: `2026-08-12T09:55:29+07:00`
- Completed: `2026-08-12T10:21:04+07:00`
- Baseline main SHA: `26e769a351e1d6f1ac6ec5f12074537f5a6d0240`
- Priority: `LOCAL-014 / P1` — establish the first exact-SHA, reproducible native proof for the BLT-style 2D plan -> semantic wall -> owned Solid3d quick workflow.

## Reserved scope

Implement and execute only a bounded **P01 positive quick-path** qualification around the real production `QS3DCONVERT2D` flow. The automation-only probe may seed simple supported Model Space LINE sources in a disposable repository-generated drawing, select them through PICKFIRST, invoke the production command path, and verify the resulting semantic/native ownership and quick fallback values. The runner must bind evidence to one clean exact Git SHA and built V25 plugin, keep runtime artifacts outside the repository, preserve the disposable DWG bytes by closing without save, and verify process/script cleanup.

This P01 result is infrastructure and one positive runtime slice only. It must not promote all of LOCAL-014.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/PlanTo3DRuntimeProbeCommands.cs` — new automation-only seed/invoke/verify command; no user-facing feature route.
- `scripts/test-bricscad-v25-plan-to-3d.ps1` — new exact-SHA disposable-copy runner.
- `scripts/preflight-plan-to-3d-runtime-probe.py` — new static/privacy/cleanup contract.
- `scripts/preflight-plan-to-3d-quick-authoring.py` — reconcile only the stale bare-`Modal` token with the current `Modal | UsePickSet` contract.
- `scripts/preflight-plan-to-3d-finish-workflow.py` — reconcile only the stale Ribbon `CollectionContainsId` token with current find-or-create/reconcile behavior.
- `docs/LOCAL-AGENT-INBOX.md` and `docs/PLAN-TO-3D-WORKFLOW.md` — record exact P01 scope/evidence while retaining full `PENDING_LOCAL` status.
- this claim file for close-out.

## P01 contract

- Start from a clean exact SHA, canonical V25 x64 Release plugin path, initialized nonblank profile, fresh ordinary `*.plan-to-3d-probe-copy.dwg`, and fresh empty artifact directory outside the repository.
- Seed only bounded simple supported LINE sources; no private/customer drawing or BLT binary/API.
- Invoke the actual `PlanTo3DCommands.Convert2D()` / `QS3DCONVERT2D` implementation after PICKFIRST setup; do not duplicate semantic capture, regeneration or native wall formulas in the probe.
- Verify expected source count, one semantic `ArchitecturalWall` owner per source, fallback `ThicknessM=0.2`, `HeightM=3.0`, `BottomOffsetM=0`, one live owned generated solid per wall, source geometry retained, distinct canonical ownership, and clean focused model health.
- Emit nonce-bound sanitized booleans/counts/hashes/version fields only: no raw path, Handle, semantic ID/name, layer/text or project/drawing identity.
- Require unchanged disposable-DWG SHA-256, no adjacent `.qsdb`/`.bak`, deleted generated `.scr`, and verified launched-process exit.

## Excluded scope

- No full LOCAL-014 PASS; `QS3DPLAN2WALLS` alias parity, `QS3DCONVERT2DADV` three-prompt cancellation, mid-prompt DWG/UCS/unit/project/source drift, unrelated dirty-state proof, deterministic mid-batch native failure injection, Undo/Redo and save/reopen remain `PENDING_LOCAL` unless separately implemented and executed.
- No edit to production Plan-to-3D orchestration, geometry fingerprint, semantic capture, builders, ownership, regeneration, Ribbon source, Core quantity/reporting or project persistence unless the P01 run exposes a concrete bug and this published claim is expanded first.
- No private sample DWG/XLSX access, no BLT binary/source/API use, no GitHub Actions, packaging, release, installer or signing work.
- No V26 runtime claim; this lane is V25-only.

## Validation plan

- Re-run all focused Plan-to-3D source gates, the new probe gate, `scripts/preflight.py`, V25 x64 Release build, and `git diff --check` on the exact candidate.
- Run the new runner against an ordinary temporary copy of `samples/generated/QS3D-Sample.dwg` with BricsCAD V25.2.10 x64 and profile `QS3D-V25-TEST`.
- Record sanitized exact-SHA/plugin/DWG hash/count/ownership/cleanup evidence; retain private runtime files only outside the repository and do not commit them.
- Re-fetch and reconcile current `origin/main` before every implementation/close-out commit; no force push and no Actions dispatch.

## Coordination

Current `ACTIVE` / `BLOCKED` claims were inspected. None reserves Plan-to-3D commands, the planned new probe/runner/gate, or LOCAL-014 P01. The active LOCAL-003 claim belongs to the same local agent but owns Level Z-chain surfaces and explicitly excludes this Plan-to-3D runtime lane. The active Build3D claim owns only `Build3DCommands.cs`, which is excluded here. Start Center/Create Similar/Workspace claims own disjoint UI surfaces.

## Completion condition

The reusable P01 runner/probe/gates and truthful docs are integrated on current `main`; one exact clean V25 SHA produces sanitized positive quick-path evidence with verified cleanup; LOCAL-014 remains OPEN/PENDING for the unexecuted matrix; and this claim is marked `COMPLETED` with exact PR/merge/evidence references.

## 2026-08-12 native wall Z-placement expansion

The merged runner/probe executed twice on clean current-main descendants in BricsCAD V25.2.10 x64. Both runs passed source retention, semantic provenance, fallback values, generated metadata/native ownership, length and thickness checks, then failed fail-closed on the native minimum-Z assertion. The refined sanitized marker reports `PLAN_TO_3D_RUNTIME_NATIVE_MIN_Z_FAILED`; BricsCAD exited, no sidecar was created, and the disposable copy was not saved.

Static comparison confirms the same already-proven `Solid3d.CreateBox` placement defect previously fixed in `StructuralSolidBuilder`: `WallSolidBuilder` still applies `Displacement(-length/2, -thickness/2, -height/2)` before rotating and moving a box to a midpoint that already includes `height/2`. BricsCAD's native box is centered by creation, so the extra negative half-height shifts a requested fallback wall below its resolved bottom.

Expand this claim narrowly to reserve:

- `src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs` — remove only the redundant negative half-dimension displacement so LINE wall native bounds match source midpoint/thickness/resolved bottom and height; preserve transactions, ownership and Level placement.
- `scripts/preflight-plan-to-3d-runtime-probe.py` — require the corrected builder placement and continue forbidding the redundant displacement.
- `src/QS3D.BricsCAD.V25/PlanTo3DRuntimeProbeCommands.cs` — retain the sanitized native-bound sub-phase diagnostics and exact bounds regression.
- `docs/LOCAL-AGENT-INBOX.md` / this claim — record only exact clean-SHA P01 evidence after strict build and runtime pass; full LOCAL-014 remains OPEN/PENDING.

No other wall/path builder, Plan-to-3D orchestration, Core, persistence, quantity, Ribbon, private fixture or workflow is added. Before the product edit, publish this expansion on `main`; then run strict V25 build, focused gates and the exact-clean-SHA P01 probe again. If the strict build remains blocked by unrelated current-main compile debt, runtime output is diagnostic only and must not be promoted to P01 PASS.

## Completion evidence

- Claim-only reservation merged through PR `#728`; native Z-placement expansion merged through claim-only PR `#743` before the product edit.
- Probe/runner/gates merged through PRs `#737`, `#738` and `#742`; the centered native LINE-wall placement correction merged through PR `#744`; the separately reserved Core null-flow compile reconciliation merged through PR `#748`.
- Exact clean source SHA: `21ca2d08427013f3ef8154708fef85fb2454ff8f`.
- Strict BricsCAD V25 `Release|x64` build: PASS, zero warnings/errors.
- Runtime: BricsCAD V25.2.10 x64, profile `QS3DReleaseAutomation-20260810`, plugin SHA-256 `3CF3F79C22A8659743E4CC72F777CD30596ED794775714C6F0A342C477E2200C`.
- Result: two bounded LINE sources -> two semantic `ArchitecturalWall` elements -> two live owned Solid3d outputs; fallback `ThicknessM=0.2`, `HeightM=3`, `BottomOffsetM=0`; source retained, ownership disjoint, native bounds verified, Core/runtime blocking Health errors `0/0`.
- Disposable synthetic copy SHA-256 remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; no `.qsdb`/backup/lock, `.scr` or launched BricsCAD process remained.
- Focused Plan-to-3D runtime/quick/finish/lifecycle/source-geometry/scoped-regeneration gates plus `scripts/preflight.py` passed on the exact candidate. The broader aggregate/full smoke state remains independent moving-main work and is not claimed by this P01.
- Boundary remains explicit in the marker: `qualification_boundary=P01_QUICK_POSITIVE_ONLY` and `production_local014_qualified=false`. LOCAL-014 stays OPEN/PENDING for every excluded matrix item listed above.
