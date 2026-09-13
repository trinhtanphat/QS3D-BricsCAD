#!/usr/bin/env python3
from pathlib import Path
import subprocess
import tempfile

source = Path("scripts/assert-v26-candidate-identity.ps1").read_text(encoding="utf-8")

required_tokens = (
    "function Get-JsonPropertyOccurrenceCount",
    "$provenanceText = Read-HeldText",
    "$metadataText = $reader.ReadToEnd()",
    "$updateText = Read-HeldText",
)
for token in required_tokens:
    if token not in source:
        raise SystemExit(f"ERROR: V26 candidate identity path-scope contract missing anchor: {token}")

helper_start = source.index("function Get-JsonPropertyOccurrenceCount")
helper_end = source.index("if ([string]::IsNullOrWhiteSpace($ExpectedInstallerSha256)", helper_start)
helper_block = source[helper_start:helper_end]

provenance_read = source.index("$provenanceText = Read-HeldText")
provenance_parse = source.index("$provenance = $provenanceText | ConvertFrom-Json", provenance_read)
metadata_read = source.index("$metadataText = $reader.ReadToEnd()")
metadata_parse = source.index("$metadata = $metadataText | ConvertFrom-Json", metadata_read)
update_read = source.index("$updateText = Read-HeldText")
update_parse = source.index("$update = $updateText | ConvertFrom-Json", update_read)

for label, start, parse in (
    ("provenance", provenance_read, provenance_parse),
    ("PACKAGE-METADATA", metadata_read, metadata_parse),
    ("update manifest", update_read, update_parse),
):
    segment = source[start:parse]
    if "Get-JsonPropertyOccurrenceCount" not in segment:
        raise SystemExit(f"ERROR: {label} identity cardinality must be proved before ConvertFrom-Json")

# PowerShell single-quoted strings preserve double quotes literally. These are
# valid JSON fixtures; only the JSON unicode escape deliberately contains a
# backslash so the behavioral probe exercises escaped-equivalent names.
probe = helper_block + r'''
$cases = @(
    @{ Json='{"product":"QS3D"}'; Name='product'; Expected=1 },
    @{ Json='{"product":"QS3D","PRODUCT":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"product":"QS3D","pro\u0064uct":"evil"}'; Name='product'; Expected=2 },
    @{ Json='{"product":"QS3D","extension":{"product":"diagnostic"}}'; Name='product'; Expected=1 },
    @{ Json='{"sourceCommit":"0123456789012345678901234567890123456789","extension":{"sourceCommit":"ignored"}}'; Name='sourceCommit'; Expected=1 },
    @{ Json='{"framework":"net8.0-windows","extension":[{"framework":"ignored"}]}'; Name='framework'; Expected=1 },
    @{ Json='{"schemaVersion":2,"extension":{"schemaVersion":999}}'; Name='schemaVersion'; Expected=1 }
)
foreach ($case in $cases) {
    $actual = Get-JsonPropertyOccurrenceCount -JsonText $case.Json -PropertyName $case.Name
    if ($actual -ne $case.Expected) {
        throw "V26 candidate root identity scope failed for $($case.Name): expected $($case.Expected), got $actual"
    }
}
'''

# The Python source above intentionally uses backslash escapes to encode the
# embedded PowerShell text. Strip only quote escapes before execution; keep
# JSON's \u escape intact.
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
    raise SystemExit(
        "ERROR: V26 candidate identity path-scope behavioral regression failed: "
        + (completed.stderr or completed.stdout).strip()
    )

print("PASS: V26 candidate identity admission is root-scoped before parser collapse")
