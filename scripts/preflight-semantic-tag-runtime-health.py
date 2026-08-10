#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs"
AGGREGATE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/SemanticTagHealthCommands.cs"
HEALTH = ROOT / "src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs"
errors = []

for path in (RUNTIME, AGGREGATE, COMMAND, HEALTH):
    if not path.is_file():
        errors.append("missing semantic tag runtime-health dependency: " + str(path.relative_to(ROOT)))

if RUNTIME.is_file():
    text = RUNTIME.read_text(encoding="utf-8")
    for token in (
        "GeneratedSemanticTagHealthService.HandlesKey",
        "StartOpenCloseTransaction()",
        "OpenMode.ForRead",
        "if (!(entity is MText tag))",
        "GeneratedGeometryService.HasMatchingOwnership(tag, project, element)",
        '"SEMANTIC_TAG_MTEXT_MISSING"',
        '"SEMANTIC_TAG_MTEXT_TYPE_MISMATCH"',
        '"SEMANTIC_TAG_MTEXT_OWNERSHIP_MISMATCH"',
        '"SEMANTIC_TAG_MTEXT_CONTENT_DRIFT"',
        '"SEMANTIC_TAG_MTEXT_HEIGHT_DRIFT"',
        '"SEMANTIC_TAG_MTEXT_POSITION_DRIFT"',
        '"SEMANTIC_TAG_MTEXT_ROTATION_DRIFT"',
        '"SEMANTIC_TAG_MTEXT_NORMAL_DRIFT"',
        "tag.Contents",
        "tag.TextHeight",
        "tag.Location",
        "tag.Rotation",
        "tag.Normal",
        "transaction.Commit();",
    ):
        if token not in text:
            errors.append("GeneratedSemanticTagRuntimeHealthService.cs missing runtime-health contract: " + token)
    for forbidden in (
        "OpenMode.ForWrite",
        ".Erase()",
        "SetProperty(",
        "ProjectStateSnapshot",
        "project.Touch()",
        "StartTransaction()",
    ):
        if forbidden in text:
            errors.append("GeneratedSemanticTagRuntimeHealthService must remain read-only; forbidden token: " + forbidden)

if AGGREGATE.is_file():
    text = AGGREGATE.read_text(encoding="utf-8")
    for token in (
        "GeneratedGridAnnotationRuntimeHealthService.Inspect(document, project)",
        "GeneratedSemanticTagRuntimeHealthService.Inspect(document, project)",
    ):
        if token not in text:
            errors.append("GeneratedSolidRuntimeHealthService.cs missing native annotation runtime-health aggregation: " + token)

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DTAGHEALTH", CommandFlags.Modal)]',
        "new GeneratedSemanticTagHealthService().Inspect(project)",
        "GeneratedSemanticTagRuntimeHealthService.Inspect(document, project)",
        "issues.Take(100)",
        "CadHandleService.Resolve(document, handles)",
        "SetImpliedSelection",
    ):
        if token not in text:
            errors.append("SemanticTagHealthCommands.cs missing combined persisted/live health command contract: " + token)
    for forbidden in (
        "SemanticTagBuilder.Build",
        "SemanticTagRemovalService.Remove",
        ".Erase()",
        "ProjectStateSnapshot",
    ):
        if forbidden in text:
            errors.append("QS3DTAGHEALTH must not mutate tag/project state; forbidden token: " + forbidden)

print("QS3D semantic-tag live runtime health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: live semantic-tag health is read-only, validates native MText/XData/content/placement state, is aggregated with existing Grid/generated-solid runtime health and remains directly runnable through QS3DTAGHEALTH.")
