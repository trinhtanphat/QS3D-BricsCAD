#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/QS3D.BricsCAD.V25"
errors = []

command_owners = {}
for path in SRC.rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for command in re.findall(r'\[CommandMethod\("(QS3D[A-Z0-9_]*)"', text, re.IGNORECASE):
        command_owners.setdefault(command.upper(), []).append(str(path.relative_to(ROOT)))

for command, owners in sorted(command_owners.items()):
    if len(owners) != 1:
        errors.append("duplicate CommandMethod " + command + ": " + ", ".join(owners))

ui_refs = []
for path in (SRC / "UI").rglob("*.xaml"):
    text = path.read_text(encoding="utf-8")
    for command in re.findall(r'\bTag="(QS3D[A-Z0-9_]*)"', text, re.IGNORECASE):
        ui_refs.append((command.upper(), str(path.relative_to(ROOT)), "XAML Tag"))

ribbon = SRC / "Ribbon/RibbonBootstrapper.cs"
if ribbon.is_file():
    text = ribbon.read_text(encoding="utf-8")
    for command in re.findall(r'new\s+RibbonButtonSpec\([^,]+,\s*"(QS3D[A-Z0-9_]*)"\)', text, re.IGNORECASE):
        ui_refs.append((command.upper(), str(ribbon.relative_to(ROOT)), "RibbonButtonSpec"))
else:
    errors.append("missing RibbonBootstrapper.cs")

for path in (SRC / "UI").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    patterns = (
        r'SendStringToExecute\(\s*"(QS3D[A-Z0-9_]*)\b',
        r'SendCommand\(\s*"(QS3D[A-Z0-9_]*)\b',
        r'Send\(\s*"(QS3D[A-Z0-9_]*)\b',
    )
    for pattern in patterns:
        for command in re.findall(pattern, text, re.IGNORECASE):
            ui_refs.append((command.upper(), str(path.relative_to(ROOT)), "UI command dispatch"))

seen_refs = set()
for command, path, kind in sorted(ui_refs):
    key = (command, path, kind)
    if key in seen_refs:
        continue
    seen_refs.add(key)
    owners = command_owners.get(command, [])
    if not owners:
        errors.append(kind + " references unregistered command " + command + " in " + path)
    elif len(owners) > 1:
        errors.append(kind + " references ambiguous command " + command + " in " + path + ": " + ", ".join(owners))

required = {
    "QS3DHEALTHALL",
    "QS3DREBARHEALTHALL",
    "QS3DBEAMREBAR3D",
    "QS3DREBARSTIRRUP3D",
    "QS3DREBARSTIRRUPHEALTH",
    "QS3DREBARTIES3D",
    "QS3DREBARTIEHEALTH",
    "QS3DAUTOLINKHOSTS",
    "QS3DWALLSNAPPREVIEW",
    "QS3DWALLSNAPAPPLY",
    "QS3DSECTIONBOX",
}
for command in sorted(required):
    if command not in command_owners:
        errors.append("required product command is not registered: " + command)

print("QS3D UI/command wiring preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS:", len(command_owners), "registered QS3D commands and", len(seen_refs), "UI/Ribbon dispatch references are uniquely wired.")
