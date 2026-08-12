#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticSheetIndexBuilder.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticViewSheetPlannerSmoke.cs"


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(f"{label}: missing {needle!r}")


def main():
    failures = []
    for path in (BUILDER, SMOKE):
        if not path.is_file():
            failures.append(f"missing required source file: {path.relative_to(ROOT)}")
    if failures:
        for failure in failures:
            print("ERROR:", failure)
        return 1

    builder = BUILDER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")

    require(builder, "public sealed class SemanticSheetIndexRow", "sheet index row", failures)
    require(builder, "public string SheetId { get; }", "stable semantic sheet identity", failures)
    require(builder, "public string Number { get; }", "sheet display number", failures)
    require(builder, "public string Name { get; }", "sheet display name", failures)
    require(builder, "public string? TitleBlockName { get; }", "sheet title-block reference", failures)
    require(builder, "public int PlacedViewCount { get; }", "sheet placed-view count", failures)
    require(builder, "private const int MaxSheets = 10000;", "sheet index bound", failures)
    require(builder, "new HashSet<string>(StringComparer.OrdinalIgnoreCase)", "case-insensitive identity guard", failures)
    if builder.count("new HashSet<string>(StringComparer.OrdinalIgnoreCase)") < 2:
        failures.append("sheet index must guard both stable IDs and display numbers case-insensitively")
    require(builder, "duplicate sheet id", "duplicate stable-ID failure", failures)
    require(builder, "duplicate sheet number", "duplicate sheet-number failure", failures)
    require(builder, ".OrderBy(x => x.Number, StringComparer.OrdinalIgnoreCase)", "deterministic number ordering", failures)
    require(builder, ".ThenBy(x => x.SheetId, StringComparer.OrdinalIgnoreCase)", "deterministic stable-ID tie-break", failures)
    require(builder, "new List<SemanticSheetIndexRow>(rows).AsReadOnly()", "defensive read-only snapshot", failures)

    for forbidden in ("Bricscad.", "Teigha.", "ObjectId", "Handle"):
        if forbidden in builder:
            failures.append(f"pure-Core Sheet Index must remain native-handle free: found {forbidden!r}")

    require(smoke, "SheetIndexIsDeterministicAndImmutable", "sheet index deterministic smoke", failures)
    require(smoke, "SheetIndexIdentityFailsClosed", "sheet index identity smoke", failures)
    require(smoke, "SheetIndexBoundsAndNullsFailClosed", "sheet index bounds/null smoke", failures)
    require(smoke, "source.Clear();", "source-list defensive-copy smoke", failures)
    require(smoke, "mutable.Add(index.Rows[0])", "read-only collection smoke", failures)
    require(smoke, "Enumerable.Repeat(sheet, 10001)", "catalog bound smoke", failures)

    if failures:
        print("QS3D Semantic Sheet Index preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: Semantic Sheet Index remains pure-Core and native-handle free.")
    print("PASS: rows preserve stable semantic SheetId separately from display number/name.")
    print("PASS: duplicate identities fail closed and output ordering is deterministic.")
    print("PASS: returned rows are a defensive read-only snapshot with bounded source size.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
