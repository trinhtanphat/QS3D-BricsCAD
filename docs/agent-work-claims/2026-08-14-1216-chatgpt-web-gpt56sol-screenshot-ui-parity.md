# Work claim — screenshot UI/workflow parity inside BricsCAD

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T12:16:16+07:00`
- Baseline main SHA: `2cb50b15cf778dbeb60950a076987ab3a20089c6`
- Priority: user-requested audit and completion against the supplied BLT3D workflow screenshot while preserving the QS3D BricsCAD-plugin product boundary

## Reserved scope

Audit the current QS3D BricsCAD ribbon/start-workspace experience against the supplied screenshot and close source-level gaps for the equivalent in-host workflow: top-level workflow grouping, project new/open/save/save-as/settings entry points, Start Center quick actions/recent-project presentation, and a compact project/status summary surface where current architecture supports it. The lane owns only screenshot-parity integration and guards; it does not replace BricsCAD with a standalone shell.

## Expected surfaces

- BricsCAD ribbon bootstrap/catalog/augmenter source responsible for QS3D workflow tabs/panels/buttons.
- Existing project command handlers/services for new/open/save/save-as/settings and Start Center entry points.
- Existing Start Center/palette/view-model source and recent-project persistence used by that experience.
- Narrow static/preflight or smoke guards for screenshot-parity command discoverability and wiring.
- A detailed implementation/audit planning Markdown document for this screenshot-parity lane.

## Excluded scope

- No standalone BLT3D desktop application and no competing CAD host.
- No changes to Plan-to-3D geometry algorithms, Curtain3D, live-sheet STT2-STT5 logic, Level native qualification, issue #1005 nested undo investigation, quantity formula/geometry calculation internals, installer/MOTW recovery, or unrelated LOCAL_ONLY BricsCAD qualification lanes.
- No release/tag/package publication and no manual GitHub Actions dispatch unless separately authorized by repository policy.

## Validation plan

- Re-audit every visible screenshot control against a real QS3D command/action or an explicitly host-owned BricsCAD control.
- Run repository-supported static/preflight guards for touched UI/ribbon/start surfaces and inspect source-level command wiring.
- Check current-main CI/status after implementation commits without weakening gates.
- Record any remaining exact-BricsCAD runtime/pixel acceptance as LOCAL_ONLY rather than claiming unexecuted evidence.

## Coordination

Recent concurrent claims are for live-sheet STT2-STT5, Plan-to-3D P02, Level native qualification, Curtain3D, issue #1005 and other non-overlapping lanes. This claim deliberately excludes those feature/runtime scopes. If a new claim reserves the same ribbon/Start Center symbols or screenshot-parity scenario, this lane will stop and coordinate before writing.

## Completion condition

A pushed main commit contains the detailed screenshot parity plan plus all non-overlapping source/test changes needed for the audited QS3D in-BricsCAD equivalent, no visible QS3D button introduced by this lane is dead/unwired, source/static validation passes, and this claim is closed with exact implementation SHAs and any remaining host-only acceptance gate documented.
