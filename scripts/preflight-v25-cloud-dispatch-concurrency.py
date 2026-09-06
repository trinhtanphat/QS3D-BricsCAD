#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
workflow_path = root / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
workflow = workflow_path.read_text(encoding="utf-8")


def validate(text: str) -> list[str]:
    errors: list[str] = []
    group = text.count("group: qs3d-v25-cloud-main-integration")
    cancel_false = text.count("cancel-in-progress: false")
    queue_max = text.count("queue: max")

    reservation = text.find('reservation="${reservation_prefix} ordinal=${committed_preview_ordinal} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"')
    reservation_post = text.find('gh api --method POST', reservation)
    fence = text.find('dispatch_fence="${dispatch_prefix} ordinal=${committed_preview_ordinal} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"')
    fence_post = text.find('gh api --method POST', fence)
    dispatch = text.find("gh workflow run release-v25-cloud.yml")

    if group != 1:
        errors.append("dispatcher requires exactly one stable V25 cloud concurrency group")
    if cancel_false != 1:
        errors.append("dispatcher transaction must not cancel an in-progress owner")
    if queue_max != 1:
        errors.append("dispatcher must retain pending integration intents with queue: max")
    if min(reservation, reservation_post, fence, fence_post, dispatch) < 0:
        errors.append("dispatcher durable reservation/fence/downstream transaction is incomplete")
    elif not (reservation < reservation_post < fence < fence_post < dispatch):
        errors.append("dispatcher durable side effects are not in expected reservation/fence/dispatch order")
    return errors


errors = validate(workflow)
if errors:
    raise SystemExit("V25 cloud dispatcher concurrency admission failed: " + "; ".join(errors))

mutations = {
    "active preemption": workflow.replace("cancel-in-progress: false", "cancel-in-progress: true", 1),
    "pending queue removal": workflow.replace("  queue: max\n", "", 1),
    "single pending replacement": workflow.replace("queue: max", "queue: single", 1),
}
for name, mutated in mutations.items():
    if mutated == workflow:
        raise SystemExit(f"V25 cloud dispatcher concurrency mutation fixture could not apply: {name}")
    if not validate(mutated):
        raise SystemExit(f"V25 cloud dispatcher concurrency mutation probe did not fail closed: {name}")

print("PASS V25 cloud dispatcher preserves active transaction ownership and all pending dispatch intents")
