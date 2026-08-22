# Work claim — V25 Start Center dark selection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-start-center-dark-selection-20260813`
- Registered: `2026-08-13T17:37:00+07:00`
- Completed: `2026-08-13T17:40:00+07:00`
- Baseline main SHA: `34cc9b3cab6a90935231df6802a9fb69e4f853b8`
- Priority: Continue the user-requested V25 dark-host audit on the Start Center. `StartCenterWindow.xaml` contains four stock-template ListBoxes (`CommandList`, `FavoriteList`, `RecentCommandList`, `RecentProjectList`). Shared dark `ListBoxItem` selected values do not own the WPF item template, leaving active/inactive host `SystemColors` highlight resources able to leak bright chrome.

## Reserved scope

Keep all Start Center list selections on QS3D-owned dark active/inactive resources. Add a presentation-only guard at the window boundary and directly on all four named ListBoxes. Preserve command launching, favorites, recent command/project behavior, filtering and all CAD/project semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/StartCenterWindow.DarkHostTheme.cs`
- `scripts/preflight-start-center-dark-selection.py`
- read-only Start Center XAML and shared Theme contracts

## Excluded scope

- command allowlist/launching, favorites/history persistence, recent-project I/O
- Start Center layout/responsive behavior, shared Theme redesign, V26
- release/installer work, GitHub Actions dispatch, native BricsCAD PASS claims without licensed runtime evidence

## Result

- Implementation: `036d938ea6c188d235cd37f713ad85fb4c0e0ded` (`fix(v25): keep Start Center selection dark`).
  - Shadows active/inactive WPF selection background keys with QS3D `BgSelectedBrush`.
  - Shadows active/inactive WPF selection text keys with QS3D `TextBrush`.
  - Publishes each key at `StartCenterWindow.Resources` and directly on `CommandList`, `FavoriteList`, `RecentCommandList`, and `RecentProjectList`.
  - Leaves command launching, favorites/history, recent-project handling and CAD/project paths untouched.
- Regression: `7de2bbd6484c9344441ebb45ddc666b5ae5199c9` (`test(ui): guard Start Center dark selection`).

## Validation actually executed

- Current Start Center XAML still exposes all four named lists and their existing double-click contracts; no Start Center behavior/layout source was changed by this lane.
- Shared Theme retains canonical `BgSelectedBrush` and the stock `ListBoxItem` contract.
- Focused regression logic — `PASS: V25 Start Center dark host-selection contract` in an isolated connector-derived fixture.
- `compare_commits(7de2bbd6484c9344441ebb45ddc666b5ae5199c9, main)` returned `identical` at validation time.
- No GitHub Actions were dispatched. Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

Review/takeoff, schedule, diagnostic, settings/material and earlier dark-host lanes are completed. Concurrent drawing/Curtain/runtime/mapping work did not touch this scope.

## Completion condition

Satisfied for repository source/regression: focused Start Center guard and regression are pushed to `main`, exact source/ancestry were verified, and native visual qualification remains pending a licensed runtime smoke.
