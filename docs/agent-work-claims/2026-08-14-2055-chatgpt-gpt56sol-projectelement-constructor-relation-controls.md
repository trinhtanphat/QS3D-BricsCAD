# Work claim — ProjectElement constructor relation control-character parity

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-projectelement-constructor-relations`
- Registered: `2026-08-14T20:55:00+07:00`
- Baseline main SHA: `bd34bc749bf1214e240de1c3b2a5ee42b52291fb`
- Priority: Core P1 invariant defect found during owner-requested whole-repository review
- Task Key: `CORE-PROJECTELEMENT-CONSTRUCTOR-RELATION-CONTROLS`
- Implementation branch: `agent/chatgpt-gpt56sol/projectelement-constructor-relation-controls`
- Integration batch: `integration/20260814-projectelement-constructor-relation-controls`

## Confirmed defect

`ProjectElement.FamilyId`, `FloorId`, and `ZoneId` setters normalize through `NormalizeOptionalRelationId(...)`, which trims surrounding whitespace and rejects control characters. The five-argument `ProjectElement` constructor currently bypasses that invariant and assigns the three relation ids with direct `Trim()` calls. As a result, a caller can construct an element containing an internal control character that the equivalent public setter rejects. The existing `ProjectElementRelationPersistabilitySmoke` covers constructor padding normalization and setter control-character rejection, but does not exercise constructor control characters.

## Reserved scope

Make constructor relation-id validation use the same canonical helper as the public setters and add focused regression coverage proving all three constructor relation arguments reject control characters while preserving padded/null canonical behavior.

## Owned surfaces

- `src/QS3D.Core/Domain/ProjectElement.cs`
- `tests/QS3D.Core.SmokeTests/ProjectElementRelationPersistabilitySmoke.cs`
- this claim file for implementation/integration close-out

## Explicit exclusions / concurrency protection

- No persistence-store or XML-validator changes; the ACTIVE QSDB negative-quantity claim owns those persistence surfaces.
- No release/workflow changes; the ACTIVE preview release-sequence claim owns those surfaces.
- No Slab/Open, Source Reconcile/Undo, drawing-fingerprint, reporting, interchange, BricsCAD adapter/runtime, signing, licensing, or LOCAL_ONLY work.
- Do not change relation dirty-tracking semantics or add a second relation normalization model.
- No manual GitHub Actions dispatch/rerun and no force-push.

## Validation plan

- Constructor padding and null behavior remain canonical and unchanged.
- `familyId`, `floorId`, and `zoneId` constructor arguments containing control characters each fail with `ArgumentException`.
- Existing setter control-character atomicity remains unchanged.
- Review the final branch diff against refreshed `main`, reconcile only non-overlapping claim/docs deltas into the declared integration branch, then perform one final integration landing.
- Report smoke/build/CI only if actually executed; no licensed BricsCAD runtime evidence is claimed from this remote-safe change.

## Completion condition

The constructor and setters share the same relation-id canonicalization boundary, focused regression source is present, the implementation is integrated through the declared integration branch, the final main landing is read back by exact SHA, and this claim is then marked `COMPLETED` with exact implementation/integration/main evidence.
