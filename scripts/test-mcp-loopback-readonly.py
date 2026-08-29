#!/usr/bin/env python3
"""Sanitized read-only loopback qualification for the embedded QS3D MCP server.

This is an engineering/local-agent probe, not an end-user setup path. End users should use
TOOL > MCP (AI) > Agent Center. The probe never prints the bearer token or raw CAD payloads and
never calls a mutating MCP tool.
"""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import sys
import urllib.error
import urllib.parse
import urllib.request
from typing import Any

PROTOCOL = "2025-06-18"
INVALID_PROTOCOL = "2024-11-05"
DEFAULT_ENDPOINT = "http://127.0.0.1:8765/mcp"
REQUIRED_TOOLS = {
    "connector_info",
    "qs3d_status",
    "cad_active_document",
    "cad_selection",
    "cad_database_snapshot",
    "cad_entity_inspect",
    "cad_view_state",
    "cad_wait_idle",
    "cad_create_line",
    "cad_create_circle",
    "cad_create_polyline",
    "cad_create_text",
    "cad_entity_transform",
    "cad_entity_delete",
    "cad_layer",
    "cad_command_catalog",
    "cad_command_sequence",
    "qs3d_run_command",
    "cad_ui_click",
    "cad_ui_type",
    "cad_ui_key",
    "cad_agent_stop",
    "cad_agent_resume",
    "cad_audit_tail",
    "cad_cancel_command",
}
READ_ONLY_TOOLS = (
    ("connector_info", {}),
    ("qs3d_status", {}),
    ("cad_active_document", {}),
    ("cad_selection", {}),
    ("cad_database_snapshot", {"limit": 5}),
    ("cad_view_state", {}),
    ("cad_wait_idle", {"timeoutMs": 2000}),
    ("cad_audit_tail", {"limit": 1}),
)


class ProbeError(RuntimeError):
    pass


def token_path_default() -> Path:
    appdata = os.environ.get("APPDATA", "").strip()
    if not appdata:
        raise ProbeError("APPDATA is unavailable; pass --token-file explicitly.")
    return Path(appdata) / "QS3D" / "mcp-bearer-token.txt"


def endpoint_parts(endpoint: str) -> tuple[str, str]:
    parsed = urllib.parse.urlparse(endpoint)
    if parsed.scheme != "http" or parsed.hostname not in {"127.0.0.1", "localhost"}:
        raise ProbeError("This local qualification probe accepts loopback HTTP endpoints only.")
    if parsed.path.rstrip("/") != "/mcp":
        raise ProbeError("Endpoint must end in /mcp.")
    base = urllib.parse.urlunparse((parsed.scheme, parsed.netloc, "", "", "", ""))
    return endpoint, base + "/healthz"


def request(
    url: str,
    method: str,
    body: dict[str, Any] | None,
    timeout: float,
    token: str | None = None,
    session: str | None = None,
    protocol_version: str = PROTOCOL,
    origin: str | None = None,
) -> tuple[int, dict[str, str], bytes]:
    payload = b"" if body is None else json.dumps(body, separators=(",", ":")).encode("utf-8")
    headers = {"Accept": "application/json, text/event-stream"}
    if body is not None:
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token
    if session:
        headers["Mcp-Session-Id"] = session
        headers["MCP-Protocol-Version"] = protocol_version
    if origin is not None:
        headers["Origin"] = origin
    req = urllib.request.Request(url, data=payload if body is not None else None, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=timeout) as response:
            return response.status, {k.lower(): v for k, v in response.headers.items()}, response.read()
    except urllib.error.HTTPError as exc:
        return exc.code, {k.lower(): v for k, v in exc.headers.items()}, exc.read()


def parse_json(raw: bytes, context: str) -> dict[str, Any]:
    try:
        value = json.loads(raw.decode("utf-8"))
    except Exception as exc:  # noqa: BLE001 - qualification needs one sanitized failure boundary.
        raise ProbeError(f"{context} returned invalid JSON.") from exc
    if not isinstance(value, dict):
        raise ProbeError(f"{context} did not return a JSON object.")
    return value


def tool_result_text(envelope: dict[str, Any], context: str) -> dict[str, Any]:
    if "error" in envelope:
        raise ProbeError(f"{context} returned JSON-RPC error.")
    result = envelope.get("result")
    if not isinstance(result, dict) or result.get("isError") is True:
        raise ProbeError(f"{context} returned an MCP tool error.")
    content = result.get("content")
    if not isinstance(content, list) or not content:
        raise ProbeError(f"{context} returned no MCP content.")
    first = content[0]
    if not isinstance(first, dict) or first.get("type") != "text" or not isinstance(first.get("text"), str):
        raise ProbeError(f"{context} returned an unexpected MCP content shape.")
    try:
        parsed = json.loads(first["text"])
    except Exception as exc:  # noqa: BLE001
        raise ProbeError(f"{context} text content was not JSON.") from exc
    if not isinstance(parsed, (dict, list)):
        raise ProbeError(f"{context} text content was not a JSON object/array.")
    return {"kind": "array", "count": len(parsed)} if isinstance(parsed, list) else parsed


def rpc_post(
    endpoint: str,
    token: str,
    session: str | None,
    request_id: int | None,
    method: str,
    params: dict[str, Any],
    timeout: float,
    protocol_version: str = PROTOCOL,
) -> tuple[int, dict[str, str], dict[str, Any] | None]:
    body: dict[str, Any] = {"jsonrpc": "2.0", "method": method, "params": params}
    if request_id is not None:
        body["id"] = request_id
    status, headers, raw = request(
        endpoint,
        "POST",
        body,
        timeout,
        token=token,
        session=session,
        protocol_version=protocol_version,
    )
    if not raw:
        return status, headers, None
    return status, headers, parse_json(raw, method)


def call_tool(endpoint: str, token: str, session: str, request_id: int, name: str, arguments: dict[str, Any], timeout: float) -> dict[str, Any]:
    status, _, envelope = rpc_post(
        endpoint,
        token,
        session,
        request_id,
        "tools/call",
        {"name": name, "arguments": arguments},
        timeout,
    )
    if status != 200 or envelope is None:
        raise ProbeError(f"tools/call {name} failed at HTTP boundary.")
    return tool_result_text(envelope, name)


def main() -> int:
    parser = argparse.ArgumentParser(description="Read-only exact-host probe for QS3D embedded MCP.")
    parser.add_argument("--endpoint", default=DEFAULT_ENDPOINT)
    parser.add_argument("--token-file")
    parser.add_argument("--timeout", type=float, default=5.0)
    args = parser.parse_args()

    try:
        endpoint, health = endpoint_parts(args.endpoint)
        token_file = Path(args.token_file) if args.token_file else token_path_default()
        token = token_file.read_text(encoding="utf-8").strip()
        if len(token) < 16:
            raise ProbeError("Bearer token file is missing or invalid.")

        hostile_origin_status, _, _ = request(
            health,
            "GET",
            None,
            args.timeout,
            origin="https://example.invalid",
        )
        if hostile_origin_status != 403:
            raise ProbeError("non-loopback MCP Origin was not rejected with HTTP 403.")

        null_origin_status, _, _ = request(
            health,
            "GET",
            None,
            args.timeout,
            origin="null",
        )
        if null_origin_status != 403:
            raise ProbeError("opaque/null MCP Origin was not rejected with HTTP 403.")

        loopback_origin_status, _, loopback_origin_raw = request(
            health,
            "GET",
            None,
            args.timeout,
            origin="http://127.0.0.1",
        )
        loopback_origin_json = parse_json(loopback_origin_raw, "healthz loopback Origin")
        if loopback_origin_status != 200 or loopback_origin_json.get("ok") is not True:
            raise ProbeError("loopback MCP Origin was not accepted.")

        health_status, _, health_raw = request(health, "GET", None, args.timeout)
        health_json = parse_json(health_raw, "healthz")
        if health_status != 200 or health_json.get("ok") is not True or health_json.get("running") is not True:
            raise ProbeError("healthz did not report a running MCP service.")

        unauthorized_body = {
            "jsonrpc": "2.0",
            "id": 1,
            "method": "initialize",
            "params": {
                "protocolVersion": PROTOCOL,
                "capabilities": {},
                "clientInfo": {"name": "qs3d-local-readonly-probe", "version": "1"},
            },
        }
        unauthorized_status, _, _ = request(endpoint, "POST", unauthorized_body, args.timeout)
        if unauthorized_status != 401:
            raise ProbeError("unauthorized initialize was not rejected with HTTP 401.")

        status, headers, initialized = rpc_post(
            endpoint,
            token,
            None,
            2,
            "initialize",
            {
                "protocolVersion": PROTOCOL,
                "capabilities": {},
                "clientInfo": {"name": "qs3d-local-readonly-probe", "version": "1"},
            },
            args.timeout,
        )
        if status != 200 or initialized is None or "error" in initialized:
            raise ProbeError("authorized initialize failed.")
        result = initialized.get("result")
        if not isinstance(result, dict) or result.get("protocolVersion") != PROTOCOL:
            raise ProbeError("initialize returned an unexpected MCP protocol version.")
        server_info = result.get("serverInfo")
        if not isinstance(server_info, dict) or server_info.get("name") != "qs3d-bricscad":
            raise ProbeError("initialize returned unexpected serverInfo.")
        session = headers.get("mcp-session-id", "").strip()
        if not session:
            raise ProbeError("initialize did not return Mcp-Session-Id.")

        try:
            invalid_post_status, _, _ = rpc_post(
                endpoint,
                token,
                session,
                3,
                "ping",
                {},
                args.timeout,
                protocol_version=INVALID_PROTOCOL,
            )
            if invalid_post_status != 400:
                raise ProbeError("mismatched MCP-Protocol-Version POST was not rejected with HTTP 400.")

            notify_status, _, _ = rpc_post(endpoint, token, session, None, "notifications/initialized", {}, args.timeout)
            if notify_status != 202:
                raise ProbeError("notifications/initialized was not accepted.")

            ping_status, _, ping = rpc_post(endpoint, token, session, 4, "ping", {}, args.timeout)
            if ping_status != 200 or ping is None or "error" in ping:
                raise ProbeError("MCP ping failed.")

            tools_status, _, tools_envelope = rpc_post(endpoint, token, session, 5, "tools/list", {}, args.timeout)
            if tools_status != 200 or tools_envelope is None:
                raise ProbeError("tools/list failed.")
            tools_result = tools_envelope.get("result")
            tools = tools_result.get("tools") if isinstance(tools_result, dict) else None
            if not isinstance(tools, list):
                raise ProbeError("tools/list returned an invalid tool array.")
            names = {entry.get("name") for entry in tools if isinstance(entry, dict) and isinstance(entry.get("name"), str)}
            missing = sorted(REQUIRED_TOOLS - names)
            if missing:
                raise ProbeError("tools/list is missing required tools: " + ", ".join(missing))

            request_id = 10
            read_results: dict[str, str] = {}
            for name, arguments in READ_ONLY_TOOLS:
                value = call_tool(endpoint, token, session, request_id, name, arguments, args.timeout)
                request_id += 1
                if name == "connector_info" and value.get("singleRepository") is not True:
                    raise ProbeError("connector_info did not report singleRepository=true.")
                if name == "connector_info" and value.get("fullCadAgent") is not True:
                    raise ProbeError("connector_info did not report fullCadAgent=true.")
                read_results[name] = "PASS"

            invalid_delete_status, _, _ = request(
                endpoint,
                "DELETE",
                None,
                args.timeout,
                token=token,
                session=session,
                protocol_version=INVALID_PROTOCOL,
            )
            if invalid_delete_status != 400:
                raise ProbeError("mismatched MCP-Protocol-Version DELETE was not rejected with HTTP 400.")

            after_invalid_delete_status, _, after_invalid_delete_ping = rpc_post(
                endpoint,
                token,
                session,
                request_id,
                "ping",
                {},
                args.timeout,
            )
            if after_invalid_delete_status != 200 or after_invalid_delete_ping is None or "error" in after_invalid_delete_ping:
                raise ProbeError("protocol-rejected DELETE incorrectly terminated the live MCP session.")

            delete_status, _, _ = request(endpoint, "DELETE", None, args.timeout, token=token, session=session)
            if delete_status != 204:
                raise ProbeError("MCP session DELETE did not return 204.")
            stale_delete_status, _, _ = request(endpoint, "DELETE", None, args.timeout, token=token, session=session)
            if stale_delete_status != 404:
                raise ProbeError("terminated MCP session reuse was not rejected with HTTP 404.")
            session = ""

            print("PASS: QS3D embedded MCP read-only loopback qualification")
            print(f" protocol={PROTOCOL}; server=qs3d-bricscad; tools={len(names)}")
            print(
                " origin_remote_403=PASS; origin_null_403=PASS; origin_loopback=PASS; auth_rejection=PASS; "
                "initialize=PASS; protocol_version_post_400=PASS; notification=PASS; ping=PASS; "
                "protocol_version_delete_400=PASS; delete_preserves_session=PASS; "
                "session_delete=PASS; stale_session_404=PASS"
            )
            print(" readonly_tools=" + ",".join(f"{name}:{read_results[name]}" for name, _ in READ_ONLY_TOOLS))
            print(" secret_output=NONE; mutation_calls=0")
            return 0
        finally:
            if session:
                try:
                    request(endpoint, "DELETE", None, args.timeout, token=token, session=session)
                except Exception:
                    pass
    except (OSError, ProbeError, urllib.error.URLError) as exc:
        print("FAIL: QS3D embedded MCP read-only loopback qualification")
        print(" reason=" + str(exc).replace("\r", " ").replace("\n", " ")[:400])
        print(" secret_output=NONE; mutation_calls=0")
        return 1


if __name__ == "__main__":
    sys.exit(main())
