#!/usr/bin/env python3
# Lane-Key: issue-3271
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "CurtainWallXlsxExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "XlsxScheduleNullRowSmoke.cs"


def require(text: str, token: str, label: str) -> int:
    position = text.find(token)
    if position < 0:
        raise SystemExit(f"FAIL: missing {label}: {token}")
    return position


def main() -> int:
    exporter = EXPORTER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    row_count = require(exporter, "var rowCount = rows.Count;", "initial row Count snapshot")
    row_loop = require(exporter, "for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)", "indexed snapshot traversal")
    row_bind = require(exporter, "if (rows.Count != rowCount)", "post-traversal Count binding")
    row_failure = require(
        exporter,
        'throw new InvalidOperationException("Curtain XLSX export row count changed during snapshot.");',
        "count-drift failure",
    )
    path_resolution = require(exporter, "var fullPath = Path.GetFullPath(path);", "filesystem boundary")

    if not (row_count < row_loop < row_bind < row_failure < path_resolution):
        raise SystemExit("FAIL: Curtain XLSX Count drift must fail after traversal and before filesystem output")

    for token in (
        "AssertCurtainWallCountDriftFailsBeforeExistingDestinationReplacement();",
        "AssertCurtainWallCountDriftFailsBeforeFilesystemCreation();",
        "CurtainWallXlsxExporter.Export(destination, new CurtainCountDriftingRows(ValidCurtainWallRow()))",
        "preserve-existing-curtain-wall-destination",
        "must-not-be-created",
        "row count changed during snapshot",
    ):
        require(smoke, token, "deterministic Curtain XLSX count-drift smoke contract")

    print(
        "PASS: Curtain XLSX binds its known row Count after indexed snapshot traversal, "
        "fails before filesystem output on drift, and keeps deterministic regression coverage registered."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
