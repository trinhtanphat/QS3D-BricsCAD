#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "SemanticCaptureService.cs"


def fail(message: str) -> None:
    print("ERROR: structural wall contact stale-clear preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
method = re.search(
    r"private static void RefreshStructuralWallConcreteContacts\(Document document, ProjectState project\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private static void Regenerate",
    text,
    re.DOTALL,
)
if method is None:
    fail("RefreshStructuralWallConcreteContacts method was not found")

body = method.group("body")
unavailable = re.search(
    r"if\s*\(!StructuralWallConcreteContactService\.TryMeasureM2\(document,\s*project,\s*wall,\s*out var contactAreaM2\)\)\s*\{(?P<body>.*?)\n\s*\}",
    body,
    re.DOTALL,
)
if unavailable is None:
    fail("unavailable live-BREP branch must be an explicit block")

unavailable_body = unavailable.group("body")
required_tokens = {
    "remove stale contact evidence": 'wall.Properties.Remove("ConcreteContactAreaM2")',
    "mark quantity dirty": "wall.MarkDirty(ElementDirtyFlags.Quantity);",
    "regenerate wall": "Regenerate(project, wall);",
    "reapply measured solid quantities": "MeasuredSolidQuantityPolicy.Apply(wall);",
    "record project change": "changed = true;",
    "stop before measured-value publication": "continue;",
}
for label, token in required_tokens.items():
    if token not in unavailable_body:
        fail(label + " is missing from the unavailable live-BREP branch")

remove_pos = unavailable_body.find('wall.Properties.Remove("ConcreteContactAreaM2")')
regenerate_pos = unavailable_body.find("Regenerate(project, wall);")
continue_pos = unavailable_body.find("continue;")
if not (0 <= remove_pos < regenerate_pos < continue_pos):
    fail("stale contact evidence must be removed before regeneration and before leaving the branch")

print("PASS: unavailable/stale wall BREP clears published contact deduction before quantity regeneration")
