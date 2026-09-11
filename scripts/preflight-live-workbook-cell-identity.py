#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "BenchmarkParity" / "QsLiveWorkbook2.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "QsLiveWorkbookApiSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

legacy_cell_key = 'WorkbookId + "|" + Sheet + "|" + Cell'
legacy_source_fingerprint = 'y.Revision + "|" + y.Quantity.ToString'

assert legacy_cell_key not in source, "legacy delimiter-only workbook cell key remains"
assert legacy_source_fingerprint not in source, "legacy delimiter-only source fingerprint remains"
assert "Segment(WorkbookId)" in source and "Segment(Sheet)" in source and "Segment(Cell)" in source
assert "y.Fingerprint" in source
assert "CollisionSafeWorkbookIdentity();" in smoke
assert '"A|B", "C", "D"' in smoke
assert '"A", "B|C", "D"' in smoke
assert '"R|1", 2d, "E"' in smoke
assert '"R", 1d, "2|E"' in smoke

print("PASS: live workbook identities use collision-safe segment framing with hostile-token regression coverage")
