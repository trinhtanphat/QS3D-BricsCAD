#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.BricsCAD.V25/SupportBundleCommands.cs"
doc = ROOT / "docs/SUPPORT-DIAGNOSTICS.md"
errors = []

for path in (source, doc):
    if not path.is_file():
        errors.append("missing support diagnostics file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DSUPPORTBUNDLE"',
        "plugin_product_version=",
        "core_product_version=",
        "brx_assembly_version=",
        "td_assembly_version=",
        "project_schema=",
        "dirty_element_count=",
        "has_drawing_fingerprint=",
        "category.",
        "No drawing path, source/generated handles, semantic IDs, Family names, project metadata, user name or machine name are included.",
    )
    for needle in required:
        if needle not in text:
            errors.append("SupportBundleCommands.cs missing diagnostics/privacy token: " + needle)

    forbidden = (
        "project.DrawingPath",
        "project.ProjectId",
        "project.Name",
        "project.Metadata",
        "document.Name",
        "document.Database.Filename",
        "Environment.UserName",
        "Environment.MachineName",
        ".SourceHandles",
        "FamilyId",
        "element.Id",
    )
    for needle in forbidden:
        if needle in text:
            errors.append("Support bundle must not access/export sensitive project/runtime identity token: " + needle)

if doc.is_file():
    text = doc.read_text(encoding="utf-8")
    for needle in (
        "DWG file name or path",
        "source CAD handles",
        "generated CAD handles",
        "semantic element IDs",
        "user name",
        "machine name",
        "LOCAL-V25-QUALIFICATION.md",
    ):
        if needle not in text:
            errors.append("Support diagnostics doc missing privacy/qualification boundary: " + needle)

print("QS3D support bundle privacy preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DSUPPORTBUNDLE exports support-relevant runtime/schema/count diagnostics while source/static guards prohibit default export of drawing paths, CAD handles, semantic IDs, Family identity, project metadata, user or machine identity.")
