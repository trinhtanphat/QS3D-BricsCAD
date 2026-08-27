#!/usr/bin/env python3
"""Guard RecognitionWindow read-only bindings against WPF TwoWay regressions."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml"
ENGINE = ROOT / "src/QS3D.Core/Recognition/RecognitionEngine.cs"


def fail(message: str) -> None:
    print("FAIL: " + message)
    raise SystemExit(1)


def main() -> int:
    xaml = XAML.read_text(encoding="utf-8")
    engine = ENGINE.read_text(encoding="utf-8")

    result_match = re.search(
        r"public sealed class RecognitionResult\s*\{(?P<body>.*?)\n\s*public sealed class RecognitionBatch",
        engine,
        re.DOTALL,
    )
    if result_match is None:
        fail("RecognitionResult source boundary was not found")

    result_body = result_match.group("body")
    requires_review = re.search(
        r"public bool RequiresReview\s*\{\s*get\s*\{.*?\}\s*\}",
        result_body,
        re.DOTALL,
    )
    if requires_review is None:
        fail("RecognitionResult.RequiresReview must remain a getter-only display property")
    if re.search(r"public bool RequiresReview\s*\{.*?\bset\s*\{", result_body, re.DOTALL):
        fail("RecognitionResult.RequiresReview unexpectedly became writable")

    expected = 'Binding="{Binding RequiresReview, Mode=OneWay}"'
    if expected not in xaml:
        fail("RecognitionWindow RequiresReview checkbox must bind explicitly with Mode=OneWay")

    if re.search(r'Binding="\{Binding RequiresReview(?:\}|,\s*Mode=(?:TwoWay|OneWayToSource)\})"', xaml):
        fail("RecognitionWindow RequiresReview binding is update-capable")

    print("PASS: RecognitionWindow binds getter-only RequiresReview as explicit OneWay display state")
    return 0


if __name__ == "__main__":
    sys.exit(main())
