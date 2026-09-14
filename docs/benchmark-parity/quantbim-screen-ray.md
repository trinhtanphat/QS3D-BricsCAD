# QuantBIM standalone screen-space ray projection

Issue: #6932  
Lane: P1 QuantBIM standalone viewer/navigation

## Purpose

The standalone IFC workbench already owns renderer-neutral camera navigation and renderer-neutral world-space IFC scene picking. This boundary joins those capabilities without making a Windows UI toolkit, BricsCAD, AutoCAD, or a specific 3D renderer authoritative for IFC selection identity.

`QuantBimStandaloneScreenRayProjector` converts a viewport pointer coordinate and `IfcCameraNavigationState` into the existing normalized `IfcSceneRay`. A host can then pass that ray to `QuantBimStandaloneScenePicker` and map the hit IFC GUID back into the standalone selection/property/QTO workflow.

## Projection convention

The projector uses perspective projection with a vertical field of view. Viewport coordinates are continuous edge coordinates with origin at the top-left: `(0, 0)` is the top-left edge, `(width, height)` is the bottom-right edge, and `(width / 2, height / 2)` lies exactly on the optical axis. This convention avoids renderer-specific half-pixel assumptions.

Horizontal projection is derived from `viewportWidth / viewportHeight`; no independent aspect ratio is accepted. The camera basis is reconstructed from current camera position, target and up vector, then orthogonalized before the ray is created.

## Fail-closed behavior

Projection rejects non-finite inputs, non-positive viewport dimensions, pointers outside the admitted viewport rectangle, vertical FOV outside 1–179 degrees, invalid aspect ratios, and degenerate camera basis vectors. `IfcSceneRay` remains responsible for final normalized-ray admission.

The source has no BricsCAD/AutoCAD/WPF/WinForms dependency. A Windows standalone renderer owns only event-coordinate capture and rendering; Core owns the deterministic coordinate-to-ray contract.

## Quantity/evidence boundary

A projected ray and reconstructed mesh are visualization/navigation mechanisms only. A hit may select an IFC identity, but mesh intersections and hit distances are never authoritative quantity evidence. Quantity takeoff, BOQ and evidence publication continue to use the generation-fenced IFC/QTO semantic sources already present in the standalone workbench.

## Validation

`QsQuantBimScreenRaySmoke` covers optical-axis projection, viewport corners, FOV widening, aspect-ratio influence, camera-origin preservation, invalid viewport size, out-of-viewport pointers, non-finite pointers and invalid FOV. `scripts/preflight-quantbim-screen-ray.py` also enforces the architecture boundary and required smoke surface.
