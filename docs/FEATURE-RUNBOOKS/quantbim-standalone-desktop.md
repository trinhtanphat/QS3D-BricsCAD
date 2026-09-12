# QuantBIM standalone Windows IFC-QTO workbench

## Scope

`src/QS3D.QuantBim.Desktop` is the first concrete Windows shell for the renderer-neutral QuantBIM contracts in `QS3D.Core`. It is intentionally independent of BricsCAD V25/V26 and references only `QS3D.Core`.

The desktop workflow can open IFC STEP through `IfcStepStandaloneSource`, browse/filter elements by entity, storey, type and classification, create a visible selection, inspect the existing property tree, calculate QTO/BOQ for the selection, export deterministic CSV, and drive the existing `QuantBimStandaloneViewportHost` through a WinForms software viewport.

## Viewport

`SoftwareViewport` consumes the canonical `IfcStandaloneScene` / `IfcSceneMesh` triangle contract and supports fit/focus, orbit, pan, zoom and isometric/top/front/right views. Selected nodes are rendered distinctly from visible unselected nodes. This keeps renderer concerns outside `QS3D.Core` and proves the host is optional rather than BricsCAD-coupled.

### Current geometry boundary

The STEP ingestion layer currently resolves semantic/product identity, relationships, property sets and base quantities but does not tessellate IFC representation items. Until a production IFC tessellator is admitted, the desktop app uses `SemanticProxyGeometryResolver`: a deterministic box mesh derived from the element's geometry reference. **Proxy geometry is navigation/selection UI evidence only and must not be treated as geometric measurement evidence.** Quantity values continue to come from parsed IFC quantities/evidence, never from proxy mesh dimensions.

A later parity slice should replace the proxy resolver with a production IFC geometry resolver while preserving the `IIfcGeometryResolver` boundary and all existing workbench/QTO behavior.

## Build and package

On a .NET 8 SDK:

```powershell
dotnet build src/QS3D.QuantBim.Desktop/QS3D.QuantBim.Desktop.csproj -c Release
dotnet run --project src/QS3D.QuantBim.Desktop/QS3D.QuantBim.Desktop.csproj -- --self-test
dotnet publish src/QS3D.QuantBim.Desktop/QS3D.QuantBim.Desktop.csproj -c Release -r win-x64 --no-self-contained
```

The project is `net8.0-windows`, WinForms-enabled, `win-x64`, framework-dependent and configured for single-file publish. `EnableWindowsTargeting=true` allows source/build validation on non-Windows SDK hosts while native UI behavior remains a Windows qualification concern.

## Verification

Run `python scripts/preflight-quantbim-desktop.py`. It fails closed if required desktop files disappear, packaging/Windows metadata is removed, QTO/property/filter/viewport wiring is lost, or BricsCAD/Teigha dependencies enter the standalone project.

The executable `--self-test` validates deterministic proxy mesh generation and triangle integrity without opening the UI. Native interactive acceptance (DPI, resize, file dialogs, long-running model responsiveness and GPU/renderer replacement) remains a separate Windows runtime qualification and is not inferred from hosted CI.
