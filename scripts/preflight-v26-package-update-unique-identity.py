#!/usr/bin/env python3
from pathlib import Path
import re
import subprocess
import tempfile

source = Path("scripts/assert-v26-candidate-identity.ps1").read_text(encoding="utf-8")
metadata_required = ("product", "target", "framework", "productVersion")
update_required = (
    "product", "target", "productVersion", "schemaVersion",
    "packageUri", "sha256", "signerThumbprint",
)

def extract_uniqueness_set(text: str, anchor: str):
    pos = text.index(anchor)
    matches = list(re.finditer(r"foreach \(\$propertyName in @\(([^)]*)\)\)", text[:pos]))
    if not matches:
        raise SystemExit(f"ERROR: no uniqueness loop before {anchor}")
    match = matches[-1]
    return tuple(re.findall(r"'([^']+)'", match.group(1))), match.start()

metadata_parse = "try { $metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop }"
update_parse = "try { $update = $updateText | ConvertFrom-Json -ErrorAction Stop }"
metadata_set, metadata_pos = extract_uniqueness_set(source, metadata_parse)
update_set, update_pos = extract_uniqueness_set(source, update_parse)
if metadata_set != metadata_required:
    raise SystemExit(f"ERROR: PACKAGE-METADATA uniqueness set drifted: {metadata_set!r}")
if update_set != update_required:
    raise SystemExit(f"ERROR: update-manifest uniqueness set drifted: {update_set!r}")
if metadata_pos > source.index(metadata_parse) or update_pos > source.index(update_parse):
    raise SystemExit("ERROR: release identity uniqueness must be checked before JSON parsing")

function_start = source.index("function Get-JsonPropertyOccurrenceCount")
function_end = source.index("if ([string]::IsNullOrWhiteSpace($ExpectedInstallerSha256)", function_start)
helper = source[function_start:function_end]
probe = helper + r'''
$cases = @(
    @{ Json='{"product":"QS3D"}'; Name='product'; Expected=1 },
    @{ Json='{"product":"QS3D","product":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"pro\u0064uct":"QS3D","product":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"target":"BricsCAD","TARGET":"evil"}'; Name='target'; Expected=2 }
)
'''
probe += r'''
foreach ($case in $cases) {
    $actual = Get-JsonPropertyOccurrenceCount -JsonText $case.Json -PropertyName $case.Name
    if ($actual -ne $case.Expected) {
        throw "duplicate-property probe failed for $($case.Name): expected $($case.Expected), got $actual"
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
    raise SystemExit("ERROR: behavioral duplicate/escaped/case-variant-key probe failed: " + (completed.stderr or completed.stdout).strip())
print("PASS: V26 package/update identity uniqueness is pre-parse and duplicate/escaped/case-variant-key aware")
