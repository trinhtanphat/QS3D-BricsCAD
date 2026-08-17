from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOWS = ROOT / ".github" / "workflows"
SHA_RE = re.compile(r"^[0-9a-fA-F]{40}$")
USES_KEY_RE = re.compile(r"^\s*-?\s*(?:uses|\"uses\"|'uses')\s*:\s*(.*)$")


def parse_uses_target(line: str):
    match = USES_KEY_RE.match(line)
    if not match:
        return None, None

    raw = match.group(1).strip()
    if not raw:
        return None, "uses value is empty"

    if raw.startswith("'"):
        quoted = re.fullmatch(r"'((?:[^']|'')*)'\s*(?:#.*)?", raw)
        if not quoted:
            return None, "uses value has malformed single-quoted scalar"
        return quoted.group(1).replace("''", "'"), None

    if raw.startswith('"'):
        quoted = re.fullmatch(r'("(?:[^"\\]|\\.)*")\s*(?:#.*)?', raw)
        if not quoted:
            return None, "uses value has malformed double-quoted scalar"
        try:
            return json.loads(quoted.group(1)), None
        except (TypeError, ValueError):
            return None, "uses value has invalid double-quoted escapes"

    value = raw
    comment_index = value.find(" #")
    if comment_index >= 0:
        value = value[:comment_index]
    value = value.strip()
    if not value or any(ch.isspace() for ch in value):
        return None, "uses value must be one plain or quoted scalar"
    return value, None


def scan_workflow_text(label: str, text: str):
    errors: list[str] = []
    if "pull_request_target:" in text or '"pull_request_target":' in text:
        errors.append(f"{label}: pull_request_target is forbidden for repository workflows")
    if "http://" in text:
        errors.append(f"{label}: plaintext HTTP is forbidden in workflow source")

    for line_number, line in enumerate(text.splitlines(), start=1):
        target, parse_error = parse_uses_target(line)
        if target is None and parse_error is None:
            continue
        if parse_error is not None:
            errors.append(f"{label}:{line_number}: {parse_error}")
            continue
        if target.startswith("./"):
            continue
        if "@" not in target:
            errors.append(f"{label}:{line_number}: external action must include an immutable ref")
            continue
        action, ref = target.rsplit("@", 1)
        if not action or not SHA_RE.fullmatch(ref):
            errors.append(
                f"{label}:{line_number}: external action ref must be one full 40-hex commit SHA: {target}"
            )
    return errors


def main():
    workflow_paths = sorted(
        [*WORKFLOWS.glob("*.yml"), *WORKFLOWS.glob("*.yaml")],
        key=lambda path: path.name,
    )

    errors: list[str] = []
    for workflow in workflow_paths:
        text = workflow.read_text(encoding="utf-8")
        errors.extend(scan_workflow_text(str(workflow.relative_to(ROOT)), text))

    if errors:
        print("GitHub Actions supply-chain preflight FAILED:")
        for error in errors:
            print(f" - {error}")
        return 1

    print("PASS: every external workflow action is pinned to a full commit SHA; pull_request_target and plaintext HTTP are absent.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
