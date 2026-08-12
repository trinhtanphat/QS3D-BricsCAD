# Work claim — Family property no-op audit suppression

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-family-property-noop-audit`
- Registered: `2026-08-12T08:31:00+07:00`
- Last Updated: `2026-08-12T08:36:00+07:00`
- Baseline main SHA: `1bb98be2ded9b886a1702693fdffc7e2c149f626`
- Priority: deterministic V25/V26 shared-UI persistence/no-op mismatch found during owner-requested `continue all`
- Task Key: `UI-FAMILY-PROPERTY-NOOP-AUDIT-SUPPRESSION`
- Implementation PR: `#653`
- Main integration commit: `24f1bc0deb6136c2bc7f467705590d995b1ece44`

## Confirmed defect

`ProjectFamilyService.SetProperty(...)` and `RemoveProperty(...)` are true no-ops when the requested Family property is already identical or absent: they return without `ProjectState.Touch()`.

`FamilyManagerWindow.OnSavePropertyClick(...)` and `OnRemovePropertyClick(...)` previously called `AuditTrail.ForProject(project).Record(...)` unconditionally after those service calls. `AuditTrail.Record(...)` touches the project. A user clicking Save on an unchanged Family property, or Remove for a property that no longer exists, therefore advanced `ChangeVersion`, changed persistence freshness and appended a misleading mutation audit event even though the domain service made no change.

The same Family Manager already records Rename only when the name actually changes, Delete only when removal succeeds, and Assign only for elements whose Family relation changed, establishing mutation-only audit semantics inside this UI. `RefreshAfterCommit(...)` only refreshes presentation/palette and does not itself persist or touch the project.

V26 links the V25 UI C# source in `QS3D.BricsCAD.V26.csproj`, so this single shared-source correction applies to both supported hosts.

## Implemented scope

`OnSavePropertyClick(...)` and `OnRemovePropertyClick(...)` now capture `project.ChangeVersion` immediately before the domain service call and append the corresponding audit event only when the service advances that version. True domain no-ops therefore remain audit/revision no-ops, while real mutations keep the existing audit action/detail and the audit-owned additional revision increment.

Atomic rollback, stale-project guards, service calls, success/status refresh behavior and all other Family operations remain unchanged.

## Static regression

Added auto-discovered `scripts/preflight-family-property-noop-audit.py` which:

- isolates both Family property handlers;
- requires pre-service `ChangeVersion` capture;
- requires the expected domain service call;
- requires the post-service version guard before the audit call;
- requires exactly one guarded audit record for each action;
- verifies the V26 project continues to linked-compile V25 C# source under the V25 root namespace with `BRICSCAD_V26` defined.

## Coordination / exclusions preserved

- `ProjectFamilyService` / `ProjectElement` domain behavior completed by the previous Family removal freshness lane was not modified.
- Family create/duplicate/rename/delete/assign behavior was not modified.
- audit schema/action canonicality, persistence format, other UI windows, release/update surfaces were not modified.
- No force-push, GitHub Actions/build/release dispatch, or licensed V25/V26 runtime qualification was performed.

## Validation evidence

- Claim registration was committed to `main` before implementation at `090d326990fb2c355e9df0bd90a0d9ebf279ae36`.
- Post-claim and immediate pre-PR readback confirmed moving `main` retained `FamilyManagerWindow.xaml.cs` blob `0bad38b7fc7be520e7e4fe84ec2e70b434a0c9a2`; intervening Audit/Zone work did not overlap this source.
- Shared UI source fix commit: `8e63f586336b0dc39f49d31ce7b517513f645379`.
- Static preflight/head commit: `de58cc152295d5135e9d2c4b39215ca1d6bac383`.
- PR #653 exact diff was reviewed before merge and contained exactly two files, `+88/-2`; the production diff only added two `beforeVersion` captures and two conditional audit guards.
- Server-side squash merge with exact expected head `de58cc152295d5135e9d2c4b39215ca1d6bac383` produced `24f1bc0deb6136c2bc7f467705590d995b1ece44`.
- Post-merge readback confirms `FamilyManagerWindow.xaml.cs` blob `3d30cec60fcfa46d8280394f45a7ed77b71088b3` contains the intended guards.
- The static preflight was committed but not executed in this connector-only environment. No WPF/.NET build or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion

`COMPLETED`: current `main` no longer creates Family property audit/revision mutations for domain-level no-ops, while real property mutations retain their existing audit trail and shared V25/V26 UI behavior.