#!/usr/bin/env python3
from pathlib import Path
import subprocess
import tempfile

source = Path("scripts/assert-v26-release-package-identity.ps1").read_text(encoding="utf-8")
helper_start = source.index("function Get-JsonPropertyOccurrenceCount")
helper_end = source.index("function Invoke-BoundedTextProcess", helper_start)
helper_block = source[helper_start:helper_end]

probe = helper_block + r'''
$bom = [char]0xFEFF
$expected = @{ product=1; target=1; framework=1; productVersion=1; version=1 }
$valid = '{"product":"QS3D","target":"BricsCAD V26 x64","framework":"net8.0-windows","productVersion":"3.1.4","version":"3.1.4.0"}'
Assert-JsonPropertyCounts -JsonText $valid -ExpectedPropertyCounts $expected -Label 'plain probe'
Assert-JsonPropertyCounts -JsonText ($bom + $valid) -ExpectedPropertyCounts $expected -Label 'BOM probe'

$doubleBomRejected = $false
try { Assert-JsonPropertyCounts -JsonText ($bom + $bom + $valid) -ExpectedPropertyCounts $expected -Label 'double BOM probe' }
catch { $doubleBomRejected = $true }
if (-not $doubleBomRejected) { throw 'double UTF-8 BOM preamble was not rejected' }
$misplacedBomRejected = $false
try { Assert-JsonPropertyCounts -JsonText (' ' + $bom + $valid) -ExpectedPropertyCounts $expected -Label 'misplaced BOM probe' }
catch { $misplacedBomRejected = $true }
if (-not $misplacedBomRejected) { throw 'non-leading UTF-8 BOM preamble was not rejected' }
'''
probe = probe.replace('\\"', '"')
with tempfile.NamedTemporaryFile("w", suffix=".ps1", encoding="utf-8", delete=False) as tmp:
    tmp.write(probe)
    probe_path = Path(tmp.name)
try:
    completed = subprocess.run(
        ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(probe_path)],
        capture_output=True,
        text=True,
        timeout=20,
    )
finally:
    probe_path.unlink(missing_ok=True)
if completed.returncode != 0:
    detail = (completed.stderr or completed.stdout).strip()
    raise SystemExit("ERROR: V26 release PACKAGE-METADATA UTF-8 BOM probe failed: " + detail)
print("PASS: V26 release PACKAGE-METADATA admits exactly one leading UTF-8 BOM")
