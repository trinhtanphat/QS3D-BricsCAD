# Work claim — WorkspacePanel constructor runtime failure

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-workspace-constructor-runtime-20260812-0747`
- Registered: `2026-08-12T07:47:00+07:00`
- Baseline main SHA: `dd8629af2ba59f1432c4cc13b1f45411a0bfea84`
- Priority: P0 owner-reported BricsCAD runtime failure. Invoking the QS3D Workspace showed a XAML constructor wrapper exception for `QS3D.BricsCAD.V25.UI.WorkspacePanel` instead of opening the palette.

## Reserved scope

Fix the source-side WorkspacePanel construction/host integration regression evidenced by the owner's real BricsCAD screenshot, preserve compact horizontal overflow behavior, and make palette startup report actionable nested initialization errors instead of recursively surfacing another palette-construction failure.

## Implemented

- `cb2c32cec1150a620ad544cb49734de049ca0d68` — `fix(ui): harden WorkspacePanel construction in BricsCAD`
- Removed the custom `UserControl.Template` that was used only to host horizontal overflow.
- Replaced it with normal named `WorkspaceOverflow` `ScrollViewer` content composition and a named `WorkspaceContentRoot` design surface while retaining the 560-DIP content floor and compact 460x420 PaletteSet host contract.
- Rebound splitter persistence to the explicit content root instead of assuming `UserControl.Content` is the three-column Grid.
- Updated `preflight-ui-layout-persistence.py` and the offline WPF palette smoke to reject custom-template regression and assert compact overflow/DataContext/focus behavior on the new structure.
- Hardened `PaletteCoordinator.Show()`, Safe Mode and status reporting so Workspace construction failures are reported to the active BricsCAD editor with nested exception messages without recursively invoking palette creation from the error path.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.LayoutPersistence.cs`
- `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`
- `scripts/preflight-ui-layout-persistence.py`
- `scripts/test-wpf-palettes-runtime.ps1`
- this claim file for close-out

## Excluded scope

- Project Browser Core persistence/canonicality, including the concurrent container-order lane.
- Premium UI planning/progress documentation owned by its docs-only claim.
- Right Panel, Quantity Insight product behavior, Ribbon redesign, unrelated model/domain/reporting code.
- No GitHub Actions dispatch and no remote claim of BricsCAD V25 runtime PASS.

## Validation actually performed

- Reviewed the final PR diff and confirmed the Workspace XAML change is limited to replacing the custom control template with normal ScrollViewer content composition; the remainder of the Workspace XAML is unchanged.
- Re-read the resulting XAML top/tail from the implementation branch and confirmed the named overflow/content-root structure and balanced closing elements are present.
- GitHub reported PR #632 mergeable; it was squash-merged to `main` as `cb2c32cec1150a620ad544cb49734de049ca0d68`.
- Confirmed the merge commit is present in subsequent current-`main` history after concurrent commits.
- Static/offline guard source was updated, but those scripts were not executed in this remote session.

## LOCAL_ONLY remainder

The existing `LOCAL-012` Workspace/modeless V25 qualification already requires real BricsCAD Workspace construction/rendering, document lifecycle and DPI validation, so no duplicate local item was created. Re-test the exact current candidate locally: run `QS3D`, confirm all three palettes open without the constructor popup, verify compact/wide Workspace overflow and splitters, and if initialization still fails capture the new command-line nested exception chain emitted by `PaletteCoordinator`.

## Coordination

The active Project Browser workspace-container claim is Core XML only and explicitly excludes BricsCAD/native/UI changes. The premium UI reconciliation claim is docs-only and explicitly excludes Workspace XAML/code-behind. No overlapping implementation surface was taken.

## Completion condition

Completed: the source-safe fix and regression guards are merged to `main`; exact licensed BricsCAD V25 runtime proof remains LOCAL_ONLY under existing `LOCAL-012`.