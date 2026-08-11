# Work claim — Family usage badge parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-usage-badge`
- Registered: `2026-08-11T22:20:00+07:00`
- Baseline main SHA: `25fe5508dc49089fd29112c4fa4e998def3d6444`
- Priority: P1 screenshot/reference parity

## Goal

Make the `FAMILY / TYPE` list show the screenshot-style semantic usage count (`N cấu kiện`) instead of the current property-definition count. Keep the existing Family objects/selection behavior unchanged and compute the badge read-only from the current canonical project.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/FamilyUsageTextConverter.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`
- `scripts/preflight-family-usage-badge.py`
- this claim file
- `WorkspaceViewModel.cs`, `WorkspacePanel.xaml.cs`, Core Family/domain and persistence code remain audit-only/no-edit surfaces.

## Functional contract

- Add a WPF `IMultiValueConverter` receiving the row `ProjectFamily` plus the Workspace `Status` as a harmless invalidation signal.
- Converter obtains only an existing current project through `ProjectContextCoordinator.TryGetReadOnly(...)`; it never creates/replaces/mutates a project.
- Verify the displayed Family belongs to the current project, then count current semantic `ProjectElement` rows whose `FamilyId` matches that Family ID case-insensitively.
- Return `N cấu kiện`; return `—` when no active document/project or the Family is stale/not owned by the current project.
- XAML Family list badge uses the converter via `MultiBinding`; Family name/category, selection, search and Add/Delete/Capture/Vẽ 3D handlers remain intact.
- No command dispatch, CAD mutation, QSDB write or Core model change.

## Validation plan

- Re-fetch latest `main` and `WorkspacePanel.xaml` immediately before the write; preserve concurrent winners.
- Add focused auto-discovered static preflight for read-only project acquisition, ownership/count logic, MultiBinding wiring and preservation of existing Family actions.
- Re-fetch final source/ancestry/status. Do not dispatch GitHub Actions.

## Completion condition

Family rows show actual semantic usage counts in the screenshot-style badge with no change to Family selection/mutation semantics, and this claim is marked `COMPLETED` with exact SHAs.
