#!/usr/bin/env python3
"""Guard the BLT Start Center rounded interactive-perimeter contract."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE_REL = Path("src/QS3D.BricsCAD.V25/UI/BltStartCenterPanel.cs")
DOC_REL = Path("docs/UI-INTERACTION-STYLE-TEMPLATE.md")


def require(text: str, token: str, rel: Path) -> None:
    if token not in text:
        raise AssertionError(f"{rel}: missing required contract token: {token!r}")


def forbid(text: str, token: str, rel: Path) -> None:
    if token in text:
        raise AssertionError(f"{rel}: forbidden legacy token remains: {token!r}")


def main() -> int:
    source = (ROOT / SOURCE_REL).read_text(encoding="utf-8")
    doc = (ROOT / DOC_REL).read_text(encoding="utf-8")

    required_source = [
        "private static readonly CornerRadius InteractiveCornerRadius = new CornerRadius(5);",
        "private static readonly ControlTemplate ScrollThumbTemplate = CreateCompactScrollThumbTemplate();",
        "style.Setters.Add(new Setter(Control.BorderBrushProperty, ShellBorderBrush));",
        "style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));",
        "style.Setters.Add(new Setter(Control.TemplateProperty, ScrollThumbTemplate));",
        "private static ControlTemplate CreateCompactScrollThumbTemplate()",
        "root.SetValue(Border.CornerRadiusProperty, InteractiveCornerRadius);",
        "CornerRadius = InteractiveCornerRadius,",
        "Padding = new Thickness(8, 2, 8, 2),",
        "button.MouseEnter += (_, __) => frame.Background = PanelHoverBrush;",
        "Margin = new Thickness(0, 0, 0, 5),",
        "private static Button CreateClickSurface(UIElement content, Cursor cursor)",
        "BorderBrush = Brushes.Transparent,",
        "BorderThickness = new Thickness(0),",
    ]
    for token in required_source:
        require(source, token, SOURCE_REL)

    # The clickable recent-project row must now own a complete rounded perimeter,
    # not the former bottom-only separator.
    forbid(source, "BorderThickness = new Thickness(0, 0, 0, 1),", SOURCE_REL)

    required_doc = [
        "Every **visible interactive surface** must own exactly one visible perimeter",
        "Use **one perimeter owner per visible interaction target**.",
        "transparent nested `Button`, `RepeatButton`, hit-test proxy",
        "the draggable thumb/handle is an interactive perimeter owner",
        "recent-project rows use a full rounded perimeter",
        "transparent internal click proxies remain borderless",
    ]
    for token in required_doc:
        require(doc, token, DOC_REL)

    print(
        "PASS: BLT Start Center visible buttons, clickable rows and draggable thumb "
        "use one rounded visible perimeter while nested transparent hit proxies remain borderless."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        raise SystemExit(1)
