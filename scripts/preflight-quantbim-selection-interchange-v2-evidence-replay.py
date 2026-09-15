from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsQuantBimSelectionInterchangeV2EvidenceReplay.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsQuantBimSelectionInterchangeV2EvidenceReplaySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

for marker in (
    "_admission.Admit(session, encoded)",
    "_publisher.Publish(session, admitted.Selection)",
    "RequireEqual(imported.BoqCsv, canonical.BoqCsv",
    "RequireEqual(imported.EvidenceCsv, canonical.EvidenceCsv",
):
    if marker not in source:
        raise SystemExit(f"missing QuantBIM V2 evidence-replay source marker: {marker}")

for marker in (
    "canonical BOQ replay",
    "canonical evidence replay",
    "internally valid but semantically tampered BOQ evidence",
):
    if marker not in smoke:
        raise SystemExit(f"missing QuantBIM V2 evidence-replay smoke marker: {marker}")

for forbidden in ("Bricscad.", "Autodesk.AutoCAD", "System.Windows", "System.Windows.Forms"):
    if forbidden in source:
        raise SystemExit(f"Core QuantBIM V2 evidence replay must remain host-neutral: {forbidden}")

print("QuantBIM V2 evidence replay preflight PASS")
