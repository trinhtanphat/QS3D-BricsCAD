#!/usr/bin/env python3
# Lane-Key: issue-3274
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "XlsxRebarScheduleExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "BbsRegressionSmoke.cs"


def require(text: str, token: str, label: str) -> int:
    position = text.find(token)
    if position < 0:
        raise SystemExit(f"FAIL: missing {label}: {token}")
    return position


def main() -> int:
    exporter = EXPORTER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    snapshot_call = require(exporter, "var snapshot = SnapshotRows(rows);", "snapshot call")
    path_resolution = require(exporter, "var fullPath = Path.GetFullPath(path);", "filesystem boundary")
    count_snapshot = require(exporter, "var count = rows.Count;", "initial row Count snapshot")
    indexed_loop = require(exporter, "for (var index = 0; index < count; index++)", "indexed snapshot traversal")
    count_bind = require(exporter, "if (rows.Count != count)", "post-traversal Count binding")
    count_failure = require(
        exporter,
        'throw new InvalidOperationException("Rebar XLSX export row count changed during snapshot.");',
        "count-drift failure",
    )
    snapshot_return = require(exporter, "return snapshot;", "validated snapshot return")

    if not snapshot_call < path_resolution:
        raise SystemExit("FAIL: Rebar XLSX snapshot validation must finish before path resolution/filesystem output")
    if not (count_snapshot < indexed_loop < count_bind < count_failure < snapshot_return):
        raise SystemExit("FAIL: Rebar XLSX Count drift must bind after indexed traversal and before snapshot return")

    for token in (
        "XlsxRejectsCountDriftBeforeReplace();",
        "XlsxRejectsCountDriftBeforeDirectoryCreation();",
        "XlsxRebarScheduleExporter.Export(path, new CountDriftingBbsRows(ValidXlsxRow()))",
        "preserve-existing-rebar-xlsx-destination",
        "must-not-be-created",
        "row count changed during snapshot",
    ):
        require(smoke, token, "deterministic Rebar XLSX count-drift smoke contract")

    print(
        "PASS: Rebar XLSX binds its known row Count after indexed snapshot traversal, "
        "fails before filesystem output on drift, and keeps deterministic regression coverage registered."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
