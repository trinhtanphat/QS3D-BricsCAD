# BLT3D parity P3 — BIM authoring / editing / modeling

## Scope

This carrier records source-proven command wiring for existing QS3D/BricsCAD workflows. It does not add a second CAD engine, duplicate Ribbon implementation, or parallel semantic database.

The P3 feature IDs covered here are:

- `bim.authoring`
- `draw`
- `tool.editing`
- `modeling`

The maximum evidence stage claimed by this carrier is `CommandWired`. The parity manifest must remain `# catalog-complete=false`.

`SemanticBehaviorPass`, `SaveReopenPass`, and `V25V26ParityPass` require later behavioral/runtime carriers and are intentionally not claimed here.

## Canonical workflow contracts

All four workflows use the existing `FeatureId` and `ParityWorkflowRegistry` contracts and are UI semantic-mutation workflows.

- `bim.authoring` requires ActiveDocument, Project, Zone, Floor, Family, AtomicMutation, and Audit.
- `draw` requires ActiveDocument, Project, AtomicMutation, and Audit.
- `tool.editing` requires ActiveDocument, Project, Selection, AtomicMutation, and Audit.
- `modeling` requires ActiveDocument, Project, Selection, AtomicMutation, and Audit.

These requirements are the canonical parity contract. This carrier does not assert that every existing native command has already passed semantic behavior, undo, save/reopen, or licensed V25/V26 runtime qualification.

## Existing source evidence

P3 reuses current QS3D/BricsCAD command and Ribbon implementations as read-only evidence inputs. The following existing guards remain authoritative evidence for command presence/routing and bounded host behavior:

- `scripts/preflight-blt3d-bim-workspace.py`
- `scripts/preflight-blt-draw-ribbon.py`
- `scripts/preflight-direct-draw-authoring-integration.py`
- `scripts/preflight-modeling-ribbon-functions.py`
- `scripts/preflight-blt3d-tool-ribbon.py`

The Direct Draw integration guard explicitly keeps licensed DrawJig/runtime behavior outside static evidence. The MODELING and TOOL guards prove routed command surfaces and bounded safety contracts but do not elevate this carrier beyond `CommandWired`.

## Merge gate

Merge only when the exact candidate remains within Issue #6393 Reservation-v2 paths, the new P3 guard and existing source guards pass, deterministic Core smoke is green, protected PR preflight/core are terminal green, review threads are resolved, and current-main freshness is satisfied. No force push or bypass is permitted.
