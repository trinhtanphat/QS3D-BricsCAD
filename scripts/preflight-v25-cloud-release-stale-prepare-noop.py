#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/release-v25-cloud.yml"
PREPARE = ROOT / "scripts/prepare-v25-cloud-release.ps1"

workflow = WORKFLOW.read_text(encoding="utf-8")
prepare = PREPARE.read_text(encoding="utf-8")
errors: list[str] = []


def require(source: str, token: str, label: str) -> None:
    if token not in source:
        errors.append(f"missing {label}: {token}")


def forbid(source: str, token: str, label: str) -> None:
    if token in source:
        errors.append(f"forbidden {label}: {token}")


def between(source: str, start: str, end: str, label: str) -> str:
    start_pos = source.find(start)
    end_pos = source.find(end, start_pos + len(start)) if start_pos >= 0 else -1
    if start_pos < 0 or end_pos < 0:
        errors.append(f"could not isolate {label}")
        return ""
    return source[start_pos:end_pos]

# Trusted current-main admission must finish before stale source code can run.
admission = between(workflow, "  source-admission:\n", "  release:\n", "source-admission job")
for token, label in (
    ("permissions:\n      contents: read", "read-only admission permissions"),
    ("outputs:\n      proceed: ${{ steps.classify.outputs.proceed }}", "admission proceed output"),
    ("ref: main", "trusted protected-main checkout"),
    ("fetch-depth: 0", "full ancestry checkout"),
    ("persist-credentials: false", "credential isolation"),
    ("id: classify", "classification step"),
    ("git merge-base --is-ancestor $sourceSha $currentMain", "dispatch ancestry validation"),
    ("$releaseRelevantPaths = @(\n", "release-relevant path set"),
    ("git diff --quiet --no-ext-diff $sourceSha $currentMain -- $releaseRelevantPaths", "pathname-safe drift classification"),
    ('"proceed=false" | Out-File -FilePath $env:GITHUB_OUTPUT', "graceful no-op output"),
    ("V25_RELEASE_SUPERSEDED", "superseded audit marker"),
    ('"proceed=true" | Out-File -FilePath $env:GITHUB_OUTPUT', "proceed output"),
):
    require(admission, token, label)

for token in ("python scripts", ".\\scripts\\", "dotnet ", "package-v25", "Invoke-RestMethod -Method Post"):
    forbid(admission, token, "stale/release execution inside trusted admission")

release = workflow[workflow.find("  release:\n"):] if "  release:\n" in workflow else ""
for token, label in (
    ("needs: source-admission", "release dependency on trusted admission"),
    ("needs.source-admission.outputs.proceed == 'true'", "release proceed gate"),
    ("ref: ${{ inputs.source_sha || github.sha }}", "bounded source checkout after admission"),
    ("Prepare exact release source commit", "prepare step after admission"),
):
    require(release, token, label)

# The release job must appear only after the admission job in workflow text.
admission_pos = workflow.find("  source-admission:\n")
release_pos = workflow.find("  release:\n")
if admission_pos < 0 or release_pos < 0 or admission_pos >= release_pos:
    errors.append("trusted source admission must precede the release job")

# Any release-relevant drift that races after admission must fail closed in prepare.
for token, label in (
    ("function Assert-ReleaseBaseIsSafe", "post-admission fail-closed helper"),
    ("main moved after dispatch with release-relevant changes", "post-admission hard failure"),
    ("git merge-base --is-ancestor $dispatch $TargetSha", "ambiguous-history ancestry check"),
    ("git diff --quiet --no-ext-diff $range -- @releaseRelevantPathspecs", "pathname-safe prepare drift check"),
):
    require(prepare, token, label)

for token in (
    "Keeping dispatched source $dispatch as the bounded release workspace",
    "publish-stage stale-source no-op can classify supersession",
    "$releaseBase = $dispatch",
):
    forbid(prepare, token, "post-admission stale handoff")

if errors:
    print("ERROR: V25 trusted stale-source admission preflight failed closed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print(
    "PASS: trusted current-main admission no-ops already-superseded V25 releases before stale source execution; post-admission release-relevant races remain fail-closed"
)
