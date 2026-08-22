#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "src/QS3D.BricsCAD.V25/Services/DirectDrawProjectPreviewContext.cs"
DISPATCHER = ROOT / "src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs"
errors = []

if not HELPER.is_file():
    errors.append("missing DirectDrawProjectPreviewContext.cs")
else:
    text = HELPER.read_text(encoding="utf-8")
    required = (
        "[ThreadStatic]",
        "private static DispatchPreviewScope? _dispatchScope;",
        "public static IDisposable BeginDispatchScope(Document document)",
        "CaptureCurrent(document)",
        "ReferenceEquals(scope.Document, document)",
        "return scope.Preview;",
        "private sealed class DispatchPreviewScope : IDisposable",
        "private readonly DispatchPreviewScope? _previous;",
        "if (!ReferenceEquals(_dispatchScope, this)) return;",
        "var previous = _previous;",
        "while (previous != null && previous._disposed)",
        "previous = previous._previous;",
        "_dispatchScope = previous;",
    )
    for token in required:
        if token not in text:
            errors.append("dispatch preview scope missing: " + token)

    if "_dispatchScope = _previous;" in text:
        errors.append("disposed nested scopes must not be restored directly into ambient dispatch state")

    begin = text.find("public static IDisposable BeginDispatchScope(Document document)")
    capture_current = text.find("CaptureCurrent(document)", begin)
    assign_scope = text.find("_dispatchScope = scope;", begin)
    if begin < 0 or capture_current < 0 or assign_scope < 0 or not (begin < capture_current < assign_scope):
        errors.append("dispatch scope must capture the current immutable preview before arming ambient state")

    capture = text.find("public static DirectDrawProjectPreviewContext Capture(Document document)")
    reuse = text.find("return scope.Preview;", capture)
    fallback = text.find("return CaptureCurrent(document);", capture)
    if capture < 0 or reuse < 0 or fallback < 0 or not (capture < reuse < fallback):
        errors.append("Capture must prefer the matching dispatch-scoped preview before live fallback")

    dispose = text.find("public void Dispose()")
    mark_disposed = text.find("_disposed = true;", dispose)
    current_guard = text.find("if (!ReferenceEquals(_dispatchScope, this)) return;", mark_disposed)
    previous = text.find("var previous = _previous;", current_guard)
    skip_disposed = text.find("while (previous != null && previous._disposed)", previous)
    walk_previous = text.find("previous = previous._previous;", skip_disposed)
    restore = text.find("_dispatchScope = previous;", walk_previous)
    if (
        dispose < 0
        or mark_disposed < 0
        or current_guard < 0
        or previous < 0
        or skip_disposed < 0
        or walk_previous < 0
        or restore < 0
        or not (dispose < mark_disposed < current_guard < previous < skip_disposed < walk_previous < restore)
    ):
        errors.append("Dispose must mark the scope, ignore non-current disposal, skip disposed ancestors, then restore live ambient state")

if not DISPATCHER.is_file():
    errors.append("missing ActiveFamilyQuickDrawCommands.cs")
else:
    text = DISPATCHER.read_text(encoding="utf-8")
    required = (
        "RequireCurrentDispatchSnapshot(",
        "using (DirectDrawProjectPreviewContext.BeginDispatchScope(document))",
        "Dispatch(document, dispatchFamily, advanced, operation);",
    )
    for token in required:
        if token not in text:
            errors.append("active-family dispatcher scope missing: " + token)

    route = text.find("var dispatchFamily = RequireCurrentDispatchSnapshot(")
    arm = text.find("using (DirectDrawProjectPreviewContext.BeginDispatchScope(document))", route)
    dispatch = text.find("Dispatch(document, dispatchFamily, advanced, operation);", arm)
    if route < 0 or arm < 0 or dispatch < 0 or not (route < arm < dispatch):
        errors.append("active-family routing must be validated before the preview scope is armed and before target dispatch")

if errors:
    print("Active Family dispatch preview scope preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Active Family dispatch preview scope preflight PASS")
