from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / "src/QS3D.Core/BenchmarkParity/QsQuantBimScreenRay.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/QsQuantBimScreenRaySmoke.cs"
text = source.read_text(encoding="utf-8")
smoke_text = smoke.read_text(encoding="utf-8")

for token in (
    "sealed class QuantBimStandaloneScreenRayProjector",
    "ProjectPerspective(",
    "IfcCameraNavigationState camera",
    "new IfcSceneRay(",
    "Continuous viewport convention",
    "top-left edge",
    "verticalFieldOfViewDegrees",
    "Viewport pointer must lie inside",
    "visualization/navigation only",
    "never becomes authoritative quantity evidence",
):
    if token not in text:
        raise SystemExit(f"QuantBIM screen-ray preflight: missing required token: {token}")

for forbidden in (
    "Bricscad", "BricsCAD", "Autodesk.AutoCAD", "Teigha", "HostApplicationServices",
    "System.Windows.Forms", "PresentationCore", "WindowsBase", "System.Windows"
):
    if forbidden in text:
        raise SystemExit(f"QuantBIM screen-ray preflight: host/UI dependency found: {forbidden}")

for token in (
    "[ModuleInitializer]",
    "center forward",
    "left points left",
    "top points up",
    "right points right",
    "bottom points down",
    "fov widens ray",
    "aspect affects horizontal ray",
    "zero viewport",
    "outside viewport",
    "non-finite pointer",
    "invalid fov",
):
    if token not in smoke_text:
        raise SystemExit(f"QuantBIM screen-ray preflight: smoke coverage missing token: {token}")

print("QuantBIM standalone screen-ray preflight passed.")
