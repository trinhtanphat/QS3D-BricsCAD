#!/usr/bin/env python3
from pathlib import Path

source = Path("scripts/new-v26-candidate-provenance.ps1").read_text(encoding="utf-8")

required_tokens = (
    "function Get-JsonPropertyOccurrenceCount",
    "@{ Version = 1; Files = 1; Name = $requiredHostNames.Count; Path = $requiredHostNames.Count; Sha256 = $requiredHostNames.Count; Length = $requiredHostNames.Count }",
    "V26 host-reference state must contain exactly",
    "foreach ($propertyName in @('product', 'target', 'framework', 'productVersion'))",
    "V26 PACKAGE-METADATA.json must contain exactly one $propertyName property.",
)
for token in required_tokens:
    if token not in source:
        raise SystemExit(f"ERROR: V26 provenance-generator input uniqueness contract missing: {token}")

host_parse = "return $text | ConvertFrom-Json -ErrorAction Stop"
host_call = "$hostState = Read-StrictUtf8Json"
metadata_parse = "$metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop"
if source.index("V26 host-reference state must contain exactly") > source.index(host_parse):
    raise SystemExit("ERROR: host-reference-state identity uniqueness must be proved before JSON parsing")
if source.index("V26 PACKAGE-METADATA.json must contain exactly one $propertyName property.") > source.index(metadata_parse):
    raise SystemExit("ERROR: PACKAGE-METADATA identity uniqueness must be proved before JSON parsing")
if source.index(host_call) < source.index("function Get-JsonPropertyOccurrenceCount"):
    raise SystemExit("ERROR: host-reference-state admission must use the duplicate-aware property helper")

print("PASS: V26 provenance generator rejects duplicate host-state/package identity before JSON parsing")
