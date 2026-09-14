from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/QsQuantBimSelectionInterchangeV2.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/QsQuantBimSelectionInterchangeV2Smoke.cs").read_text(encoding="utf-8")

for token in (
    "QS3D-QUANTBIM-SELECTION-INTERCHANGE/2",
    "QuantBimSelectionInterchangeV2Codec",
    "QuantBimSelectionInterchangeCodec.Encode",
    "QuantBimSelectionInterchangeCodec.Decode",
    "QuantBimSelectionInterchangeCodec.HashUtf8",
    "EnvelopeSha256",
    "whole-envelope integrity check failed",
    "non-canonical V1 payload",
):
    if token not in source:
        raise SystemExit(f"QuantBIM interchange V2 preflight: missing production token: {token}")

for forbidden in ("Bricscad", "BricsCAD", "Autodesk.AutoCAD", "System.Windows", "System.Windows.Forms"):
    if forbidden in source:
        raise SystemExit(f"QuantBIM interchange V2 preflight: host dependency leaked into Core: {forbidden}")

for token in (
    "metadata tamper bound by whole-envelope digest",
    "whole-envelope digest tamper",
    "deterministic V2 re-encode",
    "non-canonical line endings",
    "missing terminal newline",
):
    if token not in smoke:
        raise SystemExit(f"QuantBIM interchange V2 preflight: missing smoke token: {token}")

print("QuantBIM selection interchange V2 preflight passed.")
