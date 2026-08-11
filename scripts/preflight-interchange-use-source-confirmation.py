#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
TARGET_GUARD = ADAPTER / "Services" / "InterchangeMutationTargetGuard.cs"
errors = []

standalone = {
    "ProjectInterchangeUseSourceCommands.cs": (
        "Interchange UseSource Element",
        "InterchangeUseSourceElementImportService.Import(document, confirmedProject, json)",
    ),
    "ProjectInterchangeUseSourceCatalogCommands.cs": (
        "Interchange UseSource Catalog",
        "InterchangeUseSourceCatalogImportService.Import(document, confirmedProject, json)",
    ),
    "ProjectInterchangeUseSourceAllCommands.cs": (
        "Interchange UseSource ALL",
        "InterchangeUseSourceAllImportService.Import(document, confirmedProject, json)",
    ),
}

services = {
    "InterchangeUseSourceElementImportService.cs": "Interchange source-element import",
    "InterchangeUseSourceCatalogImportService.cs": "Interchange source-catalog import",
    "InterchangeUseSourceAllImportService.cs": "Interchange all-scope source import",
}

if not TARGET_GUARD.is_file():
    errors.append("missing exact-project mutation target guard")
else:
    target_guard = TARGET_GUARD.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
        "ReferenceEquals(currentProject, authorizedProject)",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
    ):
        if token not in target_guard:
            errors.append("exact-project mutation target guard missing token: " + token)
    if "ProjectContextCoordinator.GetOrCreate" in target_guard:
        errors.append("exact-project mutation target guard must remain non-creating")

for filename, operation in services.items():
    path = ADAPTER / "Services" / filename
    if not path.is_file():
        errors.append("missing UseSource service source: " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    for token in (
        "ProjectState authorizedProject",
        "InterchangeMutationTargetGuard.RequireExact(",
        "authorizedProject,",
        operation,
    ):
        if token not in text:
            errors.append(filename + " missing exact reviewed-target token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(filename + " must not reacquire or create a mutation target after confirmation")

for filename, (operation, import_call) in standalone.items():
    path = ADAPTER / filename
    if not path.is_file():
        errors.append("missing UseSource command source: " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    guard = "var confirmedProject = InterchangeConfirmationGuard.RequireFresh("
    version = "var previewChangeVersion = project.ChangeVersion;"
    for token in (version, guard, import_call):
        if token not in text:
            errors.append(filename + " missing reviewed-target freshness token: " + token)
    guard_pos = text.find(guard)
    import_pos = text.find(import_call)
    if guard_pos >= 0 and import_pos >= 0 and guard_pos >= import_pos:
        errors.append(filename + " must verify reviewed target freshness before UseSource mutation")
    if operation not in text:
        errors.append(filename + " missing stable operation label: " + operation)
    if "Import(document, json)" in text:
        errors.append(filename + " must pass the exact freshness-authorized project to UseSource mutation")
    validation = text.find("ProjectInterchangeJsonValidator.Validate(json)")
    active = text.find("ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)")
    readonly = text.find("ProjectContextCoordinator.TryGetReadOnly(document, out var project)")
    if min(validation, active, readonly) < 0 or not validation < active < readonly:
        errors.append(filename + " must validate input and recheck the active DWG before read-only target lookup")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(filename + " must not create/cache a target project during UseSource preview")

unified = ADAPTER / "ProjectInterchangeImportCommands.cs"
if not unified.is_file():
    errors.append("missing ProjectInterchangeImportCommands.cs")
else:
    text = unified.read_text(encoding="utf-8")
    for token in (
        "var confirmedProject = InterchangeConfirmationGuard.RequireFresh(",
        "RunKeepTarget(document, confirmedProject, json)",
        "RunUseSourceElement(document, confirmedProject, json)",
        "RunUseSourceCatalog(document, confirmedProject, json)",
        "RunUseSourceAll(document, confirmedProject, json)",
        "InterchangeUseSourceElementImportService.Import(document, confirmedProject, json)",
        "InterchangeUseSourceCatalogImportService.Import(document, confirmedProject, json)",
        "InterchangeUseSourceAllImportService.Import(document, confirmedProject, json)",
        "var currentProject = InterchangeConfirmationGuard.RequireFresh(",
        "ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)",
    ):
        if token not in text:
            errors.append("generic interchange import missing freshness/dispatch token: " + token)

    guard_pos = text.find("var confirmedProject = InterchangeConfirmationGuard.RequireFresh(")
    switch_pos = text.find("switch (choice.Value)")
    if guard_pos >= 0 and switch_pos >= 0 and guard_pos >= switch_pos:
        errors.append("generic collision-policy import must freshness-check before policy dispatch")
    for stale_call in (
        "RunUseSourceElement(document, json)",
        "RunUseSourceCatalog(document, json)",
        "RunUseSourceAll(document, json)",
        "InterchangeUseSourceElementImportService.Import(document, json)",
        "InterchangeUseSourceCatalogImportService.Import(document, json)",
        "InterchangeUseSourceAllImportService.Import(document, json)",
    ):
        if stale_call in text:
            errors.append("generic interchange import discards the authorized target: " + stale_call)

    append_guard = text.find("var currentProject = InterchangeConfirmationGuard.RequireFresh(")
    append_import = text.find("ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)")
    if append_guard >= 0 and append_import >= 0 and append_guard >= append_import:
        errors.append("append-only path must freshness-check before semantic mutation")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: standalone and unified Interchange UseSource paths pass the exact freshness-authorized live project through a non-creating mutation target guard.")
