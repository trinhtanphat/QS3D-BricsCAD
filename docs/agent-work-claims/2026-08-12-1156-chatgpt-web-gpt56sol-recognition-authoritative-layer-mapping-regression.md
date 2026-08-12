# Work claim — authoritative recognition layer mapping regression

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-recognition-authoritative-layer-mapping-20260812-1156`
- Registered: `2026-08-12T11:56:00+07:00`
- Baseline main SHA: `1bfff2b62f61a6dc9bf66db7d133f1c62b1e73d2`
- Priority: P1 recognition contract regression

## Confirmed defect

Commit `81fbed6f2e3809ab06b69f0a69da4134b1951327` intentionally made exact project layer mappings authoritative over fallback recognition and added a regression where a project maps `A-BEAM` to `Door` even though the snapshot entity type is `Line`.

Current `ProjectRecognitionService.ExactLayerMapping(...)` now returns `null` when `RecognitionEngine.IsEntityTypeCompatible(category, snapshot.EntityType)` is false. `Suggest(...)` interprets `null` as "no project mapping" and runs the fallback engine, so an exact explicit project mapping can again be silently replaced by a different fallback category. This regresses the established authoritative-mapping contract.

## Reserved scope

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- one focused Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

An exact valid project layer mapping remains authoritative regardless of fallback entity-type heuristics. Return the mapped candidate and let the existing capture-readiness/review path decide whether it is safe to auto-capture. Do not fall back to a different category merely because the mapped category/entity type pair is not a default-compatible pair.

Preserve mapping ambiguity/canonical category validation, normalized layer matching, 0.99 mapped confidence, fallback behavior when no exact mapping exists, batch behavior and all native/UI flows.

## Validation boundary

Focused source-safe regression + exact readback only. No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed without execution.
