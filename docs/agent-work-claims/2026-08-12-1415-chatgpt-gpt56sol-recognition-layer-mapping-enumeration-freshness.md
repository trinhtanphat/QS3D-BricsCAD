# Work claim — Recognition layer-mapping enumeration freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-recognition-layer-mapping-enumeration-freshness-20260812-1415`
- Registered: `2026-08-12T14:15:00+07:00`
- Completed: `2026-08-12T14:26:00+07:00`
- Baseline main SHA: `e587e389e508639762acc7238c9bdedc9fc80ade`
- Claim commit: `a292af944ed6ed4dbfa849cf90d50562212139e9`
- Source + regression integration: `57da65a1e0b23e9e520ba39488621fb5a0447e5f` (PR #934)
- Superseded PR: #932 closed without merge
- Priority: P2 deterministic Core freshness / fail-closed recognition

## Confirmed defect

`ProjectRecognitionService.SuggestBatch(...)` materialized caller-controlled snapshot enumeration while checking only `ProjectState.ChangeVersion` before/after. `ProjectState.Metadata` is a public mutable dictionary and direct layer-mapping metadata changes do not increment `ChangeVersion`.

A snapshot iterator could therefore mutate `TemplateProfileStore.LayerMappingPrefix` metadata during enumeration without tripping the existing version guard. The batch would then score all materialized snapshots against the mutated mapping state rather than failing closed on a project context that changed while the caller-controlled enumeration was running.

## Implemented fix

`SuggestBatch(...)` now captures the authoritative project layer-mapping key/value state before bounded snapshot materialization and captures it again immediately after enumeration. Any mapping add/remove/key/value change fails closed before scoring. The existing `ChangeVersion` guard remains in place.

The guard is intentionally narrow: unrelated metadata that does not participate in recognition layer mapping does not cause rejection.

## Regression evidence

`tests/QS3D.Core.SmokeTests/RecognitionLayerMappingEnumerationFreshnessSmoke.cs` is a ModuleInitializer smoke covering:

- layer-mapping value mutation during snapshot enumeration -> rejected;
- layer-mapping addition during enumeration -> rejected;
- layer-mapping removal during enumeration -> rejected;
- direct mapping mutation demonstrably leaves `ChangeVersion` unchanged, proving the old guard alone was insufficient;
- unchanged mappings preserve authoritative project-layer recognition;
- unrelated metadata mutation remains outside this narrow freshness guard.

Current `main` source and smoke were re-read after integration and contain the expected guard/regression.

## Preserved behavior

- bounded recognition snapshot enumeration limit;
- existing `ProjectState.ChangeVersion` freshness check;
- authoritative exact project-layer mapping semantics;
- fallback recognition and batch thresholds;
- template persistence canonicality;
- no category-token normalization changes.

## Excluded / not claimed

- no BricsCAD/native/UI changes;
- no GitHub Actions/full build run;
- no licensed BricsCAD V25/V26 runtime PASS.

## Completion condition

Completed: source and focused smoke are integrated to `main`, readback verified, superseded PR #932 is closed, and this claim is `COMPLETED`.