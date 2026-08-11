# Work claim — HostLinkService audit-owned project revision

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-host-link-audit-owned-revision`
- Registered: `2026-08-11T23:31:00+07:00`
- Baseline main SHA: `d77510f2c65074916c0c8c4b041a2124ee8e7353`
- Priority: P1 — one logical audited host-link mutation should advance project revision once.

## Confirmed defect

`HostLinkService` performs semantic changes inside `ProjectSemanticMutationExecutor`, then calls `project.Touch()` immediately before `AuditTrail.ForProject(project).Record(...)`. A project-bound `AuditTrail.Record(...)` already calls `ProjectState.Touch()` before appending the audit event. Link, unlink, and stale AutoHost-provenance cleanup therefore advance `ProjectState.ChangeVersion` twice for one logical audited mutation.

This is inconsistent with the repository's current audit-owned revision pattern used by other semantic operations and makes stale-plan/persistence checks observe artificial extra revisions.

## Reserved scope

- `src/QS3D.Core/Services/HostLinkService.cs`
- `tests/QS3D.Core.SmokeTests/HostLinkCanonicalizationSmoke.cs`
- this claim file

## Intended contract

- A real `LinkOpening(...)` mutation records one audit event and advances project `ChangeVersion` exactly once.
- A real `UnlinkOpening(...)` mutation records one audit event and advances project `ChangeVersion` exactly once.
- Clearing stale AutoHost provenance without `HostWallId` remains one audited mutation and advances once.
- Canonical no-op link/unlink behavior remains side-effect free.
- Existing element dirty-state updates and semantic rollback protection remain unchanged.

## Excluded scope

- No Direct Draw/AutoHost command changes.
- No native DWG/regeneration/UI changes.
- No audit contract redesign; `AuditTrail` remains the revision owner for audited semantic operations.
- No GitHub Actions dispatch.

## Validation plan

- Extend the already-registered host-link Core smoke with exact `ChangeVersion` and audit-count assertions for link/unlink, plus stale provenance cleanup when practical.
- Re-fetch source/test blobs immediately before writes and reject stale overwrites.
- Fetch exact commit diffs after publication and verify claim close remains reachable from current `main` without force-push.

## Completion condition

Host-link audited mutations produce exactly one project revision each, focused regression is pushed, and this claim is closed with exact SHAs and truthful source-only validation notes.
