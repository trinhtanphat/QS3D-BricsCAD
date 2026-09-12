#!/usr/bin/env python3
from pathlib import Path
import re
import subprocess
import tempfile

source = Path("scripts/assert-v26-candidate-identity.ps1").read_text(encoding="utf-8")
parse_anchor = "try { $provenance = $provenanceText | ConvertFrom-Json -ErrorAction Stop }"
parse_pos = source.index(parse_anchor)
preparse = source[:parse_pos]

loop = re.search(
    r"foreach \(\$hostPropertyName in @\('name', 'sha256', 'length'\)\) \{\s*"
    r"if \(\(Get-JsonPropertyOccurrenceCount -JsonText \$provenanceText -PropertyName \$hostPropertyName\) -ne \$requiredHostNames\.Count\)",
    preparse,
    re.S,
)
if not loop:
    raise SystemExit(
        "ERROR: V26 provenance must prove exact raw host-reference name/sha256/length cardinality before ConvertFrom-Json"
    )

if "V26 candidate provenance host-reference identity must contain exactly" not in preparse[loop.start():]:
    raise SystemExit("ERROR: nested host-reference duplicate rejection must fail closed with an explicit admission error")

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
$cases = @(
    @{ Json='{"name":"bricscad.exe"}'; Name='name'; Expected=1 },
    @{ Json='{"name":"bricscad.exe","name":"evil"}'; Name='name'; Expected=2 },
    @{ Json='{"na\u006de":"bricscad.exe","name":"evil"}'; Name='name'; Expected=2 },
    @{ Json='{"sha256":"a","SHA256":"b"}'; Name='sha256'; Expected=2 },
    @{ Json='{"length":1,"LENGTH":2}'; Name='length'; Expected=2 }
)
foreach ($case in $cases) {
    $actual = Get-JsonPropertyOccurrenceCount -JsonText $case.Json -PropertyName $case.Name
    if ($actual -ne $case.Expected) {
        throw "host-reference duplicate-property probe failed for $($case.Name): expected $($case.Expected), got $actual"
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
        "ERROR: behavioral host-reference duplicate/escaped/case-variant-key probe failed: "
        + (completed.stderr or completed.stdout).strip()
    )

print("PASS: V26 host-reference nested identity is duplicate-rejected pre-parse, duplicate-spelling aware, and shape-validated post-parse")
