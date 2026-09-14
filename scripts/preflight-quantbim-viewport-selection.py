from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / "src/QS3D.Core/BenchmarkParity/QsQuantBimViewportSelection.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/QsQuantBimViewportSelectionSmoke.cs"
text = source.read_text(encoding="utf-8")
smoke_text = smoke.read_text(encoding="utf-8")

for token in (
    "sealed class QuantBimStandaloneViewportSelection",
    "QuantBimStandaloneIfcSession",
    "QuantBimStandaloneScreenRayProjector",
    "QuantBimStandaloneScenePicker",
    "new IfcSelectionSet(\"Viewport selection\"",
    "_session.PropertyTree",
    "_session.Takeoff",
    "_session.BuildBoq",
    "scene.Revision",
    "GeometryReference",
    "navigation-only",
    "never becomes authoritative quantity evidence",
):
    if token not in text:
        raise SystemExit(f"QuantBIM viewport-selection preflight: missing required token: {token}")

for forbidden in (
    "Bricscad", "BricsCAD", "Autodesk.AutoCAD", "Teigha", "HostApplicationServices",
    "System.Windows.Forms", "PresentationCore", "WindowsBase", "System.Windows"
):
    if forbidden in text:
        raise SystemExit(f"QuantBIM viewport-selection preflight: host/UI dependency found: {forbidden}")

for token in (
    "[ModuleInitializer]",
    "center viewport hit",
    "picked guid",
    "picked geometry reference",
    "single selection",
    "property projection",
    "qto projection",
    "boq projection",
    "viewport miss",
    "miss selection empty",
):
    if token not in smoke_text:
        raise SystemExit(f"QuantBIM viewport-selection preflight: smoke coverage missing token: {token}")

print("QuantBIM standalone viewport-selection preflight passed.")
