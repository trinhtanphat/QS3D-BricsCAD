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

<<<<<<< HEAD
for path in [APPEND, REMAP]:
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    tokens = [
=======
# Append-only still uses the shared exact reviewed-project/change-version guard.
if APPEND.is_file():
    text = APPEND.read_text(encoding="utf-8")
    for token in [
>>>>>>> origin/main
        "previewChangeVersion = project.ChangeVersion",
        "currentProject.ChangeVersion != previewChangeVersion",
        "changed after preview",
<<<<<<< HEAD
    ]
    if path == REMAP:
        tokens += [
            "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
            'ExistingProjectMutationContext.Require(document, "Interchange Import As New")',
            "var previewProjectId = project.ProjectId;",
            "var previewUpdatedUtc = project.UpdatedUtc;",
            "var previewDrawingFingerprint = project.DrawingFingerprint ?? string.Empty;",
            "currentProject.UpdatedUtc != previewUpdatedUtc",
            "currentProject.DrawingFingerprint ?? string.Empty",
        ]
    else:
        tokens += [
            "ProjectContextCoordinator.GetOrCreate(document)",
            "ReferenceEquals(currentProject, project)",
        ]
    for token in tokens:
=======
        "ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)",
    ]:
>>>>>>> origin/main
        if token not in text:
            errors.append(str(APPEND.relative_to(ROOT)) + " missing append freshness guard token: " + token)

<<<<<<< HEAD
    if path == REMAP and "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(str(path.relative_to(ROOT)) + " preview must not create or cache replacement project state")

if APPEND.is_file() and "ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)" not in APPEND.read_text(encoding="utf-8"):
    errors.append("append import must mutate the re-resolved current project")
if REMAP.is_file() and "ProjectInterchangeRemapAppendImporter.Import(currentProject, json)" not in REMAP.read_text(encoding="utf-8"):
    errors.append("remap append must mutate the re-resolved current project")
=======
# Import-As-New preview is intentionally non-creating and freshness-binds to semantic stamps rather
# than ProjectState object identity, so a cache replacement cannot make an old preview authoritative.
if REMAP.is_file():
    text = REMAP.read_text(encoding="utf-8")
    for token in [
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "previewProjectId = project.ProjectId",
        "previewUpdatedUtc = project.UpdatedUtc",
        "previewChangeVersion = project.ChangeVersion",
        "previewDrawingFingerprint = project.DrawingFingerprint ?? string.Empty",
        'ExistingProjectMutationContext.Require(document, "Interchange Import As New")',
        "currentProject.UpdatedUtc != previewUpdatedUtc",
        "currentProject.ChangeVersion != previewChangeVersion",
        "previewDrawingFingerprint",
        "target semantic project changed after preview",
        "ProjectInterchangeRemapAppendImporter.Import(currentProject, json)",
    ]:
        if token not in text:
            errors.append(str(REMAP.relative_to(ROOT)) + " missing semantic-stamp freshness token: " + token)
    for forbidden in [
        "ProjectContextCoordinator.GetOrCreate(document)",
        "ReferenceEquals(currentProject, project)",
        "ProjectInterchangeRemapAppendImporter.Import(project, json)",
    ]:
        if forbidden in text:
            errors.append(str(REMAP.relative_to(ROOT)) + " must not use stale/creating remap freshness pattern: " + forbidden)
>>>>>>> origin/main

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

print("PASS: interchange mutation paths reject stale confirmations; Import As New uses non-creating semantic snapshot stamps while shared append/use-source paths retain their reviewed-project guard.")
