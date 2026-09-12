#!/usr/bin/env python3
from pathlib import Path

source = Path("scripts/assert-v26-candidate-identity.ps1").read_text(encoding="utf-8")

metadata_loop = "foreach ($propertyName in @('product', 'target', 'framework', 'productVersion'))"
update_loop = "foreach ($propertyName in @('product', 'target', 'productVersion', 'schemaVersion', 'packageUri', 'sha256', 'signerThumbprint'))"
metadata_parse = "try { $metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop }"
update_parse = "try { $update = $updateText | ConvertFrom-Json -ErrorAction Stop }"

missing = [token for token in (metadata_loop, update_loop, metadata_parse, update_parse) if token not in source]
if missing:
    raise SystemExit("ERROR: V26 package/update identity uniqueness contract missing: " + "; ".join(missing))

if source.index(metadata_loop) > source.index(metadata_parse):
    raise SystemExit("ERROR: PACKAGE-METADATA identity uniqueness must be checked before JSON parsing")
if source.index(update_loop) > source.index(update_parse):
    raise SystemExit("ERROR: update-manifest identity uniqueness must be checked before JSON parsing")

old_update_loop = "foreach ($propertyName in @('schemaVersion', 'packageUri', 'sha256', 'signerThumbprint'))"
if old_update_loop in source:
    raise SystemExit("ERROR: stale update-manifest uniqueness set omits product/target/productVersion")

print("PASS: V26 package metadata and update manifest release identity is uniqueness-checked before JSON parsing")
