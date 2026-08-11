#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticTitleBlockParameterMapBuilder.cs"
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

    for field in ("SheetId", "SheetNumber", "SheetName", "TitleBlockName", "PlacedViewCount"):
        require(builder, field, "explicit semantic Sheet field", failures)
    require(builder, "private const int MaxParameters = 128;", "mapping bound", failures)
    require(builder, "private const int MaxDestinationTagLength = 128;", "destination-tag bound", failures)
    require(builder, "new HashSet<string>(StringComparer.OrdinalIgnoreCase)", "case-insensitive destination uniqueness", failures)
    require(builder, "duplicate destination tag", "duplicate destination failure", failures)
    require(builder, ".OrderBy(x => x.DestinationTag, StringComparer.OrdinalIgnoreCase)", "deterministic destination ordering", failures)
    require(builder, "CultureInfo.InvariantCulture", "invariant numeric rendering", failures)
    require(builder, "sheet.TitleBlockName ?? string.Empty", "optional title-block rendering", failures)
    require(builder, "Unsupported semantic title-block source field", "unknown-field fail closed", failures)
    require(builder, "new List<SemanticTitleBlockParameterValue>(values).AsReadOnly()", "defensive map snapshot", failures)

    for forbidden in ("Bricscad.", "Teigha.", "ObjectId", "BlockReference", "AttributeReference"):
        if forbidden in builder:
            failures.append(f"pure-Core title-block mapping must not depend on native CAD APIs: found {forbidden!r}")

    require(smoke, "TitleBlockParameterMapIsDeterministicAndImmutable", "deterministic title-block smoke", failures)
    require(smoke, "TitleBlockParameterMapFailsClosed", "fail-closed title-block smoke", failures)
    require(smoke, "definitions.Clear();", "source-list defensive-copy smoke", failures)
    require(smoke, "mutable.Add(map.Values[0])", "read-only map smoke", failures)
    require(smoke, "(SemanticTitleBlockSheetField)999", "unknown enum smoke", failures)
    require(smoke, "129", "mapping bound smoke", failures)

    if failures:
        print("QS3D Semantic Title Block parameter-map preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: title-block parameter mapping is pure-Core and native-API independent.")
    print("PASS: only explicit semantic Sheet fields can supply P0 values.")
    print("PASS: destination tags are bounded, case-insensitively unique and deterministically ordered.")
    print("PASS: returned parameter values form a defensive read-only snapshot.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
