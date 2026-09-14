from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / "src/QS3D.Core/BenchmarkParity/QsQuantBimNavigation.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/QsQuantBimNavigationSmoke.cs"
text = source.read_text(encoding="utf-8")
smoke_text = smoke.read_text(encoding="utf-8")

for token in (
    "sealed class IfcCameraNavigationState",
    "sealed class QuantBimStandaloneNavigation",
    "Focus(IfcViewportFrame frame)",
    "Orbit(IfcCameraNavigationState state",
    "Pan(IfcCameraNavigationState state",
    "Dolly(IfcCameraNavigationState state",
    "PoleLimitDegrees",
    "Camera clipping range is invalid",
    "visualization/navigation only",
    "never becomes authoritative quantity evidence",
):
    if token not in text:
        raise SystemExit(f"QuantBIM navigation preflight: missing required token: {token}")

for forbidden in (
    "Bricscad", "BricsCAD", "Autodesk.AutoCAD", "Teigha", "HostApplicationServices",
    "System.Windows.Forms", "PresentationCore", "WindowsBase"
):
    if forbidden in text:
        raise SystemExit(f"QuantBIM navigation preflight: host/UI dependency found: {forbidden}")

for token in (
    "[ModuleInitializer]",
    "focus target",
    "orbit preserves distance",
    "pan preserves distance",
    "dolly minimum clamp",
    "orbit pole clamp",
    "non-finite orbit",
    "degenerate view",
    "parallel up",
):
    if token not in smoke_text:
        raise SystemExit(f"QuantBIM navigation preflight: smoke coverage missing token: {token}")

print("QuantBIM standalone camera-navigation preflight passed.")
