#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
errors = []

if not path.is_file():
    errors.append("missing WorkspacePanel.xaml.cs")
else:
    text = path.read_text(encoding="utf-8")
    required = (
        "ConfigureWorkspaceInteractions();",
        "PreviewKeyDown += OnWorkspacePreviewKeyDown;",
        "FamilyList.PreviewMouseRightButtonDown += OnFamilyListPreviewMouseRightButtonDown;",
        "InspectionList.PreviewMouseRightButtonDown += OnInspectionListPreviewMouseRightButtonDown;",
        'CreateMenuItem("Nhân bản Family", OnAddClick)',
        'CreateMenuItem("Xóa Family", OnDeleteClick)',
        'CreateMenuItem("Bóc đối tượng CAD đang chọn", OnCaptureSelectedClick)',
        'CreateMenuItem("Vẽ / Cập nhật 3D", OnView3DClick)',
        'CreateMenuItem("Focus", OnFocusSelectedClick)',
        'CreateMenuItem("Cô lập", OnIsolateSelectedClick)',
        'CreateMenuItem("Khôi phục cô lập", OnUnisolateClick)',
        'CreateMenuItem("Định vị / Zoom chọn", OnLocateSelectedClick)',
        'CreateMenuItem("Mặt bằng", OnTopViewClick)',
        "modifiers == ModifierKeys.Control && e.Key == Key.S",
        "modifiers == ModifierKeys.Control && e.Key == Key.F",
        "modifiers == ModifierKeys.Control && e.Key == Key.B",
        "modifiers == ModifierKeys.None && e.Key == Key.F5",
        "e.Key == Key.Delete && FamilyList.IsKeyboardFocusWithin",
        "FamilySearch.Focus();",
        "FamilySearch.SelectAll();",
        "OnSaveClick(this, new RoutedEventArgs());",
        "OnQuantityClick(this, new RoutedEventArgs());",
        "OnRefreshClick(this, new RoutedEventArgs());",
        "OnDeleteClick(this, new RoutedEventArgs());",
        "item.IsSelected = true;",
        'TryFindResource("Bg2Brush") as Brush',
        'TryFindResource("TextBrush") as Brush',
        'TryFindResource("BorderStrongBrush") as Brush',
        "private void Send(string command)",
        "var normalized = (command ?? string.Empty).Trim();",
        'document.SendStringToExecute(normalized + " ", true, false, false)',
    )
    for needle in required:
        if needle not in text:
            errors.append("Workspace interaction contract missing: " + needle)

    delete_guard = text.find("e.Key == Key.Delete && FamilyList.IsKeyboardFocusWithin")
    delete_call = text.find("OnDeleteClick(this, new RoutedEventArgs());", delete_guard)
    if delete_guard < 0 or delete_call < delete_guard:
        errors.append("Delete shortcut must remain scoped to keyboard focus within FamilyList")

    shortcut_start = text.find("private void OnWorkspacePreviewKeyDown")
    send_start = text.find("private void Send(string command)")
    if shortcut_start < 0 or send_start < 0 or shortcut_start > send_start:
        errors.append("Workspace keyboard routing must remain inside WorkspacePanel and reuse the guarded instance Send path")

    forbidden = (
        'SendStringToExecute("QS3DSAVE',
        'SendStringToExecute("QS3DBQ',
        'SendStringToExecute("QS3DBUILD3D',
    )
    for needle in forbidden:
        if needle in text:
            errors.append("Workspace shortcut/context code must not duplicate raw CAD command dispatch: " + needle)

print("QS3D Workspace interaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Workspace keyboard shortcuts/right-click actions reuse guarded handlers and the normalized instance Send path; Delete stays scoped to FamilyList focus.")
