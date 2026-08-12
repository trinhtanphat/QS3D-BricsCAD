# Work claim — HostLinkService global semantic element identity integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-host-link-global-element-integrity-20260812-0910`
- Registered: `2026-08-12T09:10:00+07:00`
- Baseline main SHA: `78a298e7e509c2de65f3efb638016f0a5adc448a`
- Priority: P1 — prevent audited host-link mutation on a project whose semantic element identity set is already ambiguous.

## Confirmed defect

`HostLinkService.LinkOpening(...)` and `UnlinkOpening(...)` resolve only the requested opening/host via `ProjectState.FindElement(...)`. `FindUnique` detects duplicate IDs only when they match the requested ID. Therefore an unrelated duplicate pair such as `DUP` + `dup` can coexist while linking a unique `OPEN` to unique `WALL` still enters `ProjectSemanticMutationExecutor`, changes relations/dirty state and appends an audit event. QSDB persistence, DependencyGraph, BulkEdit and other Core mutation boundaries treat duplicate semantic element identity as invalid globally.

## Reserved surfaces

- `src/QS3D.Core/Services/HostLinkService.cs`
- `tests/QS3D.Core.SmokeTests/HostLinkGlobalElementIntegritySmoke.cs` — new focused regression
- this claim file

## Intended fix

- Preflight the full `project.Elements` collection for case-insensitive duplicate IDs before LinkOpening or UnlinkOpening target lookup/mutation.
- Keep null-element behavior fail-closed, canonical no-op host-link behavior, physical-cut safety, rollback/audit-owned revision semantics and dependency canonicalization unchanged.
- Add focused smoke proving unrelated duplicate element identities cause both link and unlink to fail before HostWallId/dependencies/audit/project revision mutation; valid linking remains functional.

## Coordination

The older `2026-08-11-2331` HostLink audit-owned-revision claim is `COMPLETED`; this lane is independent and does not change its one-revision-per-audited-mutation contract. Current Recognition claim owns only `src/QS3D.Core/Recognition/RecognitionEngine.cs` and its dedicated smoke.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed.
