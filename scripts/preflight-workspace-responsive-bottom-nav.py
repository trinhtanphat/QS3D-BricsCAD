#!/usr/bin/env python3
"""Fail closed unless WorkspacePanel keeps the #4147 responsive bottom-nav contract."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ResponsiveBottomNavigation.cs"


def fail(message: str) -> None:
    print("ERROR: workspace responsive bottom-nav preflight failed closed: " + message, file=sys.stderr)
    raise SystemExit(1)


if not RUNTIME.is_file():
    fail("missing responsive workspace source: " + str(RUNTIME.relative_to(ROOT)))

source = RUNTIME.read_text(encoding="utf-8")

required = (
    "protected override void OnInitialized(EventArgs e)",
    "ApplyResponsiveWorkspaceShell();",
    "WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;",
    "WorkspaceOverflow.PanningMode = PanningMode.None;",
    "WorkspaceContentRoot.MinWidth = 0d;",
    "new GridLength(2.2d, GridUnitType.Star)",
    "new GridLength(3.2d, GridUnitType.Star)",
    "new GridLength(2.1d, GridUnitType.Star)",
    "body.ColumnDefinitions[0].MinWidth = 0d;",
    "body.ColumnDefinitions[2].MinWidth = 0d;",
    "body.ColumnDefinitions[4].MinWidth = 0d;",
    "WorkspaceContentRoot.RowDefinitions[2].Height = new GridLength(42d);",
    'CreateNavigationButton("Mô hình"',
    'CreateNavigationButton("Cấu kiện"',
    'CreateNavigationButton("Hoàn thiện"',
    'CreateNavigationButton("Thống kê"',
    'CreateNavigationButton("⋯"',
    'CreateMoreMenuItem("Kiểm tra mô hình"',
    'CreateMoreMenuItem("Làm mới"',
    "OnQuantityClick(sender, e);",
    "OnHealthClick(sender, e);",
    "OnRefreshClick(sender, e);",
    "FamilyList.Focus();",
    "ModelTree.Focus();",
)

for token in required:
    if token not in source:
        fail("responsive workspace source is missing: " + token)

if "HorizontalScrollBarVisibility = ScrollBarVisibility.Auto" in source:
    fail("responsive source must not restore automatic horizontal overflow")

print("PASS workspace responsive bottom navigation source contract")
