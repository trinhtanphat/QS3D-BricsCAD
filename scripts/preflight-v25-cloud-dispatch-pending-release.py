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
    "github.event_name == 'workflow_run'",
    "github.event.workflow_run.conclusion == 'success'",
    "github.event.workflow_run.head_branch == 'main'",
    'current_main="$(gh api "repos/${GITHUB_REPOSITORY}/commits/main" --jq \'.sha\')"',
    'source_sha="${current_main,,}"',
    'active_release_runs',
    'release-v25-cloud.yml/runs?per_page=30',
)
for token in required:
    if token not in text:
        errors.append(f"missing pending-release wakeup contract: {token}")

if 'A V25 cloud release is already queued or running; this landing stays pending for the next batch decision.' not in text:
    errors.append("active-release defer behavior changed without preserving explicit pending semantics")

if "github.event.workflow_run.conclusion == 'success'" not in text or "github.event.workflow_run.conclusion != 'success'" in text:
    errors.append("release-completion wakeup must remain success-only to avoid automatic retry loops after failed publishers")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: V25 cloud dispatcher re-evaluates exact current main after a successful publisher completion without auto-retrying failed releases")
