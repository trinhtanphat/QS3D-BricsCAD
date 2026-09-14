from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsQuantBimSelectionInterchangeV2Admission.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsQuantBimSelectionInterchangeV2AdmissionSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "QuantBimSelectionInterchangeV2Codec.Decode(encoded)",
    "session.Path, package.DocumentPath",
    "session.Revision, package.Revision",
    "matches.Count != 1",
    "new IfcSelectionSet(package.SelectionName, canonicalGuids)",
)
required_smoke = (
    "stale revision",
    "foreign path",
    "unknown selection guid",
    "duplicate IFC GlobalId parser invariant",
)
for marker in required_source:
    if marker not in source:
        raise SystemExit(f"missing QuantBIM V2 generation-admission source marker: {marker}")
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit(f"missing QuantBIM V2 generation-admission smoke marker: {marker}")

for forbidden in ("Bricscad.", "Autodesk.AutoCAD", "System.Windows", "System.Windows.Forms"):
    if forbidden in source:
        raise SystemExit(f"Core QuantBIM V2 admission must remain host-neutral: {forbidden}")

print("QuantBIM V2 generation admission preflight PASS")
