#!/usr/bin/env python3
from pathlib import Path
import subprocess
import tempfile

source = Path("scripts/assert-v26-release-package-identity.ps1").read_text(encoding="utf-8")

required_tokens = (
    "function Get-JsonPropertyOccurrenceCount",
    "$metadataExpectedPropertyCounts = @{",
    "product = 1",
    "target = 1",
    "framework = 1",
    "productVersion = 1",
    "version = 1",
    "Assert-JsonPropertyCounts -JsonText $metadataText -ExpectedPropertyCounts $metadataExpectedPropertyCounts -Label 'V26 package metadata'",
)
for token in required_tokens:
    if token not in source:
        raise SystemExit(f"ERROR: V26 release package identity JSON uniqueness contract missing: {token}")

helper_start = source.index("function Get-JsonPropertyOccurrenceCount")
helper_end = source.index("function Invoke-BoundedTextProcess", helper_start)
helper_block = source[helper_start:helper_end]
metadata_read = source.index("$metadataText = Read-BoundedStrictUtf8Stream")
metadata_assert = source.index("Assert-JsonPropertyCounts -JsonText $metadataText", metadata_read)
metadata_parse = source.index("$metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop", metadata_read)
if not (metadata_read < metadata_assert < metadata_parse):
    raise SystemExit("ERROR: V26 release PACKAGE-METADATA identity cardinality must be proved before full-document ConvertFrom-Json")

probe = helper_block + r'''
$cases = @(
    @{ Json='{"product":"QS3D"}'; Name='product'; Expected=1 },
    @{ Json='{"product":"QS3D","product":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"product":"QS3D","PRODUCT":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"product":"QS3D","pro\u0064uct":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"product":"QS3D","extension":{"product":"diagnostic"}}'; Name='product'; Expected=1 },
    @{ Json='{"target":"BricsCAD V26 x64","extension":{"target":"ignored"}}'; Name='target'; Expected=1 },
    @{ Json='{"framework":"net8.0-windows","extension":[{"framework":"ignored"}]}'; Name='framework'; Expected=1 },
    @{ Json='{"productVersion":"3.1.4","extension":{"productVersion":"ignored"}}'; Name='productVersion'; Expected=1 },
    @{ Json='{"version":"3.1.4.0","extension":{"version":"ignored"}}'; Name='version'; Expected=1 }
)
foreach ($case in $cases) {
    $actual = Get-JsonPropertyOccurrenceCount -JsonText $case.Json -PropertyName $case.Name
    if ($actual -ne $case.Expected) { throw "V26 release package identity scope failed for $($case.Name): expected $($case.Expected), got $actual" }
}
$expected = @{ product=1; target=1; framework=1; productVersion=1; version=1 }
$valid = '{"product":"QS3D","target":"BricsCAD V26 x64","framework":"net8.0-windows","productVersion":"3.1.4","version":"3.1.4.0","extension":{"product":"nested","version":"nested"}}'
Assert-JsonPropertyCounts -JsonText $valid -ExpectedPropertyCounts $expected -Label 'probe'
$duplicate = '{"product":"QS3D","PRODUCT":"evil","target":"BricsCAD V26 x64","framework":"net8.0-windows","productVersion":"3.1.4","version":"3.1.4.0"}'
$rejected = $false
try { Assert-JsonPropertyCounts -JsonText $duplicate -ExpectedPropertyCounts $expected -Label 'probe' } catch { $rejected = $true }
if (-not $rejected) { throw 'case-variant duplicate release package identity was not rejected' }
$escapedDuplicate = '{"product":"QS3D","pro\u0064uct":"evil","target":"BricsCAD V26 x64","framework":"net8.0-windows","productVersion":"3.1.4","version":"3.1.4.0"}'
$rejected = $false
try { Assert-JsonPropertyCounts -JsonText $escapedDuplicate -ExpectedPropertyCounts $expected -Label 'probe' } catch { $rejected = $true }
if (-not $rejected) { throw 'escaped-equivalent duplicate release package identity was not rejected' }
'''
probe = probe.replace('\\"', '"')
with tempfile.NamedTemporaryFile("w", suffix=".ps1", encoding="utf-8", delete=False) as tmp:
    tmp.write(probe)
    probe_path = Path(tmp.name)
try:
    completed = subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(probe_path)], capture_output=True, text=True, timeout=20)
finally:
    probe_path.unlink(missing_ok=True)
if completed.returncode != 0:
    raise SystemExit("ERROR: behavioral V26 release package identity JSON uniqueness probe failed: " + (completed.stderr or completed.stdout).strip())
print("PASS: V26 release PACKAGE-METADATA identity is root-scoped and unique before parser collapse")
