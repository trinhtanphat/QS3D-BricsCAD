# Work claim — ProjectStateSnapshot relation fidelity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-snapshot-relation-fidelity`
- Registered: `2026-08-11T23:19:00+07:00`
- Priority: P1 — preserve exact reachable semantic state across rollback snapshots.

## Confirmed defect

`ProjectStateSnapshot.CopyInto(...)` reconstructs each `ProjectElement` by passing `FamilyId`, `FloorId`, and `ZoneId` through the `ProjectElement` constructor. Those three relation properties are public mutable strings, while the constructor trims them. A reachable in-memory state such as `element.FamilyId = "  FAM  "` is therefore silently canonicalized during snapshot capture. If a later semantic mutation fails, `Restore(...)` cannot return the project to the exact pre-operation state.

This is especially relevant while current Floor/Zone safety work deliberately treats padded mutable references as recoverable input rather than rewriting the public setters.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/ProjectSemanticMutationExecutorSmoke.cs`
- this claim file

## Intended contract

- Detached snapshot capture/restore must preserve the exact `FamilyId`, `FloorId`, and `ZoneId` strings that were reachable before a mutation.
- Existing constructor validation/canonicalization for newly authored `ProjectElement` objects remains unchanged.
- Existing project identity/category/metadata/audit/version rollback behavior remains unchanged.

## Excluded scope

- No changes to `ProjectElement` setters or constructor policy.
- No changes to `ProjectFloorService` / `ProjectZoneService`; those files are reserved by another active canonical-reference claim.
- No persistence migration/schema behavior changes.
- No native DWG/UI/runtime changes, no GitHub Actions dispatch.

## Validation plan

- Extend the already auto-registered semantic mutation smoke with a preexisting element whose mutable relation ids contain surrounding whitespace.
- Inject a semantic mutation failure and assert rollback restores all three relation strings byte-for-byte.
- Re-fetch both reserved blobs before each write; stale writes must fail rather than overwrite concurrent work.
- Review exact commit diffs and verify the close-out commit remains reachable from current `main` without force-push.

## Completion condition

Semantic rollback snapshots preserve exact mutable relation strings, focused regression source is pushed, and this claim is closed with exact commit SHAs and truthful source-only validation notes.
