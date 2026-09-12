#!/usr/bin/env python3
from pathlib import Path
import subprocess
import tempfile

SOURCE = Path("scripts/assert-v25-commercial-draft-identity.ps1")
source = SOURCE.read_text(encoding="utf-8")
errors = []

required_tokens = (
    "function Get-JsonPropertyOccurrenceCount",
    "function Assert-JsonPropertyCounts",
    "[StringComparison]::OrdinalIgnoreCase",
    "$provenanceExpectedPropertyCounts = @{",
    "schemaVersion = 1",
    "product = 1",
    "target = 1",
    "releaseTag = 1",
    "productVersion = 1",
    "sourceCommit = 1",
    "signerThumbprint = 1",
    "packageFile = 1",
    "packageSha256 = 1",
    "updateManifestFile = 1",
    "updateManifestSha256 = 1",
    "Assert-JsonPropertyCounts -JsonText $provenanceText -ExpectedPropertyCounts $provenanceExpectedPropertyCounts",
    "$metadataExpectedPropertyCounts = @{",
    "gitCommit = 1",
    "Assert-JsonPropertyCounts -JsonText $text -ExpectedPropertyCounts $metadataExpectedPropertyCounts",
)
for token in required_tokens:
    if token not in source:
        errors.append("V25 commercial-draft unique-identity contract missing token: " + token)

strict_reader = "[Text.UTF8Encoding]::new($false, $true), $false, 4096, $true)"
if source.count(strict_reader) != 2:
    errors.append(
        "V25 commercial-draft held text and ZIP metadata readers must both disable BOM auto-detection while using strict UTF-8"
    )

if not errors:
    helper_start = source.index("function Get-JsonPropertyOccurrenceCount")
    assert_start = source.index("function Assert-JsonPropertyCounts", helper_start)

    provenance_counts = source.index("$provenanceExpectedPropertyCounts = @{")
    provenance_assert = source.index(
        "Assert-JsonPropertyCounts -JsonText $provenanceText -ExpectedPropertyCounts $provenanceExpectedPropertyCounts",
        provenance_counts,
    )
    provenance_parse = source.index("$provenance = $provenanceText | ConvertFrom-Json -ErrorAction Stop", provenance_assert)
    if not (helper_start < assert_start < provenance_counts < provenance_assert < provenance_parse):
        errors.append("V25 commercial-draft provenance identity cardinality must be proved before ConvertFrom-Json")

    metadata_counts = source.index("$metadataExpectedPropertyCounts = @{")
    metadata_assert = source.index(
        "Assert-JsonPropertyCounts -JsonText $text -ExpectedPropertyCounts $metadataExpectedPropertyCounts",
        metadata_counts,
    )
    metadata_parse = source.index("return $text | ConvertFrom-Json -ErrorAction Stop", metadata_assert)
    if not (helper_start < assert_start < metadata_counts < metadata_assert < metadata_parse):
        errors.append("V25 commercial-draft PACKAGE-METADATA identity cardinality must be proved before ConvertFrom-Json")

    # Behavioral probe against the production property-name decoder. PowerShell's
    # object JSON consumer treats case variants as the same logical property and
    # JSON escapes can spell the same property with different source bytes. Only
    # top-level identity properties belong to the admission contract: nested
    # diagnostic/extension objects must not create false duplicate identities.
    helper = source[helper_start:assert_start]
    probe = helper + r'''
$cases = @(
    @{ Json='{"sourceCommit":"a"}'; Name='sourceCommit'; Expected=1 },
    @{ Json='{"sourceCommit":"a","sourceCommit":"b"}'; Name='sourceCommit'; Expected=2 },
    @{ Json='{"sourceCommit":"a","SOURCECOMMIT":"b"}'; Name='sourceCommit'; Expected=2 },
    @{ Json='{"sourceCommit":"a","source\u0043ommit":"b"}'; Name='sourceCommit'; Expected=2 },
    @{ Json='{"productVersion":"x","PRODUCTVERSION":"y"}'; Name='productVersion'; Expected=2 },
    @{ Json='{"gitCommit":"a","git\u0043ommit":"b"}'; Name='gitCommit'; Expected=2 },
    @{ Json='{"sourceCommit":"a","extension":{"sourceCommit":"nested"}}'; Name='sourceCommit'; Expected=1 },
    @{ Json='{"gitCommit":"a","extension":{"GITCOMMIT":"nested"}}'; Name='gitCommit'; Expected=1 },
    @{ Json='{"product":"QS3D","items":[{"product":"nested"}]}'; Name='product'; Expected=1 }
)
foreach ($case in $cases) {
    $actual = Get-JsonPropertyOccurrenceCount -JsonText $case.Json -PropertyName $case.Name
    if ($actual -ne $case.Expected) {
        throw "V25 commercial-draft duplicate-property probe failed for $($case.Name): expected $($case.Expected), got $actual"
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
        errors.append(
            "behavioral V25 commercial-draft duplicate/escaped/case-variant/top-level-scope property probe failed: "
            + (completed.stderr or completed.stdout).strip()
        )

print("QS3D V25 commercial-draft unique release identity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    raise SystemExit(f"FAILED with {len(errors)} error(s).")

print("PASS: V25 commercial-draft strict UTF-8 provenance and PACKAGE-METADATA reject duplicate top-level release identity before JSON parsing without counting nested extension keys.")
