# Work claim — QS3D Start Center workflow hub

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-start-center-20260811-2002`
- Registered: `2026-08-11T20:02:00+07:00`
- Baseline main SHA: `d5f7cce7e266f1ed476c490958eaac155f7e4e82`
- Priority: continue the owner-requested BLT3D-familiar clean-room UI/UX wave with a source-safe workflow hub that reduces command hunting without creating a standalone application or a second semantic/CAD model.

## Reserved scope

Implement a new BricsCAD-hosted **QS3D Start Center** modeless WPF workflow hub. The hub owns a hard-coded allowlisted QS3D command catalogue, search/filter/grouping, favorites and recent-command state, recent-DWG state with normalized/deduplicated paths and missing-file status, current active-document/project summary, quick modeling/quantity/review entry points, and guarded project/document actions. Every click-time action must resolve the current active BricsCAD document instead of retaining a stale document object. Add focused static/preflight coverage and park only the exact native WPF/BricsCAD runtime proof in the canonical local inbox.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/StartCenterCommands.cs` (new user-facing `QS3DSTART` command/coordinator surface; exact filename may be narrowed after source orientation)
- `src/QS3D.BricsCAD.V25/UI/StartCenterWindow.xaml` (new)
- `src/QS3D.BricsCAD.V25/UI/StartCenterWindow.xaml.cs` (new)
- `src/QS3D.BricsCAD.V25/Services/StartCenterUserStateStore.cs` (new, or an equally narrow new state-store file if existing persistence conventions require a different name)
- `src/QS3D.BricsCAD.V25/Services/StartCenterCommandCatalog.cs` (new, or an equally narrow new allowlist/catalog file)
- `scripts/preflight-start-center.py` (new)
- `docs/UI-START-CENTER-2026-08-11.md` (new)
- `docs/LOCAL-AGENT-INBOX.md` only to add/update the minimum exact Start Center V25 rendering/document-switch/recent-open qualification scenario
- this claim file for close-out status

## Excluded scope

- No Ribbon implementation or edits while `2026-08-11-chatgpt-web-gpt56sol-ribbon-information-architecture.md` is ACTIVE; in particular no `RibbonBootstrapper.cs`, `ProjectRibbonAugmenter.cs`, `QuickWorkflowRibbonAugmenter.cs` or new Ribbon augmenter in this reservation. Ribbon entry-point wiring can be reserved later only after the active Ribbon lane is completed/released and claims are rechecked.
- No edits to existing modeless schedule/revision viewer files reserved by `2026-08-11-chatgpt-web-modeless-viewer-project-identity.md`; Start Center is a global click-time-resolved workflow launcher, not a document-bound historical viewer.
- No `Theme.xaml`, `WorkspacePanel*`, `RightPanel*`, Core semantic/persistence services, Direct Draw/Create Similar implementation, Room Auto, recognition, Level Z-chain, quantity-engine, generated-ownership, geometry-builder, installer/release/signing or GitHub Actions work.
- No standalone `QS3D.exe`, proprietary BLT asset/source reuse, or second CAD/semantic engine.

## Validation plan

- Re-fetch current `main` and active claims before implementation writes and before final integration.
- Read existing WPF/modeless, command-dispatch, project-context and user-state conventions before coding; reuse existing QS3D commands instead of duplicating their business logic.
- Keep launcher execution allowlisted and deterministic; reject arbitrary command text and normalize persisted paths before dedupe/open.
- Add a focused auto-discovered static preflight for `QS3DSTART`, command allowlisting, click-time active-document resolution, no standalone product drift, recent/favorites persistence boundaries and no dependency on the reserved Ribbon surfaces.
- Inspect the final pushed diff/full files and ancestry. Do not claim BricsCAD V25/WPF rendering, HiDPI, Unicode, focus, native document-open/save or multi-DWG runtime behavior without local evidence.

## Coordination

Active neighboring lanes own Ribbon IA, specific modeless schedule/revision project identity, Direct Draw Create Similar, generated-source recognition, Room Auto regeneration, Core mutation atomicity, LOCAL-003 Level Z-chain and LOCAL-013 Excel Locate qualification. This reservation uses only new Start Center surfaces plus a narrow local-inbox handoff and explicitly leaves Ribbon discoverability to the active Ribbon claim.

## Completion condition

The source-safe Start Center command/window/catalog/state store and focused static contract are pushed to current `main`; the minimum exact V25 runtime scenario is present in `docs/LOCAL-AGENT-INBOX.md`; this claim records the implementation SHA(s) and is marked `COMPLETED`. Ribbon entry wiring remains explicitly unclaimed while its neighboring reservation is active.