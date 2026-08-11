#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
errors = []

standalone = {
    "ProjectInterchangeUseSourceCommands.cs": (
        "Interchange UseSource Element",
        "InterchangeUseSourceElementImportService.Import(document, json)",
    ),
    "ProjectInterchangeUseSourceCatalogCommands.cs": (
        "Interchange UseSource Catalog",
        "InterchangeUseSourceCatalogImportService.Import(document, json)",
    ),
    "ProjectInterchangeUseSourceAllCommands.cs": (
        "Interchange UseSource ALL",
        "InterchangeUseSourceAllImportService.Import(document, json)",
    ),
}

for filename, (operation, import_call) in standalone.items():
    path = ADAPTER / filename
    if not path.is_file():
        errors.append("missing UseSource command source: " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    guard = "InterchangeConfirmationGuard.RequireFresh("
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

unified = ADAPTER / "ProjectInterchangeImportCommands.cs"
if not unified.is_file():
    errors.append("missing ProjectInterchangeImportCommands.cs")
else:
    text = unified.read_text(encoding="utf-8")
    for token in (
        "var confirmedProject = InterchangeConfirmationGuard.RequireFresh(",
        "RunKeepTarget(document, confirmedProject, json)",
        "RunUseSourceElement(document, json)",
        "RunUseSourceCatalog(document, json)",
        "RunUseSourceAll(document, json)",
        "var currentProject = InterchangeConfirmationGuard.RequireFresh(",
        "ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)",
    ):
        if token not in text:
            errors.append("generic interchange import missing freshness/dispatch token: " + token)

    guard_pos = text.find("var confirmedProject = InterchangeConfirmationGuard.RequireFresh(")
    switch_pos = text.find("switch (choice.Value)")
    if guard_pos >= 0 and switch_pos >= 0 and guard_pos >= switch_pos:
        errors.append("generic collision-policy import must freshness-check before policy dispatch")

    append_guard = text.find("var currentProject = InterchangeConfirmationGuard.RequireFresh(")
    append_import = text.find("ProjectInterchangeAppendOnlyImporter.Import(currentProject, json)")
    if append_guard >= 0 and append_import >= 0 and append_guard >= append_import:
        errors.append("append-only path must freshness-check before semantic mutation")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: standalone and unified Interchange UseSource/append entrypoints preserve reviewed-target freshness checks before mutation dispatch.")
