#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"


def split_top_level(text: str, delimiter: str = ",") -> list[str]:
    parts: list[str] = []
    start = 0
    depth = 0
    in_string = False
    escaped = False
    for index, ch in enumerate(text):
        if in_string:
            if escaped:
                escaped = False
            elif ch == "\\":
                escaped = True
            elif ch == '"':
                in_string = False
            continue
        if ch == '"':
            in_string = True
            continue
        if ch == "(":
            depth += 1
            continue
        if ch == ")":
            depth -= 1
            continue
        if ch == delimiter and depth == 0:
            parts.append(text[start:index].strip())
            start = index + 1
    parts.append(text[start:].strip())
    return [part for part in parts if part]


def split_concat(text: str) -> list[str]:
    parts: list[str] = []
    start = 0
    depth = 0
    in_string = False
    escaped = False
    for index, ch in enumerate(text):
        if in_string:
            if escaped:
                escaped = False
            elif ch == "\\":
                escaped = True
            elif ch == '"':
                in_string = False
            continue
        if ch == '"':
            in_string = True
            continue
        if ch == "(":
            depth += 1
            continue
        if ch == ")":
            depth -= 1
            continue
        if ch == "+" and depth == 0:
            parts.append(text[start:index].strip())
            start = index + 1
    parts.append(text[start:].strip())
    return [part for part in parts if part]


def decode_csharp_string(token: str) -> str:
    token = token.strip()
    if not (len(token) >= 2 and token[0] == '"' and token[-1] == '"'):
        raise ValueError(f"expected C# string literal, got: {token}")
    return json.loads(token)


def eval_term(term: str) -> str:
    term = term.strip()
    if term.startswith('"'):
        return decode_csharp_string(term)
    if term.startswith("Numeric(") and term.endswith(")"):
        args = split_top_level(term[len("Numeric("):-1])
        names = [decode_csharp_string(arg) for arg in args]
        return ",".join(json.dumps(name) + ':{"type":"number"}' for name in names)
    if term == "ConfirmProperty()":
        return '"confirmMutation":{"type":"boolean"}'
    if term == "ActionIdProperty()":
        return '"actionId":{"type":"string","minLength":1,"maxLength":128,"description":"Stable retry identity used to query or replay mutation acknowledgement."}'
    if term == "CommonLayerConfirm()":
        return ',"layer":{"type":"string","maxLength":255},' + eval_term("ConfirmProperty()")
    raise ValueError(f"unsupported tools/list schema expression: {term}")


def eval_expr(expr: str) -> str:
    return "".join(eval_term(term) for term in split_concat(expr))


def extract_tools(server: str) -> list[str]:
    start_marker = "var tools = new List<string>"
    start = server.find(start_marker)
    if start < 0:
        raise ValueError("tools/list descriptor block not found")
    end = server.find("return ", start)
    if end < 0:
        raise ValueError("tools/list descriptor block end not found")

    descriptors: list[str] = []
    for raw_line in server[start:end].splitlines():
        line = raw_line.strip()
        if not line.startswith('Tool("'):
            continue
        if line.endswith(","):
            line = line[:-1]
        if not (line.startswith("Tool(") and line.endswith(")")):
            raise ValueError(f"malformed Tool(...) descriptor line: {raw_line.strip()}")
        args = split_top_level(line[len("Tool("):-1])
        if len(args) < 3:
            raise ValueError(f"tool descriptor has fewer than three arguments: {raw_line.strip()}")

        name = decode_csharp_string(args[0])
        description = decode_csharp_string(args[1])
        properties = eval_expr(args[2])
        required = [decode_csharp_string(token) for token in args[3:]]
        required_json = "" if not required else ',"required":' + json.dumps(required, separators=(",", ":"))
        descriptor = (
            '{"name":' + json.dumps(name, separators=(",", ":"))
            + ',"description":' + json.dumps(description, separators=(",", ":"))
            + ',"inputSchema":{"type":"object","properties":{' + properties
            + '},"additionalProperties":false' + required_json + "}}"
        )
        descriptors.append(descriptor)
    if not descriptors:
        raise ValueError("no tools/list descriptors were reconstructed")
    return descriptors


def main() -> int:
    if not SERVER.is_file():
        print("ERROR: missing", SERVER.relative_to(ROOT))
        return 1
    server = SERVER.read_text(encoding="utf-8")
    try:
        descriptors = extract_tools(server)
        payload = '{"jsonrpc":"2.0","id":1,"result":{"tools":[' + ",".join(descriptors) + "]}}"
        parsed = json.loads(payload)
    except (ValueError, json.JSONDecodeError) as exc:
        print("ERROR: MCP tools/list generated JSON regression:", exc)
        return 1

    tools = parsed.get("result", {}).get("tools", [])
    transform = next((tool for tool in tools if tool.get("name") == "cad_entity_transform"), None)
    if transform is None:
        print("ERROR: cad_entity_transform missing from reconstructed tools/list")
        return 1
    schema = transform.get("inputSchema", {})
    properties = schema.get("properties", {})
    required = schema.get("required", [])
    if properties.get("confirmMutation") != {"type": "boolean"}:
        print("ERROR: cad_entity_transform confirmMutation schema missing or invalid")
        return 1
    if "confirmMutation" not in required:
        print("ERROR: cad_entity_transform confirmMutation is not required")
        return 1

    status = next((tool for tool in tools if tool.get("name") == "cad_mutation_status"), None)
    if status is None:
        print("ERROR: cad_mutation_status missing from reconstructed tools/list")
        return 1
    status_schema = status.get("inputSchema", {})
    status_properties = status_schema.get("properties", {})
    status_required = status_schema.get("required", [])
    action_id = status_properties.get("actionId")
    if not isinstance(action_id, dict) or action_id.get("type") != "string" or action_id.get("minLength") != 1 or action_id.get("maxLength") != 128:
        print("ERROR: cad_mutation_status actionId schema missing or invalid")
        return 1
    if "actionId" not in status_required:
        print("ERROR: cad_mutation_status actionId is not required")
        return 1

    print(f"PASS MCP tools/list generated JSON ({len(tools)} tools; cad_entity_transform and cad_mutation_status confirmed)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
