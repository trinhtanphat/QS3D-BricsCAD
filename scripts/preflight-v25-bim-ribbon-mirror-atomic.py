#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/BltBimRibbonMirrorAugmenter.cs"
text = SOURCE.read_text(encoding="utf-8")

if "if (mirrored == null) continue;" in text or "return null;\n        }\n\n        private static void CopyRasterizedImageProperty" in text:
    raise SystemExit("FAIL BIM ribbon mirror atomic guard: unsupported item shapes can still be silently omitted")

required = {
    "staged mirrors": "var stagedPanels = new List<object>(PanelSpecs.Length);",
    "stage before destructive replacement": "stagedPanels.Add(BuildMirroredPanel(sources[index], PanelSpecs[index]));",
    "publish staged panels": "foreach (var panel in stagedPanels)\n                    Add(bimPanels, panel);",
    "rollback failed publication": "RemoveQs3dOwnedBimPanels(bimPanels);",
    "unsupported item fails closed": "throw new InvalidOperationException(\"Unsupported QS3D Ribbon item type: \" + typeName + \".\");",
}
missing = [name for name, token in required.items() if token not in text]
if missing:
    raise SystemExit("FAIL BIM ribbon mirror atomic guard: missing " + ", ".join(missing))

stage = text.index("var stagedPanels = new List<object>(PanelSpecs.Length);")
remove = text.index("RemoveQs3dOwnedBimPanels(bimPanels);", stage)
publish = text.index("foreach (var panel in stagedPanels)", remove)
initialized = text.index("_initialized = true;", publish)
if not stage < remove < publish < initialized:
    raise SystemExit("FAIL BIM ribbon mirror atomic guard: staging/publication ordering regressed")

print("PASS V25 BIM ribbon mirror stages completely, rejects unknown shapes, and rolls back partial publication")
