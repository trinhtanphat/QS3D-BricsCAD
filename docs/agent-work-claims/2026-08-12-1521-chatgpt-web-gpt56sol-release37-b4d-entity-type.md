# Work claim — Release #37 B4D exact-layer entity-type compatibility

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release37-b4d-entity-type-20260812-1521`
- Registered: `2026-08-12T15:21:00+07:00`
- Baseline main SHA: `fe74b1318a1ee228f471d25de314001e919238a7`
- Priority: P1 release preflight / real Core recognition defect

## Confirmed defect

`ProjectRecognitionService.ExactLayerMapping(...)` currently returns an authoritative 0.99-confidence project-layer candidate after category parsing without applying the shared `RecognitionEngine.IsEntityTypeCompatible(category, snapshot.EntityType)` boundary.

This bypasses the fallback engine's entity-type safety. The existing `LogicRegressionSmoke.RecognitionRejectsEntityTypeMismatch()` counterexample maps layer `A-WALL` to `ArchitecturalWall` and supplies a `DBText`; the fallback correctly rejects it, but the project exact-layer path can still return a Wall candidate. Release run #37 therefore fails `preflight-b4d-recognition-mass.py` on the missing compatibility guard.

## Reserved scope

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- this claim file

Existing regression and preflight are already present and should remain unchanged unless readback proves a necessary registration/gate correction.

## Expected fix

After parsing an exact project layer category, apply the same `RecognitionEngine.IsEntityTypeCompatible(category, snapshot.EntityType)` guard used by normal recognition. An incompatible exact mapping must not manufacture a candidate; `Suggest(...)` should fall through to the existing fallback recognition path. Preserve exact-layer authority when entity type is compatible, mapping validation, batch mapping-enumeration freshness, thresholds, and all persistence semantics.

## Regression evidence

Existing source regression:
- `LogicRegressionSmoke.RecognitionRejectsEntityTypeMismatch()` — DBText on mapped Wall layer remains unrecognized; compatible LINE on the same mapped layer remains accepted as `ArchitecturalWall`.

Existing release gate:
- `scripts/preflight-b4d-recognition-mass.py` requires `RecognitionEngine.IsEntityTypeCompatible(category, snapshot.EntityType)` in `ProjectRecognitionService.cs`.

## Excluded scope

- no recognition-rule redesign;
- no template persistence changes;
- no BricsCAD/native/UI changes;
- no GitHub Actions rerun/dispatch;
- no licensed BricsCAD runtime qualification claim.

## Completion condition

Source fix is integrated on `main`, exact source/preflight/regression are re-read, the claim is marked `COMPLETED` with exact SHA evidence, and no unrelated concurrent work is overwritten.
