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
        'candidate.Closed += (_, __) => ReleaseCandidate(candidate);',
        '_unpublishedCandidate = candidate;',
        '_publicationInFlightCandidate = candidate;',
        'Application.ShowModelessWindow(IntPtr.Zero, candidate, true);',
        'if (!candidate.IsLoaded)',
        'CloseUnpublishedCandidate(candidate)',
        'if (candidate.IsLoaded)',
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
        'candidate.Closed += (_, __) => ReleasePublishedWindow(candidate);',
    )
    for token in forbidden:
        if token in text:
            errors.append("Audit command must not regress protected failure/publication semantics: " + token)

    # Preserve read-only project access and stable/redacted host-facing errors while
    # proving the stronger atomic publication contract. The exact candidate must be
    # reserved as unpublished + publication-in-flight before native publication can
    # reenter, and publishing singleton authority remains conditional on IsLoaded.
    show_start = text.find("public void ShowAuditLog()")
    prepare_start = text.find("private static bool PrepareUnpublishedCandidate", show_start + 1)
    show = text[show_start:prepare_start] if show_start >= 0 and prepare_start > show_start else ""
    construct_pos = show.find("var candidate = new AuditLogWindow(document);")
    closed_pos = show.find("candidate.Closed += (_, __) => ReleaseCandidate(candidate);", construct_pos + 1)
    unpublished_pos = show.find("_unpublishedCandidate = candidate;", closed_pos + 1)
    inflight_pos = show.find("_publicationInFlightCandidate = candidate;", unpublished_pos + 1)
    show_pos = show.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);", inflight_pos + 1)
    loaded_guard_pos = show.find("if (!candidate.IsLoaded)", show_pos + 1)
    loaded_publish_pos = show.find("if (candidate.IsLoaded)", loaded_guard_pos + 1)
    publish_window_pos = show.find("_window = candidate;", loaded_publish_pos + 1)
    publish_identity_pos = show.find("_nativeDatabaseIdentity = nativeDatabaseIdentity;", publish_window_pos + 1)
    if min(
        construct_pos,
        closed_pos,
        unpublished_pos,
        inflight_pos,
        show_pos,
        loaded_guard_pos,
        loaded_publish_pos,
        publish_window_pos,
        publish_identity_pos,
    ) < 0:
        errors.append("Audit command cannot prove reserved candidate show/load/publication ordering")
    elif not (
        construct_pos
        < closed_pos
        < unpublished_pos
        < inflight_pos
        < show_pos
        < loaded_guard_pos
        < loaded_publish_pos
        < publish_window_pos
        < publish_identity_pos
    ):
        errors.append(
            "Audit command must construct -> attach exact Closed owner -> reserve unpublished -> reserve publication-in-flight -> show -> reject non-loaded -> confirm loaded -> publish window -> publish native identity"
        )

    # A failed native show must remain redacted and must attempt terminal cleanup
    # through the same exact unpublished candidate rather than leaking it.
    show_catch_pos = show.find("catch (System.Exception)", show_pos + 1)
    cleanup_in_catch_pos = show.find("CloseUnpublishedCandidate(candidate)", show_catch_pos + 1)
    redacted_in_catch_pos = show.find(
        'const string showFailure = "Nhật ký thay đổi lỗi: không thể mở nhật ký thay đổi.";',
        cleanup_in_catch_pos + 1,
    )
    if min(show_catch_pos, cleanup_in_catch_pos, redacted_in_catch_pos) < 0:
        errors.append("Audit command cannot prove redacted failed-publication cleanup ordering")
    elif not show_pos < show_catch_pos < cleanup_in_catch_pos < redacted_in_catch_pos:
        errors.append("Audit command failed-publication path must cleanup exact candidate before emitting stable redacted status")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DAUDIT keeps read-only/error-redaction behavior and atomically publishes only the exact loaded reserved candidate.")
