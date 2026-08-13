# Work claim — V25 Start Center dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-start-center-dark-selection-20260813`
- Registered: `2026-08-13T17:37:00+07:00`
- Baseline main SHA: `34cc9b3cab6a90935231df6802a9fb69e4f853b8`
- Priority: Continue the user-requested V25 dark-host audit on the Start Center. `StartCenterWindow.xaml` contains four stock-template ListBoxes (`CommandList`, `FavoriteList`, `RecentCommandList`, `RecentProjectList`). Shared dark `ListBoxItem` selected values do not own the WPF item template, leaving active/inactive host `SystemColors` highlight resources able to leak bright chrome.

## Reserved scope

Keep all Start Center list selections on QS3D-owned dark active/inactive resources. Add a presentation-only guard at the window boundary and directly on all four named ListBoxes. Preserve command launching, favorites, recent command/project behavior, filtering and all CAD/project semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/StartCenterWindow.DarkHostTheme.cs` (new)
- `scripts/preflight-start-center-dark-selection.py` (new focused regression)
- read-only Start Center XAML and shared Theme contracts

## Excluded scope

- command allowlist/launching, favorites/history persistence, recent-project I/O
- Start Center layout/responsive behavior, shared Theme redesign, V26
- release/installer work, GitHub Actions dispatch, native BricsCAD PASS claims without licensed runtime evidence

## Validation plan

- Require all four active/inactive WPF selection background/text keys.
- Require root plus `CommandList`, `FavoriteList`, `RecentCommandList`, and `RecentProjectList` resource pins.
- Preserve all current list double-click contracts; assert the presentation partial contains no command/file/project mutation path.
- Re-fetch exact pushed source/test and verify ancestry.

## Coordination

Review/takeoff, schedule, diagnostic, settings/material and earlier dark-host lanes are completed. Current drawing/Curtain/runtime/mapping work is unrelated. No recent Start Center dark-selection claim was found.

## Completion condition

Focused Start Center guard + regression are pushed to current `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with exact SHAs and validation actually executed.
