# Work claim — WorkspacePanel constructor runtime failure

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-workspace-constructor-runtime-20260812-0747`
- Registered: `2026-08-12T07:47:00+07:00`
- Baseline main SHA: `dd8629af2ba59f1432c4cc13b1f45411a0bfea84`
- Priority: P0 owner-reported BricsCAD runtime failure. Invoking the QS3D Workspace shows a XAML constructor wrapper exception for `QS3D.BricsCAD.V25.UI.WorkspacePanel` instead of opening the palette.

## Reserved scope

Fix the source-side WorkspacePanel construction/host integration regression evidenced by the owner's real BricsCAD screenshot, preserve compact horizontal overflow behavior, and make the `QS3D` entrypoint report actionable nested initialization errors instead of an unhandled generic constructor popup.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.LayoutPersistence.cs`
- `src/QS3D.BricsCAD.V25/Commands.cs`
- focused Workspace/palette preflight or offline WPF smoke source as required
- `docs/LOCAL-AGENT-INBOX.md` only to narrow the existing LOCAL-012 runtime scenario if source changes materially change it
- this claim file for close-out

## Excluded scope

- Project Browser Core persistence/canonicality, including the active container-order lane.
- Premium UI planning/progress documentation currently owned by a docs-only claim.
- Right Panel, Quantity Insight product behavior, Ribbon redesign, unrelated model/domain/reporting code.
- No GitHub Actions dispatch and no remote claim of BricsCAD V25 runtime PASS.

## Validation plan

- Preserve well-formed XAML and the 560-DIP Workspace design floor with horizontal overflow at compact host widths.
- Avoid replacing the `UserControl` control template solely to provide overflow; use normal content composition compatible with BricsCAD palette hosting and adjust splitter persistence to target the explicit content grid.
- Add/update static/offline regression assertions for the resulting structure.
- Route `QS3D` palette startup through the repository command guard and surface nested exception messages for future initialization failures.
- Re-read all touched files from current `main` immediately before each write and recheck concurrent claims.

## Coordination

The active Project Browser workspace-container claim is Core XML only and explicitly excludes BricsCAD/native/UI changes. The active premium UI reconciliation claim is docs-only and explicitly excludes Workspace XAML/code-behind. This claim owns only the concrete Workspace constructor/runtime source regression reported by the owner.

## Completion condition

A source-safe fix and regression guard are pushed on current `main`, the existing LOCAL-012 handoff is updated if necessary for exact V25 re-test, and this claim is marked `COMPLETED` with implementation SHA(s).