# QuantBIM standalone viewport multi-selection

This P1 boundary adds deterministic multi-selection interaction on top of the existing standalone screen-ray and scene-picking pipeline. It is renderer-neutral and lives in `QS3D.Core`, so a Windows UI host can remain optional and BricsCAD/AutoCAD assemblies are not required.

## Contract

`QuantBimViewportMultiSelection` reduces an existing `IfcSelectionSet` plus one canonical viewport pick into the next selection set. Supported modes are Replace, Add and Toggle. IFC GUID identity is case-insensitive, duplicate adds are idempotent, and output ordering remains deterministic through the existing `IfcSelectionSet` contract.

A viewport miss is explicit: callers choose whether the miss preserves the current selection or clears it. Invalid interaction modes fail closed. The reducer may also accept a picked GUID directly for deterministic host-independent testing.

## Architecture

The reducer does not perform ray projection, geometry intersection, property-tree construction, QTO, BOQ creation or evidence publication. Those responsibilities remain with the existing `QuantBimStandaloneScreenRayProjector`, `QuantBimStandaloneScenePicker`, `QuantBimStandaloneViewportSelection` and generation-bound IFC session/workbench APIs. Reconstructed scene geometry therefore remains visualization/navigation evidence and never becomes authoritative commercial quantity evidence.

A Windows standalone host can map ordinary click to Replace, modifier-click to Add/Toggle, and then pass the resulting canonical `IfcSelectionSet` back into existing property/QTO/BOQ/state workflows.

## Validation

Run `python scripts/preflight-quantbim-viewport-multiselection.py`, the `QS3D.Core.SmokeTests` project, and repository Shared/Hybrid CI. The Core implementation is REMOTE_SAFE; licensed BricsCAD behavior is not required for this standalone interaction boundary.
