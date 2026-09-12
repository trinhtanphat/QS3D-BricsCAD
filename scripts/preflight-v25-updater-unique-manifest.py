#!/usr/bin/env python3
from pathlib import Path
import re
import subprocess
import sys
import tempfile

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

    helper_end = text.find("function Convert-ToStrictSemVer", helper)
    if helper_end <= helper:
        fail("V25 updater duplicate-property helper boundary could not be isolated")

    ignore_case = text.find("[StringComparison]::OrdinalIgnoreCase", helper, helper_end)
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


def behavioral_probe(text: str) -> None:
    helper_start = text.find("function Get-JsonPropertyOccurrenceCount")
    helper_end = text.find("function Convert-ToStrictSemVer", helper_start)
    if helper_start < 0 or helper_end <= helper_start:
        fail("V25 updater duplicate-property helper could not be isolated for behavioral verification")

    helper = text[helper_start:helper_end]
    probe = helper + r'''
$cases = @(
    @{ Json='{"product":"QS3D"}'; Name='product'; Expected=1 },
    @{ Json='{"product":"QS3D","product":"OTHER"}'; Name='product'; Expected=2 },
    @{ Json='{"target":"BricsCAD V25 x64","TARGET":"OTHER"}'; Name='target'; Expected=2 },
    @{ Json='{"product":"QS3D","pro\u0064uct":"OTHER"}'; Name='product'; Expected=2 },
    @{ Json='{"signerThumbprint":"a","SIGNERTHUMBPRINT":"b"}'; Name='signerThumbprint'; Expected=2 },
    @{ Json='{"productVersion":"1.2.3","product\u0056ersion":"9.9.9"}'; Name='productVersion'; Expected=2 }
)
foreach ($case in $cases) {
    $actual = Get-JsonPropertyOccurrenceCount -JsonText $case.Json -PropertyName $case.Name
    if ($actual -ne $case.Expected) {
        throw "V25 updater duplicate-property probe failed for $($case.Name): expected $($case.Expected), got $actual"
    }
}
'''
    with tempfile.NamedTemporaryFile("w", suffix=".ps1", encoding="utf-8", delete=False) as tmp:
        tmp.write(probe)
        probe_path = Path(tmp.name)
    try:
        completed = subprocess.run(
            ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(probe_path)],
            capture_output=True,
            text=True,
            timeout=30,
        )
    finally:
        probe_path.unlink(missing_ok=True)
    if completed.returncode != 0:
        fail(
            "behavioral V25 updater duplicate/escaped/case-variant property probe failed: "
            + (completed.stderr or completed.stdout).strip()
        )


def mutation_regressions(text: str) -> None:
    admitted = text
    validate_source(admitted)

    raw = admitted.find("$manifestText = Get-Content -LiteralPath $manifestPath -Raw")
    parse = admitted.find("$manifest = $manifestText | ConvertFrom-Json", raw)
    loop = admitted.find("foreach ($propertyName in @(", raw)
    if loop >= 0 and parse >= 0:
        loop_end = admitted.find("\n        try", loop)
        if loop_end > loop:
            block = admitted[loop:loop_end]
            late = (
                admitted[:loop]
                + admitted[loop_end:parse]
                + "$manifest = $manifestText | ConvertFrom-Json -ErrorAction Stop\n        "
                + block
                + admitted[parse + len("$manifest = $manifestText | ConvertFrom-Json -ErrorAction Stop"):]
            )
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
        behavioral_probe(text)
        mutation_regressions(text)
    except (AssertionError, OSError, subprocess.SubprocessError) as exc:
        print(f"ERROR: {exc}")
        return 1
    print("V25 updater manifest uniqueness guard passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
