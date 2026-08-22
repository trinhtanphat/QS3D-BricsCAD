# Work claim — authoritative recognition layer mapping regression

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-recognition-authoritative-layer-mapping-20260812-1156`
- Registered: `2026-08-12T11:56:00+07:00`
- Completed: `2026-08-12T11:59:00+07:00`
- Baseline main SHA: `1bfff2b62f61a6dc9bf66db7d133f1c62b1e73d2`
- Priority: P1 recognition contract regression

## Confirmed defect

Commit `81fbed6f2e3809ab06b69f0a69da4134b1951327` intentionally made exact project layer mappings authoritative over fallback recognition and added a regression where a project maps `A-BEAM` to `Door` even though the snapshot entity type is `Line`.

`ProjectRecognitionService.ExactLayerMapping(...)` had later started returning `null` when `RecognitionEngine.IsEntityTypeCompatible(category, snapshot.EntityType)` was false. `Suggest(...)` interpreted `null` as "no project mapping" and ran the fallback engine, so an exact explicit project mapping could again be silently replaced by a different fallback category.

## Implemented contract

An exact valid project layer mapping remains authoritative regardless of fallback entity-type heuristics. `ExactLayerMapping(...)` now returns the explicit mapped candidate once the normalized layer and category token validate. Fallback recognition still runs when no exact mapping exists.

Mapping ambiguity/canonical category validation, normalized layer matching, mapped confidence, batch behavior and native/UI flows are otherwise unchanged.

## Integration evidence

- Claim: `ee43c14a3accf7b93131128443576651ce41cac1`
- Source fix: `378f4bc9910975bb6b1a8aaee4e38cfdf4790f7f`
- Focused regression: `8e7b5e3aa00e722b95902f9a08bc6e3d9e4ac043`
- Readback: current `main` showed `ProjectRecognitionService.cs` without the compatibility-to-null short circuit and the new `ProjectRecognitionAuthoritativeLayerMappingSmoke.cs` present.

## Validation boundary

Focused source-safe regression + exact readback only. No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed without execution.
