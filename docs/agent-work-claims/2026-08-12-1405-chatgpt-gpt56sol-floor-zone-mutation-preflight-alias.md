# Work claim — Floor/Zone mutation preflight canonical-repair aliases

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T14:05:00+07:00`
- Baseline main SHA: `6ca953768c76f8d916e8c87c982c06c7dc298245`
- Priority: `P0 source/static gate regression — mutation-integrity preflight still requires obsolete active-alias no-op smoke names after canonical repair semantics landed`

## Reserved scope

Reconcile `scripts/preflight-project-floor-zone-mutation-integrity.py` with the current registered Floor/Zone smoke contract. Active Floor/Zone aliases are canonical repairs that advance the project revision once, while already-canonical activation and canonical-equivalent element assignment remain no-ops.

## Expected surfaces

- `scripts/preflight-project-floor-zone-mutation-integrity.py`
- Read-only verification of `tests/QS3D.Core.SmokeTests/ProjectFloorZoneMutationIntegritySmoke.cs`
- Read-only verification of `src/QS3D.Core/Domain/ProjectFloorService.cs`
- Read-only verification of `src/QS3D.Core/Domain/ProjectZoneService.cs`

## Excluded scope

- Floor/Zone production mutation semantics or audit/UI wrappers.
- Property schema-version work or any other current active claim.
- BricsCAD runtime qualification, Actions dispatch, packaging/release.

## Evidence

PR #821 explicitly recorded `scripts/preflight-project-floor-zone-mutation-integrity.py` as a known stale gate because it hard-coded `FloorActiveCanonicalIdentityIsNoOp` / `ZoneActiveCanonicalIdentityIsNoOp`. Current smoke instead runs `FloorActiveAliasIsCanonicalRepair` / `ZoneActiveAliasIsCanonicalRepair`, verifies alias repair advances `ChangeVersion` once and stores the canonical ID, then verifies a subsequent canonical activation is a no-op.

## Validation

- Implementation commit: `50d444ad4f6eabc2ba0ed8f100442969c00915ed` (`test(preflight): track Floor Zone alias repair semantics`).
- Only `scripts/preflight-project-floor-zone-mutation-integrity.py` changed.
- The gate now requires `FloorActiveAliasIsCanonicalRepair` and `ZoneActiveAliasIsCanonicalRepair`, plus evidence that alias repair advances `ChangeVersion` once and writes canonical active IDs.
- Existing assignment canonical no-op, null-target atomicity, module registration, and source-ordering guards are preserved.
- Production Floor/Zone services and smoke implementation were read-only.
- No GitHub Actions were dispatched. No executable preflight/build or licensed BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied by pushed implementation `50d444ad4f6eabc2ba0ed8f100442969c00915ed` and this completion record on `main`.
