# Work claim — Release #37 B4D exact-layer entity-type compatibility

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release37-b4d-entity-type-20260812-1521`
- Registered: `2026-08-12T15:21:00+07:00`
- Baseline main SHA: `fe74b1318a1ee228f471d25de314001e919238a7`
- Priority: P1 release preflight / real Core recognition defect

## Confirmed defect

`ProjectRecognitionService.ExactLayerMapping(...)` returned an authoritative 0.99-confidence project-layer candidate after category parsing without applying the shared `RecognitionEngine.IsEntityTypeCompatible(category, snapshot.EntityType)` boundary.

This bypassed the fallback engine's entity-type safety. The existing `LogicRegressionSmoke.RecognitionRejectsEntityTypeMismatch()` counterexample maps layer `A-WALL` to `ArchitecturalWall` and supplies a `DBText`; the fallback correctly rejects it, while the project exact-layer path previously could still return a Wall candidate. Release run #37 therefore failed `preflight-b4d-recognition-mass.py` on the missing compatibility guard.

## Reserved scope

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- this claim file

## Integrated fix

- Claim: `24dab23c941daddea6591ba92a539dd2eee6e7df`
- Source fix: `7d00030f8c7cbbfc74f36687d8767284a45700eb`

`ExactLayerMapping(...)` now applies `RecognitionEngine.IsEntityTypeCompatible(category, snapshot.EntityType)` immediately after parsing the exact mapped category. Incompatible mappings return no exact candidate, so `Suggest(...)` falls through to the existing recognition engine. Compatible exact mappings retain their authoritative 0.99 candidate behavior.

## Regression / readback evidence

Existing source regression remains unchanged and already covers both sides:
- `LogicRegressionSmoke.RecognitionRejectsEntityTypeMismatch()` — DBText on mapped Wall layer must remain unrecognized; compatible LINE on the same mapped layer remains accepted as `ArchitecturalWall`.

Existing release gate remains unchanged:
- `scripts/preflight-b4d-recognition-mass.py` requires `RecognitionEngine.IsEntityTypeCompatible(category, snapshot.EntityType)` in `ProjectRecognitionService.cs`.

Post-write source readback confirmed the compatibility guard in current `ProjectRecognitionService.cs` while retaining mapping validation and batch mapping-enumeration freshness.

## Limitations

- GitHub Actions were not rerun or dispatched.
- No aggregate preflight/build/package/release PASS is claimed from this remote write.
- No licensed BricsCAD V25/V26 runtime qualification is claimed.
