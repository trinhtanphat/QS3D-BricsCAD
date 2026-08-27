#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerReviewUi.cs"


def require(text: str, needle: str, message: str) -> None:
    if needle not in text:
        raise SystemExit("FAIL: " + message)


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    require(text, "RequireToken(projectId, nameof(projectId))", "ProjectId must pass through the canonical token guard")
    require(text, "RequireToken(drawingFingerprint, nameof(drawingFingerprint))", "DrawingFingerprint must pass through the canonical token guard")
    require(text, "char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])", "surrounding whitespace must fail closed")
    require(text, "value.Any(char.IsControl)", "control characters must fail closed")
    require(text, "return value;", "validated provenance must be preserved exactly")
    require(text, "StringComparison.Ordinal", "project and drawing provenance must retain exact ordinal matching")

    method_start = text.index("private static string RequireToken")
    method_end = text.index("private sealed class Controller", method_start)
    method = text[method_start:method_end]
    if ".Trim(" in method or ".Trim()" in method:
        raise SystemExit("FAIL: RequireToken must not normalize provenance with Trim")

    print("PASS: coordination review provenance tokens are exact and fail closed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
