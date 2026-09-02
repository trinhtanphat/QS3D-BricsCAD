from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = (ROOT / ".github/workflows/release-v26.yml").read_text(encoding="utf-8")
PROVENANCE = (ROOT / "scripts/new-v26-candidate-provenance.ps1").read_text(encoding="utf-8")
ASSERT = (ROOT / "scripts/assert-v26-candidate-identity.ps1").read_text(encoding="utf-8")
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
    ("PACKAGE-METADATA.json", "held ZIP metadata"),
    ("packageSha256", "provenance package digest"),
    ("[string]$update.sha256", "update-manifest package digest"),
    ("BricsCAD V26 x64", "V26 target identity"),
    ("net8.0-windows", "V26 framework identity"),
    ("[string]$AdmittedScript", "held-generation admitted action parameter"),
    ("& $scriptItem.FullName", "publication while candidate handles remain held"),
    ("after publication", "post-publication generation continuity assertion"),
):
    require(ASSERT, token, label)

require(RUNBOOK, "Lane-Key: `issue-5313`", "canonical lane key")
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
    (held_publish_token, "publication under held admitted generations"),
    ("GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}", "publisher token on held-admission step"),
):
    require(WORKFLOW, token, label)

if not (WORKFLOW.index(create_token) < WORKFLOW.index(upload_token)):
    raise SystemExit("FAIL v26 candidate identity: provenance must be created before artifact upload")
if WORKFLOW.count(held_publish_token) != 2:
    raise SystemExit("FAIL v26 candidate identity: signed and unsigned publication must both execute under held candidate generations")
if "- name: Publish V26 GitHub Release" in WORKFLOW:
    raise SystemExit("FAIL v26 candidate identity: publication must not be split into a later step after held-generation admission")

print("PASS v26 post-job-boundary candidate semantic identity and publication continuity")
