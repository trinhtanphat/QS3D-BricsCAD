#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts/publish-v26-release.ps1"
HELPER = ROOT / "scripts/invoke-v26-held-release-upload.ps1"

errors = []
if not PUBLISHER.is_file():
    errors.append("missing scripts/publish-v26-release.ps1")
    publisher = ""
else:
    publisher = PUBLISHER.read_text(encoding="utf-8")

if not HELPER.is_file():
    errors.append("missing scripts/invoke-v26-held-release-upload.ps1")
    helper = ""
else:
    helper = HELPER.read_text(encoding="utf-8")

required_publisher = [
    "& .\\scripts\\invoke-v26-held-release-upload.ps1",
    "$admittedAssets[$name] = $admittedAsset",
    "$expectedLength = [int64]$admittedAssets[$expectedAsset].Length",
    "$expectedHash = [string]$admittedAssets[$expectedAsset].Sha256",
    "-AdmittedAssets $admittedAssets",
]
for token in required_publisher:
    if token not in publisher:
        errors.append(f"V26 publisher missing held-generation token: {token}")

for forbidden in [
    "Invoke-RestMethod -Method Post -Uri ($uploadBase + '?name=' + [Uri]::EscapeDataString($name)) -Headers $headers -ContentType $contentType -InFile $asset",
    "$localLength = [int64](Get-Item -LiteralPath $localAsset).Length",
    "verify-v26-held-file.ps1 -Operation Hash -Path $localAsset",
]:
    if forbidden in publisher:
        errors.append(f"V26 publisher still reopens/uploads by pathname: {forbidden}")

required_helper = [
    "[IO.FileShare]::Read",
    "[Security.Cryptography.SHA256]::Create()",
    "$held.Stream.Position = 0",
    "[System.Net.Http.StreamContent]::new($held.Stream)",
    "[System.Net.Http.HttpClient]::new()",
    "UploadedAssetId",
    "Sha256",
    "CanonicalPath",
    "LastWriteTimeUtcTicks",
    "ReparsePoint",
]
for token in required_helper:
    if token not in helper:
        errors.append(f"V26 held-upload helper missing invariant token: {token}")

if helper:
    hash_pos = helper.find("ComputeHash($held.Stream)")
    rewind_pos = helper.find("$held.Stream.Position = 0")
    content_pos = helper.find("[System.Net.Http.StreamContent]::new($held.Stream)")
    send_pos = helper.find("SendAsync")
    dispose_pos = helper.rfind("$held.Stream.Dispose()")
    if min(hash_pos, rewind_pos, content_pos, send_pos, dispose_pos) < 0 or not (
        hash_pos < rewind_pos < content_pos < send_pos < dispose_pos
    ):
        errors.append("V26 held-upload helper must hash -> rewind -> stream-upload -> dispose the same admitted generation")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("PASS V26 held release upload generation binding")
