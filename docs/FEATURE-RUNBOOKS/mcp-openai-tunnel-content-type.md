# OpenAI Secure MCP Tunnel forwarded Content-Type

Lane-Key: `issue-5227`

## Contract

The embedded MCP endpoint intentionally accepts MCP POST traffic only when `Content-Type` is JSON and returns HTTP 415 for missing or wrong media type. The OpenAI Secure Tunnel must satisfy that origin contract; the embedded server must not be relaxed to compensate for a forwarding omission.

`McpOpenAiSecureTunnelManager.WriteRuntimeConfig(...)` therefore emits this non-secret MCP forwarding configuration:

```yaml
mcp:
  server_urls:
    - channel: main
      url: "http://127.0.0.1:8765/mcp"
  extra_headers:
    Authorization: env:QS3D_TUNNEL_MCP_AUTH
    Content-Type: application/json
  discovery_extra_headers:
    Authorization: env:QS3D_TUNNEL_MCP_AUTH
```

The bearer remains child-process environment state only. Generated YAML stores the `env:QS3D_TUNNEL_MCP_AUTH` reference, never the bearer value.

## Tunnel-client semantics

The official `openai/tunnel-client` runtime config exposes `mcp.extra_headers` for static headers sent to the MCP origin. Its static-header end-to-end coverage verifies those configured MCP headers are present on origin requests while control-plane traffic remains separately scoped. `Content-Type: application/json` therefore belongs in `mcp.extra_headers`; no Cloudflare transport change is required.

## Regression

Run:

```text
python scripts/preflight-mcp-openai-tunnel-content-type.py
python scripts/preflight-embedded-mcp.py
python scripts/preflight-mcp-production-hardening.py
```

The dedicated guard requires the generated OpenAI config to contain the JSON Content-Type inside `mcp.extra_headers`, requires Authorization to remain an environment reference, forbids persisting the bearer in the generated-config function, and pins the V2 HTTP 415 JSON media-type admission contract.

Final licensed runtime acceptance is direct CAD-only traffic through `ChatGPT Web → OpenAI Tunnel → MCP → CAD`, using `cad_active_document` and `cad_view_state`; `qs3d_*` is not part of this acceptance.
