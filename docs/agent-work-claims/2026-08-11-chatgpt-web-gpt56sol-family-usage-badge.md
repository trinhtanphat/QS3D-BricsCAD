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
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilyUsageBadge.cs` (new isolated partial)
- `scripts/preflight-family-usage-badge.py`
- this claim file
- `WorkspacePanel.xaml`, `WorkspaceViewModel.cs`, `WorkspacePanel.xaml.cs`, Core Family/domain and persistence code are **audit-only/no-edit** surfaces. The partial upgrades only the existing generated Family badge binding at runtime, so concurrent XAML/Workspace winners remain untouched.

## Implementation shape

- `FamilyUsageTextConverter` is a read-only `IMultiValueConverter`. Value 0 is the row `ProjectFamily`; value 1 is Workspace `Status`, used only as an invalidation signal so counts refresh after ordinary Workspace actions/status changes.
- `WorkspacePanel.FamilyUsageBadge.cs` registers a class-level Loaded handler, hooks `FamilyList.ItemContainerGenerator.StatusChanged` and `FamilyList.LayoutUpdated` once, walks only generated `FamilyList` item visuals, finds the existing TextBlock whose original binding is `Properties.Count`, and replaces that one binding with a `MultiBinding` using the converter.
- Each upgraded TextBlock is marked through an attached property so repeated layout/load events remain idempotent and do not touch unrelated property-count badges elsewhere in the Workspace.

## Functional contract

- Converter obtains only an existing current project through `ProjectContextCoordinator.TryGetReadOnly(...)`; it never creates/replaces/mutates a project.
- Verify the displayed Family belongs to the current project, then count current semantic `ProjectElement` rows whose `FamilyId` matches that Family ID case-insensitively.
- Return `N cấu kiện`; return `—` when no active document/project or the Family is stale/not owned by the current project.
- Runtime binding upgrade targets only generated items under `FamilyList` and only the original `Properties.Count` badge; Family name/category, property-panel counts, selection, search and Add/Delete/Capture/Vẽ 3D handlers remain intact.
- No command dispatch, CAD mutation, QSDB write or Core model change.

## Validation plan

- Re-fetch latest `main` and audit current `WorkspacePanel.xaml` before source write; preserve concurrent winners.
- Add focused auto-discovered static preflight for read-only project acquisition, ownership/count logic, idempotent FamilyList-only visual binding upgrade, MultiBinding invalidation and preservation of existing XAML Family actions/property-panel count.
- Re-fetch final source/ancestry/status. Do not dispatch GitHub Actions.

## Completion condition

Family rows show actual semantic usage counts in the screenshot-style badge with no change to Family selection/mutation semantics, and this claim is marked `COMPLETED` with exact SHAs.
