# Work claim — basic drawing tools bound to active Family

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-basic-drawing-20260813`
- Registered: `2026-08-13T17:15:00+07:00`
- Baseline main SHA: `092a5d28305ccddac09f79711d310cab93dde6f7`
- Priority: owner-requested QS3D first-version workflow from the supplied UI/command reference: panel -> Add -> properties -> active Family -> Line/Rectangle/Circle.

## Reserved scope

Implement a narrow BricsCAD V25 basic-drafting surface that reads and freshness-checks the canonical active QS3D Family, creates native LINE / closed rectangular POLYLINE / CIRCLE geometry from normal editor point prompts, and persists a versioned QS3D drafting-context marker on the operation-owned entity so the selected Family/category/floor/zone context is not merely cosmetic. Add compact Workspace buttons for the three tools and truthful command documentation/static guards.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/BasicDrawingCommands.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml` (small basic-drawing toolbar only)
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.BasicDrawing.cs` (new partial UI dispatch)
- `scripts/preflight-basic-drawing-active-family.py` (new static contract gate)
- `docs/COMMANDS.md`
- this claim closeout; existing LOCAL-008 handoff may be updated only after its currently reserved shared inbox surface is free.

## Excluded scope

- No changes to `WorkspacePanel.MultiSelectionProperties.cs`, Curtain selection/Undo, Source Reconcile, Family Manager, Zone/Floor manager, release/versioning, or current Direct Draw semantic/native builders.
- Do not reinterpret arbitrary Rectangle/Circle sketches as BIM semantics or auto-run `SemanticCaptureService`; category-specific `QS3DDRAW*` remains the semantic/native creation path.
- No V26 UI/command parity claim: the current V26 adapter does not yet contain the V25 Workspace/command surface.
- No GitHub Actions dispatch and no licensed-BricsCAD runtime PASS claim.

## Validation plan

- Re-read current main before every write and preserve concurrent changes.
- Static gate must assert unique command registrations, active-Family/project freshness, versioned drafting-context XData, point acquisition/cancel boundaries, three distinct native entity types, Workspace wiring, and truthful docs.
- Review exact source after writes; use repository source/preflight structure only. Exact V25 interactive geometry/UI validation remains LOCAL_ONLY.

## Coordination

Issue #982 currently still has an ACTIVE claim on `WorkspacePanel.MultiSelectionProperties.cs` and `docs/LOCAL-AGENT-INBOX.md`; this lane deliberately avoids both until that claim closes. Current Curtain Undo/mapping/display claims are unrelated and excluded.

## Completion condition

All three commands are present and discoverable from Workspace, each invocation binds to the canonical active Family and refuses stale modeless context before CAD commit, created entities carry the active drafting context, documentation/static guard are landed on current main, and this claim is marked `COMPLETED` with exact commit evidence. No CI/runtime qualification is implied.