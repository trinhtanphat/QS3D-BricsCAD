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

    quantity_bound = require(
        source,
        'RequireSupportedCount(element.Quantities.Count, "element " + element.Id + " quantities", MaximumSnapshotNestedEntries);',
        "quantity cardinality bound",
    )
    quantity_validate = require(source, "RequireCanonicalQuantities(element);", "quantity validation call")
    if quantity_bound >= quantity_validate:
        raise AssertionError("quantity cardinality bound must run before quantity identity validation")

    helper_start = require(source, "private static void RequireCanonicalQuantities(ProjectElement element)", "quantity validation helper")
    helper_end = source.find("private static bool HasControlCharacter", helper_start)
    if helper_end < 0:
        raise AssertionError("quantity validation helper must remain bounded before generic helper section")
    helper = source[helper_start:helper_end]

    for token, label in (
        ("string.IsNullOrWhiteSpace(quantity.Key)", "blank quantity-name rejection"),
        ("HasControlCharacter(quantity.Key)", "control-character rejection"),
        ("var canonicalName = quantity.Key.Trim();", "canonical-name derivation"),
        ("string.Equals(canonicalName, quantity.Key, StringComparison.Ordinal)", "exact quantity identity check"),
        ("XmlConvert.VerifyXmlChars(canonicalName)", "XML-safe quantity identity validation"),
        ("canonicalNames.Add(canonicalName)", "case-insensitive canonical-collapse rejection"),
        ("double.IsNaN(quantity.Value)", "NaN rejection"),
        ("double.IsInfinity(quantity.Value)", "infinity rejection"),
        ("quantity.Value < 0d", "negative-value rejection"),
    ):
        require(helper, token, label)

    for token in (
        "RejectsNonCanonicalQuantityIdentityWithoutMutation();",
        'const string padded = " NetVolumeM3 ";',
        "ProjectStateSnapshot.Capture(project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        '!element.Quantities.ContainsKey("NetVolumeM3")',
        "PreservesCanonicalUnicodeQuantityIdentity();",
        'const string quantityName = "KhốiLượng-\\U0001F680";',
        "copy.Quantities.ContainsKey(quantityName)",
    ):
        require(smoke, token, "deterministic snapshot quantity-identity coverage")

    if registration.count("ProjectStateSnapshotFamilyIdentitySmoke.Run();") != 1:
        raise AssertionError("host snapshot smoke must remain registered exactly once")

    print("PASS: project state snapshot quantity identity preflight")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        raise SystemExit(1)
