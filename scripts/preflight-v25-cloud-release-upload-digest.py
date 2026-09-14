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

if helper.index("$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()") > helper.index("$response.Dispose()"):
    raise SystemExit("V25 cloud upload response must be read before the HTTP response is disposed")
if helper.index("$uploadedAsset.digest") > helper.index("$response.Dispose()"):
    raise SystemExit("V25 cloud upload digest admission must complete before the HTTP response is disposed")

required_workflow = [
    ".\\scripts\\upload-v25-held-release-asset.ps1",
    "-ExpectedSha256 $spec.Hash",
    "-ExpectedSize $spec.Size",
]
for token in required_workflow:
    if token not in workflow:
        raise SystemExit(f"cloud preview workflow lost held upload identity binding: {token}")

print("V25 cloud release upload digest preflight: PASS")
