#!/usr/bin/env python3
from pathlib import Path
import re
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


def require_absent(source: str, token: str, label: str) -> None:
    if token in source:
        errors.append(f"forbidden {label}: {token}")


def require_before(source: str, first: str, second: str, label: str) -> None:
    first_pos = source.find(first)
    second_pos = source.find(second)
    if first_pos < 0 or second_pos < 0 or first_pos >= second_pos:
        errors.append(f"invalid ordering for {label}: {first!r} must precede {second!r}")


def block_between(source: str, start: str, end: str, label: str) -> tuple[str, int, int]:
    start_pos = source.find(start)
    end_pos = source.find(end, start_pos + len(start)) if start_pos >= 0 else -1
    if start_pos < 0 or end_pos < 0 or start_pos >= end_pos:
        errors.append(f"could not isolate {label}")
        return "", -1, -1
    return source[start_pos:end_pos], start_pos, end_pos


def find_after(source: str, token: str, after: int, label: str) -> int:
    pos = source.find(token, max(after, 0))
    if pos < 0:
        errors.append(f"missing {label} after guarded boundary: {token}")
    return pos


# Release stale-source branch: a superseded SOURCE_SHA must complete successfully
# before *any* durable GitHub release mutation. Mutation tokens are anchored to
# executable PowerShell calls, not generic variable literals or comments.
stale_block, stale_start, stale_end = block_between(
    release,
    "if ($preMutationReleaseDriftStatus -eq 1) {",
    "if ($preMutationReleaseDriftStatus -ne 0) {",
    "pre-mutation release-relevant drift branch",
)

stale_marker = "V25_RELEASE_SUPERSEDED source_sha="
require(stale_block, stale_marker, "superseded release audit marker")
require(stale_block, "::notice title=V25 release source superseded::", "superseded release notice")
require(stale_block, "successful no-op before the first persistent release mutation", "stale no-op summary")
require(stale_block, "preview ordinal ownership is not reassigned", "immutable-ordinal stale summary")
require(stale_block, "exit 0", "successful stale-release no-op exit")
require_before(stale_block, stale_marker, "exit 0", "superseded audit before successful exit")
require_absent(stale_block, "throw ", "throw inside successful stale no-op")

release_mutations = (
    '$release = Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases"',
    ".\\scripts\\upload-v25-held-release-asset.ps1 `",
    "$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri",
)
mutation_positions: list[int] = []
for mutation in release_mutations:
    pos = release.find(mutation)
    mutation_positions.append(pos)
    if pos < 0:
        errors.append(f"missing guarded release mutation: {mutation}")
    elif stale_end >= 0 and pos <= stale_end:
        errors.append(f"stale no-op does not precede release mutation: {mutation}")
    if mutation in stale_block:
        errors.append(f"stale no-op unexpectedly contains persistent release mutation: {mutation}")

if all(pos >= 0 for pos in mutation_positions):
    if mutation_positions != sorted(mutation_positions):
        errors.append("release mutation order changed: draft create -> held-asset upload -> publish PATCH is required")

# Canonical wake-up semantics: workflow_run success on main is only a signal to
# re-evaluate protected main. It must not transfer a prior preview ordinal.
for token, label in (
    ('- "QS3D Cloud V25 Preview Build & Release"', "canonical release workflow_run trigger"),
    ("github.event.workflow_run.conclusion == 'success'", "successful workflow_run admission"),
    ("github.event.workflow_run.head_branch == 'main'", "workflow_run main-branch admission"),
    ('current_main="$(gh api "repos/${GITHUB_REPOSITORY}/commits/main" --jq \'.sha\')"', "protected-main API source"),
):
    require(dispatch, token, label)

workflow_run_block, _, workflow_run_end = block_between(
    dispatch,
    'if [[ "${GITHUB_EVENT_NAME}" == "workflow_run" ]]; then',
    "release_relevant_pathspecs=(",
    "workflow_run current-main source selection",
)
require(workflow_run_block, 'source_sha="${current_main,,}"', "workflow_run current-main source selection")
require_absent(workflow_run_block, "workflow_run.head_sha", "upstream head rebinding in workflow_run source selection")

# Immutable ownership must be decided in the executable prior-owner branch and
# must terminate before any append-only ledger mutation or downstream dispatch.
for token, label in (
    ("reservation_owner_source=", "reservation-owner tracking"),
    ("dispatch_fence_owner_source=", "dispatch-fence-owner tracking"),
    ("reservation_owner_conflict=", "reservation-owner conflict tracking"),
    ("dispatch_fence_owner_conflict=", "dispatch-fence-owner conflict tracking"),
):
    require(dispatch, token, label)

owner_block, owner_start, owner_end = block_between(
    dispatch,
    'if [[ -n "${reservation_owner_source}" || -n "${dispatch_fence_owner_source}" ]]; then',
    "if (( exact_dispatch_fence_run_id > 0 )); then",
    "immutable prior-owner decision block",
)
for token, label in (
    ("conflicting exact and prior ownership", "exact/prior ownership conflict rejection"),
    ("incomplete or mismatched prior reservation/fence ownership", "dangling ownership rejection"),
    ('git merge-base --is-ancestor "${reservation_owner_source}" "${source_sha}"', "prior-owner ancestry check"),
    ("will not reassign or duplicate-dispatch that ordinal", "immutable no-reassignment decision"),
    ("The protected main ProductVersion must advance before the next automatic preview dispatch.", "fresh ProductVersion requirement"),
    ("exit 0", "prior-owner no-op exit"),
):
    require(owner_block, token, label)
for mutation in ("gh api --method POST", "gh workflow run release-v25-cloud.yml"):
    require_absent(owner_block, mutation, "durable mutation inside immutable prior-owner block")

# Retry admission authenticates the prior dispatcher attempt using actual run
# metadata and refuses concurrent replacement. Completion alone is explicitly
# not treated as publication evidence.
retry_block, retry_start, retry_end = block_between(
    dispatch,
    "if (( exact_dispatch_fence_run_id > 0 )); then",
    'final_main="$(gh api "repos/${GITHUB_REPOSITORY}/commits/main" --jq \'.sha\')"',
    "prior dispatch-fence retry admission block",
)
for token, label in (
    ('prior_dispatch_run_json="$(gh api "repos/${GITHUB_REPOSITORY}/actions/runs/${exact_dispatch_fence_run_id}")"', "prior run lookup"),
    ("prior_dispatch_status=", "prior run status extraction"),
    ("prior_dispatch_conclusion=", "prior run conclusion extraction"),
    ("prior_dispatch_path=", "prior run workflow-path extraction"),
    ("prior_dispatch_repository=", "prior run repository extraction"),
    ("prior_dispatch_event=", "prior run event extraction"),
    ("prior_dispatch_head_branch=", "prior run branch extraction"),
    ("prior_dispatch_head_sha=", "prior run head-SHA extraction"),
    ('.github/workflows/dispatch-v25-cloud-after-main-integration.yml', "canonical dispatcher path check"),
    ('"${prior_dispatch_head_branch}" != "main"', "canonical dispatcher branch check"),
    ('case "${prior_dispatch_event}" in', "prior event provenance classification"),
    ('"${prior_dispatch_status}" != "completed"', "concurrent retry suppression"),
    ("completion proves only that the dispatch request attempt ended, not that downstream publication succeeded", "no publication inference from dispatcher completion"),
):
    require(retry_block, token, label)
for mutation in ("gh api --method POST", "gh workflow run release-v25-cloud.yml"):
    require_absent(retry_block, mutation, "durable mutation inside retry provenance block")

# Immediately before the first durable side effect, protected main is read via
# API, fetched independently, read again, and release-relevant drift is checked.
final_block, final_start, final_end = block_between(
    dispatch,
    'final_main="$(gh api "repos/${GITHUB_REPOSITORY}/commits/main" --jq \'.sha\')"',
    "if (( exact_reservation == 0 )); then",
    "final protected-main admission block",
)
for token, label in (
    ("qs3d-final-main-admission", "independent final-main fetch ref"),
    ("fetched_final_main=", "fetched final-main identity"),
    ("confirmed_final_main=", "second protected-main API confirmation"),
    ('git merge-base --is-ancestor "${source_sha}" "${final_main}"', "final source ancestry check"),
    ('git diff --quiet --no-ext-diff "${source_sha}..${final_main}"', "final release-relevant drift check"),
    ("exits before durable side effects", "final drift no-op marker"),
):
    require(final_block, token, label)
for mutation in ("gh api --method POST", "gh workflow run release-v25-cloud.yml"):
    require_absent(final_block, mutation, "durable mutation before final protected-main admission completes")

reservation_start = dispatch.find("if (( exact_reservation == 0 )); then")
reservation_post = find_after(dispatch, "gh api --method POST", reservation_start, "reservation ledger POST")
fence_assign = find_after(dispatch, 'dispatch_fence="${dispatch_prefix}', reservation_post, "dispatch-fence identity")
fence_post = find_after(dispatch, "gh api --method POST", fence_assign, "dispatch-fence ledger POST")
downstream_dispatch = find_after(dispatch, "gh workflow run release-v25-cloud.yml", fence_post, "downstream release dispatch")

ordered_boundaries = [
    (workflow_run_end, owner_start, "workflow_run source selection before ownership decision"),
    (owner_end, retry_start, "immutable ownership decision before retry admission"),
    (retry_end, final_start, "retry admission before final protected-main admission"),
    (final_end, reservation_post, "final protected-main admission before reservation mutation"),
    (reservation_post, fence_post, "reservation mutation before dispatch-fence mutation"),
    (fence_post, downstream_dispatch, "dispatch fence before downstream workflow dispatch"),
]
for left, right, label in ordered_boundaries:
    if left < 0 or right < 0 or left >= right:
        errors.append(f"invalid executable ordering for {label}")

for forbidden in (
    "handoff_rebind",
    "V25 preview reservation handoff admitted",
    'source_sha="${reservation_owner_source}"',
    'source_sha="${dispatch_fence_owner_source}"',
):
    require_absent(dispatch, forbidden, "preview-ordinal ownership transfer support")

# 10307 is a burned historical identity. The incident fix must commit a strictly
# newer canonical identity, and all product/assembly version surfaces stay bound
# to the same ordinal. Future monotonic bumps remain valid.
try:
    root = ET.parse(VERSION_PROJECT).getroot()
    values: dict[str, str] = {}
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

print("PASS: V25 stale release no-ops before durable release mutation; dispatcher keeps immutable ownership, authenticated retry, final-main admission, and ordered durable side effects")
