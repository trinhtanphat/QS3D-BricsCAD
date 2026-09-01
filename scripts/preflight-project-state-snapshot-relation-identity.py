#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectStateSnapshotFamilyIdentitySmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"


def require(text: str, token: str, label: str) -> int:
    pos = text.find(token)
    if pos < 0:
        raise AssertionError(f"missing {label}: {token}")
    return pos


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    source_count = require(
        source,
        'RequireSupportedCount(element.SourceHandles.Count, "element " + element.Id + " source handles", MaximumSnapshotNestedEntries);',
        "source-handle bound",
    )
    dependency_count = require(
        source,
        'RequireSupportedCount(element.DependsOn.Count, "element " + element.Id + " dependencies", MaximumSnapshotNestedEntries);',
        "dependency bound",
    )
    source_validate = require(
        source,
        'RequireCanonicalRelationIdentities(element.SourceHandles, element.Id, "source handle");',
        "source-handle canonical validation",
    )
    dependency_validate = require(
        source,
        'RequireCanonicalRelationIdentities(element.DependsOn, element.Id, "dependency id");',
        "dependency canonical validation",
    )
    property_validate = require(source, "RequireCanonicalElementProperties(element);", "property validation")

    if not (source_count < source_validate < property_validate):
        raise AssertionError("source-handle bound/canonical validation must precede element payload validation")
    if not (dependency_count < dependency_validate < property_validate):
        raise AssertionError("dependency bound/canonical validation must precede element payload validation")

    helper_start = require(
        source,
        "private static void RequireCanonicalRelationIdentities(IEnumerable<string> values, string elementId, string role)",
        "relation validation helper",
    )
    helper_end = source.find("private static void RequireCanonicalElementProperties", helper_start)
    if helper_end < 0:
        raise AssertionError("relation validation helper must precede element property validation helper")
    helper = source[helper_start:helper_end]
    for token, label in (
        ("new HashSet<string>(StringComparer.OrdinalIgnoreCase)", "case-insensitive duplicate set"),
        ("string.IsNullOrWhiteSpace(value)", "blank rejection"),
        ("string.Equals(value, value.Trim(), StringComparison.Ordinal)", "surrounding-whitespace canonicality"),
        ("HasControlCharacter(value)", "control-character rejection"),
        ("XmlConvert.VerifyXmlChars(value)", "XML text validation"),
        ("seen.Add(value)", "duplicate rejection"),
    ):
        require(helper, token, label)

    for token in (
        'ExpectRejectedRelation(true, " A1 ", "padded source handle")',
        'ExpectRejectedRelation(false, " HOST ", "padded dependency")',
        'ExpectRejectedDuplicate(true, "A1", "a1", "case-insensitive duplicate source handle")',
        'ExpectRejectedDuplicate(false, "HOST", "host", "case-insensitive duplicate dependency")',
        'const string handle = "HANDLE-\\U0001F680"',
        'const string dependency = "HOST-\\U0001F680"',
    ):
        require(smoke, token, "deterministic relation-identity smoke coverage")

    if registration.count("ProjectStateSnapshotFamilyIdentitySmoke.Run();") != 1:
        raise AssertionError("host snapshot smoke must remain registered exactly once")

    print("PASS: project state snapshot relation identity preflight")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        raise SystemExit(1)
