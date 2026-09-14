from pathlib import Path

root = Path(__file__).resolve().parents[1]
helper = (root / "scripts/upload-v25-held-release-asset.ps1").read_text(encoding="utf-8")
workflow = (root / ".github/workflows/release-v25-cloud.yml").read_text(encoding="utf-8")

required_helper = [
    "$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()",
    "ConvertFrom-Json -ErrorAction Stop",
    "$uploadedAsset.id",
    "$uploadedAsset.name",
    "$uploadedAsset.state",
    "$uploadedAsset.size",
    "$uploadedAsset.digest",
    "$expectedDigest = 'sha256:' + $ExpectedSha256.ToLowerInvariant()",
    "^sha256:[0-9A-Fa-f]{64}$",
    "GitHub release asset upload returned a mismatched SHA-256 digest",
]
for token in required_helper:
    if token not in helper:
        raise SystemExit(f"missing V25 cloud upload response identity guard token: {token}")

ordered_tokens = [
    "$actualHash = ([BitConverter]::ToString($sha256.ComputeHash($stream))).Replace('-', '')",
    "$response = $client.PostAsync($uploadUri, $content).GetAwaiter().GetResult()",
    "$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()",
    "$uploadedAsset.digest",
    "$response.Dispose()",
]
positions = [helper.index(token) for token in ordered_tokens]
if positions != sorted(positions) or len(set(positions)) != len(positions):
    raise SystemExit(
        "V25 cloud upload identity ordering must remain held-hash -> POST -> response parse -> digest admission -> response dispose"
    )

required_workflow = [
    ".\\scripts\\upload-v25-held-release-asset.ps1",
    "-ExpectedSha256 $spec.Hash",
    "-ExpectedSize $spec.Size",
]
for token in required_workflow:
    if token not in workflow:
        raise SystemExit(f"cloud preview workflow lost held upload identity binding: {token}")

print("V25 cloud release upload digest preflight: PASS")
