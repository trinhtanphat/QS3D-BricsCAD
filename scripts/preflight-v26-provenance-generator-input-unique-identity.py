#!/usr/bin/env python3
from pathlib import Path
import subprocess
import tempfile

source = Path("scripts/new-v26-candidate-provenance.ps1").read_text(encoding="utf-8")

required_tokens = (
    "function Get-JsonPropertyOccurrenceCount",
    "function Assert-JsonPropertyCounts",
    "Version = 1",
    "Files = 1",
    "Name = $requiredHostNames.Count",
    "Path = $requiredHostNames.Count",
    "Sha256 = $requiredHostNames.Count",
    "Length = $requiredHostNames.Count",
    "-ExpectedPropertyCounts $hostExpectedPropertyCounts",
    "product = 1",
    "target = 1",
    "framework = 1",
    "productVersion = 1",
    "Assert-JsonPropertyCounts -JsonText $metadataText -ExpectedPropertyCounts $metadataExpectedPropertyCounts",
)
for token in required_tokens:
    if token not in source:
        raise SystemExit(f"ERROR: V26 provenance-generator input uniqueness contract missing: {token}")

helper_start = source.index("function Get-JsonPropertyOccurrenceCount")
reader_start = source.index("function Read-StrictUtf8Json")
host_parse = source.index("return $text | ConvertFrom-Json -ErrorAction Stop", reader_start)
host_assert = source.index("Assert-JsonPropertyCounts -JsonText $text", reader_start)
if not (helper_start < reader_start < host_assert < host_parse):
    raise SystemExit("ERROR: host-reference-state property cardinality must be proved before ConvertFrom-Json")

host_expected = source.index("$hostExpectedPropertyCounts = @{")
host_call = source.index("$hostState = Read-StrictUtf8Json")
if not (host_expected < host_call):
    raise SystemExit("ERROR: host-reference-state expected cardinalities must be bound before the JSON reader is invoked")

metadata_expected = source.index("$metadataExpectedPropertyCounts = @{")
metadata_assert = source.index("Assert-JsonPropertyCounts -JsonText $metadataText")
metadata_parse = source.index("$metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop")
if not (metadata_expected < metadata_assert < metadata_parse):
    raise SystemExit("ERROR: PACKAGE-METADATA identity cardinality must be proved before ConvertFrom-Json")

# Exercise the production property-name decoder rather than trusting lexical tokens only.
# PowerShell JSON consumers can collapse literal, JSON-escaped-equivalent, and case-variant names.
helper_end = source.index("function Assert-JsonPropertyCounts", helper_start)
helper = source[helper_start:helper_end]
probe = helper + r'''
$cases = @(
    @{ Json='{"Name":"bricscad.exe"}'; Name='Name'; Expected=1 },
    @{ Json='{"Name":"bricscad.exe","Name":"evil"}'; Name='Name'; Expected=2 },
    @{ Json='{"Na\u006de":"bricscad.exe","name":"evil"}'; Name='Name'; Expected=2 },
    @{ Json='{"Sha256":"a","SHA256":"b"}'; Name='Sha256'; Expected=2 },
    @{ Json='{"product":"QS3D","PRODUCT":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"product":"QS3D","pro\u0064uct":"evil"}'; Name='product'; Expected=2 }
)
foreach ($case in $cases) {
    $actual = Get-JsonPropertyOccurrenceCount -JsonText $case.Json -PropertyName $case.Name
    if ($actual -ne $case.Expected) {
        throw "provenance-generator duplicate-property probe failed for $($case.Name): expected $($case.Expected), got $actual"
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
    raise SystemExit(
        "ERROR: behavioral V26 provenance-generator duplicate/escaped/case-variant-key probe failed: "
        + (completed.stderr or completed.stdout).strip()
    )

print("PASS: V26 provenance generator duplicate-rejects host-state/package identity before parsing and recognizes escaped/case-variant names")
