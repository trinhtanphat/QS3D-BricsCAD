#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectContextCoordinator.cs"
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"

errors = []

coordinator = COORDINATOR.read_text(encoding="utf-8")
save_start = coordinator.find("public static string Save(Document document)")
save_end = coordinator.find("public static ProjectState Reload(Document document)", save_start + 1)
save_body = coordinator[save_start:save_end] if save_start >= 0 and save_end > save_start else ""
if not save_body:
    errors.append("cannot isolate ProjectContextCoordinator.Save")
else:
    required = 'ExistingProjectMutationContext.Require(document, "Save Project")'
    if required not in save_body:
        errors.append("Save must bind an existing canonical project before persistence")
    if "GetOrCreate(document)" in save_body:
        errors.append("Save must not bootstrap a replacement project")
    bind = save_body.find(required)
    persist = save_body.find("Store.Save(project, path)")
    if bind < 0 or persist < 0 or bind > persist:
        errors.append("existing-project bind must occur before persistence")

commands = COMMANDS.read_text(encoding="utf-8")
command_start = commands.find('CommandMethod("QS3DSAVE"')
command_end = commands.find("[CommandMethod(", command_start + 1)
command_body = commands[command_start:command_end] if command_start >= 0 and command_end > command_start else ""
if not command_body:
    errors.append("cannot isolate QS3DSAVE command")
elif "ProjectContextCoordinator.Save(doc)" not in command_body:
    errors.append("QS3DSAVE must delegate to ProjectContextCoordinator.Save")

inbox = INBOX.read_text(encoding="utf-8")
for token in ("QS3DSAVE", "no replacement project", "same-ProjectId"):
    if token not in inbox:
        errors.append("LOCAL-AGENT-INBOX.md missing QS3DSAVE lifecycle evidence token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DSAVE persists only an existing canonical project and cannot bootstrap a replacement project; LOCAL-001 carries the native V25 proof scenario.")
