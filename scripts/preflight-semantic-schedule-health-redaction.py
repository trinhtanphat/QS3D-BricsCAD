#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/SemanticScheduleHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing SemanticScheduleHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '"SEMANTIC_SCHEDULE_CATALOG_INVALID"',
        '"Catalog SemanticSchedule không hợp lệ và không thể chẩn đoán chi tiết."',
        '"SEMANTIC_SCHEDULE_TEMPLATE_INVALID"',
        "catch (Exception ex) when (IsCatalogDataFailure(ex))",
        "catch (Exception ex) when (IsTemplateFailure(ex))",
        "invalid.Add(column.Header);",
        "private const int MaxIssues = 768;",
        "private const int MaxExamples = 5;",
    )
    for token in required:
        if token not in text:
            errors.append("missing semantic-schedule health redaction token: " + token)

    forbidden = (
        "+ ex.Message",
        "ex.Message +",
        "invalid.Add(column.Header + \" (\" + ex.Message + \")\");",
        "OpenMode.ForWrite",
        ".UpgradeOpen(",
        "project.Touch(",
        ".Save(",
        ".Erase(",
    )
    for token in forbidden:
        if token in text:
            errors.append("semantic-schedule health regressed redaction/read-only contract: " + token)

print("QS3D semantic-schedule Core health redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: semantic-schedule catalog/template failures remain bounded, redacted, and read-only.")
