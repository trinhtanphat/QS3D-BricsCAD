# OpenAI Secure MCP Tunnel forwarded Content-Type and local-origin auth

Lane-Key: `issue-5237`

## Contract

The embedded MCP endpoint intentionally accepts MCP POST traffic only when `Content-Type` is JSON and returns HTTP 415 for missing or wrong media type. The OpenAI Secure Tunnel must satisfy that origin contract; the embedded server must not be relaxed to compensate for a forwarding omission.

OpenAI/ChatGPT `Authorization` and QS3D local tunnel-origin authentication are **two separate authentication layers** and must not share one HTTP header. Connector-forwarded `Authorization` remains available for the OpenAI/ChatGPT/OAuth layer. The supervised loopback hop uses `X-QS3D-MCP-Local-Authorization`, scoped to the OpenAI Secure Tunnel provider, for the QS3D local bearer.

`McpOpenAiSecureTunnelManager.WriteRuntimeConfig(...)` therefore emits this non-secret MCP forwarding configuration:

```yaml
mcp:
  server_urls:
    - channel: main
      url: "http://127.0.0.1:8765/mcp"
  extra_headers:
    X-QS3D-MCP-Local-Authorization: env:QS3D_TUNNEL_MCP_AUTH
    Content-Type: application/json
  discovery_extra_headers:
    X-QS3D-MCP-Local-Authorization: env:QS3D_TUNNEL_MCP_AUTH
```

The bearer remains child-process environment state only. Generated YAML stores the `env:QS3D_TUNNEL_MCP_AUTH` reference, never the bearer value. The secret is not placed on the command line or copied to diagnostics/public metadata.

The active embedded server accepts the dedicated header only when `McpTransportProvider.OpenAiSecureTunnel` is selected. It parses the Bearer scheme, compares the token in constant time, treats the header as a security-sensitive singleton, and fails closed on a malformed/wrong dedicated credential. It does not fall back to connector `Authorization` after a bad dedicated local credential. Existing direct-local `Authorization: Bearer <local-token>` compatibility and OAuth/public `Authorization` validation remain intact. A non-OpenAI/public request cannot use the dedicated header to bypass its existing auth policy.

## Tunnel-client semantics

The official `openai/tunnel-client` runtime config exposes `mcp.extra_headers` for static headers sent to the MCP origin. Connector-forwarded request headers can be applied after static extra headers. Therefore QS3D must avoid a name collision; it must not depend on header precedence to protect the local credential.

`Content-Type: application/json` belongs in `mcp.extra_headers`. The local origin credential belongs in `X-QS3D-MCP-Local-Authorization`. Connector/OAuth `Authorization` remains a separate header. No Cloudflare transport change is required.

## Regression

Run:

```text
python scripts/preflight-mcp-openai-tunnel-local-auth.py
python scripts/preflight-mcp-openai-tunnel-content-type.py
python scripts/preflight-mcp-transport-providers.py
python scripts/preflight-embedded-mcp.py
python scripts/preflight-mcp-production-hardening.py
```

The collision regression models static `X-QS3D-MCP-Local-Authorization: Bearer LOCAL_TOKEN` together with connector `Authorization: Bearer CONNECTOR_TOKEN` and requires initialize admission to use the unchanged local credential. Negative coverage requires missing/wrong/malformed/duplicate local credentials to fail as appropriate and prevents non-OpenAI/public use of the dedicated header as an auth bypass.

The Content-Type guard requires the generated OpenAI config to contain JSON Content-Type inside `mcp.extra_headers`, requires the dedicated local bearer env reference exactly once for runtime and once for discovery, forbids `Authorization: env:QS3D_TUNNEL_MCP_AUTH`, forbids persisting the bearer in the generated-config function, and pins the V2 HTTP 415 JSON media-type admission contract.

Final licensed runtime acceptance is direct CAD traffic through `ChatGPT Web → OpenAI control plane → openai/tunnel-client → embedded MCP → CAD`, using `connector_info` and `cad_active_document`; `qs3d_*` is not required to prove this transport fix.
