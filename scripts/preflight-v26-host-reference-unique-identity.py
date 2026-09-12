#!/usr/bin/env python3
from pathlib import Path
import re

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

print("PASS: V26 host-reference nested identity is duplicate-rejected pre-parse and shape-validated post-parse")
