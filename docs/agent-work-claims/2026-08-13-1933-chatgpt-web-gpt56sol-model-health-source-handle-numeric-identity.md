# Work claim — Model Health numeric SourceHandle identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-model-health-source-handle-numeric-identity-20260813`
- Registered: `2026-08-13T19:33:00+07:00`
- Baseline main SHA: `eb1cdaa7e4d0629f966c96c772d27a3e3a17a6a5`
- Priority: P1 diagnostic identity parity. Canonical semantic ownership already uses `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity`, so CAD handle spellings such as `A`, `00a`, and `0xA` are one identity. `ModelHealthService` still compares SourceHandles and live handles only after trimming/case folding, so numeric aliases can evade both intra-element duplicate and cross-element ownership diagnostics and can create a false orphan when the live set uses another numeric alias.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthSourceHandleSmoke.cs`
- this claim file for closeout

## Intended change

Route both semantic SourceHandles and supplied live source-handle sets through the shared `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity` identity function before duplicate/ownership/orphan comparisons. Preserve malformed textual compatibility, existing trimming behavior, issue codes/severity, and the recently added one-warning-per-intra-element-duplicate contract.

## Excluded scope

- no semantic ownership resolver or generated-owner policy changes;
- no persistence/report/revision/locate changes;
- no new source-handle syntax rejection/canonical casing policy;
- no UI/BricsCAD native work, sibling Platform migration, GitHub Actions or native qualification.

## Validation plan

- refresh current `main` and recent source-handle claims after claim publication;
- extend focused smoke with: `A` + `00a` in one element -> one `DUPLICATE_SOURCE_HANDLE`; two elements `A` / `0xA` -> existing `DUPLICATE_HANDLE`; source `A` with live `00a` -> no `ORPHAN_HANDLE`; malformed textual handles retain case-insensitive trimmed compatibility;
- re-fetch exact source/test diffs and verify ancestry against moving `main` before closeout;
- report only validation actually executed; no managed/native PASS without tooling/runtime.

## Coordination

The semantic ownership numeric-identity feature is completed history and provides the shared identity contract; this is a diagnostics parity follow-up. The immediately preceding duplicate-SourceHandles ModelHealth lane is completed and no longer reserved. Recent exact commit search found no current numeric ModelHealth/source-handle claim.

## Completion condition

Model Health uses the same numeric CAD-handle identity as canonical semantic ownership for duplicate, cross-owner and liveness checks, malformed textual compatibility remains intact under focused regression source, pushed diffs/ancestry are verified, and this claim is marked `COMPLETED`.
