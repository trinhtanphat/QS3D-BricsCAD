# Work claim — Semantic Element Table audit-owned ChangeVersion touch

- Status: `ACTIVE`
- Agent: `gpt56sol-chatgpt-web`
- Registered: `2026-08-11T23:05:00+07:00`
- Baseline main SHA: `77ce051af6db8c53aa77dda79620f63f0d3173e0`
- Priority: remove the remaining duplicate Semantic Element native Table project Touch and repair the stale transaction-boundary preflight so the source/gate contract matches current `AuditTrail.Record` semantics.

## Confirmed defect

`AuditTrail.Record(...)` already calls `ProjectState.Touch()` before appending its audit event. `SemanticElementTableBuilder.Build(...)` and `Remove(...)` still call `project.Touch()` again after their audit records, so one logical Table mutation advances `ChangeVersion` twice.

The existing `scripts/preflight-native-table-transaction-boundary.py` was introduced when explicit `project.Touch()` calls were intentionally placed before CAD commit. It still requires exactly one explicit `project.Touch()` in both Semantic Element and shared native Table methods. Since the shared service was corrected to audit-owned single-touch in PR #510, this preflight is now stale and would reject the current intended source contract.

`ProjectStateSnapshot` restores both `AuditEvents` and `ChangeVersion`, so an audit-owned Touch remains inside the rollback-capable semantic phase before CAD commit.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/SemanticElementTableBuilder.cs`
- `scripts/preflight-native-table-transaction-boundary.py`
- this claim file

## Intended contract

- Semantic Element Build/Remove retain metadata mutations and the same audit events, but no explicit `project.Touch()` after audit.
- The transaction-boundary gate requires snapshot -> audit -> CAD commit -> committed flag -> guarded rollback, with no explicit service/builder Touch.
- The gate verifies `AuditTrail.Record` remains the project Touch + audit-event owner and `ProjectStateSnapshot` restores AuditEvents and ChangeVersion.
- Preserve active-DWG/ModelSpace guards, CAD transaction ordering, ownership/XData, rollback compound failures, rendering, placement and runtime-health behavior.

## Excluded scope

- current active Regeneration, Updater, Interchange, Active Family and other agent lanes
- custom Semantic Schedule Table single-touch already completed via PR #517
- shared `ProjectOwnedNativeTableArtifactService` source already completed via PR #510
- global `AuditTrail` behavior changes
- BricsCAD V25 native/runtime qualification

## Validation plan

- remove only the two redundant Semantic Element `project.Touch()` calls;
- update the existing transaction-boundary gate rather than adding a competing duplicate gate;
- require audit-owned Touch and snapshot restore coverage explicitly;
- compare latest `main` for overlap before PR/merge;
- do not dispatch GitHub Actions.

## Completion condition

Semantic Element native Table mutations use one audit-owned ChangeVersion advancement, the existing transaction-boundary preflight matches that contract for both Semantic Element and shared native tables, the focused changes are merged to `main`, and runtime evidence remains `LOCAL_ONLY` unless produced locally.
