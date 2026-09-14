from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / "src/QS3D.Core/BenchmarkParity/QsQuantBimViewportMultiSelection.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/QsQuantBimViewportMultiSelectionSmoke.cs"
source_text = source.read_text(encoding="utf-8")
smoke_text = smoke.read_text(encoding="utf-8")

for token in (
    "sealed class QuantBimViewportMultiSelection",
    "QuantBimSelectionInteractionMode",
    "Replace",
    "Add",
    "Toggle",
    "QuantBimViewportSelectionResult",
    "IfcSelectionSet",
    "StringComparer.OrdinalIgnoreCase",
    "clearOnMiss",
):
    if token not in source_text:
        raise SystemExit(f"QuantBIM viewport multi-selection preflight: missing required token: {token}")

for forbidden in (
    "Bricscad", "BricsCAD", "Autodesk.AutoCAD", "Teigha", "HostApplicationServices",
    "System.Windows.Forms", "PresentationCore", "WindowsBase", "System.Windows"
):
    if forbidden in source_text:
        raise SystemExit(f"QuantBIM viewport multi-selection preflight: host/UI dependency found: {forbidden}")

for token in (
    "[ModuleInitializer]",
    "add is case-insensitive and idempotent",
    "toggle removes existing GUID",
    "replace yields single picked GUID",
    "miss can preserve selection",
    "miss can clear selection explicitly",
    "invalid interaction mode",
):
    if token not in smoke_text:
        raise SystemExit(f"QuantBIM viewport multi-selection preflight: smoke coverage missing token: {token}")

print("QuantBIM viewport multi-selection preflight passed.")
