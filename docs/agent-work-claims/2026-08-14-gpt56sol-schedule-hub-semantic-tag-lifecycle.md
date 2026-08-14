# Work claim — Schedule Hub Semantic Tag lifecycle

- Status: `ACTIVE`
- Agent: `gpt56sol / ChatGPT web`
- Registered: `2026-08-14T16:58:30+07:00`
- Baseline main SHA: `9e35e9c6e58fef8231f1c972388fb893225f7680`
- Priority: UI completeness — expose the already-implemented Semantic Tag lifecycle in the drawing-bound Schedule Hub without changing native tag ownership or command behavior.

## Reserved scope

Add one focused Semantic Tag section to `ScheduleHubWindow.xaml` that dispatches the existing canonical commands through the existing `OnCommandClick` path:

- `QS3DTAG` — place/update owned Semantic MText tag;
- `QS3DTAGREFRESH` — refresh an existing owned tag;
- `QS3DTAGHEALTH` — read-only persisted/live health and locate;
- `QS3DTAGREMOVE` — explicit destructive removal.

Add a focused static preflight that proves the four launcher tags exist exactly once, use the generic Schedule Hub dispatcher, and each resolves to an existing adapter `CommandMethod` declaration.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml`
- `scripts/preflight-schedule-hub-semantic-tags.py`
- this claim file for closeout

## Excluded scope

- No changes to `SemanticTagCommands`, `SemanticTagRemovalCommands`, `SemanticTagHealthCommands`, builder/removal services, semantic ownership, persistence, rendering or native CAD mutation semantics.
- No native MLeader, sheet/layout, viewport/title-block or other LOCAL_ONLY documentation work.
- No GitHub Actions dispatch, release/tag operations, private drawings or licensed BricsCAD execution.
- Do not touch unrelated ACTIVE claims or source lanes.

## Validation plan

- Re-read current command declarations and Schedule Hub dispatcher on the exact post-claim `main`.
- Require each launcher tag exactly once in Schedule Hub XAML and `Click="OnCommandClick"`.
- Require one adapter `CommandMethod` declaration for each command.
- XML-parse Schedule Hub XAML in the focused preflight.
- Read back implementation SHA/diff from `main` and close this claim without claiming licensed runtime PASS.
