#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "update-v25.ps1"

REQUIRED_PROPERTIES = (
    "schemaVersion",
    "product",
    "target",
    "productVersion",
    "version",
    "packageUri",
    "sha256",
    "signerThumbprint",
)


def fail(message: str) -> None:
    raise AssertionError(message)


def validate_source(text: str) -> None:
    helper = text.find("function Get-JsonPropertyOccurrenceCount")
    if helper < 0:
        fail("V25 updater must define raw JSON property occurrence counting before manifest parsing")

    ignore_case = text.find("[StringComparison]::OrdinalIgnoreCase", helper)
    if ignore_case < 0:
        fail("V25 updater JSON property-name comparison must match PowerShell case-insensitive property semantics")

    raw_read = text.find("$manifestText = Get-Content -LiteralPath $manifestPath -Raw")
    if raw_read < 0:
        fail("V25 updater must retain raw manifest text for uniqueness admission")

    parse = text.find("$manifest = $manifestText | ConvertFrom-Json", raw_read)
    if parse < 0:
        fail("V25 updater must parse the already-admitted raw manifest text")

    loop_match = re.search(
        r"foreach\s*\(\$propertyName\s+in\s+@\((?P<props>.*?)\)\)\s*\{(?P<body>.*?)\}",
        text[raw_read:parse],
        flags=re.DOTALL,
    )
    if not loop_match:
        fail("V25 updater must enforce manifest identity uniqueness before ConvertFrom-Json")

    props = set(re.findall(r"['\"]([^'\"]+)['\"]", loop_match.group("props")))
    missing = [name for name in REQUIRED_PROPERTIES if name not in props]
    if missing:
        fail("V25 updater uniqueness set is missing release/security identity: " + ", ".join(missing))

    body = loop_match.group("body")
    if "Get-JsonPropertyOccurrenceCount -JsonText $manifestText -PropertyName $propertyName" not in body:
        fail("V25 updater uniqueness loop must count decoded raw-manifest property names")
    if "-ne 1" not in body:
        fail("V25 updater must require exactly one occurrence of every critical manifest identity property")

    if not (helper < raw_read < parse):
        fail("V25 updater raw uniqueness helper/admission must precede JSON parsing")


def mutation_regressions(text: str) -> None:
    # Guard against lexical regressions that would restore parser-before-admission behavior.
    admitted = text
    validate_source(admitted)

    late = admitted.replace(
        "$manifest = $manifestText | ConvertFrom-Json",
        "$manifest = $manifestText | ConvertFrom-Json\n        # mutation: parser moved before uniqueness admission",
        1,
    )
    # The synthetic marker above does not actually move code, so explicitly construct a late-order mutant.
    raw = admitted.find("$manifestText = Get-Content -LiteralPath $manifestPath -Raw")
    parse = admitted.find("$manifest = $manifestText | ConvertFrom-Json", raw)
    loop = admitted.find("foreach ($propertyName in @(", raw)
    if loop >= 0 and parse >= 0:
        loop_end = admitted.find("\n        try", loop)
        if loop_end > loop:
            block = admitted[loop:loop_end]
            late = admitted[:loop] + admitted[loop_end:parse] + "$manifest = $manifestText | ConvertFrom-Json -ErrorAction Stop\n        " + block + admitted[parse + len("$manifest = $manifestText | ConvertFrom-Json -ErrorAction Stop"):]
            try:
                validate_source(late)
            except AssertionError:
                pass
            else:
                fail("guard self-test failed: parser-before-uniqueness mutant was accepted")

    missing = admitted.replace("'signerThumbprint'", "'signerThumbprint_REMOVED'", 1)
    try:
        validate_source(missing)
    except AssertionError:
        pass
    else:
        fail("guard self-test failed: missing signerThumbprint uniqueness was accepted")

    case_sensitive = admitted.replace("[StringComparison]::OrdinalIgnoreCase", "[StringComparison]::Ordinal", 1)
    try:
        validate_source(case_sensitive)
    except AssertionError:
        pass
    else:
        fail("guard self-test failed: case-sensitive duplicate-name comparison was accepted")


def main() -> int:
    text = TARGET.read_text(encoding="utf-8")
    try:
        validate_source(text)
        mutation_regressions(text)
    except AssertionError as exc:
        print(f"ERROR: {exc}")
        return 1
    print("V25 updater manifest uniqueness guard passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
