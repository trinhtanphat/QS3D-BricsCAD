#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedSemanticTagHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '"SEMANTIC_TAG_RENDER_INVALID"',
        '"Không thể render lại semantic tag vì semantic/project data không hợp lệ."',
        "catch (Exception ex) when (IsDiagnosticDataFailure(ex))",
        "exception is InvalidOperationException",
        "exception is ArgumentException",
        "exception is FormatException",
        "exception is OverflowException",
        "exception is KeyNotFoundException",
        "exception is NullReferenceException",
    )
    for token in required:
        if token not in text:
            errors.append("missing semantic-tag health render isolation token: " + token)

    forbidden = (
        '"Không thể render lại semantic tag: " + ex.Message',
        "+ ex.Message",
        "catch (Exception ex)\n",
        "catch (Exception ex)\r\n",
        "OpenMode.ForWrite",
        ".UpgradeOpen(",
        "project.Touch(",
        ".Save(",
        ".Erase(",
    )
    for token in forbidden:
        if token in text:
            errors.append("semantic-tag health render path regressed redaction/read-only contract: " + token)

print("QS3D semantic-tag Core health render-redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: semantic-tag render data failures are redacted, bounded, and read-only.")
