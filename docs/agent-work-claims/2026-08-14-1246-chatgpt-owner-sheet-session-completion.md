# Work claim — owner spreadsheet/session completion

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-owner-sheet-session`
- Registered: `2026-08-14T12:46:00+07:00`
- Baseline main SHA observed before registration: `6f47dec11b46261c414e1ce0944d349787b60397`
- Priority: owner requested `continue all`, then a full project/session review and completion of every remaining non-overlapping source gap grounded in the supplied TEST QS3D spreadsheet, linked usage document, prior PDFs, and current repository state.

## Reserved scope

Audit and, only where current source still proves a gap, implement the remaining owner-reference behavior in these bounded lanes:

1. Direct Draw post-authoring ergonomics: preserve the current BricsCAD camera/zoom for the QS workflow, keep the newly generated component selected/highlighted, and prevent implicit `QS3DVIEW3D` from interrupting repeated authoring.
2. Generic `SE` closed-profile authoring: active/preferred Family + selected closed planar 2D polylines -> semantic elements + native 3D geometry using existing QS3D builders/ownership/rollback contracts, without weakening the separate wall-centerline Plan-to-3D workflow.
3. Spreadsheet-reported command/runtime regressions that remain reproducible from source, including `QS3DSETUP` WPF resource reachability, Core smoke-test top-level crash behavior, and the sheet's component-detail/property workflow only when the current source still lacks the requested path.
4. Focused static/smoke guards and an owner-reference audit/planning note that records implemented, already-satisfied, and LOCAL_ONLY items without claiming unrun BricsCAD evidence.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/DirectDrawCommands.cs` and a focused Direct Draw preflight/guard.
- A dedicated V25 generic closed-profile command/service (or the smallest existing appropriate surface) plus focused preflight coverage.
- Existing setup/settings/smoke/property files only if read-back proves a current defect.
- `scripts/` focused source guards for any touched contract.
- `docs/` owner-reference audit/plan and this claim close-out.

## Explicit exclusions / active coordination

Do not edit or take ownership of:

- the active BLT-reference UI registration follow-up (`ReferenceWorkspaceTreeAugmenter.cs`, `WorkspacePanel.CompactShell.cs`, its parity guard/plan);
- active NETLOAD/startup lifecycle surfaces (`WorkspacePanel.xaml.cs`, `RightPanel.xaml.cs`, `PluginEntry.cs`, `PaletteCoordinator.cs`, `DocumentLifecycleCoordinator.cs`, `RibbonInitializationCoordinator.cs`);
- active Source Reconcile / issue #1005 dictionary-affinity work;
- active Curtain method/gate and Level/rebar lanes (#1106/#1125 and successors);
- LOCAL_ONLY licensed-runtime execution, release/tag/signing, or unrelated workflow dispatch.

If a newly-landed claim reserves any symbol in this scope before a source write, this lane will skip that symbol and continue only the non-overlapping remainder.

## Validation plan

- Refresh `main` and active claims immediately before each write.
- Re-read touched source at current HEAD before replacement; never apply stale content.
- Add focused source/static/smoke contracts for every behavioral change.
- Preserve semantic ownership, project snapshot/rollback, scoped regeneration, drawing-unit/UCS freshness, and native-builder boundaries.
- Read back all pushed files and inspect current-main status/CI without weakening gates.
- Mark real-BricsCAD visual/interactive acceptance as `PENDING_LOCAL` unless exact licensed evidence already exists on current SHA.

## Completion condition

Every non-overlapping source gap supported by the owner spreadsheet/linked document/prior session has either (a) a pushed implementation + regression guard on `main`, (b) verified current source proving it is already satisfied, or (c) an explicit LOCAL_ONLY/active-claim handoff. The claim is then marked `COMPLETED` with exact commit evidence.