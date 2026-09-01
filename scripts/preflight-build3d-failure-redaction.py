#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = (
    'if (GeneratedSolidHandlesMatch(project, ownershipBefore))',
    'semanticRollback.Restore(project);',
    'new AggregateException(operationError, restoreError)',
    'native ownership đã thay đổi trước lỗi post-commit; giữ trạng thái đã commit để tránh lệch CAD/semantic.',
    'catch (Exception)\n            {\n                Report(document, "QS3DBUILD3D lỗi: không thể hoàn tất native rebuild cho selection hiện tại.");',
    'var uiSyncFailed = false;',
    'try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }',
    'try { document.Editor.Regen(); } catch { uiSyncFailed = true; }',
    'catch { uiSyncFailed = true; }',
    'try { PaletteCoordinator.SetStatus(status); } catch { uiSyncFailed = true; }',
    'try { document.Editor.WriteMessage("\\nQS3D " + status); } catch { uiSyncFailed = true; }',
    'Build 3D UI sync warning: native rebuild đã commit; một phần viewport/selection/UI không thể đồng bộ.',
)
for token in required:
    if token not in text:
        errors.append("missing Build3D stable/fail-isolated contract: " + token)

for forbidden in ("operationError.Message", "ex.Message", "uiError.Message", "exception.Message", "Exception.Message"):
    if forbidden in text:
        errors.append("raw exception detail remains in Build3D user-visible surface: " + forbidden)

ownership = text.find("var ownershipBefore = CaptureGeneratedSolidHandles")
regenerate = text.find(".RegenerateDirtySubset(project, regenerationScope)", ownership)
handoff = text.find("document.Editor.SetImpliedSelection(sourceIds.ToArray())", regenerate)
build = text.find("built = BuildCategory(document, project, category, sourceType)", handoff)
catch = text.find("catch (Exception operationError)", build)
match = text.find("if (GeneratedSolidHandlesMatch(project, ownershipBefore))", catch)
restore = text.find("semanticRollback.Restore(project);", match)
committed_report = text.find("native ownership đã thay đổi trước lỗi post-commit; giữ trạng thái đã commit", restore)
touch = text.find("project.Touch();", committed_report)
finalize_call = text.find("FinalizeUi(document, elementIds, sourceHandles, built, regenerated, category, project);", touch)
if min(ownership, regenerate, handoff, build, catch, match, restore, committed_report, touch, finalize_call) < 0 or not (
    ownership < regenerate < handoff < build < catch < match < restore < committed_report < touch < finalize_call
):
    errors.append("Build3D must preserve precommit rollback vs committed-ownership discrimination and only finalize UI after project.Touch")

finalize = text.find("private static void FinalizeUi")
write = text.find("private static void Write", finalize)
if finalize < 0 or write <= finalize:
    errors.append("Build3D FinalizeUi boundary is missing")
else:
    body = text[finalize:write]
    ordered = [
        body.find("PaletteCoordinator.RefreshProject()"),
        body.find("document.Editor.Regen()"),
        body.find("var generatedHandles = elementIds"),
        body.find("PaletteCoordinator.SetStatus(status)"),
        body.find('document.Editor.WriteMessage("\\nQS3D " + status)'),
        body.find("if (uiSyncFailed)"),
    ]
    if min(ordered) < 0 or ordered != sorted(ordered):
        errors.append("committed Build3D UI must preserve refresh -> regen -> generated selection -> status -> editor -> stable warning order")
    if body.count("catch") < 5:
        errors.append("committed Build3D Palette/Regen/selection/status/editor cells must fail independently")
    if "BuildCategory(" in body or "RegenerateDirtySubset" in body or "semanticRollback" in body:
        errors.append("FinalizeUi must remain UI-only after native/semantic commit")

if errors:
    print("Build3D failure redaction guard FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Build3D failure redaction guard PASS")
