#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs"
AGGREGATE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticTagBuilder.cs"
errors = []

for path in (RUNTIME, AGGREGATE, COMMANDS, BUILDER):
    if not path.is_file():
        errors.append("missing semantic-tag runtime-health file: " + str(path.relative_to(ROOT)))

if RUNTIME.is_file():
    text = RUNTIME.read_text(encoding="utf-8")
    for token in (
        "GeneratedSemanticTagHealthService.HandlesKey",
        "StartOpenCloseTransaction()",
        "GetObjectId(false, new Handle(value), 0)",
        "transaction.GetObject(id, OpenMode.ForRead, true) as Entity",
        "entity == null || entity.IsErased",
        "if (!(entity is MText tag))",
        "GeneratedGeometryService.HasMatchingOwnership(tag, project, element)",
        "InspectContent(element, tag, issues)",
        "InspectTextHeight(document, element, tag, issues)",
        "InspectPlacement(element, tag, issues)",
        '"SEMANTIC_TAG_MTEXT_MISSING"',
        '"SEMANTIC_TAG_MTEXT_TYPE_MISMATCH"',
        '"SEMANTIC_TAG_MTEXT_OWNERSHIP_MISMATCH"',
        '"SEMANTIC_TAG_MTEXT_CONTENT_DRIFT"',
        '"SEMANTIC_TAG_MTEXT_HEIGHT_DRIFT"',
        '"SEMANTIC_TAG_MTEXT_POSITION_DRIFT"',
        '"SEMANTIC_TAG_MTEXT_ROTATION_DRIFT"',
        '"SEMANTIC_TAG_MTEXT_NORMAL_DRIFT"',
        "return issues.AsReadOnly();",
    ):
        if token not in text:
            errors.append("GeneratedSemanticTagRuntimeHealthService.cs missing diagnostic token: " + token)

    missing = text.find('"SEMANTIC_TAG_MTEXT_MISSING"')
    type_check = text.find("if (!(entity is MText tag))")
    ownership = text.find("GeneratedGeometryService.HasMatchingOwnership(tag, project, element)")
    content = text.find("InspectContent(element, tag, issues)")
    height = text.find("InspectTextHeight(document, element, tag, issues)")
    placement = text.find("InspectPlacement(element, tag, issues)")
    if min(missing, type_check, ownership, content, height, placement) < 0 or not type_check < ownership < content < height < placement:
        errors.append("Semantic tag runtime health must validate live type/ownership before content, sizing and placement drift checks.")

if AGGREGATE.is_file():
    text = AGGREGATE.read_text(encoding="utf-8")
    grid = text.find("GeneratedGridAnnotationRuntimeHealthService.Inspect(document, project)")
    tag = text.find("GeneratedSemanticTagRuntimeHealthService.Inspect(document, project)")
    result = text.find("return issues.AsReadOnly();", tag)
    if grid < 0 or tag < 0 or result < 0 or not grid < tag < result:
        errors.append("GeneratedSolidRuntimeHealthService must aggregate Grid and Semantic Tag runtime health before returning.")

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    health_start = text.find('CommandMethod("QS3DHEALTH"')
    locate_start = text.find('CommandMethod("QS3DLOCATE"')
    health = text[health_start:locate_start if locate_start >= 0 else len(text)]
    if "Cad.GeneratedSolidRuntimeHealthService.Inspect(doc, project)" not in health:
        errors.append("QS3DHEALTH no longer invokes the V25 runtime-health aggregate.")

if BUILDER.is_file() and RUNTIME.is_file():
    builder = BUILDER.read_text(encoding="utf-8")
    runtime = RUNTIME.read_text(encoding="utf-8")
    for token in (
        'output.Append("\\\\P")',
        'output.Append("\\\\\\\\")',
        'output.Append("\\\\{")',
        'output.Append("\\\\}")',
    ):
        if token not in builder or token not in runtime:
            errors.append("Semantic Tag builder/runtime plain-MText encoding contract drifted: " + token)

print("QS3D semantic-tag runtime-health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: QS3DHEALTH runtime diagnostics guard live semantic-tag MText existence, type, ownership, content, height, position, rotation and normal without treating tags as Solid3d.")
