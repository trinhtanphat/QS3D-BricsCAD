# Work claim — Family property no-op audit suppression

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-family-property-noop-audit`
- Registered: `2026-08-12T08:31:00+07:00`
- Last Updated: `2026-08-12T08:31:00+07:00`
- Baseline main SHA: `1bb98be2ded9b886a1702693fdffc7e2c149f626`
- Priority: deterministic V25/V26 shared-UI persistence/no-op mismatch found during owner-requested `continue all`
- Task Key: `UI-FAMILY-PROPERTY-NOOP-AUDIT-SUPPRESSION`

## Confirmed defect

`ProjectFamilyService.SetProperty(...)` and `RemoveProperty(...)` are true no-ops when the requested Family property is already identical or absent: they return without `ProjectState.Touch()`.

`FamilyManagerWindow.OnSavePropertyClick(...)` and `OnRemovePropertyClick(...)` currently call `AuditTrail.ForProject(project).Record(...)` unconditionally after those service calls. `AuditTrail.Record(...)` touches the project. A user clicking Save on an unchanged Family property, or Remove for a property that no longer exists, therefore advances `ChangeVersion`, changes persistence freshness and appends a misleading mutation audit event even though the domain service made no change.

The same Family Manager already records Rename only when the name actually changes, Delete only when removal succeeds, and Assign only for elements whose Family relation changed, establishing mutation-only audit semantics inside this UI. `RefreshAfterCommit(...)` only refreshes presentation/palette and does not itself persist or touch the project.

V26 links the V25 UI C# source in `QS3D.BricsCAD.V26.csproj`, so this single shared-source correction applies to both supported hosts.

## Reserved scope

In the two Family property handlers, capture the project `ChangeVersion` immediately before the domain service call and record `family.property.set` / `family.property.remove` only when the service advanced that version. Preserve the existing service call, atomic rollback wrapper, audit detail for real mutations, success/status refresh behavior and stale-project guards.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs`
- one focused static source preflight under `scripts/` pinning mutation-only audit guards for both handlers and V26 linked-source parity
- this claim file

## Excluded scope

- `ProjectFamilyService` / `ProjectElement` domain behavior completed by the previous Family removal freshness lane.
- Family create/duplicate/rename/delete/assign behavior.
- audit schema/action canonicality, persistence format, other UI windows, release/update surfaces.
- runtime UI redesign or any licensed BricsCAD V25/V26 execution claim.
- GitHub Actions/build/release dispatch.

## Validation plan

- Static preflight proves each property handler captures a pre-service `ChangeVersion` and gates its audit `Record(...)` call on a post-service version change.
- Preserve unconditional status/refresh after a successful no-op click; only persistence/audit mutation is suppressed.
- Preserve real mutation audit details and the shared V25-source inclusion in the V26 project.
- Re-fetch moving `main` and the exact Family Manager source blob after claim publication and immediately before integration; review exact PR diff before merge.
- No executable WPF/native runtime PASS is claimed remotely.

## Coordination

Exact path history shows the latest `FamilyManagerWindow.xaml.cs` changes were prior modeless lifecycle/manager reconciliation work on 2026-08-11; no discovered current claim owns Family property no-op audit semantics. At registration time the open PR list is empty. A concurrent `zone create null preflight` claim on current `main` is unrelated.

## Completion condition

Current `main` no longer creates Family property audit/revision mutations for domain-level no-ops, while real property mutations retain their existing audit trail and UI refresh behavior, with focused static regression and this claim marked `COMPLETED`.
