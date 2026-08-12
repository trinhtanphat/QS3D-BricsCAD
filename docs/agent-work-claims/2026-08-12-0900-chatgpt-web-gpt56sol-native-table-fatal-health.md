# Work claim — project-owned native Table fatal runtime-health propagation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-native-table-fatal-health-20260812-0900`
- Registered: `2026-08-12T09:00:00+07:00`
- Baseline main SHA: `bd5a2bd242ddc924fd68c84867492e96d0e96ccd`
- Priority: owner-requested continue-all shared native runtime-health integrity hardening

## Confirmed defect

`ProjectOwnedNativeTableArtifactService.Inspect(...)` catches all `Exception` values while validating persisted metadata, building/validating the authoritative snapshot, and reading live Table cells. Its shared `TryResolve(...)` helper also uses `catch { return false; }`. These broad recovery paths can absorb fatal runtime exceptions and downgrade them to ordinary `DOCUMENTATION_TABLE_*` diagnostics or a missing handle before the outer native runtime-health isolation boundary can enforce its fatal-exception policy. Door/Opening, Room Finish, Material Usage, BQ and BBS native Table health all delegate to this shared service.

## Reserved scope

- Keep ordinary recoverable metadata/render/cell/handle failures fail-visible through existing documentation-table diagnostics.
- Do not swallow `OutOfMemoryException`, `StackOverflowException`, or `AccessViolationException` in the shared diagnostic recovery paths.
- Apply one shared recoverable-exception predicate to metadata validation, snapshot rendering, cell reads and handle resolution.
- Preserve current diagnostic codes, ownership checks, `OpenMode.ForRead`, row/cell detail limits and mutation lifecycle semantics.
- Add one focused static regression preflight for the shared Table family.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs`
- `scripts/preflight-native-table-runtime-health-fatal.py`
- this claim file

## Excluded scope

- No changes to Table content calculations or individual Door/Room/Material/BQ/BBS builders.
- No changes to Table build/remove ownership semantics beyond allowing fatal exceptions to propagate through the already-shared resolver.
- No changes to unrelated active claims.
- No GitHub Actions, release publication, force push, or licensed BricsCAD V25/V26 runtime PASS claim.

## Validation plan

- Re-fetch current shared source after claim registration before editing.
- Add `IsRecoverableDiagnosticFailure(Exception)` matching the native aggregator fatal exclusions.
- Filter the three health catches and the shared `TryResolve(...)` catch so recoverable behavior stays unchanged while fatal exceptions propagate.
- Add a focused source preflight requiring all filtered catches, all fatal exclusions, existing health diagnostics and `OpenMode.ForRead`.
- Re-fetch final source/preflight from current `main`, verify ancestry, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer lets the shared project-owned native Table health path swallow native fatal exception classes, all existing recoverable diagnostics remain intact/read-only, regression source pins the contract, and this claim is `COMPLETED` with exact integration evidence.
