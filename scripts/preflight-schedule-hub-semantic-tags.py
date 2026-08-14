#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml.cs"
COMMAND_FILES = [
    ROOT / "src/QS3D.BricsCAD.V25/SemanticTagCommands.cs",
    ROOT / "src/QS3D.BricsCAD.V25/SemanticTagHealthCommands.cs",
    ROOT / "src/QS3D.BricsCAD.V25/SemanticTagRemovalCommands.cs",
]
TARGETS = (
    "QS3DTAG",
    "QS3DTAGREFRESH",
    "QS3DTAGHEALTH",
    "QS3DTAGREMOVE",
)
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


xaml = read(XAML)
code = read(CODE)
commands = "\n".join(read(path) for path in COMMAND_FILES)

try:
    root = ET.fromstring(xaml)
except ET.ParseError as exc:
    errors.append("Schedule Hub XAML XML parse failed: " + str(exc))
    root = None

if root is not None:
    buttons = []
    for element in root.iter():
        if element.tag.rsplit("}", 1)[-1] == "Button":
            buttons.append(element)

    for command in TARGETS:
        matches = [button for button in buttons if button.attrib.get("Tag") == command]
        if len(matches) != 1:
            errors.append(command + ": expected exactly one Schedule Hub launcher, found " + str(len(matches)))
            continue
        button = matches[0]
        if button.attrib.get("Click") != "OnCommandClick":
            errors.append(command + ": launcher must use OnCommandClick")
        if not (button.attrib.get("Content") or "").strip():
            errors.append(command + ": launcher must have visible Content")

if 'Text="SEMANTIC TAG / ANNOTATION"' not in xaml:
    errors.append("Schedule Hub: missing Semantic Tag section heading")

for token in (
    "private void OnCommandClick",
    'EnsureActive("chạy " + normalizedCommand)',
    '_document.SendStringToExecute(normalizedCommand + " ", true, false, false)',
):
    if token not in code:
        errors.append("Schedule Hub dispatcher missing contract token: " + token)

for command in TARGETS:
    declaration = re.compile(r'\[CommandMethod\(\s*"' + re.escape(command) + r'"\s*,')
    count = len(declaration.findall(commands))
    if count != 1:
        errors.append(command + ": expected exactly one adapter CommandMethod declaration, found " + str(count))

health = read(ROOT / "src/QS3D.BricsCAD.V25/SemanticTagHealthCommands.cs")
if "ProjectContextCoordinator.TryGetReadOnly" not in health:
    errors.append("QS3DTAGHEALTH must retain read-only project lookup")
for forbidden in (
    "SemanticTagBuilder.Build",
    "SemanticTagRemovalService.Remove",
    ".Erase(",
):
    if forbidden in health:
        errors.append("QS3DTAGHEALTH must remain read-only; forbidden token: " + forbidden)

if errors:
    print("Schedule Hub Semantic Tag preflight: FAIL")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Schedule Hub Semantic Tag preflight: PASS — Place/Refresh/Health/Remove are exposed exactly once through the drawing-bound generic dispatcher and resolve to canonical adapter commands; Health remains read-only.")
