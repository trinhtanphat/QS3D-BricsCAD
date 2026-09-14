# QuantBIM standalone camera navigation

## Purpose

`QuantBimStandaloneNavigation` provides the renderer-neutral camera state used by a Windows standalone IFC-QTO host. It sits above the existing generation-fenced IFC session, canonical reconstructed scene and `QuantBimStandaloneViewport` framing logic, so concrete renderers do not invent incompatible orbit/pan/zoom semantics.

## Boundary

Core owns deterministic camera/navigation math only. A Windows renderer remains responsible for windowing, GPU/device creation, screen-to-world projection, pointer gestures and drawing. BricsCAD/AutoCAD, WinForms, WPF and renderer assemblies are deliberately absent from `QS3D.Core`.

The flow is:

1. open IFC through the canonical standalone session;
2. build the canonical `IfcStandaloneScene` from that same generation;
3. obtain `FitAll` or `FitSelection` from `QuantBimStandaloneViewport`;
4. create/reset camera state with `QuantBimStandaloneNavigation.Focus`;
5. map host gestures to `Orbit`, `Pan` and `Dolly`;
6. use the existing scene picker to convert renderer rays back into canonical IFC GUID selection.

## Navigation semantics

- `Focus` targets the exact viewport-frame center and begins from a deterministic isometric direction;
- `Orbit` preserves target and distance, applies yaw plus pitch, and clamps pitch to ±89 degrees so the camera basis cannot collapse at a pole;
- `Pan` translates position and target together in normalized camera right/up axes;
- `Dolly` changes target distance multiplicatively and clamps it to radius-aware minimum/maximum limits;
- clipping planes are regenerated from current distance and scene radius after every operation;
- all public numeric input must be finite and invalid/degenerate camera states fail closed.

## Quantity/evidence authority

Camera state, reconstructed triangle meshes, view bounds, hit points and navigation transforms are visualization/navigation data only. They never become authoritative quantity evidence. Quantity takeoff, BOQ, publication and interchange continue to use the canonical IFC semantic/QTO/evidence pipeline.

## Compatibility

The implementation uses `System` math only and remains compatible with the host-neutral Core target. No BricsCAD/AutoCAD or concrete Windows UI/rendering dependency is introduced. The standalone executable can therefore select its renderer independently while reusing the same deterministic navigation contract.

## Validation

`QsQuantBimNavigationSmoke` runs through a module initializer and covers focus, orbit distance preservation, pole clamping, pan, dolly/clamping, clipping and fail-closed invalid vectors/input. `scripts/preflight-quantbim-navigation.py` locks the expected architecture and prevents host/UI dependencies from entering this Core boundary.
