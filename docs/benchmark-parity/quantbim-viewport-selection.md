# QuantBIM standalone viewport selection

Issue: #6945  
Lane: P1 QuantBIM standalone mode

## Purpose

The standalone IFC workbench already has generation-bound semantic/QTO parsing, reconstructed scene geometry, renderer-neutral camera/navigation, screen-to-world ray projection, and deterministic scene picking. This boundary composes those existing capabilities into one selection workflow so a Windows renderer does not invent its own IFC identity or quantity semantics.

`QuantBimStandaloneViewportSelection` builds the scene from the exact bound `QuantBimStandaloneIfcSession`, projects the viewport pointer with `QuantBimStandaloneScreenRayProjector`, resolves the nearest IFC hit with `QuantBimStandaloneScenePicker`, and converts that hit into an `IfcSelectionSet`. Property tree, takeoff and BOQ are then obtained through the existing session/workbench APIs.

## Generation and identity rules

- The scene revision must equal the exact session revision.
- A picked GUID must resolve to exactly one element in the bound session document.
- The hit geometry reference must equal the bound element geometry reference.
- A miss yields an empty deterministic selection/properties/QTO/BOQ result.
- Any identity mismatch fails closed rather than silently selecting or recomputing against another generation.

## Architecture boundary

The source is shared Core only. It has no BricsCAD, AutoCAD, WPF or WinForms dependency. A future Windows standalone renderer is responsible for drawing and pointer events only; Core remains authoritative for screen-ray convention, scene hit identity and conversion to the semantic selection/QTO workflow.

Reconstructed mesh, ray intersection, hit point, hit distance and triangle identity are visualization/navigation evidence only. They never become authoritative quantity evidence. Quantity takeoff and BOQ remain sourced from the generation-fenced IFC semantic/QTO document.

## Validation

`QsQuantBimViewportSelectionSmoke` exercises an actual IFC STEP session, reconstructed geometry, center-screen hit, GUID/geometry-reference identity, semantic selection/property projection, QTO/BOQ projection and deterministic miss behavior. `scripts/preflight-quantbim-viewport-selection.py` enforces the composition and host-independence boundary.
