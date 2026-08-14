#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs"
LINE = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs"
PATH = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs"
errors = []


def read(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        errors.append(f"cannot read {path.relative_to(ROOT)}: {exc}")
        return ""


command = read(COMMAND)
line = read(LINE)
path = read(PATH)

aggregate_calls = {
    "LINE frame": r"CurtainWallFrameSolidBuilder\.BuildSelectedLineWalls\(\s*document\s*,\s*project\s*,\s*allowInteractiveSelection\s*:\s*false\s*\)",
    "path frame": r"CurtainWallPathFrameSolidBuilder\.BuildSelectedOpenPolylines\(\s*document\s*,\s*project\s*,\s*allowInteractiveSelection\s*:\s*false\s*\)",
}
for label, pattern in aggregate_calls.items():
    if not re.search(pattern, command, re.MULTILINE):
        errors.append(f"QS3DCURTAIN3D does not force non-interactive {label} selection")

if len(re.findall(r"if\s*\(validatedSelection\.LineSourceIds\.Count\s*>\s*0\)", command)) < 3:
    errors.append("QS3DCURTAIN3D lost one or more LINE empty-partition guards")
if len(re.findall(r"if\s*\(validatedSelection\.PathSourceIds\.Count\s*>\s*0\)", command)) < 3:
    errors.append("QS3DCURTAIN3D lost one or more path empty-partition guards")

builders = (
    (
        "LINE frame",
        line,
        r"BuildSelectedLineWalls\s*\(\s*Document\s+document\s*,\s*ProjectState\s+project\s*,\s*bool\s+allowInteractiveSelection\s*=\s*true\s*\)",
    ),
    (
        "path frame",
        path,
        r"BuildSelectedOpenPolylines\s*\(\s*Document\s+document\s*,\s*ProjectState\s+project\s*,\s*bool\s+allowInteractiveSelection\s*=\s*true\s*\)",
    ),
)

for label, text, signature in builders:
    if not re.search(signature, text, re.MULTILINE):
        errors.append(f"{label} builder does not preserve interactive standalone selection as the default")
    guard = text.find("if (!allowInteractiveSelection)")
    fallback = text.find("document.Editor.GetSelection()")
    if guard < 0:
        errors.append(f"{label} builder has no non-interactive fail-closed guard")
    if fallback < 0:
        errors.append(f"{label} builder no longer preserves the standalone interactive fallback")
    if guard >= 0 and fallback >= 0 and guard > fallback:
        errors.append(f"{label} builder checks non-interactive mode only after opening the selection prompt")
    if "interactive selection is disabled inside QS3DCURTAIN3D" not in text:
        errors.append(f"{label} builder does not expose a deterministic fail-closed aggregate diagnostic")

if errors:
    print("Curtain3D non-interactive frame-build preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print(
    "Curtain3D non-interactive frame-build preflight PASS: aggregate LINE/path frame phases "
    "cannot fall back to Editor.GetSelection(), while standalone builders retain their interactive default."
)
