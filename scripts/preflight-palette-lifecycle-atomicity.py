#!/usr/bin/env python3
# Lane-Key: issue-4297
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
errors = []


def method_block(text: str, signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    brace = text.find("{", start)
    if brace < 0:
        errors.append("missing method body: " + signature)
        return ""
    depth = 0
    for index in range(brace, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[start:index + 1]
    errors.append("unterminated method: " + signature)
    return ""


if not SOURCE.is_file():
    errors.append("missing PaletteCoordinator.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    ensure = method_block(text, "public static void EnsureCreated()")
    required_ensure = (
        "if (_workspace != null && _properties != null && _right != null && _quantityInsight != null) return;",
        "if (_workspace != null || _properties != null || _right != null || _quantityInsight != null) DisposeCore(false);",
        "var layout = UserUiLayoutStore.Get();",
        "try",
        "_workspacePanel = new WorkspacePanel();",
        "_propertiesVisual = _workspacePanel.CreatePropertiesPaletteVisual();",
        "_rightPanel = new RightPanel();",
        "_quantityInsightPanel = new QuantityInsightPanel();",
        '_workspace = CreatePaletteSet(',
        '"QS3D — Mô hình"',
        '"Mô hình"',
        '_workspacePanel);',
        '_properties = CreatePaletteSet(',
        '"QS3D — Thuộc tính"',
        '"Thuộc tính"',
        '_propertiesVisual);',
        "_properties.StateChanged += OnPropertiesPaletteStateChanged;",
        '_right = CreatePaletteSet(',
        '"QS3D — Bản vẽ & Lớp"',
        '"Quản lý"',
        '_rightPanel);',
        '_quantityInsight = CreatePaletteSet(',
        '"QS3D — Diễn giải khối lượng"',
        '"Khối lượng"',
        '_quantityInsightPanel);',
        "catch",
        "DisposeCore(false);",
        "throw;",
    )
    cursor = 0
    for token in required_ensure:
        pos = ensure.find(token, cursor)
        if pos < 0:
            errors.append("EnsureCreated missing ordered publication/sibling rollback contract: " + token)
            break
        cursor = pos + len(token)

    for field in ("_workspace", "_properties", "_right", "_quantityInsight"):
        if f"{field} = new PaletteSet(" in ensure:
            errors.append(
                "native PaletteSet must not be field-published through an object initializer before configuration rollback is armed: "
                + field
            )

    create = method_block(text, "private static PaletteSet CreatePaletteSet(")
    required_create = (
        "PaletteSet? palette = null;",
        "try",
        "palette = new PaletteSet(title, guid);",
        "palette.DockEnabled = DockSides.Left | DockSides.Right;",
        "palette.Dock = dock;",
        "palette.Visible = false;",
        "palette.KeepFocus = false;",
        "palette.MinimumSize = minimumSize;",
        "palette.DeviceIndependentSize = initialSize;",
        "palette.AddVisual(visualTitle, visual, true);",
        "return palette;",
        "catch",
        "if (palette != null)",
        "try { palette.Dispose(); }",
        "catch",
        "throw;",
    )
    cursor = 0
    for token in required_create:
        pos = create.find(token, cursor)
        if pos < 0:
            errors.append("CreatePaletteSet missing ordered pre-publication rollback contract: " + token)
            break
        cursor = pos + len(token)

    if create:
        constructor = create.find("palette = new PaletteSet(title, guid);")
        first_config = create.find("palette.DockEnabled = DockSides.Left | DockSides.Right;")
        add_visual = create.find("palette.AddVisual(visualTitle, visual, true);")
        publish_return = create.find("return palette;")
        rollback = create.find("try { palette.Dispose(); }")
        rethrow = create.rfind("throw;")
        if not (0 <= constructor < first_config < add_visual < publish_return < rollback < rethrow):
            errors.append(
                "CreatePaletteSet must own the exact native instance locally through configuration/AddVisual, return only after success, and dispose it before rethrow on failure."
            )

    dispose = method_block(text, "private static void DisposeCore(bool persistLayout)")
    required_dispose = (
        "if (persistLayout) PersistPaletteLayout();",
        "UnsubscribeFromPropertiesPaletteStateChanges();",
        "DisposePalette(ref _properties);",
        "DisposePalette(ref _workspace);",
        "DisposePalette(ref _right);",
        "DisposePalette(ref _quantityInsight);",
        "_workspacePanel = null;",
        "_propertiesVisual = null;",
        "_rightPanel = null;",
        "_quantityInsightPanel = null;",
    )
    cursor = 0
    for token in required_dispose:
        pos = dispose.find(token, cursor)
        if pos < 0:
            errors.append("palette teardown missing ordered best-effort four-palette ownership contract: " + token)
            break
        cursor = pos + len(token)

    dispose_palette = method_block(text, "private static void DisposePalette(ref PaletteSet? palette)")
    for token in (
        "var current = palette;",
        "palette = null;",
        "if (current == null) return;",
        "try { current.Dispose(); }",
        "catch",
    ):
        if token not in dispose_palette:
            errors.append("published palette teardown must remain isolated through DisposePalette: " + token)

    for forbidden in (
        "_workspace.Dispose();",
        "_properties.Dispose();",
        "_right.Dispose();",
        "_quantityInsight.Dispose();",
    ):
        if forbidden in text:
            errors.append("palette teardown must remain isolated through DisposePalette: " + forbidden)

    for token in (
        "PersistPaletteLayout();",
        "ResetPreservingVisibility();",
        "var workspaceVisible = IsWorkspaceVisible;",
        "var propertiesVisible = IsPropertiesVisible;",
        "var rightVisible = IsRightPanelVisible;",
        "var quantityVisible = IsQuantityInsightVisible;",
        "var ownerReferenceBimActive = workspaceVisible && rightVisible && !propertiesVisible && !quantityVisible;",
        "_workspacePanel?.SetDedicatedPropertiesPaletteActive(propertiesVisible);",
        "SetVisibility(workspaceVisible, propertiesVisible, rightVisible, quantityVisible);",
        "private static void SetVisibility(bool workspace, bool properties, bool right, bool quantityInsight)",
        "if (_workspace != null) _workspace.Visible = workspace;",
        "if (_properties != null) _properties.Visible = properties;",
        "if (_right != null) _right.Visible = right;",
        "if (_quantityInsight != null) _quantityInsight.Visible = quantityInsight;",
        "UserUiLayoutStore.Update(layout =>",
    ):
        if token not in text:
            errors.append("palette lifecycle lost centralized layout/visibility behavior: " + token)

print("QS3D palette lifecycle atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: four native palettes remain locally owned and rollback-disposable through configuration/AddVisual, publish only after success, and retain sibling teardown/layout/visibility contracts.")
