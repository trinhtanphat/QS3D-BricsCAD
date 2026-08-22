#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

command = ROOT / "src/QS3D.BricsCAD.V25/SectionReviewCommands.cs"
hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
ribbon = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs"

for path in (command, hub, ribbon):
    if not path.is_file():
        errors.append("missing section-review file: " + str(path.relative_to(ROOT)))

owners = {}
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for name in re.findall(r'\[CommandMethod\("([^\"]+)"', text):
        owners.setdefault(name.upper(), []).append(str(path.relative_to(ROOT)))
for name in ("QS3DSECTIONBOX", "QS3DSECTIONPLANE", "QS3DCLIPDISPLAY"):
    found = owners.get(name, [])
    if len(found) != 1:
        errors.append(name + " must have exactly one CommandMethod owner; found: " + ", ".join(found))

if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'BimDetailCommand = "_BIMSECTION _Detail "',
        'SectionPlaneCommand = "_SECTIONPLANE "',
        'ClipDisplayCommand = "_CLIPDISPLAY "',
        'CommandMethod("QS3DSECTIONBOX"',
        'CommandMethod("QS3DSECTIONPLANE"',
        'CommandMethod("QS3DCLIPDISPLAY"',
        "SendStringToExecute",
        "ModelReviewService.HighlightSelection(document, false)",
        "cần BricsCAD BIM hỗ trợ lệnh BIMSECTION",
    ):
        if needle not in text:
            errors.append("native section-review contract missing: " + needle)
    if '"BIMSECTION Detail "' in text or '"BIMSECTION D "' in text:
        errors.append("native command strings must retain underscore localization prefix")

if hub.is_file():
    text = hub.read_text(encoding="utf-8")
    for name in ("QS3DSECTIONBOX", "QS3DSECTIONPLANE", "QS3DCLIPDISPLAY"):
        if 'Tag="' + name + '"' not in text:
            errors.append("Domain Hub does not expose " + name)

if ribbon.is_file():
    text = ribbon.read_text(encoding="utf-8")
    for name in ("QS3DSECTIONBOX", "QS3DSECTIONPLANE", "QS3DCLIPDISPLAY"):
        if '"' + name + '"' not in text:
            errors.append("Ribbon does not expose " + name)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: native localized-safe BIM Detail section box, section-plane and clip-display review commands plus UI wiring are present.")
