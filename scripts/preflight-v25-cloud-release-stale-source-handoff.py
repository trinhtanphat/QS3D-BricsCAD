#!/usr/bin/env python3
from pathlib import Path
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
RELEASE = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"
DISPATCH = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
VERSION_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V25" / "QS3D.BricsCAD.V25.csproj"

release = RELEASE.read_text(encoding="utf-8")
dispatch = DISPATCH.read_text(encoding="utf-8")
errors: list[str] = []


def require(source: str, token: str, label: str) -> None:
    if token not in source:
        errors.append(f"missing {label}: {token}")


def forbid(source: str, token: str, label: str) -> None:
    if token in source:
        errors.append(f"forbidden {label}: {token}")


def slice_block(source: str, start: str, end: str, label: str) -> tuple[str, int, int]:
    start_pos = source.find(start)
    end_pos = source.find(end, start_pos + len(start)) if start_pos >= 0 else -1
    if start_pos < 0 or end_pos < 0 or start_pos >= end_pos:
        errors.append(f"could not isolate {label}")
        return "", -1, -1
    return source[start_pos:end_pos], start_pos, end_pos


def find_after(source: str, token: str, after: int, label: str) -> int:
    pos = source.find(token, max(after, 0))
    if pos < 0:
        errors.append(f"missing {label}: {token}")
    return pos


def read_version_project_text() -> str:
    workspace_text = VERSION_PROJECT.read_text(encoding="utf-8")
    release_tag = os.environ.get("RELEASE_TAG", "").strip()
    if not release_tag:
        return workspace_text

    source_sha = os.environ.get("SOURCE_SHA", "").strip().lower()
    if not re.fullmatch(r"[0-9a-f]{40}", source_sha):
        errors.append("release-workspace source version validation requires an exact 40-hex SOURCE_SHA")
        return ""
    if not re.fullmatch(r"v[^\s]+", release_tag):
        errors.append(f"release-workspace source version validation requires a canonical v-prefixed RELEASE_TAG; found {release_tag!r}")
        return ""

    try:
        workspace_root = ET.fromstring(workspace_text)
    except ET.ParseError as exc:
        errors.append(f"could not parse bounded workspace V25 version project: {exc}")
        return ""
    workspace_versions = [node.text.strip() for node in workspace_root.iter("Version") if node.text and node.text.strip()]
    requested_version = release_tag[1:]
    if len(workspace_versions) != 1 or workspace_versions[0] != requested_version:
        errors.append(
            "bounded release workspace Version must exactly match RELEASE_TAG before immutable source validation; "
            f"tag={release_tag!r} versions={workspace_versions!r}"
        )
        return ""

    head = subprocess.run(
        ["git", "rev-parse", "--verify", "HEAD"],
        cwd=ROOT,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    if head.returncode != 0:
        errors.append(f"could not resolve release-workspace HEAD for immutable source validation: {head.stderr.strip()}")
        return ""
    if head.stdout.strip().lower() != source_sha:
        errors.append(
            "release-workspace HEAD must equal SOURCE_SHA before immutable source version validation; "
            f"head={head.stdout.strip().lower()!r} source_sha={source_sha!r}"
        )
        return ""

    project_path = VERSION_PROJECT.relative_to(ROOT).as_posix()
    committed = subprocess.run(
        ["git", "show", f"{source_sha}:{project_path}"],
        cwd=ROOT,
        check=False,
        capture_output=True,
        text=True,
        encoding="utf-8",
    )
    if committed.returncode != 0:
        errors.append(
            "could not read immutable V25 version project from SOURCE_SHA: "
            f"{committed.stderr.strip()}"
        )
        return ""
    return committed.stdout


# An admitted release stays pinned to exact SOURCE_SHA. Protected main may
# advance while build/package verification runs; ancestry is revalidated at
# each durable release boundary, but release-relevant drift does not turn a
# successful release workflow into a no-op.
publish = release[release.find("      - name: Publish GitHub prerelease"):]
for token, label in (
    ("git merge-base --is-ancestor $env:SOURCE_SHA $preMutationMain", "pre-mutation source ancestry"),
    ("git merge-base --is-ancestor $env:SOURCE_SHA $preMutationPublishMain", "moved pre-mutation source ancestry"),
    ("git merge-base --is-ancestor $env:SOURCE_SHA $finalMain", "final source ancestry"),
    ("git merge-base --is-ancestor $env:SOURCE_SHA $publishMain", "moved final source ancestry"),
    ("$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri", "final publication PATCH"),
):
    require(publish, token, label)
for token in (
    "V25_RELEASE_SUPERSEDED source_sha=$env:SOURCE_SHA",
    "successful no-op before the first persistent release mutation",
    "$preMutationReleaseRelevantPaths",
    "$finalReleaseRelevantPaths",
):
    forbid(publish, token, "green-without-release/stale-drift suppression")

release_mutations = [
    '$release = Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases"',
    ".\\scripts\\upload-v25-held-release-asset.ps1 `",
    "$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri",
]
release_mutation_positions = [release.find(token) for token in release_mutations]
for token, pos in zip(release_mutations, release_mutation_positions):
    if pos < 0:
        errors.append(f"missing guarded release mutation: {token}")
if all(pos >= 0 for pos in release_mutation_positions) and release_mutation_positions != sorted(release_mutation_positions):
    errors.append("release mutation order changed: draft create -> held asset upload -> publish PATCH is required")

# A successful workflow_run is only a wake-up signal. The dispatcher re-reads
# protected main and never adopts the upstream run's head as an ordinal owner.
for token, label in (
    ('- "QS3D Cloud V25 Preview Build & Release"', "canonical release workflow_run trigger"),
    ("github.event.workflow_run.conclusion == 'success'", "successful workflow_run gate"),
    ("github.event.workflow_run.head_branch == 'main'", "workflow_run main-branch gate"),
):
    require(dispatch, token, label)
wake_block, wake_start, wake_end = slice_block(
    dispatch,
    'current_main="$(gh api "repos/${GITHUB_REPOSITORY}/commits/main"',
    "release_relevant_pathspecs=(",
    "workflow_run current-main source selection",
)
for token, label in (
    ('if [[ "${GITHUB_EVENT_NAME}" == "workflow_run" ]]; then', "workflow_run source branch"),
    ('source_sha="${current_main,,}"', "workflow_run protected-main source"),
):
    require(wake_block, token, label)
forbid(wake_block, "workflow_run.head_sha", "upstream-head source rebinding")

# Prior ownership is immutable. Conflicts/dangling pairs fail closed; a valid
# prior owner exits successfully before any append-only ledger mutation.
owner_block, owner_start, owner_end = slice_block(
    dispatch,
    'if [[ -n "${reservation_owner_source}" || -n "${dispatch_fence_owner_source}" ]]; then',
    "if (( exact_dispatch_fence_run_id > 0 )); then",
    "immutable prior-owner branch",
)
for token, label in (
    ("conflicting exact and prior ownership", "exact/prior ownership conflict rejection"),
    ("incomplete or mismatched prior reservation/fence ownership", "dangling owner/fence rejection"),
    ('git merge-base --is-ancestor "${reservation_owner_source}" "${source_sha}"', "prior-owner ancestry check"),
    ("will not reassign or duplicate-dispatch that ordinal", "immutable no-reassignment decision"),
    ("The protected main ProductVersion must advance before the next automatic preview dispatch.", "fresh ProductVersion requirement"),
    ("exit 0", "immutable prior-owner no-op exit"),
):
    require(owner_block, token, label)
for token in ("gh api --method POST", "gh workflow run release-v25-cloud.yml"):
    forbid(owner_block, token, "durable mutation inside immutable prior-owner branch")

# Retry admission authenticates the exact prior dispatcher run and refuses a
# concurrent replacement. Dispatcher completion is not publication evidence.
retry_block, retry_start, retry_end = slice_block(
    dispatch,
    "if (( exact_dispatch_fence_run_id > 0 )); then",
    'final_main="$(gh api "repos/${GITHUB_REPOSITORY}/commits/main"',
    "prior dispatch-fence retry branch",
)
for token, label in (
    ("actions/runs/${exact_dispatch_fence_run_id}", "prior dispatcher run lookup"),
    ("prior_dispatch_status=", "prior run status"),
    ("prior_dispatch_conclusion=", "prior run conclusion"),
    ("prior_dispatch_path=", "prior run workflow path"),
    ("prior_dispatch_repository=", "prior run repository"),
    ("prior_dispatch_event=", "prior run event"),
    ("prior_dispatch_head_branch=", "prior run branch"),
    ("prior_dispatch_head_sha=", "prior run head SHA"),
    ('.github/workflows/dispatch-v25-cloud-after-main-integration.yml', "canonical dispatcher path"),
    ('"${prior_dispatch_head_branch}" != "main"', "canonical dispatcher main branch"),
    ('case "${prior_dispatch_event}" in', "prior event provenance classification"),
    ('"${prior_dispatch_status}" != "completed"', "concurrent retry suppression"),
    ("completion proves only that the dispatch request attempt ended, not that downstream publication succeeded", "no publication inference from dispatcher completion"),
):
    require(retry_block, token, label)
for token in ("gh api --method POST", "gh workflow run release-v25-cloud.yml"):
    forbid(retry_block, token, "durable mutation inside retry admission")

# Final protected-main admission is immediately before durable side effects and
# uses API -> independent fetch -> API confirmation plus ancestry/drift checks.
final_block, final_start, final_end = slice_block(
    dispatch,
    'final_main="$(gh api "repos/${GITHUB_REPOSITORY}/commits/main"',
    "if (( exact_reservation == 0 )); then",
    "final protected-main admission",
)
for token, label in (
    ("qs3d-final-main-admission", "independent protected-main fetch ref"),
    ("fetched_final_main=", "fetched protected-main identity"),
    ("confirmed_final_main=", "second protected-main API confirmation"),
    ('git merge-base --is-ancestor "${source_sha}" "${final_main}"', "final source ancestry"),
    ('git diff --quiet --no-ext-diff "${source_sha}..${final_main}"', "final release-relevant drift check"),
    ("exits before durable side effects", "superseded final-admission no-op"),
):
    require(final_block, token, label)
for token in ("gh api --method POST", "gh workflow run release-v25-cloud.yml"):
    forbid(final_block, token, "durable mutation before final protected-main admission completes")

reservation_start = dispatch.find("if (( exact_reservation == 0 )); then")
reservation_post = find_after(dispatch, "gh api --method POST", reservation_start, "reservation ledger POST")
fence_assign = find_after(dispatch, 'dispatch_fence="${dispatch_prefix}', reservation_post, "dispatch-fence identity")
fence_post = find_after(dispatch, "gh api --method POST", fence_assign, "dispatch-fence ledger POST")
downstream_dispatch = find_after(dispatch, "gh workflow run release-v25-cloud.yml", fence_post, "downstream release dispatch")

ordered_anchors = [wake_start, owner_start, retry_start, final_start, reservation_post, fence_post, downstream_dispatch]
if any(pos < 0 for pos in ordered_anchors) or ordered_anchors != sorted(ordered_anchors) or len(set(ordered_anchors)) != len(ordered_anchors):
    errors.append("dispatcher executable order must remain wake-up -> immutable owner -> retry provenance -> final main admission -> reservation POST -> fence POST -> downstream dispatch")

for forbidden in (
    "handoff_rebind",
    "V25 preview reservation handoff admitted",
    'source_sha="${reservation_owner_source}"',
    'source_sha="${dispatch_fence_owner_source}"',
):
    forbid(dispatch, forbidden, "preview-ordinal ownership transfer support")

# 10307 is burned. Validate the immutable committed source identity. The V25
# release workflow may intentionally synchronize ProductVersion only in its
# bounded workspace before aggregate preflight; RELEASE_TAG + SOURCE_SHA prove
# that state and make this guard read the exact committed source instead.
try:
    version_project_text = read_version_project_text()
    root = ET.fromstring(version_project_text) if version_project_text else None
    values: dict[str, str] = {}
    if root is not None:
        for name in ("Version", "FileVersion", "InformationalVersion"):
            matches = [node.text.strip() for node in root.iter(name) if node.text and node.text.strip()]
            if len(matches) != 1:
                errors.append(f"V25 project must contain exactly one {name}; found {len(matches)}")
            else:
                values[name] = matches[0]
    version = values.get("Version", "")
    match = re.fullmatch(r"0\.1\.0-preview\.([1-9][0-9]*)", version)
    if not match:
        errors.append(f"V25 Version is not a canonical preview identity: {version!r}")
    else:
        ordinal = int(match.group(1))
        if ordinal <= 10307:
            errors.append(f"V25 preview ordinal must advance beyond burned 10307; found {ordinal}")
        if values.get("FileVersion") != f"0.1.0.{ordinal}":
            errors.append("V25 FileVersion is not bound to the committed preview ordinal")
        if values.get("InformationalVersion") != version:
            errors.append("V25 InformationalVersion is not bound to Version")
except (ET.ParseError, OSError) as exc:
    errors.append(f"could not parse V25 version project: {exc}")

if errors:
    print("ERROR: V25 cloud stale-source recovery preflight failed closed:", file=sys.stderr)
    for error in errors:
        print(f" - {error}", file=sys.stderr)
    raise SystemExit(1)

print("PASS: V25 release stays pinned to exact ancestor source across main advancement; dispatcher preserves immutable ordinal ownership and ordered durable side effects")
