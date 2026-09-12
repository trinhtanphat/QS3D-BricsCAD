#!/usr/bin/env python3
from pathlib import Path
import subprocess
import tempfile

source = Path("scripts/new-v26-candidate-provenance.ps1").read_text(encoding="utf-8")

required_tokens = (
    "function Get-JsonPropertyOccurrenceCount",
    "function Get-JsonTopLevelArrayObjectTexts",
    "function Assert-JsonPropertyCounts",
    "function Assert-JsonArrayObjectPropertyCounts",
    "Version = 1",
    "Files = 1",
    "$hostFileExpectedPropertyCounts = @{",
    "Name = 1",
    "Path = 1",
    "Sha256 = 1",
    "Length = 1",
    "-ArrayPropertyName 'Files'",
    "-ExpectedArrayObjectCount $requiredHostNames.Count",
    "-ExpectedArrayObjectPropertyCounts $hostFileExpectedPropertyCounts",
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
host_root_assert = source.index("Assert-JsonPropertyCounts -JsonText $text", reader_start)
host_array_assert = source.index("Assert-JsonArrayObjectPropertyCounts -JsonText $text", reader_start)
if not (helper_start < reader_start < host_root_assert < host_array_assert < host_parse):
    raise SystemExit("ERROR: host-reference-state root and Files[] cardinality must be proved before ConvertFrom-Json")

host_expected = source.index("$hostExpectedPropertyCounts = @{")
host_record_expected = source.index("$hostFileExpectedPropertyCounts = @{")
host_call = source.index("$hostState = Read-StrictUtf8Json")
if not (host_expected < host_record_expected < host_call):
    raise SystemExit("ERROR: host-reference-state root/record cardinalities must be bound before the JSON reader is invoked")

metadata_expected = source.index("$metadataExpectedPropertyCounts = @{")
metadata_assert = source.index("Assert-JsonPropertyCounts -JsonText $metadataText")
metadata_parse = source.index("$metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop")
if not (metadata_expected < metadata_assert < metadata_parse):
    raise SystemExit("ERROR: PACKAGE-METADATA identity cardinality must be proved before ConvertFrom-Json")

# Exercise the production lexical decoder and path scoping directly. PowerShell JSON
# consumers can collapse literal, JSON-escaped-equivalent, and case-variant names;
# unrelated nested extension keys must not count as duplicate identity at the consumed path.
helpers = source[helper_start:reader_start]
probe = helpers + r'''
$rootCases = @(
    @{ Json='{"Name":"bricscad.exe"}'; Name='Name'; Expected=1 },
    @{ Json='{"Name":"bricscad.exe","Name":"evil"}'; Name='Name'; Expected=2 },
    @{ Json='{"Na\u006de":"bricscad.exe","name":"evil"}'; Name='Name'; Expected=2 },
    @{ Json='{"Sha256":"a","SHA256":"b"}'; Name='Sha256'; Expected=2 },
    @{ Json='{"product":"QS3D","PRODUCT":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"product":"QS3D","pro\u0064uct":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"product":"QS3D","extension":{"product":"diagnostic"}}'; Name='product'; Expected=1 },
    @{ Json='{"Version":1,"Files":[],"extension":{"Version":99}}'; Name='Version'; Expected=1 },
    @{ Json='{"framework":"net8.0-windows","extension":[{"framework":"ignored"}]}'; Name='framework'; Expected=1 }
)
foreach ($case in $rootCases) {
    $actual = Get-JsonPropertyOccurrenceCount -JsonText $case.Json -PropertyName $case.Name
    if ($actual -ne $case.Expected) {
        throw "provenance-generator root/path probe failed for $($case.Name): expected $($case.Expected), got $actual"
    }
}

$validHost = '{"Version":1,"Files":[{"Name":"a","Path":"p1","Sha256":"s1","Length":1,"extension":{"Name":"nested"}},{"Name":"b","Path":"p2","Sha256":"s2","Length":2}],"extension":{"Files":[]}}'
$recordCounts = @{ Name=1; Path=1; Sha256=1; Length=1 }
$objects = @(Get-JsonTopLevelArrayObjectTexts -JsonText $validHost -ArrayPropertyName 'Files' -Label 'probe')
if ($objects.Count -ne 2) { throw "provenance-generator Files[] probe expected 2 direct objects, got $($objects.Count)" }
Assert-JsonArrayObjectPropertyCounts -JsonText $validHost -ArrayPropertyName 'Files' -ExpectedObjectCount 2 -ExpectedPropertyCounts $recordCounts -Label 'probe'

$duplicateRecord = '{"Version":1,"Files":[{"Name":"a","NAME":"evil","Path":"p","Sha256":"s","Length":1}]}'
$rejected = $false
try {
    Assert-JsonArrayObjectPropertyCounts -JsonText $duplicateRecord -ArrayPropertyName 'Files' -ExpectedObjectCount 1 -ExpectedPropertyCounts $recordCounts -Label 'probe'
}
catch { $rejected = $true }
if (-not $rejected) { throw 'provenance-generator Files[] duplicate record identity was not rejected' }

$escapedDuplicateRecord = '{"Version":1,"Files":[{"Name":"a","Na\u006de":"evil","Path":"p","Sha256":"s","Length":1}]}'
$rejected = $false
try {
    Assert-JsonArrayObjectPropertyCounts -JsonText $escapedDuplicateRecord -ArrayPropertyName 'Files' -ExpectedObjectCount 1 -ExpectedPropertyCounts $recordCounts -Label 'probe'
}
catch { $rejected = $true }
if (-not $rejected) { throw 'provenance-generator Files[] escaped-equivalent duplicate record identity was not rejected' }
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
        "ERROR: behavioral V26 provenance-generator duplicate/path-scope probe failed: "
        + (completed.stderr or completed.stdout).strip()
    )

print("PASS: V26 provenance generator proves root and Files[] identity cardinality before parsing with path-aware duplicate rejection")
