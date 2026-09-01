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

    row_count = require(exporter, 'BindKnownCount(rows, MaxDataRows, "export rows")', "top-level bound known-count contract")
    row_loop = require(exporter, "for (var rowIndex = 0; rowIndex < rowCount.Value; rowIndex++)", "top-level indexed snapshot traversal")
    before_indexer = require(exporter, 'rowCount.Revalidate(rows, "before row indexer")', "pre-indexer count rebound")
    row_read = require(exporter, "var sourceRow = rows[rowIndex];", "single caller row indexer read")
    after_indexer = require(exporter, 'rowCount.Revalidate(rows, "after row indexer")', "post-indexer count rebound")
    row_snapshot = require(exporter, "snapshot.Add(SnapshotRow(sourceRow, rowIndex));", "semantic row snapshot")
    after_snapshot = require(exporter, 'rowCount.Revalidate(rows, "after row snapshot")', "post-snapshot count rebound")
    final_bind = require(exporter, 'rowCount.Revalidate(rows, "after snapshot traversal")', "final count rebound")
    cell_validation = require(exporter, "ValidateCellText(snapshot);", "cell validation boundary")
    path_resolution = require(exporter, "var fullPath = Path.GetFullPath(path);", "filesystem mutation boundary")

    nested_count = require(exporter, "var count = RequireConsistentKnownCount(source, MaxCellTextLength + 1, label);", "nested consistent known-count snapshot")
    nested_loop = require(exporter, "for (var index = 0; index < count; index++)", "nested indexed snapshot traversal")
    nested_bind = require(exporter, "if (source.Count != count)", "nested post-traversal count binding")
    nested_failure = require(exporter, 'throw new InvalidOperationException("Door/opening XLSX " + label + " count changed during snapshot.");', "nested count-drift failure")

    bind_helper = require(exporter, "private static KnownCountContract<T> BindKnownCount<T>(IEnumerable<T> source, int maximum, string label)", "bound known-count helper")
    compatibility_helper = require(exporter, "private static int RequireConsistentKnownCount<T>(IEnumerable<T> source, int maximum, string label)", "nested compatibility known-count helper")
    contract = require(exporter, "private sealed class KnownCountContract<T>", "known-count contract")
    contract_text = exporter[contract:]
    read_only = contract + require(contract_text, "if (_readOnlyCount) observe(((IReadOnlyCollection<T>)source).Count);", "IReadOnlyCollection known-count read")
    generic = contract + require(contract_text, "if (_genericCount) observe(((ICollection<T>)source).Count);", "generic ICollection known-count read")
    non_generic = contract + require(contract_text, "if (_nonGenericCount) observe(((ICollection)source).Count);", "non-generic ICollection known-count read")
    conflict = contract + require(contract_text, "exposes conflicting known collection counts", "known-count conflict rejection")
    negative = contract + require(contract_text, "count must be non-negative", "known-count negative rejection")
    maximum = contract + require(contract_text, "count exceeds the supported maximum", "known-count maximum rejection")
    deterministic_required = contract + require(contract_text, "must expose a deterministic collection count", "known-count interface requirement")

    if not (row_count < row_loop < before_indexer < row_read < after_indexer < row_snapshot < after_snapshot < final_bind < cell_validation < path_resolution):
        raise SystemExit("FAIL: top-level Count contract must surround every caller row indexer/snapshot and remain before validation/filesystem output")
    if exporter.count("var sourceRow = rows[rowIndex];") != 1:
        raise SystemExit("FAIL: top-level row source must retain a single caller indexer read per traversal iteration")
    if not (nested_count < nested_loop < nested_bind < nested_failure):
        raise SystemExit("FAIL: nested ElementIds/HostIds count binding must remain around indexed traversal")
    if not (bind_helper < compatibility_helper < contract < min(read_only, generic, non_generic, conflict, negative, maximum, deterministic_required)):
        raise SystemExit("FAIL: known-count helpers must delegate to a contract that owns all Count reads and rejection rules")

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
        "PASS: Door/opening XLSX binds every deterministic known Count surface at admission, "
        "revalidates the exact contract around caller row indexer/snapshot traversal, preserves nested/final drift checks, "
        "rejects count conflicts/bounds before filesystem output, and keeps historical deterministic regressions."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
