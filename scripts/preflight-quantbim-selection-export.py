from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/QsQuantBimSelectionExport.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/QsQuantBimSelectionExportSmoke.cs").read_text(encoding="utf-8")

for token in (
    "QuantBimSelectionExportBundle",
    "QuantBimSelectionExportPublisher",
    "QuantBimTraceableTakeoffEngine",
    "session.ValidateGeneration",
    "RequireEquivalentBoq",
    "session.ExportCsv",
    "ExportEvidenceCsv",
):
    if token not in source:
        raise SystemExit(f"QuantBIM selection export preflight: missing production token: {token}")

for forbidden in ("Bricscad", "BricsCAD", "Autodesk.AutoCAD", "System.Windows", "System.Windows.Forms"):
    if forbidden in source:
        raise SystemExit(f"QuantBIM selection export preflight: host dependency leaked into Core: {forbidden}")

for token in (
    "stale generation",
    "missing selected guid",
    "BOQ CSV header",
    "evidence CSV header",
):
    if token not in smoke:
        raise SystemExit(f"QuantBIM selection export preflight: missing smoke token: {token}")

print("QuantBIM selection export preflight passed.")
