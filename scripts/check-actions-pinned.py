from __future__ import annotations

import json
import re
import stat
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOWS = ROOT / ".github" / "workflows"
MAX_WORKFLOW_SOURCE_BYTES = 1024 * 1024
SHA_RE = re.compile(r"^[0-9a-fA-F]{40}$")
PLAIN_KEY_RE = re.compile(r"[A-Za-z0-9_.-]+")
TOP_LEVEL_ON_RE = re.compile(r"^(?:on|\"on\"|'on')\s*:\s*(.*)$")
ANCHOR_PREFIX_RE = re.compile(r"^&[^\s,\[\]{}]+(?:\s+|$)")


def _decode_quoted_scalar(raw: str):
    if raw.startswith("'"):
        match = re.fullmatch(r"'((?:[^']|'')*)'", raw)
        if not match:
            return None
        return match.group(1).replace("''", "'")
    if raw.startswith('"'):
        try:
            value = json.loads(raw)
        except (TypeError, ValueError):
            return None
        return value if isinstance(value, str) else None
    return raw


def _strip_yaml_comment(line: str) -> str:
    single = False
    double = False
    escaped = False
    index = 0
    while index < len(line):
        char = line[index]
        if double:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                double = False
            index += 1
            continue
        if single:
            if char == "'" and index + 1 < len(line) and line[index + 1] == "'":
                index += 2
                continue
            if char == "'":
                single = False
            index += 1
            continue
        if char == '"':
            double = True
        elif char == "'":
            single = True
        elif char == "#":
            return line[:index]
        index += 1
    return line


def _strip_yaml_anchor_prefix(raw: str) -> str:
    value = raw.strip()
    match = ANCHOR_PREFIX_RE.match(value)
    if match is None:
        return value
    return value[match.end():].strip()


def _outside_quote_delimiters(line: str):
    positions = [0]
    single = False
    double = False
    escaped = False
    index = 0
    while index < len(line):
        char = line[index]
        if double:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                double = False
            index += 1
            continue
        if single:
            if char == "'" and index + 1 < len(line) and line[index + 1] == "'":
                index += 2
                continue
            if char == "'":
                single = False
            index += 1
            continue
        if char == '"':
            double = True
        elif char == "'":
            single = True
        elif char in "{,":
            positions.append(index + 1)
        index += 1
    return positions


def _parse_mapping_entry(line: str, start: int):
    index = start
    while index < len(line) and line[index].isspace():
        index += 1
    if index < len(line) and line[index] == "-":
        index += 1
        while index < len(line) and line[index].isspace():
            index += 1
    if index >= len(line):
        return None

    key_start = index
    if line[index] in "'\"":
        quote = line[index]
        index += 1
        escaped = False
        while index < len(line):
            char = line[index]
            if quote == '"' and escaped:
                escaped = False
                index += 1
                continue
            if quote == '"' and char == "\\":
                escaped = True
                index += 1
                continue
            if quote == "'" and char == "'" and index + 1 < len(line) and line[index + 1] == "'":
                index += 2
                continue
            if char == quote:
                index += 1
                break
            index += 1
        else:
            return None
    else:
        match = PLAIN_KEY_RE.match(line, index)
        if not match:
            return None
        index = match.end()

    key_raw = line[key_start:index]
    while index < len(line) and line[index].isspace():
        index += 1
    if index >= len(line) or line[index] != ":":
        return None
    key = _decode_quoted_scalar(key_raw)
    if key is None:
        return None

    value_start = index + 1
    index = value_start
    single = False
    double = False
    escaped = False
    square_depth = 0
    curly_depth = 0
    while index < len(line):
        char = line[index]
        if double:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                double = False
            index += 1
            continue
        if single:
            if char == "'" and index + 1 < len(line) and line[index + 1] == "'":
                index += 2
                continue
            if char == "'":
                single = False
            index += 1
            continue
        if char == '"':
            double = True
        elif char == "'":
            single = True
        elif char == "[":
            square_depth += 1
        elif char == "]" and square_depth:
            square_depth -= 1
        elif char == "{":
            curly_depth += 1
        elif char == "}" and curly_depth:
            curly_depth -= 1
        elif char in ",}" and not square_depth and not curly_depth:
            break
        index += 1

    return key, line[value_start:index].strip()


def iter_mapping_entries(line: str):
    code = _strip_yaml_comment(line)
    seen = set()
    for start in _outside_quote_delimiters(code):
        if start in seen:
            continue
        seen.add(start)
        entry = _parse_mapping_entry(code, start)
        if entry is not None:
            yield entry


def _split_flow_scalars(raw: str):
    raw = raw.strip()
    if not (raw.startswith("[") and raw.endswith("]")):
        return None
    body = raw[1:-1]
    values = []
    start = 0
    single = False
    double = False
    escaped = False
    index = 0
    while index < len(body):
        char = body[index]
        if double:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                double = False
            index += 1
            continue
        if single:
            if char == "'" and index + 1 < len(body) and body[index + 1] == "'":
                index += 2
                continue
            if char == "'":
                single = False
            index += 1
            continue
        if char == '"':
            double = True
        elif char == "'":
            single = True
        elif char == ",":
            values.append(body[start:index].strip())
            start = index + 1
        index += 1
    if single or double:
        return None
    values.append(body[start:].strip())

    decoded = []
    for value in values:
        if not value:
            return None
        scalar = _decode_quoted_scalar(value)
        if scalar is None:
            return None
        decoded.append(scalar)
    return decoded


def _find_root_flow_mapping(text: str):
    for line_number, original in enumerate(text.splitlines(), start=1):
        code = _strip_yaml_comment(original).strip().lstrip("\ufeff")
        if not code:
            continue
        if code.startswith("%"):
            continue
        if code.startswith("---") and (len(code) == 3 or code[3].isspace()):
            code = code[3:].strip()
            if not code:
                continue
        code = _strip_yaml_anchor_prefix(code)
        if not code:
            continue
        return line_number if code.startswith("{") else None
    return None


def _find_on_alias(text: str):
    for line_number, original in enumerate(text.splitlines(), start=1):
        code = _strip_yaml_comment(original).rstrip()
        if not code.strip():
            continue
        indent = len(code) - len(code.lstrip(" "))
        if indent != 0:
            continue
        match = TOP_LEVEL_ON_RE.match(code)
        if not match:
            continue
        raw = _strip_yaml_anchor_prefix(match.group(1))
        if raw.startswith("*"):
            return line_number
    return None


def _has_forbidden_pull_request_target(text: str):
    on_block_indent = None
    on_child_indent = None

    for line_number, original in enumerate(text.splitlines(), start=1):
        code = _strip_yaml_comment(original).rstrip()
        if not code.strip():
            continue
        indent = len(code) - len(code.lstrip(" "))

        if on_block_indent is not None:
            if indent <= on_block_indent:
                on_block_indent = None
                on_child_indent = None
            else:
                stripped = code.strip()
                if on_child_indent is None:
                    on_child_indent = indent
                if indent == on_child_indent:
                    if stripped.startswith("-"):
                        event_raw = stripped[1:].strip()
                        event = _decode_quoted_scalar(event_raw)
                        if event == "pull_request_target":
                            return line_number
                    entry = _parse_mapping_entry(code, indent)
                    if entry is not None and entry[0] == "pull_request_target":
                        return line_number

        if indent != 0:
            continue
        match = TOP_LEVEL_ON_RE.match(code)
        if not match:
            continue
        raw = _strip_yaml_anchor_prefix(match.group(1))
        if raw.startswith("*"):
            continue
        if not raw:
            on_block_indent = 0
            on_child_indent = None
            continue
        if raw.startswith("["):
            events = _split_flow_scalars(raw)
            if events is not None and "pull_request_target" in events:
                return line_number
            continue
        if raw.startswith("{"):
            for key, _ in iter_mapping_entries(raw):
                if key == "pull_request_target":
                    return line_number
            continue
        event = _decode_quoted_scalar(raw)
        if event == "pull_request_target":
            return line_number

    return None


def parse_uses_target(raw: str):
    raw = _strip_yaml_anchor_prefix(raw)
    if not raw:
        return None, "uses value is empty"
    if raw.startswith("*"):
        return None, "uses alias cannot be safety-checked; expand the action reference explicitly"

    if raw.startswith("'"):
        quoted = re.fullmatch(r"'((?:[^']|'')*)'", raw)
        if not quoted:
            return None, "uses value has malformed single-quoted scalar"
        return quoted.group(1).replace("''", "'"), None

    if raw.startswith('"'):
        if not re.fullmatch(r'"(?:[^"\\]|\\.)*"', raw):
            return None, "uses value has malformed double-quoted scalar"
        try:
            value = json.loads(raw)
        except (TypeError, ValueError):
            return None, "uses value has invalid double-quoted escapes"
        if not isinstance(value, str):
            return None, "uses value must be a string scalar"
        return value, None

    if any(ch.isspace() for ch in raw):
        return None, "uses value must be one plain or quoted scalar"
    return raw, None


def scan_workflow_text(label: str, text: str):
    errors: list[str] = []

    root_flow_line = _find_root_flow_mapping(text)
    if root_flow_line is not None:
        errors.append(
            f"{label}:{root_flow_line}: root flow-style workflow mapping cannot be safety-checked; "
            "use a block-style top-level workflow mapping"
        )

    on_alias_line = _find_on_alias(text)
    if on_alias_line is not None:
        errors.append(
            f"{label}:{on_alias_line}: on alias cannot be safety-checked; expand workflow triggers explicitly"
        )
    forbidden_line = _has_forbidden_pull_request_target(text)
    if forbidden_line is not None:
        errors.append(
            f"{label}:{forbidden_line}: pull_request_target is forbidden for repository workflows"
        )
    if "http://" in text:
        errors.append(f"{label}: plaintext HTTP is forbidden in workflow source")

    for line_number, line in enumerate(text.splitlines(), start=1):
        for key, raw_value in iter_mapping_entries(line):
            if key != "uses":
                continue

            target, parse_error = parse_uses_target(raw_value)
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


def discover_workflow_paths(workflows_dir: Path):
    errors: list[str] = []
    try:
        entries = list(workflows_dir.iterdir())
    except OSError as exc:
        return [], [f"{workflows_dir}: cannot enumerate workflows: {exc}"]

    candidates = [path for path in entries if path.suffix in {".yml", ".yaml"}]
    candidates.sort(key=lambda path: (path.name.casefold(), path.name))

    try:
        workflow_root = workflows_dir.resolve(strict=True)
    except OSError as exc:
        return [], [f"{workflows_dir}: cannot resolve workflow directory: {exc}"]

    seen_names: dict[str, str] = {}
    validated: list[Path] = []
    for candidate in candidates:
        collision_key = candidate.name.casefold()
        previous = seen_names.get(collision_key)
        if previous is not None and previous != candidate.name:
            errors.append(
                f"{candidate}: case-insensitive workflow filename collision with {previous}"
            )
        else:
            seen_names[collision_key] = candidate.name

        try:
            metadata = candidate.lstat()
        except OSError as exc:
            errors.append(f"{candidate}: cannot inspect workflow candidate: {exc}")
            continue
        if stat.S_ISLNK(metadata.st_mode):
            errors.append(f"{candidate}: workflow candidate must not be a symlink")
            continue
        if not stat.S_ISREG(metadata.st_mode):
            errors.append(f"{candidate}: workflow candidate must be a regular file")
            continue
        if metadata.st_size > MAX_WORKFLOW_SOURCE_BYTES:
            errors.append(
                f"{candidate}: workflow source exceeds {MAX_WORKFLOW_SOURCE_BYTES} bytes"
            )
            continue

        try:
            resolved = candidate.resolve(strict=True)
            resolved.relative_to(workflow_root)
        except (OSError, ValueError) as exc:
            errors.append(f"{candidate}: workflow candidate escapes workflow directory: {exc}")
            continue
        validated.append(candidate)

    if errors:
        return [], errors
    return validated, []


def read_workflow_source(workflow: Path):
    try:
        with workflow.open("rb") as stream:
            raw = stream.read(MAX_WORKFLOW_SOURCE_BYTES + 1)
    except OSError as exc:
        return None, f"{workflow}: cannot read workflow source: {exc}"
    if len(raw) > MAX_WORKFLOW_SOURCE_BYTES:
        return None, f"{workflow}: workflow source exceeds {MAX_WORKFLOW_SOURCE_BYTES} bytes"
    try:
        return raw.decode("utf-8"), None
    except UnicodeDecodeError as exc:
        return None, f"{workflow}: workflow source is not valid UTF-8: {exc}"


def main():
    workflow_paths, errors = discover_workflow_paths(WORKFLOWS)
    if not errors:
        for workflow in workflow_paths:
            text, read_error = read_workflow_source(workflow)
            if read_error is not None:
                errors.append(read_error)
                continue
            errors.extend(scan_workflow_text(str(workflow.relative_to(ROOT)), text))

    if errors:
        print("GitHub Actions supply-chain preflight FAILED:")
        for error in errors:
            print(f" - {error}")
        return 1

    print(
        "PASS: workflow discovery is deterministic and bounded; every external workflow action is pinned "
        "to a full commit SHA; pull_request_target, root flow-style workflow mappings, and plaintext HTTP are absent."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
