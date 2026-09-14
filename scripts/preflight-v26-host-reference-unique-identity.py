#!/usr/bin/env python3
from pathlib import Path
import subprocess
import tempfile

source = Path("scripts/assert-v26-candidate-identity.ps1").read_text(encoding="utf-8")
parse_anchor = "try { $provenance = $provenanceText | ConvertFrom-Json -ErrorAction Stop }"
parse_pos = source.index(parse_anchor)
preparse = source[:parse_pos]

required_preparse = (
    "$hostReferenceExpectedPropertyCounts = @{ name=1; sha256=1; length=1 }",
    "Assert-JsonArrayObjectPropertyCounts -JsonText $provenanceText -ArrayPropertyName 'hostReferences' -ExpectedObjectCount $requiredHostNames.Count -ExpectedPropertyCounts $hostReferenceExpectedPropertyCounts -Label 'V26 candidate provenance'",
)
for required in required_preparse:
    if required not in preparse:
        raise SystemExit(
            "ERROR: V26 provenance must prove per-record host-reference name/sha256/length cardinality before ConvertFrom-Json: "
            + required
        )


# Keep the post-parse per-entry checks as a second fence: global raw cardinality alone
# cannot prove each admitted object has the required value shape.
postparse = source[parse_pos:]
for required in (
    "$hostReferences.Count -ne $requiredHostNames.Count",
    "$matches.Count -ne 1",
    "$hostReference.sha256 -cnotmatch '^[0-9a-f]{64}$'",
    "$hostReference.length -le 0",
):
    if required not in postparse:
        raise SystemExit(f"ERROR: post-parse host-reference fence drifted or disappeared: {required}")

# Exercise the production property-name decoder rather than trusting lexical tokens only.
# This catches the same duplicate forms PowerShell JSON parsing can collapse: literal,
# escaped-equivalent, and case-variant names.
function_start = source.index("function Get-JsonPropertyOccurrenceCount")
function_end = source.index("if ([string]::IsNullOrWhiteSpace($ExpectedInstallerSha256)", function_start)
helper = source[function_start:function_end]
probe = helper + r'''
$expected = @{ name=1; sha256=1; length=1 }
$sha = ('a' * 64) -join ''
$valid = '{"hostReferences":[{"name":"bricscad.exe","sha256":"' + $sha + '","length":1}]}'
Assert-JsonArrayObjectPropertyCounts -JsonText $valid -ArrayPropertyName 'hostReferences' -ExpectedObjectCount 1 -ExpectedPropertyCounts $expected -Label 'valid host references'

$badCases = @(
    '{"hostReferences":[{"name":"bricscad.exe","NAME":"evil","sha256":"' + $sha + '","length":1}]}',
    '{"hostReferences":[{"na\u006de":"bricscad.exe","name":"evil","sha256":"' + $sha + '","length":1}]}',
    '{"hostReferences":[{"name":"bricscad.exe","sha256":"' + $sha + '","SHA256":"b","length":1}]}',
    '{"hostReferences":[{"name":"bricscad.exe","sha256":"' + $sha + '","length":1,"LENGTH":2}]}'
)
foreach ($bad in $badCases) {
    $rejected = $false
    try { Assert-JsonArrayObjectPropertyCounts -JsonText $bad -ArrayPropertyName 'hostReferences' -ExpectedObjectCount 1 -ExpectedPropertyCounts $expected -Label 'duplicate host reference' }
    catch { $rejected = $true }
    if (-not $rejected) { throw 'duplicate/case/escaped-equivalent host-reference property was not rejected' }
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
        "ERROR: behavioral host-reference duplicate/escaped/case-variant-key probe failed: "
        + (completed.stderr or completed.stdout).strip()
    )

print("PASS: V26 host-reference nested identity is duplicate-rejected pre-parse, duplicate-spelling aware, and shape-validated post-parse")
