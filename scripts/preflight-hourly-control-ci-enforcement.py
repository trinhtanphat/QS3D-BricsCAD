#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"
WATCHED_DOC = "docs/HOURLY-AGENT-CONTROL.md"
errors = []

if not WORKFLOW.is_file():
    errors.append("missing .github/workflows/ci.yml")
else:
    text = WORKFLOW.read_text(encoding="utf-8")
    lines = text.splitlines()

    push_start = next((i for i, line in enumerate(lines) if re.match(r'^\s{2}["\']?push["\']?\s*:', line)), None)
    pull_request_start = next((i for i, line in enumerate(lines) if re.match(r'^\s{2}["\']?pull_request["\']?\s*:', line)), None)
    if push_start is None or pull_request_start is None or pull_request_start <= push_start:
        errors.append("could not resolve shared-CI push trigger block")
    else:
        push_block = "\n".join(lines[push_start:pull_request_start])
        if f'"{WATCHED_DOC}"' not in push_block and f"'{WATCHED_DOC}'" not in push_block:
            errors.append(f"shared branch-push CI does not watch {WATCHED_DOC}")

    scope_marker = "Classify validation scope"
    scope_index = next((i for i, line in enumerate(lines) if scope_marker in line), None)
    generic_guard_index = next((i for i, line in enumerate(lines) if "Generic source guard" in line), None)
    if scope_index is None or generic_guard_index is None or generic_guard_index <= scope_index:
        errors.append("could not resolve validation-scope classification block")
    else:
        scope_block = "\n".join(lines[scope_index:generic_guard_index])
        if WATCHED_DOC not in scope_block:
            errors.append(f"validation scope does not classify {WATCHED_DOC} for source guards")

    if "python scripts/preflight-all.py" not in text:
        errors.append("shared CI no longer executes aggregate discovered feature preflights")
    if "steps.scope.outputs.source_validation == 'true'" not in text:
        errors.append("shared CI no longer gates source guards on classified source_validation")

if errors:
    print("Hourly-control CI enforcement preflight FAILED:")
    for error in errors:
        print(" -", error)
    raise SystemExit(1)

print("PASS: hourly-control policy edits trigger branch CI and source-guard discovery.")
