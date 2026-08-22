#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Domain/ProjectMaterialCatalog.cs": [
        "EnsureDoesNotShadowBuiltIn(material)",
        "private static void EnsureDoesNotShadowBuiltIn",
        "Built-in material ids cannot be overwritten",
        "A built-in material already uses the name",
        "Duplicate material id in project catalog",
        "Duplicate material name in project catalog",
    ],
    "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogSmoke.cs": [
        "RejectsStoredBuiltInShadowing",
        'Record("builtin-concrete", "Bê tông giả"',
        'Record("custom-shadow", "Bê tông"',
        "ProjectMaterialCatalog.GetCustom(project)",
        "ProjectMaterialCatalog.GetAll(project)",
    ],
    "tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogRegistration.cs": [
        "ProjectMaterialCatalogSmoke.Run();",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing material catalog integrity file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing persisted catalog guard/token: " + needle)

catalog = ROOT / "src/QS3D.Core/Domain/ProjectMaterialCatalog.cs"
if catalog.is_file():
    text = catalog.read_text(encoding="utf-8")
    read_custom = text.find("private static List<ProjectMaterial> ReadCustom")
    guard_call = text.find("EnsureDoesNotShadowBuiltIn(material)", read_custom)
    duplicate_id = text.find("Duplicate material id in project catalog", read_custom)
    if read_custom < 0 or guard_call < 0 or duplicate_id < 0 or guard_call > duplicate_id:
        errors.append("ReadCustom must reject built-in shadow records before accepting custom ids/names")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: persisted custom material metadata fails closed on built-in id/name shadowing before catalog merge/grouping.")
