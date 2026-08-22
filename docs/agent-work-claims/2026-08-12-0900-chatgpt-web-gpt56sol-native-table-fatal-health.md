# Work claim — project-owned native Table fatal runtime-health propagation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-native-table-fatal-health-20260812-0900`
- Registered: `2026-08-12T09:00:00+07:00`
- Baseline main SHA: `bd5a2bd242ddc924fd68c84867492e96d0e96ccd`
- Priority: owner-requested continue-all shared native runtime-health integrity hardening

## Confirmed defect

`ProjectOwnedNativeTableArtifactService.Inspect(...)` caught all `Exception` values while validating persisted metadata, building/validating the authoritative snapshot, and reading live Table cells. Its shared `TryResolve(...)` helper also used `catch { return false; }`. Those broad recovery paths could absorb fatal runtime exceptions and downgrade them to ordinary `DOCUMENTATION_TABLE_*` diagnostics or a missing handle before the outer native runtime-health isolation boundary enforced its fatal-exception policy. Door/Opening, Room Finish, Material Usage, BQ and BBS native Table health all delegate to this shared service.

## Implemented scope

- Ordinary recoverable metadata/render/cell/handle failures remain fail-visible through existing documentation-table diagnostics.
- `OutOfMemoryException`, `StackOverflowException`, and `AccessViolationException` are no longer swallowed in the shared diagnostic recovery paths.
- Metadata validation, snapshot rendering, cell reads and handle resolution now use one shared `IsRecoverableDiagnosticFailure(Exception)` predicate.
- Current diagnostic codes, ownership checks, `OpenMode.ForRead`, row/cell detail limits and mutation lifecycle semantics remain intact.
- Build/Remove rollback catches remain unchanged and still rethrow operation failures; this lane only changes diagnostic recovery and the shared resolver's fatal propagation.
- Added one focused static regression preflight for the shared Table family.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs`
- `scripts/preflight-native-table-runtime-health-fatal.py`
- this claim file

## Integration evidence

- Claim registration: `3436a5515b912db3cd9c9b59467ad48c4866fe1a`
- Source fix: `398474ddea51243179b80eb9b0b54e051b515970`
- Focused regression preflight: `e7a2576e0e0bdaad3b3483534e02d210f1e9159e`

## Validation performed

- Re-fetched the shared source after source integration; blob `d8118254ef3a89c76743c24a90216df5d774ff5a` contains filtered metadata/render/cell catches, filtered `TryResolve(...)`, explicit fatal exclusions, and retains `OpenMode.ForRead` in health inspection.
- Verified Build/Remove rollback catches remain broad only to restore project state and then rethrow; they were not changed into diagnostic swallowing paths.
- Re-fetched `scripts/preflight-native-table-runtime-health-fatal.py`; blob `aeac93de12c5d2d6788de51a4361bf2dad9febdc` isolates the health scope plus shared resolver, requires three filtered health catches and all fatal exclusions, and deliberately does not flag the rethrowing Build/Remove rollback catches.
- Door/Opening and Room Finish wrappers were directly re-read and continue delegating health to this shared service; Material Usage/BQ/BBS use the same project-owned native Table artifact family.
- V26 links the shared V25 adapter source, so this hardening is shared by V26 source build without a duplicate implementation.

## Validation boundary

Remote source/static readback only. This session did not execute the preflight process, a full .NET build/test, GitHub Actions, or licensed BricsCAD V25/V26 runtime. No native runtime, private-DWG, installer, signing or release PASS is claimed.

## Excluded scope

- No Table content calculation or individual Door/Room/Material/BQ/BBS builder changes.
- No unrelated ownership/release/runtime behavior changes.
- No changes to unrelated active claims.
- No GitHub Actions, release publication or force push.

## Completion condition

Satisfied on the source/static contract: current `main` no longer lets shared project-owned native Table health swallow native fatal exception classes, existing recoverable diagnostics remain intact/read-only, regression source pins the contract, and exact integration evidence is recorded above.
