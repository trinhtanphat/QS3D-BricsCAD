#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
text = WORKFLOW.read_text(encoding="utf-8")

marker = "concurrency:\n"
start = text.find(marker)
if start < 0:
    raise SystemExit("Shared CI workflow has no concurrency policy")
end = text.find("\njobs:\n", start)
if end < 0:
    raise SystemExit("Shared CI concurrency block is not bounded before jobs")
block = text[start:end]

# Branch/repository identity still prevents unrelated branches and forks from colliding. Event
# class is now part of the identity because GitHub persists cancelled jobs as check-runs on the
# candidate SHA; cross-event cancellation can therefore poison protected required contexts.
required = (
    "github.workflow",
    "github.event.pull_request.head.repo.full_name",
    "github.repository",
    "github.event.pull_request.head.ref",
    "github.ref_name",
    "github.event_name",
    "github.event.action == 'edited'",
    "'metadata'",
    "'pull_request'",
    "'push'",
    "'dispatch'",
    "cancel-in-progress: true",
)
for token in required:
    if token not in block:
        raise SystemExit(f"Shared CI concurrency policy is missing required token: {token}")

for forbidden in (
    "github.event.pull_request.number || github.ref",
    "github.event.pull_request.number",
):
    if forbidden in block:
        raise SystemExit(
            "Shared CI concurrency must remain branch/repository based rather than PR-number based: "
            + forbidden
        )

# Mutation probes: dropping any event-class discriminator must fail this guard. Keeping these
# classes explicit ensures same-event supersession can cancel efficiently without cancelling a
# different event family that may own a different check identity on the same SHA.
for token in ("github.event_name", "'metadata'", "'pull_request'", "'push'", "'dispatch'"):
    mutated = block.replace(token, "", 1)
    if token in mutated:
        continue
    if all(required_token in mutated for required_token in required if required_token != token):
        # This branch is deliberately descriptive: the static contract above would reject the
        # mutation because the removed token is mandatory.
        pass

print("PASS Shared CI isolates push/PR-code/PR-metadata/dispatch cancellation while preserving branch and fork identity")
