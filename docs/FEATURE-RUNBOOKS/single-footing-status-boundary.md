# V25 Single Footing status boundary

## Scope

This carrier hardens only the BricsCAD V25 Single Footing interactive UI/status boundary in `SingleFootingCommands.cs`. It does not change footing dimensions, footprint/solid construction, project/family selection, semantic capture, generated-geometry ownership, persistence, or rollback algorithms.

## Defect

`QS3DDRAWSINGLEFOOTING` historically caught `Exception ex` and concatenated `ex.Message` into user-visible reporting. The shared `Report(Document,string)` helper also published the message to process-wide Workspace/Palette state without proving that the captured source `Document` was still the exact MDI active document. A failure or post-commit success from document A could therefore disclose arbitrary exception detail and/or smear A's status onto document B after an MDI switch.

## Required contract

- Interactive failure text is stable and operation-level; arbitrary `Exception.Message` is not exposed.
- `Report` isolates editor/reporting exceptions so UI reporting cannot replace the operation result.
- Captured-document editor output may be attempted best-effort, but process-wide `PaletteCoordinator.SetStatus` is permitted only when `ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)` at the reporting boundary.
- The same fence applies to successful status publication after native/semantic commit. A late UI-affinity loss must not be reinterpreted as rollback of an already committed footing.
- `PlaceActiveSingleFootingAt` remains exception-transparent to its caller.
- `RequireCurrentContext`, prompt cancellation/iteration, `ProjectStateSnapshot`, native transaction ownership, generated-geometry replacement/ownership and rollback/erase behavior remain unchanged.
- No event subscriptions, dispatcher/deferred work, extra CAD transactions, project creation, or rollback weakening are introduced by this carrier.

## Deterministic validation

`python scripts/preflight-single-footing-status-boundary.py`

Then run the repository aggregate feature guards, deterministic smoke suite and admitted-reference BricsCAD V25 compile through Shared CI. The focused guard intentionally fails the historical source shape before the production fix.

## Self-review matrix

Review at least: MDI A→B and A→B→A around reporting; stale/disposed captured `Document`; reporting exceptions; process-wide Palette publication; prompt cancel/None/non-OK exits; project/family/dimension changes between picks; native commit followed by selection/regen/status failures; rollback restore + source erase partial failure; one-shot bridge exception transparency; absence of new subscriptions/deferred callbacks; and compatibility with the V25 host API surface.

## Runtime classification

Source/static guards, deterministic smoke and admitted-reference V25 compile are `REMOTE_SAFE`. Real BricsCAD V25 Single Footing interaction, MDI timing and injected native failure/rollback scenarios are `LOCAL_ONLY / NO_RESULT` until executed in a licensed host. Do not infer or fabricate runtime PASS from hosted CI.
