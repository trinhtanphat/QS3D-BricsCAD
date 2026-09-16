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
    "Add-Type -AssemblyName System.Net.Http",
    "[System.Net.Http.StreamContent]::new($held.Stream)",
    "[System.Net.Http.HttpClientHandler]::new()",
    "$handler.AllowAutoRedirect = $false",
    "[System.Net.Http.HttpClient]::new($handler)",
    "$response.StatusCode -ne [System.Net.HttpStatusCode]::Created",
    "ConvertFrom-Json -ErrorAction Stop",
    "[string]$uploaded.state, 'uploaded'",
    "[string]$uploaded.digest",
    "UploadedAssetId",
    "Sha256",
    "CanonicalPath",
    "LastWriteTimeUtcTicks",
    "ReparsePoint",
]
for token in required_helper:
    if token not in helper:
        errors.append(f"V26 held-upload helper missing invariant token: {token}")

for forbidden in [
    "[System.Net.Http.HttpClient]::new()",
    "$response.IsSuccessStatusCode",
    ": $responseBody",
]:
    if forbidden in helper:
        errors.append(f"V26 held-upload helper contains unsafe authority token: {forbidden}")

if helper:
    bootstrap_pos = helper.find("Add-Type -AssemblyName System.Net.Http")
    first_http_type_pos = helper.find("[System.Net.Http.")
    if bootstrap_pos < 0 or first_http_type_pos < 0 or bootstrap_pos >= first_http_type_pos:
        errors.append("V26 held-upload helper must preload System.Net.Http before the first System.Net.Http type resolution")

    hash_pos = helper.find("ComputeHash($held.Stream)")
    rewind_pos = helper.find("$held.Stream.Position = 0")
    handler_pos = helper.find("[System.Net.Http.HttpClientHandler]::new()")
    redirect_pos = helper.find("$handler.AllowAutoRedirect = $false")
    auth_pos = helper.find("DefaultRequestHeaders.Authorization")
    content_pos = helper.find("[System.Net.Http.StreamContent]::new($held.Stream)")
    send_pos = helper.find("SendAsync")
    status_pos = helper.find("$response.StatusCode -ne [System.Net.HttpStatusCode]::Created")
    parse_pos = helper.find("ConvertFrom-Json -ErrorAction Stop")
    state_pos = helper.find("[string]$uploaded.state, 'uploaded'")
    digest_pos = helper.find("[string]$uploaded.digest")
    dispose_pos = helper.rfind("$held.Stream.Dispose()")
    positions = [hash_pos, rewind_pos, handler_pos, redirect_pos, auth_pos, content_pos, send_pos, status_pos, parse_pos, state_pos, digest_pos, dispose_pos]
    if min(positions) < 0 or positions != sorted(positions):
        errors.append("V26 held-upload helper must hash -> rewind -> disable redirects -> authorize -> stream -> send -> require 201 -> parse -> bind state/digest -> dispose")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("PASS V26 held release upload generation and authority binding")
