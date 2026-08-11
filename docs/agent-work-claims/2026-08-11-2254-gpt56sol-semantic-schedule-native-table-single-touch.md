# Work claim — Semantic Schedule native Table single ChangeVersion touch

- Status: `ACTIVE`
- Agent: `gpt56sol-chatgpt-web`
- Registered: `2026-08-11T22:54:00+07:00`
- Baseline main SHA: `104d4f741849ac286065be01f5a529310d0e62c3`
- Priority: prevent custom Semantic Schedule native Table Build/Remove from advancing `ProjectState.ChangeVersion` twice for one logical mutation.

## Confirmed defect

`AuditTrail.ForProject(project).Record(...)` already owns a `ProjectState.Touch()`. `SemanticScheduleNativeTableBuilder.Build(...)` and `Remove(...)` each call `project.Touch()` again immediately after the audit event. One successful custom-schedule Table mutation therefore advances `ChangeVersion` twice, creating artificial freshness/version churn.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/SemanticScheduleNativeTableBuilder.cs`
- one focused auto-discovered source regression gate under `scripts/`
- this claim file

## Intended contract

- Build/Remove retain the same metadata mutations and audit events.
- `AuditTrail.Record(...)` remains the single project-version advancement for these logical native Table mutations.
- Preserve header-only zero-match rendering, per-schedule owner slots, XData ownership, transaction/rollback behavior, health diagnostics, and stored placement.

## Excluded scope

- active Core Semantic Schedule placement planner claim (`docs/agent-work-claims/20260811T2229-semantic-schedule-placement-core.md`)
- Semantic Schedule schema/catalog/rendering changes
- generic `ProjectOwnedNativeTableArtifactService` single-touch lane already completed via PR #510
- UI/Schedule Hub, quantity/reporting, updater/installer and local runtime work
- global `AuditTrail` behavior
- BricsCAD V25 native/runtime qualification

## Validation plan

- remove only the redundant explicit `project.Touch()` calls after custom-schedule native Table audit records;
- add a focused source gate that keeps Build/Remove audit events while prohibiting explicit project Touch in this builder and confirming `AuditTrail.Record` remains the touch owner;
- compare latest `main` for overlap before PR/merge;
- do not dispatch GitHub Actions.

## Completion condition

The focused source fix and regression gate are merged to `main`, this claim is marked `COMPLETED`, and exact V25 runtime evidence remains `LOCAL_ONLY` unless produced by a local agent.
