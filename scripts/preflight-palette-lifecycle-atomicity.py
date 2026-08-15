#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing PaletteCoordinator.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    ensure_start = text.find("public static void EnsureCreated()")
    show_start = text.find("public static void Show()", ensure_start + 1)
    ensure = text[ensure_start:show_start] if ensure_start >= 0 and show_start > ensure_start else ""
    required_ensure = (
        "if (_workspace != null && _right != null && _quantityInsight != null) return;",
        "if (_workspace != null || _right != null || _quantityInsight != null) DisposeCore(false);",
        "var layout = UserUiLayoutStore.Get();",
        "try",
        "_workspacePanel = new WorkspacePanel();",
        "_rightPanel = new RightPanel();",
        "_quantityInsightPanel = new QuantityInsightPanel();",
        '_workspace = new PaletteSet("QS3D — Mô hình", WorkspaceGuid)',
        '_workspace.AddVisual("Mô hình", _workspacePanel, true);',
        '_right = new PaletteSet("QS3D — Bản vẽ & Lớp", RightGuid)',
        '_right.AddVisual("Quản lý", _rightPanel, true);',
        '_quantityInsight = new PaletteSet("QS3D — Diễn giải khối lượng", QuantityInsightGuid)',
        '_quantityInsight.AddVisual("Khối lượng", _quantityInsightPanel, true);',
        "catch",
        "DisposeCore(false);",
        "throw;",
    )
    cursor = 0
    for token in required_ensure:
        pos = ensure.find(token, cursor)
        if pos < 0:
            errors.append("EnsureCreated missing ordered fail-atomic palette creation contract: " + token)
            break
        cursor = pos + len(token)

    if "if (_workspace != null || _right != null || _quantityInsight != null) Dispose();" in ensure:
        errors.append("partial palette recovery must not persist incomplete palette dimensions")

    dispose_start = text.find("public static void Dispose()")
    persist_start = text.find("private static void PersistPaletteLayout()", dispose_start + 1)
    dispose = text[dispose_start:persist_start] if dispose_start >= 0 and persist_start > dispose_start else ""
    required_dispose = (
        "DisposeCore(true);",
        "private static void DisposeCore(bool persistLayout)",
        "if (persistLayout) PersistPaletteLayout();",
        "DisposePalette(ref _workspace);",
        "DisposePalette(ref _right);",
        "DisposePalette(ref _quantityInsight);",
        "_workspacePanel = null;",
        "_rightPanel = null;",
        "_quantityInsightPanel = null;",
        "private static void DisposePalette(ref PaletteSet? palette)",
        "var current = palette;",
        "palette = null;",
        "if (current == null) return;",
        "try { current.Dispose(); }",
        "catch",
    )
    cursor = 0
    for token in required_dispose:
        pos = dispose.find(token, cursor)
        if pos < 0:
            errors.append("palette teardown missing ordered best-effort ownership contract: " + token)
            break
        cursor = pos + len(token)

    helper_start = dispose.find("private static void DisposePalette(ref PaletteSet? palette)")
    helper = dispose[helper_start:] if helper_start >= 0 else ""
    null_pos = helper.find("palette = null;")
    native_dispose_pos = helper.find("current.Dispose();")
    if null_pos < 0 or native_dispose_pos < 0 or null_pos >= native_dispose_pos:
        errors.append("palette ownership must be cleared before native Dispose so a throwing native teardown cannot retain published ownership")

    for forbidden in (
        "_workspace.Dispose();",
        "_right.Dispose();",
        "_quantityInsight.Dispose();",
    ):
        if forbidden in text:
            errors.append("palette teardown must remain isolated through DisposePalette, found direct dispose: " + forbidden)

    # Existing behavior boundaries now flow through one visibility helper so Show/Reset cannot drift.
    for token in (
        "PersistPaletteLayout();",
        "ResetPreservingVisibility();",
        "var workspaceVisible = IsWorkspaceVisible;",
        "var rightVisible = IsRightPanelVisible;",
        "var quantityVisible = IsQuantityInsightVisible;",
        "SetVisibility(workspaceVisible, rightVisible, quantityVisible);",
        "private static void SetVisibility(bool workspace, bool right, bool quantityInsight)",
        "if (_workspace != null) _workspace.Visible = workspace;",
        "if (_right != null) _right.Visible = right;",
        "if (_quantityInsight != null) _quantityInsight.Visible = quantityInsight;",
        "UserUiLayoutStore.Update(layout =>",
    ):
        if token not in text:
            errors.append("palette lifecycle hardening lost centralized layout/visibility behavior: " + token)

print("QS3D palette lifecycle atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: palette creation rolls back partial ownership without persisting incomplete dimensions; teardown isolates native Dispose and reset visibility flows through one centralized helper.")
