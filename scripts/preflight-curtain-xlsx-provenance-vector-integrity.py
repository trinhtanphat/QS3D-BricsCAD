#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CurtainWallXlsxExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CurtainWallXlsxProvenanceVectorIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
required_source = [
    "Source Handles count must match Element IDs count",
    "row.SourceHandles.Count != row.ElementIds.Count",
]
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Curtain XLSX provenance-vector guard failed: production cardinality invariant is missing: " + ", ".join(missing))

if not SMOKE.exists():
    raise SystemExit("Curtain XLSX provenance-vector guard failed: deterministic smoke is missing")
smoke = SMOKE.read_text(encoding="utf-8")
for token in ("CurtainWallXlsxProvenanceVectorIntegritySmoke", "short source-handle vector", "long source-handle vector", "matched provenance vector"):
    if token not in smoke:
        raise SystemExit("Curtain XLSX provenance-vector guard failed: smoke contract token missing: " + token)

print("PASS curtain XLSX provenance vector integrity source guard")
