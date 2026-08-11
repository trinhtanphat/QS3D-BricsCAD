#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = [
    ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeCommands.cs",
    ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeRemapAppendCommands.cs",
]

errors = []
for path in FILES:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    required = [
        "previewChangeVersion = project.ChangeVersion",
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ReferenceEquals(currentProject, project)",
        "currentProject.ChangeVersion != previewChangeVersion",
        "changed after preview",
    ]
    for token in required:
        if token not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing freshness guard token: " + token)

append = FILES[0].read_text(encoding="utf-8") if FILES[0].is_file() else ""
remap = FILES[1].read_text(encoding="utf-8") if FILES[1].is_file() else ""
if "ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)" not in append:
    errors.append("append import must mutate the re-resolved current project")
if "ProjectInterchangeRemapAppendImporter.Import(currentProject, json)" not in remap:
    errors.append("remap append must mutate the re-resolved current project")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: confirmed Append and Remap Append mutations are bound to the exact reviewed project instance/change version.")
