#!/usr/bin/env python3
# Lane-Key: door-opening-xlsx-row-count-integrity-20260820
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "DoorOpeningXlsxExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DoorOpeningXlsxCountIntegritySmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"


def require(text: str, token: str, label: str) -> int:
    position = text.find(token)
    if position < 0:
        raise SystemExit(f"FAIL: missing {label}: {token}")
    return position


def main() -> int:
    exporter = EXPORTER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    row_count = require(exporter, "var rowCount = rows.Count;", "top-level initial count snapshot")
    row_loop = require(exporter, "for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)", "top-level indexed snapshot traversal")
    row_bind = require(exporter, "if (rows.Count != rowCount)", "top-level post-traversal count binding")
    row_failure = require(exporter, 'throw new InvalidOperationException("Door/opening XLSX export row count changed during snapshot.");', "top-level count-drift failure")
    cell_validation = require(exporter, "ValidateCellText(snapshot);", "cell validation boundary")
    path_resolution = require(exporter, "var fullPath = Path.GetFullPath(path);", "filesystem mutation boundary")

    nested_count = require(exporter, "var count = source.Count;", "nested initial count snapshot")
    nested_loop = require(exporter, "for (var index = 0; index < count; index++)", "nested indexed snapshot traversal")
    nested_bind = require(exporter, "if (source.Count != count)", "nested post-traversal count binding")
    nested_failure = require(exporter, 'throw new InvalidOperationException("Door/opening XLSX " + label + " count changed during snapshot.");', "nested count-drift failure")

    if not (row_count < row_loop < row_bind < row_failure < cell_validation < path_resolution):
        raise SystemExit("FAIL: top-level count drift must bind after traversal and before validation/filesystem output")
    if not (nested_count < nested_loop < nested_bind < nested_failure):
        raise SystemExit("FAIL: nested ElementIds/HostIds count drift must bind after indexed traversal")

    for token in (
        "DoorOpeningXlsxExporter.Export(destination, new CountDriftingRows(ValidRow()))",
        "row count changed during snapshot",
        "preserve-existing-door-opening-destination",
        "must-not-be-created",
    ):
        require(smoke, token, "deterministic count-drift smoke contract")
    require(registration, "DoorOpeningXlsxCountIntegritySmoke.Run();", "smoke registration")

    print(
        "PASS: Door/opening XLSX binds top-level and nested known Count values after indexed snapshots, "
        "fails before filesystem output on drift, and keeps deterministic regression coverage registered."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
