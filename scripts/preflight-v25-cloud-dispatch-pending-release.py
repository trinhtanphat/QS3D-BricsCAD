#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
workflow = root / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
text = workflow.read_text(encoding="utf-8")
errors = []

required = (
    'workflow_run:',
    'workflows:',
    '"QS3D Cloud V25 Preview Build & Release"',
    'types:',
    '- completed',
    'branches:',
    '- main',
    "github.event_name == 'workflow_run'",
    "github.event.workflow_run.conclusion == 'success'",
    "github.event.workflow_run.head_branch == 'main'",
    'current_main="$(gh api "repos/${GITHUB_REPOSITORY}/commits/main" --jq \' .sha\')"'.replace("' .sha'", "'.sha'"),
    'source_sha="${current_main,,}"',
    'active_release_statuses=( requested queued in_progress waiting pending )',
    'runs?status=${active_release_status}&per_page=1',
    "active_release_query_status=$?",
    "'.total_count'",
    'active_release_runs=$((active_release_runs + active_release_status_count))',
    'Could not query V25 cloud release runs in status',
)
for token in required:
    if token not in text:
        errors.append(f"missing pending-release wakeup contract: {token}")

for stale in (
    'release-v25-cloud.yml/runs?per_page=30',
    'release-v25-cloud.yml/runs?per_page=100',
):
    if stale in text:
        errors.append(
            "pending-release wakeup must not regress to a fixed-page active-run scan: " + stale
        )

if 'A V25 cloud release is already queued or running; this landing stays pending for the next batch decision.' not in text:
    errors.append("active-release defer behavior changed without preserving explicit pending semantics")

if "github.event.workflow_run.conclusion == 'success'" not in text or "github.event.workflow_run.conclusion != 'success'" in text:
    errors.append("release-completion wakeup must remain success-only to avoid automatic retry loops after failed publishers")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: V25 cloud dispatcher re-evaluates exact current main after a successful main-branch publisher completion, "
    "admits every non-terminal release-run status through bounded total-count queries, and does not auto-retry failed releases"
)
