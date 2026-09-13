# QuantBIM standalone scene picking

## Purpose

`QuantBimStandaloneScenePicker` provides a renderer-neutral 3D hit-test boundary for the shared QuantBIM scene contract. A Windows renderer can convert a screen-space click into a world-space ray and ask Core which IFC scene node was hit, without recreating IFC identity mapping in UI code.

The picker consumes the existing `IfcStandaloneScene` generated from the generation-fenced IFC session and returns the nearest IFC GUID, geometry reference, world-space hit point, ray distance and triangle index.

## Determinism and safety

- the input ray is normalized in Core and rejects zero/non-finite direction;
- IFC GUIDs must be unique case-insensitively in the scene;
- each triangle is validated and degenerate geometry fails closed;
- intersections behind the ray origin are ignored;
- the nearest hit is selected deterministically;
- equal-distance hits on different IFC identities are rejected as ambiguous instead of silently choosing by traversal order;
- a geometric miss returns `null`;
- no BricsCAD, AutoCAD, WinForms, WPF or renderer dependency is introduced into `QS3D.Core`.

## Quantity/evidence boundary

Picking is visualization/navigation behavior only. The returned `GeometryReference` is useful for trace/navigation identity, but triangle geometry and hit coordinates are never promoted into authoritative measurement evidence. Quantity takeoff, BOQ and publication continue to use the canonical IFC QTO/evidence workflow.

## Host integration

A standalone host should:

1. open/parse IFC through `QuantBimStandaloneIfcSession`;
2. build a scene from the same generation;
3. derive a world-space ray from its concrete camera/viewport implementation;
4. call `QuantBimStandaloneScenePicker.PickNearest`;
5. convert the returned GUID into the canonical `IfcSelectionSet` and reuse existing property/QTO/BOQ workflows.

Screen-to-ray projection remains a renderer/camera concern because Core intentionally has no concrete UI or graphics dependency.

## Validation

`QsQuantBimScenePickingSmoke` is module-initialized and covers nearest-hit ordering, ray normalization, miss behavior, duplicate GUID rejection, degenerate triangle rejection and ambiguous equal-distance identity rejection. `scripts/preflight-quantbim-scene-picking.py` guards the architecture and smoke contract.
