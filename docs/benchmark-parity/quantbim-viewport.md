# QuantBIM standalone viewport framing

Issue #6887 adds a host-neutral viewport projection over the existing `IfcStandaloneScene` so a Windows standalone shell can frame the complete IFC model or the current selection without requiring BricsCAD.

## Contract

`QuantBimStandaloneViewport` computes deterministic finite model bounds, center, bounding-sphere radius, camera distance and near/far clipping planes. `FitAll` consumes every scene node. `FitSelection` consumes only nodes already marked selected by the existing standalone selection pipeline and fails closed when the selection is empty.

The projection validates field of view, node/mesh presence and clipping order. Degenerate point-like geometry receives a small positive radius floor so the standalone camera remains usable without fabricating model dimensions.

## Architecture boundary

The viewport module depends only on `QS3D.Core.BenchmarkParity` scene DTOs. It must not reference BricsCAD, AutoCAD, Teigha or any native CAD host. A desktop renderer may translate the returned frame into its own camera API, but host-specific camera objects belong outside Core.

Scene meshes remain visualization/navigation artifacts. Authoritative quantities, BOQ rows and evidence continue to come from the standalone IFC/QTO workbench and publication contracts; viewport bounds or reconstructed mesh dimensions must never be promoted into quantity evidence.

## Compatibility and validation

The implementation uses APIs compatible with the existing `netstandard2.0` Core target. `QsQuantBimViewportSmoke` is module-initialized and exercises fit-all, fit-selection, deterministic centers, clipping order, degenerate geometry, empty selection/scene refusal and invalid field-of-view refusal. `scripts/preflight-quantbim-viewport.py` is auto-discovered by the repository feature-source preflight and guards the Core-only architecture boundary.
