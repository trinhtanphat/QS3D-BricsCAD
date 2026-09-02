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
    'source_sha="$(gh api "repos/${GITHUB_REPOSITORY}/commits/main" --jq \'.sha\')"',
    'active_release_runs',
    'release-v25-cloud.yml/runs?per_page=30',
)
for token in required:
    if token not in text:
        errors.append(f"missing pending-release wakeup contract: {token}")

if 'A V25 cloud release is already queued or running; this landing stays pending for the next batch decision.' not in text:
    errors.append("active-release defer behavior changed without preserving explicit pending semantics")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: V25 cloud dispatcher re-evaluates current main after publisher completion and cannot silently lose a deferred release decision")
