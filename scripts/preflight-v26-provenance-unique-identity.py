from pathlib import Path
import hashlib
import json
import os
import subprocess
import tempfile
import zipfile

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

loop_body_end = ASSERT.find("try { $provenance =", loop_at)
if loop_body_end < 0:
    raise SystemExit("FAIL v26 provenance unique identity: uniqueness loop is not adjacent to provenance parsing")
loop_body = ASSERT[loop_at:loop_body_end]
for token in (
    "Get-JsonPropertyOccurrenceCount -JsonText $provenanceText -PropertyName $propertyName",
    'throw "V26 candidate provenance must contain exactly one $propertyName property."',
):
    if token not in loop_body:
        raise SystemExit(
            f"FAIL v26 provenance unique identity: uniqueness loop is missing required fail-closed behavior: {token}"
        )


def run_windows_duplicate_behavior() -> None:
    if os.name != "nt":
        return

    source_commit = "a" * 40
    installer_sha = "b" * 64
    release_tag = "v1.2.3"
    host_names = ("bricscad.exe", "BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll")

    with tempfile.TemporaryDirectory(prefix="qs3d-v26-provenance-duplicates-") as temp:
        root = Path(temp)
        package = root / "QS3D-BricsCAD-V26.zip"
        metadata = {
            "product": "QS3D",
            "target": "BricsCAD V26 x64",
            "framework": "net8.0-windows",
            "productVersion": "1.2.3",
        }
        with zipfile.ZipFile(package, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            archive.writestr("PACKAGE-METADATA.json", json.dumps(metadata, separators=(",", ":")))
        package_sha = hashlib.sha256(package.read_bytes()).hexdigest().upper()

        checksum = root / "QS3D-BricsCAD-V26.zip.sha256"
        checksum.write_text(f"{package_sha}  QS3D-BricsCAD-V26.zip\n", encoding="utf-8")
        provenance_path = root / "QS3D-BricsCAD-V26.provenance.json"
        provenance = {
            "product": "QS3D",
            "target": "BricsCAD V26 x64",
            "releaseTag": release_tag,
            "sourceCommit": source_commit,
            "productVersion": "1.2.3",
            "packageSha256": package_sha,
            "installerSha256": installer_sha,
            "hostReferences": [
                {"name": name, "sha256": f"{index + 1:064x}", "length": index + 1}
                for index, name in enumerate(host_names)
            ],
        }

        command = [
            "powershell",
            "-NoLogo",
            "-NoProfile",
            "-NonInteractive",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            str(ASSERT_PATH),
            "-PackageZip",
            str(package),
            "-ChecksumPath",
            str(checksum),
            "-ProvenancePath",
            str(provenance_path),
            "-ExpectedSourceCommit",
            source_commit,
            "-ExpectedReleaseTag",
            release_tag,
            "-ExpectedInstallerSha256",
            installer_sha,
        ]

        def invoke(label: str, raw_provenance: str, expect_success: bool) -> None:
            provenance_path.write_text(raw_provenance, encoding="utf-8")
            completed = subprocess.run(
                command,
                cwd=ROOT,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                timeout=45,
                check=False,
            )
            passed = completed.returncode == 0
            if passed != expect_success:
                tail = completed.stdout[-3000:] if completed.stdout else "<no output>"
                expectation = "PASS" if expect_success else "FAIL"
                raise SystemExit(
                    f"FAIL v26 provenance unique identity: behavioral case {label!r} expected {expectation}, "
                    f"exit={completed.returncode}:\n{tail}"
                )

        valid = json.dumps(provenance, separators=(",", ":"))
        invoke("valid provenance", valid, True)

        for field in CRITICAL_PROVENANCE_FIELDS:
            encoded_value = json.dumps(provenance[field], separators=(",", ":"))
            duplicate = valid[:-1] + f',"{field}":{encoded_value}' + "}"
            invoke(f"duplicate {field}", duplicate, False)


run_windows_duplicate_behavior()
print("PASS V26 provenance release-critical identity fields are duplicate-sensitive before JSON parsing")
