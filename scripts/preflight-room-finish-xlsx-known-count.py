#!/usr/bin/env python3
# Lane-Key: issue-4215
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXPORTER = ROOT / "src" / "QS3D.Core" / "Export" / "RoomFinishXlsxExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RoomFinishXlsxKnownCountContractSmoke.cs"


def require(text: str, token: str, label: str) -> int:
    position = text.find(token)
    if position < 0:
        raise SystemExit(f"FAIL: missing {label}: {token}")
    return position


def main() -> int:
    exporter = EXPORTER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    row_count = require(
        exporter,
        'var rowCount = RequireConsistentKnownCount(rows, MaxDataRows, "export rows");',
        "top-level consistent known-count binding",
    )
    row_loop = require(exporter, "for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)", "indexed row traversal")
    row_bind = require(exporter, "if (rows.Count != rowCount)", "legacy post-snapshot drift check")
    row_failure = require(
        exporter,
        'throw new InvalidOperationException("Room-finish XLSX export row count changed during snapshot.");',
        "legacy drift failure classification",
    )
    path_resolution = require(exporter, "var fullPath = Path.GetFullPath(path);", "filesystem boundary")

    helper = require(
        exporter,
        "private static int RequireConsistentKnownCount<T>(IEnumerable<T> source, int maximum, string label)",
        "known-count helper",
    )
    read_only = require(exporter, "var readOnly = source as IReadOnlyCollection<T>;", "IReadOnlyCollection count")
    generic = require(exporter, "var generic = source as ICollection<T>;", "generic ICollection count")
    non_generic = require(exporter, "var nonGeneric = source as ICollection;", "non-generic ICollection count")
    conflict = require(exporter, "exposes conflicting known collection counts", "conflicting-count rejection")
    negative = require(exporter, "count must be non-negative", "negative-count rejection")
    maximum = require(exporter, "count exceeds the supported maximum", "maximum-count rejection")
    deterministic_required = require(exporter, "must expose a deterministic collection count", "deterministic-count requirement")

    if not (row_count < row_loop < row_bind < row_failure < path_resolution):
        raise SystemExit("FAIL: Room-finish top-level count contract must bind before traversal and retain drift checking before filesystem mutation")
    if not (helper < read_only < generic < non_generic < deterministic_required):
        raise SystemExit("FAIL: Room-finish known-count helper must inspect all deterministic collection interfaces")
    if min(conflict, negative, maximum) < helper:
        raise SystemExit("FAIL: Room-finish count rejection contract must live inside RequireConsistentKnownCount")
    if "var count = RequireConsistentKnownCount(source" in exporter:
        raise SystemExit("FAIL: issue-4215 must not broaden into nested concrete-list count semantics")
    require(exporter, "var count = source.Count;", "existing nested concrete-list snapshot behavior")
    require(exporter, "if (source.Count != count)", "existing nested post-snapshot drift check")

    for token in (
        "KnownCountRows",
        "IReadOnlyList<RoomFinishScheduleRow>",
        "ICollection<RoomFinishScheduleRow>",
        "ICollection",
        "RejectsConflictingKnownCountsBeforeTraversalOrFilesystem",
        "RejectsNegativeKnownCountBeforeTraversalOrFilesystem",
        "RejectsOversizedKnownCountBeforeTraversalOrFilesystem",
        "AcceptsHonestMultiInterfaceKnownCounts",
        "rows.IndexerReads != 0",
        "Directory.Exists(root)",
    ):
        require(smoke, token, "Room-finish deterministic known-count regression")

    print(
        "PASS: Room-finish XLSX binds top-level deterministic Count interfaces before traversal, "
        "rejects invalid/conflicting counts before filesystem mutation, preserves legacy drift classification, "
        "and leaves nested concrete-list semantics unchanged."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
