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
    "publication boundary": "var publicationStarted = false;",
    "publication start marker": "publicationStarted = true;",
    "publish staged panels": "foreach (var panel in stagedPanels)\n                    Add(bimPanels, panel);",
    "rollback only after publication begins": "if (publicationStarted && bimPanels != null)",
    "rollback failed publication": "RemoveQs3dOwnedBimPanels(bimPanels);",
    "unsupported item fails closed": "throw new InvalidOperationException(\"Unsupported QS3D Ribbon item type: \" + typeName + \".\");",
}
missing = [name for name, token in required.items() if token not in text]
if missing:
    raise SystemExit("FAIL BIM ribbon mirror atomic guard: missing " + ", ".join(missing))

stage = text.index("var stagedPanels = new List<object>(PanelSpecs.Length);")
publication_start = text.index("publicationStarted = true;", stage)
remove = text.index("RemoveQs3dOwnedBimPanels(bimPanels);", publication_start)
publish = text.index("foreach (var panel in stagedPanels)", remove)
initialized = text.index("_initialized = true;", publish)
rollback_guard = text.index("if (publicationStarted && bimPanels != null)", initialized)
if not stage < publication_start < remove < publish < initialized < rollback_guard:
    raise SystemExit("FAIL BIM ribbon mirror atomic guard: staging/publication/rollback ordering regressed")

print("PASS V25 BIM ribbon mirror stages completely, preserves prior mirror on staging failure, rejects unknown shapes, and rolls back partial publication")
