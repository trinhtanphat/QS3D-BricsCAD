#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/AuditCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing AuditCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DAUDIT", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'var candidate = new AuditLogWindow(document);',
        'candidate.Closed += (_, __) => ReleasePublishedWindow(candidate);',
        'Application.ShowModelessWindow(IntPtr.Zero, candidate, true);',
        'if (!candidate.IsLoaded) return;',
        '_window = candidate;',
        '_nativeDatabaseIdentity = nativeDatabaseIdentity;',
        'Đã mở Nhật ký thay đổi • chưa có QS3D project hiện hữu; không tạo project mới.',
        'const string status = "Nhật ký thay đổi lỗi: không thể mở nhật ký thay đổi.";',
        'document.Editor.WriteMessage("\\nQS3DAUDIT error: không thể mở nhật ký thay đổi.")',
        'PaletteCoordinator.SetStatus(status)',
    )
    for token in required:
        if token not in text:
            errors.append("Audit command contract missing token: " + token)

    forbidden = (
        'catch (System.Exception ex)',
        'ex.Message',
        'QS3DAUDIT error: " +',
        'Nhật ký thay đổi lỗi: " +',
        '_window = new AuditLogWindow(document);',
        '_window.Closed += (_, __) => _window = null;',
        'Application.ShowModelessWindow(IntPtr.Zero, _window, true);',
    )
    for token in forbidden:
        if token in text:
            errors.append("Audit command must not regress protected failure/publication semantics: " + token)

    # Keep the original read-only + redacted-error contract, and additionally
    # prove the fail-closed single-instance publication ordering introduced by
    # the modeless lifecycle fix. Publication must happen only after the exact
    # candidate was shown successfully and is still loaded.
    show_start = text.find("public void ShowAuditLog()")
    prepare_start = text.find("private static bool PreparePublishedWindow", show_start + 1)
    show = text[show_start:prepare_start] if show_start >= 0 and prepare_start > show_start else ""
    construct_pos = show.find("var candidate = new AuditLogWindow(document);")
    closed_pos = show.find("candidate.Closed += (_, __) => ReleasePublishedWindow(candidate);", construct_pos + 1)
    show_pos = show.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);", closed_pos + 1)
    loaded_pos = show.find("if (!candidate.IsLoaded) return;", show_pos + 1)
    publish_window_pos = show.find("_window = candidate;", loaded_pos + 1)
    publish_identity_pos = show.find("_nativeDatabaseIdentity = nativeDatabaseIdentity;", publish_window_pos + 1)
    if min(construct_pos, closed_pos, show_pos, loaded_pos, publish_window_pos, publish_identity_pos) < 0:
        errors.append("Audit command cannot prove candidate show/load/publication ordering")
    elif not (
        construct_pos
        < closed_pos
        < show_pos
        < loaded_pos
        < publish_window_pos
        < publish_identity_pos
    ):
        errors.append(
            "Audit command must construct candidate -> attach exact Closed owner -> show -> confirm loaded -> publish window -> publish native identity"
        )

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DAUDIT keeps read-only/error-redaction behavior and publishes the exact modeless candidate only after successful loaded confirmation.")
