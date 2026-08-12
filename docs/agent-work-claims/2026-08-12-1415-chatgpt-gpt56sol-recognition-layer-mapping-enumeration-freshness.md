# Work claim — Recognition layer-mapping enumeration freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-recognition-layer-mapping-enumeration-freshness-20260812-1415`
- Registered: `2026-08-12T14:15:00+07:00`
- Baseline main SHA: `e587e389e508639762acc7238c9bdedc9fc80ade`
- Priority: P2 deterministic Core freshness / fail-closed recognition

## Confirmed defect

`ProjectRecognitionService.SuggestBatch(...)` materializes caller-controlled snapshot enumeration while checking only `ProjectState.ChangeVersion` before/after. `ProjectState.Metadata` is a public mutable dictionary and direct layer-mapping metadata changes do not increment `ChangeVersion`.

A snapshot iterator can therefore mutate `TemplateProfileStore.LayerMappingPrefix` metadata during enumeration without tripping the existing version guard. The batch then scores all materialized snapshots against the mutated mapping state rather than failing closed on a project context that changed while the caller-controlled enumeration was running.

## Reserved scope

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- one focused Core smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Expected fix

Capture the authoritative project layer-mapping state before enumerating snapshots, then verify that same state immediately after bounded materialization. Reject any add/remove/key/value mutation before scoring. Preserve the existing `ChangeVersion` guard, bounded enumeration limit, authoritative exact-layer semantics, fallback recognition, thresholds, and template persistence canonicality.

## Regression plan

- iterator changes a layer-mapping value without touching `ChangeVersion` -> fail closed;
- iterator adds/removes a layer mapping without touching `ChangeVersion` -> fail closed;
- unchanged mapping state keeps ordinary project-recognition batch behavior;
- unrelated metadata mutation remains outside this narrowly reserved freshness contract.

## Excluded scope

- no template persistence format changes;
- no category-token normalization changes;
- no BricsCAD/native/UI changes;
- no GitHub Actions or licensed runtime qualification.

## Completion condition

Source and focused smoke are integrated to `main`, current source/test are re-read, and this claim is marked `COMPLETED` with exact integration SHA(s).