#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
APPEND = ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeCommands.cs"
REMAP = ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeRemapAppendCommands.cs"
UNIFIED = ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeImportCommands.cs"
USE_SOURCE = [
    ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeUseSourceCommands.cs",
    ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeUseSourceCatalogCommands.cs",
    ROOT / "src/QS3D.BricsCAD.V25/ProjectInterchangeUseSourceAllCommands.cs",
]
GUARD = ROOT / "src/QS3D.BricsCAD.V25/Services/InterchangeConfirmationGuard.cs"

errors = []
for path in [APPEND, REMAP, UNIFIED] + USE_SOURCE + [GUARD]:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))

for path in [APPEND, REMAP]:
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for token in [
        "previewChangeVersion = project.ChangeVersion",
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ReferenceEquals(currentProject, project)",
        "currentProject.ChangeVersion != previewChangeVersion",
        "changed after preview",
    ]:
        if token not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing freshness guard token: " + token)

if APPEND.is_file() and "ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)" not in APPEND.read_text(encoding="utf-8"):
    errors.append("append import must mutate the re-resolved current project")
if REMAP.is_file() and "ProjectInterchangeRemapAppendImporter.Import(currentProject, json)" not in REMAP.read_text(encoding="utf-8"):
    errors.append("remap append must mutate the re-resolved current project")

for path in USE_SOURCE:
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for token in [
        "previewChangeVersion = project.ChangeVersion",
        "InterchangeConfirmationGuard.RequireFresh(",
        "project,\n                    previewChangeVersion",
    ]:
        if token not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing UseSource freshness guard token: " + token)

if UNIFIED.is_file():
    unified = UNIFIED.read_text(encoding="utf-8")
    for token in [
        "previewChangeVersion = project.ChangeVersion",
        "RunAppendOnly(document, project, previewChangeVersion, json)",
        "var confirmedProject = InterchangeConfirmationGuard.RequireFresh(",
        '"Interchange Import policy"',
        "RunKeepTarget(document, confirmedProject, json)",
        "long reviewedChangeVersion",
        '"Interchange Import / Append-only"',
        "ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)",
    ]:
        if token not in unified:
            errors.append("unified import missing confirmation freshness token: " + token)
    if "RunKeepTarget(document, project, json)" in unified:
        errors.append("unified KeepTarget must use the freshness-verified project instance")
    if "ProjectInterchangeAppendOnlyImporter.Import(project, json)" in unified:
        errors.append("unified append-only must not mutate the stale preview project reference")

if GUARD.is_file():
    guard = GUARD.read_text(encoding="utf-8")
    for token in [
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ReferenceEquals(currentProject, reviewedProject)",
        "currentProject.ChangeVersion != reviewedChangeVersion",
        "changed after preview",
    ]:
        if token not in guard:
            errors.append("confirmation guard missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: all mutating interchange confirmation paths are bound to the exact reviewed document/project/change version before mutation.")
