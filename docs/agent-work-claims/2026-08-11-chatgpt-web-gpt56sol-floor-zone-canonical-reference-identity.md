# Work claim — Floor/Zone canonical reference identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-zone-canonical-reference-identity`
- Registered: `2026-08-11T22:43:00+07:00`
- Baseline main SHA observed: `be1befd7ee8e4026bedf497c1bb13abfc26240df`
- Priority: P1 — `ProjectElement.FloorId` / `ZoneId` and project active Floor/Zone ids are mutable strings. Current Floor/Zone safety paths compare some of these values raw, so recoverable padded/case-varied references can disappear from update/reference/delete checks or bypass active-item deletion protection even though canonical project lookups trim ids.

## Reserved scope

- `ProjectFloorService`: canonicalize direct `ProjectElement.FloorId` matching in `ReferencesFloor()` and canonicalize `ActiveFloorId` before delete protection.
- `ProjectZoneService`: canonicalize `ProjectElement.ZoneId` matching in update/reference/delete paths through one local helper and canonicalize `ActiveZoneId` before delete protection.
- Add deterministic Core smoke coverage and focused static preflight for these exact identity boundaries.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFloorZoneCanonicalReferenceSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/ProjectFloorZoneCanonicalReferenceSmokeRegistration.cs` (new)
- `scripts/preflight-project-floor-zone-canonical-reference.py` (new)
- this claim file for close-out

## Excluded scope

- No Floor/Level/Zone WPF or Workspace UI, no `ProjectElement` setter rewrite, no persistence migration, no vertical-level property semantics beyond existing trimmed `Property()` behavior, no quantity/CAD/Ribbon/updater/release/GitHub Actions.
- No change to assignment behavior except safety/read matching; assignment may still canonicalize a padded current relation as an explicit mutation.

## Validation plan

- Floor smoke: padded/case-varied `FloorId` counts as a reference and blocks delete; padded active Floor id blocks delete; vertical Bottom/Top level behavior remains covered by existing helper semantics.
- Zone smoke: padded/case-varied `ZoneId` counts as a reference, participates in update dirtying, blocks delete; padded active Zone id blocks delete; unrelated items remain deletable.
- Focused preflight rejects raw active/reference comparisons in the reserved safety paths.
- Re-fetch current `main` before source write and preserve concurrent winners.

## Completion condition

- Floor/Zone service safety and read paths use canonical identity consistently for mutable relation/active ids, with deterministic regression coverage and a completed pushed claim.
