from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOWS = ROOT / ".github" / "workflows"
SHA_RE = re.compile(r"^[0-9a-fA-F]{40}$")
USES_RE = re.compile(r"^\s*-?\s*uses:\s*([^\s#]+)")

workflow_paths = sorted(
    [*WORKFLOWS.glob("*.yml"), *WORKFLOWS.glob("*.yaml")],
    key=lambda path: path.name,
)

errors: list[str] = []
for workflow in workflow_paths:
    text = workflow.read_text(encoding="utf-8")
    if "pull_request_target:" in text or '"pull_request_target":' in text:
        errors.append(f"{workflow.relative_to(ROOT)}: pull_request_target is forbidden for repository workflows")
    if "http://" in text:
        errors.append(f"{workflow.relative_to(ROOT)}: plaintext HTTP is forbidden in workflow source")

    for line_number, line in enumerate(text.splitlines(), start=1):
        match = USES_RE.match(line)
        if not match:
            continue
        target = match.group(1)
        if target.startswith("./"):
            continue
        if "@" not in target:
            errors.append(f"{workflow.relative_to(ROOT)}:{line_number}: external action must include an immutable ref")
            continue
        action, ref = target.rsplit("@", 1)
        if not action or not SHA_RE.fullmatch(ref):
            errors.append(
                f"{workflow.relative_to(ROOT)}:{line_number}: external action ref must be one full 40-hex commit SHA: {target}"
            )

if errors:
    print("GitHub Actions supply-chain preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    raise SystemExit(1)

print("PASS: every external workflow action is pinned to a full commit SHA; pull_request_target and plaintext HTTP are absent.")
