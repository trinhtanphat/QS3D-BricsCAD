from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / "src/QS3D.Core/BenchmarkParity/QsQuantBimViewport.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/QsQuantBimViewportSmoke.cs"
text = source.read_text(encoding="utf-8")
smoke_text = smoke.read_text(encoding="utf-8")

required = [
    "sealed class QuantBimStandaloneViewport",
    "FitAll(IfcStandaloneScene scene)",
    "FitSelection(IfcStandaloneScene scene)",
    "IfcViewportBounds",
    "IfcViewportFrame",
    "Viewport selection is empty",
    "Viewport field of view must be finite",
    "Geometry is used only for navigation/visualization",
]
for token in required:
    if token not in text:
        raise SystemExit(f"QuantBIM viewport preflight: missing required token: {token}")

for forbidden in ("Bricscad", "BricsCAD", "Autodesk.AutoCAD", "Teigha", "HostApplicationServices"):
    if forbidden in text:
        raise SystemExit(f"QuantBIM viewport preflight: host-specific dependency found: {forbidden}")

for token in ("[ModuleInitializer]", "FitAll(scene)", "FitSelection(scene)", "degenerate radius floor", "invalid fov"):
    if token not in smoke_text:
        raise SystemExit(f"QuantBIM viewport preflight: smoke coverage missing token: {token}")

print("QuantBIM standalone viewport preflight passed.")
