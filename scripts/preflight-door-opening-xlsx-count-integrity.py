#!/usr/bin/env python3
# Lane-Key: door-opening-xlsx-row-count-integrity-20260820
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "DoorOpeningXlsxExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DoorOpeningXlsxCountIntegritySmoke.cs"
KNOWN_COUNT_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DoorOpeningXlsxKnownCountContractSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"


def require(text: str, token: str, label: str) -> int:
    position = text.find(token)
    if position < 0:
        raise SystemExit(f"FAIL: missing {label}: {token}")
    return position


def main() -> int:
    exporter = EXPORTER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    known_count_smoke = KNOWN_COUNT_SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    row_count = require(
        exporter,
        'var rowCount = RequireConsistentKnownCount(rows, rows.Count, MaxDataRows, "export rows");',
        "top-level consistent known-count snapshot",
    )
    row_loop = require(exporter, "for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)", "top-level indexed snapshot traversal")
    row_bind = require(exporter, "if (rows.Count != rowCount)", "top-level post-traversal count binding")
    row_failure = require(exporter, 'throw new InvalidOperationException("Door/opening XLSX export row count changed during snapshot.");', "top-level count-drift failure")
    cell_validation = require(exporter, "ValidateCellText(snapshot);", "cell validation boundary")
    path_resolution = require(exporter, "var fullPath = Path.GetFullPath(path);", "filesystem mutation boundary")

    nested_count = require(
        exporter,
        "var count = RequireConsistentKnownCount(source, source.Count, MaxCellTextLength + 1, label);",
        "nested consistent known-count snapshot",
    )
    nested_loop = require(exporter, "for (var index = 0; index < count; index++)", "nested indexed snapshot traversal")
    nested_bind = require(exporter, "if (source.Count != count)", "nested post-traversal count binding")
    nested_failure = require(exporter, 'throw new InvalidOperationException("Door/opening XLSX " + label + " count changed during snapshot.");', "nested count-drift failure")

    helper = require(exporter, "private static int RequireConsistentKnownCount<T>", "known-count consistency helper")
    read_only = require(exporter, "var readOnly = source as IReadOnlyCollection<T>;", "IReadOnlyCollection known count")
    generic = require(exporter, "var generic = source as ICollection<T>;", "generic ICollection known count")
    non_generic = require(exporter, "var nonGeneric = source as ICollection;", "non-generic ICollection known count")
    conflict = require(exporter, "exposes conflicting known collection counts", "known-count conflict rejection")
    negative = require(exporter, "count must be non-negative", "known-count negative rejection")
    maximum = require(exporter, "count exceeds the supported maximum", "known-count maximum rejection")

    if not (row_count < row_loop < row_bind < row_failure < cell_validation < path_resolution):
        raise SystemExit("FAIL: top-level known-count binding/drift checks must occur around traversal and before validation/filesystem output")
    if not (nested_count < nested_loop < nested_bind < nested_failure):
        raise SystemExit("FAIL: nested ElementIds/HostIds count binding must occur around indexed traversal")
    if not (helper < read_only < generic < non_generic):
        raise SystemExit("FAIL: known-count helper must bind read-only, generic and non-generic deterministic count interfaces")
    if min(conflict, negative, maximum) < helper:
        raise SystemExit("FAIL: known-count rejection contract must be implemented inside RequireConsistentKnownCount")

    for token in (
        "DoorOpeningXlsxExporter.Export(destination, new CountDriftingRows(ValidRow()))",
        "row count changed during snapshot",
        "preserve-existing-door-opening-destination",
        "must-not-be-created",
    ):
        require(smoke, token, "deterministic count-drift smoke contract")
    require(registration, "DoorOpeningXlsxCountIntegritySmoke.Run();", "count-drift smoke registration")

    for token in (
        "ConflictingKnownCountRows",
        "IReadOnlyList<DoorOpeningScheduleRow>",
        "ICollection<DoorOpeningScheduleRow>",
        "ICollection",
        "rows.IndexerReads != 0",
        "conflicting known collection counts",
        "Directory.Exists(root)",
    ):
        require(known_count_smoke, token, "deterministic conflicting-known-count regression")

    print(
        "PASS: Door/opening XLSX binds all deterministic known Count interfaces before traversal, "
        "preserves post-snapshot drift checks, rejects count conflicts/bounds before filesystem output, "
        "and keeps deterministic regression coverage."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
