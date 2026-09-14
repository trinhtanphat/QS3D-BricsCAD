from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / "src/QS3D.Core/BenchmarkParity/QsQuantBimScenePicking.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/QsQuantBimScenePickingSmoke.cs"
text = source.read_text(encoding="utf-8")
smoke_text = smoke.read_text(encoding="utf-8")

for token in (
    "sealed class IfcSceneRay",
    "sealed class IfcScenePickHit",
    "sealed class QuantBimStandaloneScenePicker",
    "PickNearest(IfcStandaloneScene scene, IfcSceneRay ray)",
    "unique IFC guid",
    "Scene triangle is degenerate",
    "Scene pick is ambiguous between IFC elements",
    "visualization/navigation evidence only",
    "never becomes authoritative quantity evidence",
):
    if token not in text:
        raise SystemExit(f"QuantBIM scene-picking preflight: missing required token: {token}")

for forbidden in ("Bricscad", "BricsCAD", "Autodesk.AutoCAD", "Teigha", "HostApplicationServices", "System.Windows.Forms", "PresentationCore"):
    if forbidden in text:
        raise SystemExit(f"QuantBIM scene-picking preflight: host/UI dependency found: {forbidden}")

for token in (
    "[ModuleInitializer]",
    "nearest guid",
    "miss returns null",
    "zero direction",
    "duplicate guid",
    "degenerate triangle",
    "ambiguous equal-distance identities",
):
    if token not in smoke_text:
        raise SystemExit(f"QuantBIM scene-picking preflight: smoke coverage missing token: {token}")

print("QuantBIM standalone scene-picking preflight passed.")
