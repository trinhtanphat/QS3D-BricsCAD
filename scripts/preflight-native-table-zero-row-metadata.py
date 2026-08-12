#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "ProjectOwnedNativeTableArtifactService.cs"

source = SOURCE.read_text(encoding="utf-8")

snapshot_contract = "if (snapshot.Rows.Count == 0 || snapshot.Rows.Count > MaxRows)"
persisted_contract = "rows <= 0 || rows > MaxRows"
legacy_contract = "rows < 0 || rows > MaxRows"

if snapshot_contract not in source:
    raise SystemExit("missing native documentation Table snapshot 1..MaxRows invariant")
if persisted_contract not in source:
    raise SystemExit("persisted native Table RowCount does not reject zero")
if legacy_contract in source:
    raise SystemExit("legacy zero-row persisted metadata acceptance remains")

row_persist = "project.Metadata[definition.RowCountKey] = snapshot.Rows.Count.ToString(CultureInfo.InvariantCulture);"
if row_persist not in source:
    raise SystemExit("native Table builder no longer persists snapshot row count through the expected contract")

print("native table zero-row metadata preflight: PASS")
