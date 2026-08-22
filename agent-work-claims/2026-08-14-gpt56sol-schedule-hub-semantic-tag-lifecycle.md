# Work claim — Schedule Hub Semantic Tag lifecycle

- Status: `COMPLETED`
- Agent: `gpt56sol / ChatGPT web`
- Registered: `2026-08-14T16:58:30+07:00`
- Completed: `2026-08-14T17:04:30+07:00`
- Baseline main SHA: `9e35e9c6e58fef8231f1c972388fb893225f7680`
- Priority: UI completeness — expose the already-implemented Semantic Tag lifecycle in the drawing-bound Schedule Hub without changing native tag ownership or command behavior.

## Reserved scope

Add one focused Semantic Tag section to `ScheduleHubWindow.xaml` that dispatches the existing canonical commands through the existing `OnCommandClick` path:

- `QS3DTAG` — place/update owned Semantic MText tag;
- `QS3DTAGREFRESH` — refresh an existing owned tag;
- `QS3DTAGHEALTH` — read-only persisted/live health and locate;
- `QS3DTAGREMOVE` — explicit destructive removal.

Add a focused static preflight that proves the four launcher tags exist exactly once, use the generic Schedule Hub dispatcher, and each resolves to an existing adapter `CommandMethod` declaration.

## Implemented

- `bead1507d3ef43ba1f33733f6badd4f0ba3c1879` — `feat(ui): expose Semantic Tag lifecycle in Schedule Hub`
  - added a dedicated `SEMANTIC TAG / ANNOTATION` card in the right Schedule Hub column;
  - exposed Place/Update, Refresh, read-only Health and explicit Remove through the existing `OnCommandClick` dispatcher;
  - kept command ownership/PICKFIRST/native mutation behavior in the existing adapter commands unchanged.
- `b8da2d6dc6fd08876cc89baf8cbfc834bf5a2387` — `test(ui): guard Schedule Hub Semantic Tag lifecycle`
  - XML-parses the Schedule Hub XAML;
  - requires each of `QS3DTAG`, `QS3DTAGREFRESH`, `QS3DTAGHEALTH`, `QS3DTAGREMOVE` exactly once with `Click="OnCommandClick"` and visible content;
  - requires the drawing-bound generic dispatcher contract and exactly one adapter `CommandMethod` declaration for every launcher;
  - keeps the `QS3DTAGHEALTH` project lookup/read-only boundary guarded against tag build/remove/erase mutations.

## Validation

- Post-claim source was re-read on `main`: `QS3DTAG` and `QS3DTAGREFRESH` retain `UsePickSet`; `QS3DTAGREMOVE` retains its PICKFIRST lifecycle; `QS3DTAGHEALTH` retains read-only `TryGetReadOnly` behavior.
- Schedule Hub code-behind still uses `EnsureActive(...)` and `_document.SendStringToExecute(...)` in its generic document-bound `OnCommandClick` path.
- Read-back of `ScheduleHubWindow.xaml` on `main` confirms all four launchers in the dedicated card between Door/Opening and Rebar.
- `b8da2d6dc6fd08876cc89baf8cbfc834bf5a2387` is the current `main` head at validation time and directly descends from `bead1507d3ef43ba1f33733f6badd4f0ba3c1879`.
- GitHub reports no combined status entries for `b8da2d6dc6fd08876cc89baf8cbfc834bf5a2387`; GitHub Actions were not dispatched under this claim.
- No licensed BricsCAD V25 runtime execution was performed under this claim, so this closeout does not claim native visual/runtime PASS.

## Excluded scope preserved

- No changes to `SemanticTagCommands`, `SemanticTagRemovalCommands`, `SemanticTagHealthCommands`, builder/removal services, semantic ownership, persistence, rendering or native CAD mutation semantics.
- No native MLeader, sheet/layout, viewport/title-block or other LOCAL_ONLY documentation work.
- No release/tag operations, private drawings or licensed BricsCAD execution.
- No unrelated ACTIVE claim/source lane was touched.

## Completion result

Schedule Hub now exposes the complete existing Semantic Tag Place/Refresh/Health/Remove lifecycle through the canonical drawing-bound dispatcher and has a focused regression guard. The remote-safe UI wiring gap is closed; any remaining exact-V25/native documentation qualification remains outside this source claim.
