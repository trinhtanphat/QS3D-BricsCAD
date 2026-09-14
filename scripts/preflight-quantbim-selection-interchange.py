from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/QsQuantBimSelectionInterchange.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/QsQuantBimSelectionInterchangeSmoke.cs").read_text(encoding="utf-8")

for token in (
    "QS3D-QUANTBIM-SELECTION-INTERCHANGE/1",
    "QuantBimSelectionExportBundle",
    "QuantBimSelectionInterchangePackage",
    "QuantBimSelectionInterchangeCodec",
    "SHA256.Create()",
    "StrictUtf8",
    "GuidCount",
    "payload integrity check failed",
):
    if token not in source:
        raise SystemExit(f"QuantBIM selection interchange preflight: missing production token: {token}")

for forbidden in ("Bricscad", "BricsCAD", "Autodesk.AutoCAD", "System.Windows", "System.Windows.Forms"):
    if forbidden in source:
        raise SystemExit(f"QuantBIM selection interchange preflight: host dependency leaked into Core: {forbidden}")

for token in (
    "deterministic re-encode",
    "tampered BOQ hash",
    "non-canonical line endings",
    "missing terminal newline",
):
    if token not in smoke:
        raise SystemExit(f"QuantBIM selection interchange preflight: missing smoke token: {token}")

print("QuantBIM selection interchange preflight passed.")
