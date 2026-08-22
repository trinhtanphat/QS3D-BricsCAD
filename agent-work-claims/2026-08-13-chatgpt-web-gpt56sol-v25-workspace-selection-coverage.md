# Work claim — V25 Workspace dark selection coverage

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-v25-workspace-selection-coverage-20260813`
- Registered: `2026-08-13T14:27:30+07:00`
- Completed: `2026-08-13T14:38:10+07:00`
- Baseline main SHA: `0413a7951f1946b54211f04ea3de2b901b0576a0`
- Priority: Follow-up to the screenshot-visible white-selection fix. Source inspection showed the same Workspace also uses Family `ListBox`, property/inspection `ListView`, a second room-finish `TreeView`, and additional ComboBoxes. Their container styles still rely on stock WPF templates, so host/system active or inactive selection resources can leak bright chrome through the same mechanism already confirmed on `ModelTree`.

## Reserved scope

Extend the presentation-only V25 Workspace host-theme guard from the screenshot-visible Zone/Floor + ModelTree controls to the rest of the Workspace selection/control surface. Publish the canonical ComboBox style as a direct Workspace resource for all descendant ComboBoxes, and shadow active/inactive WPF selection background/text resources at the Workspace resource boundary so TreeView/ListBox/ListView containers use QS3D dark selection colors without changing selection semantics or business logic.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.DarkHostTheme.cs`
- `scripts/preflight-workspace-dark-selection.py`
- read-only verification of `src/QS3D.BricsCAD.V25/UI/Theme.xaml` and `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`

## Excluded scope

- Level/native geometry and `LOCAL-003` runtime qualification
- Workspace responsive layout, ScrollViewer/compact-shell sizing, commands, model/domain selection behavior
- shared `Theme.xaml` redesign, V26 behavior, Quantity Insight/Right Panel, installer/release work
- native BricsCAD visual PASS claims without an actual licensed runtime smoke

## Result

- Implementation: `5bd8587b51748ac61710a56d1b727af9a3fbb133` (`fix(v25): cover Workspace dark selection resources`).
  - Republishes the canonical QS3D implicit `ComboBox` style directly at `WorkspacePanel.Resources`, so current and future descendant ComboBoxes resolve the local dark style before host/application-level implicit styles.
  - Retains explicit local Zone/Floor style + background/foreground/border resource pins for the originally reported controls.
  - Shadows active and inactive WPF `SystemColors` selection background/text keys at `WorkspacePanel.Resources`, covering both Workspace TreeViews, `FamilyList`, `PropertyList`, and `InspectionList`; `ModelTree` remains locally pinned for immediate already-created-container refresh.
- Regression: `c3c45a02d5174e2db56dedb0e5677190b670f64d` (`test(ui): cover all Workspace dark selection surfaces`).
- Concurrent Workspace layout persistence work was explicitly non-overlapping and changed `WorkspacePanel.CompactShell.cs`; compare from the regression commit to later `main` showed our regression commit remains an ancestor with no subsequent changes to the dark-host bridge/test surfaces.

## Validation actually executed

- Re-fetched current-main `WorkspacePanel.DarkHostTheme.cs` and `scripts/preflight-workspace-dark-selection.py`; exact pushed Workspace-level resource publication/shadowing is present.
- `compare_commits(c3c45a02d5174e2db56dedb0e5677190b670f64d, 15eef2ae8a2aaad865fe8b642d3e62ef5ab72f98)` returned `status=ahead`, `behind_by=0`, merge-base equal to the regression commit; the later changed-file set did not include this lane's bridge/test files.
- `python3 -m py_compile scripts/preflight-workspace-dark-selection.py` — PASS on an isolated fixture containing the exact pushed regression logic.
- `python3 scripts/preflight-workspace-dark-selection.py` — `PASS: V25 Workspace dark host-selection coverage contract` on an isolated fixture containing the exact pushed bridge/test contracts plus the required current Theme/Workspace surface markers.
- The execution container still cannot resolve `github.com` for Git CLI and has no `dotnet`, `csc`, `mcs`, `msbuild`, or `pwsh`, so no fresh full repository build was claimed from this environment.
- Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.
- No GitHub Actions were dispatched by this lane.

## Coordination

`LOCAL-003` became externally blocked again and remained unrelated to this presentation-only Workspace resource lane. The parallel Workspace layout persistence claim explicitly excluded `WorkspacePanel.DarkHostTheme.cs`; it completed independently without modifying this lane's surfaces.

## Completion condition

Satisfied for repository source/regression: broader Workspace dark selection/control resources and focused regression are pushed to `main`, remote ancestry/current source were verified, and native BricsCAD visual qualification remains explicitly unclaimed pending a licensed local runtime smoke.
