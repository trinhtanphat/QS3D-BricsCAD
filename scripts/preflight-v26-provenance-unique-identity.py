from pathlib import Path
import os
import subprocess

ROOT = Path(__file__).resolve().parents[1]
ASSERT_PATH = ROOT / "scripts/assert-v26-candidate-identity.ps1"
ASSERT = ASSERT_PATH.read_text(encoding="utf-8")

CRITICAL_PROVENANCE_FIELDS = (
    "product",
    "target",
    "releaseTag",
    "sourceCommit",
    "productVersion",
    "packageSha256",
    "installerSha256",
    "hostReferences",
)

unique_loop = "foreach ($propertyName in @('product', 'target', 'releaseTag', 'sourceCommit', 'productVersion', 'packageSha256', 'installerSha256', 'hostReferences'))"
if unique_loop not in ASSERT:
    raise SystemExit(
        "FAIL v26 provenance unique identity: release-critical provenance properties are not all uniqueness-checked before parsing"
    )

parse_token = "$provenance = $provenanceText | ConvertFrom-Json -ErrorAction Stop"
loop_at = ASSERT.find(unique_loop)
parse_at = ASSERT.find(parse_token)
if parse_at < 0 or loop_at < 0 or loop_at >= parse_at:
    raise SystemExit(
        "FAIL v26 provenance unique identity: byte-level duplicate checks must precede ConvertFrom-Json provenance parsing"
    )

for field in CRITICAL_PROVENANCE_FIELDS:
    expected = f"V26 candidate provenance must contain exactly one {field} property."
    if expected not in ASSERT:
        raise SystemExit(
            f"FAIL v26 provenance unique identity: missing fail-closed duplicate diagnostic for {field}"
        )

# The aggregate guard is intentionally portable. The production assertion itself is Windows-only
# because it holds Win32 file generations; on Windows, smoke the parser once so lexical decoys alone
# cannot satisfy this guard.
if os.name == "nt":
    completed = subprocess.run(
        [
            "powershell",
            "-NoLogo",
            "-NoProfile",
            "-NonInteractive",
            "-Command",
            f"$null = [ScriptBlock]::Create((Get-Content -LiteralPath '{str(ASSERT_PATH).replace("'", "''")}' -Raw)); exit 0",
        ],
        cwd=ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        timeout=30,
        check=False,
    )
    if completed.returncode != 0:
        raise SystemExit(
            "FAIL v26 provenance unique identity: production assertion is not parseable PowerShell:\n"
            + (completed.stdout or "<no output>")[-3000:]
        )

print("PASS V26 provenance release-critical identity fields are duplicate-sensitive before JSON parsing")
