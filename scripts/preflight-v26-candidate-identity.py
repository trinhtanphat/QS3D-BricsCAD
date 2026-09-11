from pathlib import Path
import hashlib
import json
import os
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = (ROOT / ".github/workflows/release-v26.yml").read_text(encoding="utf-8")
PROVENANCE = (ROOT / "scripts/new-v26-candidate-provenance.ps1").read_text(encoding="utf-8")
ASSERT_PATH = ROOT / "scripts/assert-v26-candidate-identity.ps1"
ASSERT = ASSERT_PATH.read_text(encoding="utf-8")
RUNBOOK = (ROOT / "docs/FEATURE-RUNBOOKS/v26-candidate-identity.md").read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL v26 candidate identity: missing {label}: {token}")


for token, label in (
    ("FileShare]::Read", "held-generation read sharing"),
    ("PACKAGE-METADATA.json", "ZIP metadata admission"),
    ("BricsCAD V26 x64", "V26 target binding"),
    ("net8.0-windows", "V26 framework binding"),
    ("sourceCommit", "source provenance"),
    ("packageSha256", "package digest provenance"),
):
    require(PROVENANCE, token, label)

for token, label in (
    ("FileShare]::Read", "held downloaded generations"),
    ("ExpectedSourceCommit", "workflow SHA admission"),
    ("ExpectedReleaseTag", "release tag admission"),
    ("[string]$ExpectedPackageUri", "exact HTTPS package URI admission parameter"),
    ("[string]$ExpectedSignerThumbprint", "exact signer admission parameter"),
    ("[int]$ExpectedManifestSchemaVersion = 2", "manifest schema admission parameter"),
    ("PACKAGE-METADATA.json", "held ZIP metadata"),
    ("packageSha256", "provenance package digest"),
    ("[string]$update.sha256", "update-manifest package digest"),
    ("[string]$update.packageUri", "update-manifest package URI"),
    ("[string]$update.signerThumbprint", "update-manifest signer thumbprint"),
    ("[int]$update.schemaVersion", "update-manifest schema version"),
    ("V26 update manifest package URI mismatch", "package URI mismatch rejection"),
    ("V26 update manifest signer thumbprint mismatch", "signer mismatch rejection"),
    ("V26 update manifest schema version mismatch", "schema mismatch rejection"),
    ("BricsCAD V26 x64", "V26 target identity"),
    ("net8.0-windows", "V26 framework identity"),
    ("[string]$AdmittedScript", "held-generation admitted action parameter"),
    ("Open-Held -Path $AdmittedScript -Label 'V26 admitted publication script'", "publisher script held-generation admission"),
    ("Read-HeldText -Held $scriptHeld -Label 'V26 admitted publication script' -MaxBytes $maxAdmittedScriptBytes", "bounded strict-UTF8 publisher read"),
    ("[ScriptBlock]::Create($scriptText)", "publisher execution compiled from admitted bytes"),
    ("& $admittedScriptBlock", "publication from admitted publisher bytes"),
    ("after publication", "post-publication generation continuity assertion"),
):
    require(ASSERT, token, label)

if "& $scriptItem.FullName" in ASSERT:
    raise SystemExit("FAIL v26 candidate identity: publisher must not be reopened by pathname after admission")
if "$held.Add($scriptHeld)" not in ASSERT:
    raise SystemExit("FAIL v26 candidate identity: publisher script generation must remain held through publication")
if "$maxAdmittedScriptBytes = 262144" not in ASSERT:
    raise SystemExit("FAIL v26 candidate identity: publisher script admission must retain an explicit size bound")

# Signed manifests need the extra release identity; unsigned candidates legitimately have no update
# manifest and must not be forced to depend on a signing variable. The implementation therefore
# accepts optional expected manifest fields globally, but must fail closed if a held manifest exists
# without all of them and must validate them before publisher admission.
update_block = ASSERT.find("if ($null -ne $updateHeld)")
publisher_block = ASSERT.find("$admittedScriptBlock = $null")
if update_block < 0 or publisher_block < 0 or update_block >= publisher_block:
    raise SystemExit("FAIL v26 candidate identity: update-manifest admission must precede publisher admission")
for token in (
    "ExpectedPackageUri is required when UpdateManifestPath is supplied",
    "ExpectedSignerThumbprint is required when UpdateManifestPath is supplied",
    "[string]$update.packageUri",
    "[string]$update.signerThumbprint",
    "[int]$update.schemaVersion",
):
    if ASSERT.find(token, update_block, publisher_block) < 0:
        raise SystemExit(f"FAIL v26 candidate identity: held update-manifest block is missing exact release binding: {token}")

require(RUNBOOK, "Lane-Key: `issue-5313`", "original candidate-identity lane provenance")
require(RUNBOOK, "Issue #5399", "publisher-generation hardening provenance")
require(RUNBOOK, "exact admitted publisher-script bytes", "publisher-generation execution contract")
require(RUNBOOK, "exact workflow SHA", "source-boundary documentation")

create_token = "new-v26-candidate-provenance.ps1"
upload_token = "Upload V26 qualification artifacts"
admit_token = "assert-v26-candidate-identity.ps1"
held_publish_token = "-AdmittedScript '.\\scripts\\publish-v26-release.ps1'"
for token, label in (
    (create_token, "provenance creation"),
    ("dist/QS3D-BricsCAD-V26.provenance.json", "provenance artifact upload"),
    (admit_token, "release-job semantic admission"),
    ("-ExpectedSourceCommit $env:GITHUB_SHA", "exact workflow SHA argument"),
    ("-ExpectedReleaseTag $env:RELEASE_TAG", "exact release tag argument"),
    ("-ExpectedPackageUri $expectedPackageUri", "exact repository/tag/asset URI argument"),
    ("-ExpectedSignerThumbprint $expectedSignerThumbprint", "exact admitted signer argument"),
    ("-ExpectedManifestSchemaVersion 2", "exact update-manifest schema argument"),
    (held_publish_token, "publication under held admitted generations"),
    ("GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}", "publisher token on held-admission step"),
):
    require(WORKFLOW, token, label)

if not (WORKFLOW.index(create_token) < WORKFLOW.index(upload_token)):
    raise SystemExit("FAIL v26 candidate identity: provenance must be created before artifact upload")
if WORKFLOW.count(held_publish_token) != 2:
    raise SystemExit("FAIL v26 candidate identity: signed and unsigned publication must both execute under held candidate generations")
if WORKFLOW.count("-ExpectedPackageUri $expectedPackageUri") != 1 or WORKFLOW.count("-ExpectedSignerThumbprint $expectedSignerThumbprint") != 1:
    raise SystemExit("FAIL v26 candidate identity: manifest URI/signer admission arguments must be confined to the signed publication path")

signed_branch = WORKFLOW.find("if ($env:V26_RELEASE_REQUEST_SIGN_PACKAGE -eq 'true') {")
unsigned_branch = WORKFLOW.find("else {", signed_branch)
if signed_branch < 0 or unsigned_branch < 0 or signed_branch >= unsigned_branch:
    raise SystemExit("FAIL v26 candidate identity: unable to locate signed V26 publication branch")
for token in (
    "$expectedPackageUri = \"https://github.com/$env:GITHUB_REPOSITORY/releases/download/$env:RELEASE_TAG/QS3D-BricsCAD-V26.zip\"",
    "$expectedSignerThumbprint = $env:QS3D_SIGNING_CERT_THUMBPRINT",
    "-ExpectedPackageUri $expectedPackageUri",
    "-ExpectedSignerThumbprint $expectedSignerThumbprint",
    "-ExpectedManifestSchemaVersion 2",
):
    if WORKFLOW.find(token, signed_branch, unsigned_branch) < 0:
        raise SystemExit(f"FAIL v26 candidate identity: signed publication branch is missing exact manifest admission token: {token}")

if "- name: Publish V26 GitHub Release" in WORKFLOW:
    raise SystemExit("FAIL v26 candidate identity: publication must not be split into a later step after held-generation admission")


def run_windows_manifest_identity_behavior() -> None:
    if os.name != "nt":
        return

    source_commit = "a" * 40
    installer_sha = "b" * 64
    signer = "C" * 40
    release_tag = "v1.2.3"
    package_uri = "https://github.com/trinhtanphat/QS3D-BricsCAD/releases/download/v1.2.3/QS3D-BricsCAD-V26.zip"
    host_names = ("bricscad.exe", "BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll")

    with tempfile.TemporaryDirectory(prefix="qs3d-v26-manifest-admission-") as temp:
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
        provenance = root / "QS3D-BricsCAD-V26.provenance.json"
        provenance.write_text(
            json.dumps(
                {
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
                },
                separators=(",", ":"),
            ),
            encoding="utf-8",
        )
        manifest_path = root / "QS3D-BricsCAD-V26.update.json"
        valid_manifest = {
            "schemaVersion": 2,
            "product": "QS3D",
            "target": "BricsCAD V26 x64",
            "productVersion": "1.2.3",
            "packageUri": package_uri,
            "sha256": package_sha,
            "signerThumbprint": signer,
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
            str(provenance),
            "-UpdateManifestPath",
            str(manifest_path),
            "-ExpectedSourceCommit",
            source_commit,
            "-ExpectedReleaseTag",
            release_tag,
            "-ExpectedPackageUri",
            package_uri,
            "-ExpectedSignerThumbprint",
            signer,
            "-ExpectedManifestSchemaVersion",
            "2",
            "-ExpectedInstallerSha256",
            installer_sha,
        ]

        def invoke(label: str, manifest: dict, expect_success: bool) -> None:
            manifest_path.write_text(json.dumps(manifest, separators=(",", ":")), encoding="utf-8")
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
                    f"FAIL v26 candidate identity: behavioral case {label!r} expected {expectation}, "
                    f"exit={completed.returncode}:\n{tail}"
                )

        invoke("valid signed manifest", dict(valid_manifest), True)
        uri_mutant = dict(valid_manifest)
        uri_mutant["packageUri"] = "https://example.invalid/QS3D-BricsCAD-V26.zip"
        invoke("mismatched package URI", uri_mutant, False)
        signer_mutant = dict(valid_manifest)
        signer_mutant["signerThumbprint"] = "D" * 40
        invoke("mismatched signer", signer_mutant, False)
        schema_mutant = dict(valid_manifest)
        schema_mutant["schemaVersion"] = 3
        invoke("mismatched schema", schema_mutant, False)


run_windows_manifest_identity_behavior()
print("PASS v26 candidate semantic identity, manifest URI/signer/schema binding, held publisher generation, and publication continuity")
