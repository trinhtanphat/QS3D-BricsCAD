from pathlib import Path
import sys

path = Path("src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs")
text = path.read_text(encoding="utf-8")
anchor = "foreach (var update in pending)"
start = text.find(anchor)
end = text.find("if (pending.Count > 0) project.Touch();", start)
if start < 0 or end < 0:
    print("[FAIL] WallSolidBuilder semantic commit block not found")
    sys.exit(1)
block = text[start:end]
commit = block.find("GeneratedGeometryService.CommitReplacement")
required = [
    'update.Element.Properties["LengthM"]',
    'update.Element.Properties["ThicknessM"]',
    'update.Element.Properties["HeightM"]',
    'CadElementVerticalPlacement.CommitSnapshot',
]
if commit < 0 or any(block.find(token) < 0 for token in required):
    print("[FAIL] WallSolidBuilder generated-solid commit contract incomplete")
    sys.exit(1)
if any(block.find(token) > commit for token in required):
    print("[FAIL] WallSolidBuilder mutates geometry semantics after CommitReplacement, re-staling the new generated solid")
    sys.exit(1)
print("PASS: wall semantic geometry and vertical snapshot are finalized before CommitReplacement clears stale state on the new solid.")
