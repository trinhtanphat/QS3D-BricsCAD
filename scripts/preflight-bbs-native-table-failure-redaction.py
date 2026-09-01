#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PATH = ROOT / "src/QS3D.BricsCAD.V25/BbsNativeTableCommands.cs"
text = PATH.read_text(encoding="utf-8") if PATH.is_file() else ""
errors = []

if not text:
    errors.append("missing BbsNativeTableCommands.cs")

for token in (
    'catch (Exception) { ReportFailure(document, "QS3DBBSTABLE", "tạo/cập nhật BBS Table"); }',
    'catch (Exception) { ReportFailure(document, "QS3DBBSTABLEREFRESH", "refresh BBS Table"); }',
    'catch (Exception) { ReportFailure(document, "QS3DBBSTABLEREMOVE", "xóa BBS Table"); }',
    'catch (Exception) { ReportFailure(document, "QS3DBBSTABLEHEALTH", "kiểm tra BBS Table health"); }',
    'private const string PostCommitUiWarning = "BBS Table: thao tác CAD/project đã hoàn tất; viewport/UI chưa đồng bộ đầy đủ.";',
    'private static void ReportFailure(Document document, string command, string operation)',
    'Report(document, command + " lỗi: không thể " + operation + "; kiểm tra project/CAD state và thử lại.");',
    'private static bool TryWrite(Document document, string message)',
):
    if token not in text:
        errors.append("missing BBS Table redaction token: " + token)

for forbidden in ('catch (Exception ex)', 'ex.Message', 'exception.Message', 'GetBaseException()', 'StackTrace', 'UI sync warning:'):
    if forbidden in text:
        errors.append("raw host exception detail is reachable from BBS Table user reporting: " + forbidden)

def ordered(scope, label, *tokens):
    position = 0
    for token in tokens:
        found = scope.find(token, position)
        if found < 0:
            errors.append(label + " missing/late token: " + token)
            return
        position = found + len(token)

build_start = text.find('public void Build()')
refresh_start = text.find('public void Refresh()', build_start + 1)
remove_start = text.find('public void Remove()', refresh_start + 1)
health_start = text.find('public void Health()', remove_start + 1)
helper_start = text.find('private static QS3D.Core.Domain.ProjectState RequireExistingProject', health_start + 1)
finalize_start = text.find('private static void FinalizeUi', helper_start + 1)
report_failure_start = text.find('private static void ReportFailure', finalize_start + 1)

if min(build_start, refresh_start, remove_start, health_start, helper_start, finalize_start, report_failure_start) < 0:
    errors.append("unable to isolate BBS Table command/helper scopes")
else:
    build = text[build_start:refresh_start]
    refresh = text[refresh_start:remove_start]
    remove = text[remove_start:health_start]
    health = text[health_start:helper_start]
    finalize = text[finalize_start:report_failure_start]

    ordered(
        build,
        "Build freshness/native lifecycle",
        'ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)',
        'var expectedProjectId = previewProject.ProjectId;',
        'var expectedChangeVersion = previewProject.ChangeVersion;',
        'document.Editor.GetPoint(',
        'var project = RequireExistingProject(document, "BBS Table");',
        'project.ChangeVersion != expectedChangeVersion',
        'var regenerated = RegenerateSemantic(project);',
        'BbsNativeTableBuilder.Build(document, project, world)',
        'FinalizeUi(document,',
    )
    ordered(
        refresh,
        "Refresh native lifecycle",
        'RequireModelSpace(document);',
        'var project = RequireExistingProject(document, "BBS Table refresh");',
        'BbsNativeTableBuilder.StoredPosition(project)',
        'var regenerated = RegenerateSemantic(project);',
        'BbsNativeTableBuilder.Build(document, project, position)',
        'FinalizeUi(document,',
    )
    ordered(
        remove,
        "Remove native lifecycle",
        'var project = RequireExistingProject(document, "BBS Table remove");',
        'BbsNativeTableBuilder.Remove(document, project);',
        'FinalizeUi(document,',
    )
    if 'ProjectContextCoordinator.TryGetReadOnly(document, out var project)' not in health:
        errors.append("BBS Table Health must remain read-only")
    if 'RequireExistingProject(' in health or 'RegenerateSemantic(' in health or 'BbsNativeTableBuilder.Build(' in health or 'BbsNativeTableBuilder.Remove(' in health:
        errors.append("BBS Table Health acquired a mutation path")

    for token in (
        'try { document.Editor.Regen(); } catch { uiSyncFailed = true; }',
        'try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }',
        'try { PaletteCoordinator.SetStatus(message); } catch { uiSyncFailed = true; }',
        'if (!TryWrite(document, "\\nQS3D " + message)) uiSyncFailed = true;',
        'if (!uiSyncFailed) return;',
        'try { PaletteCoordinator.SetStatus(message + " • " + PostCommitUiWarning); } catch { }',
        'TryWrite(document, "\\nQS3D " + PostCommitUiWarning);',
    ):
        if token not in finalize:
            errors.append("post-commit UI sync is not independently fail-isolated: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: BBS native Table commands preserve freshness/native mutation ordering, redact caught host exceptions, keep Health read-only, and fail-isolate post-commit viewport/palette/editor synchronization with a stable durable-commit warning.")
