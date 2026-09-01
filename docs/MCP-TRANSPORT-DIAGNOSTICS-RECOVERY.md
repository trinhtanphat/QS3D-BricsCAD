# QS3D MCP transport diagnostics & recovery

This note is a focused companion to `MCP-CANONICAL-RUNBOOK.md`. The canonical architecture and LOCAL-024 boundary remain authoritative.

## Cloudflare installer

Agent Center keeps the Cloudflare install action single-flight. While the installer is active, the install button is disabled, its label shows live progress, and exactly one **Hủy cài Cloudflare Tunnel** control is owned by the cloudflared bootstrapper. Each managed download attempt is bounded to 120 seconds with bounded read/write timeouts and at most three attempts. A cancelled or failed download cleans its temporary file and must not intentionally replace the previous verified binary.

QS3D searches for an already-installed `cloudflared` before downloading another copy. WinGet, Program Files, `QS3D_CLOUDFLARED_PATH` and PATH candidates are accepted only after Windows Authenticode validation with a Cloudflare signer. Agent Center exposes the resolved **Source / Path / Trust=VERIFIED** state so support can see which binary is actually in use.

Manual recovery remains:

```powershell
winget install --id Cloudflare.cloudflared --source winget
cloudflared --version
```

Agent Center also provides **Copy WinGet recovery command** so the user does not have to retype the package id. After WinGet finishes, return to Agent Center and Refresh. A trusted WinGet binary is reused instead of duplicated.

## OpenAI Secure Tunnel trust

`OpenAI Secure MCP Tunnel` is the no-user-owned-public-hostname path. It does not require the user to own/configure a Cloudflare account, Cloudflare-managed domain, or QS3D public MCP hostname. The official OpenAI tunnel runtime remains provider-managed; QS3D does not claim that the provider implementation can never contain an internal companion runtime.

`tunnel-client.exe` is verified when selected and again immediately before launch. A valid Authenticode signature must identify OpenAI. If an official release is intentionally unsigned, set the official release SHA-256 as `QS3D_OPENAI_TUNNEL_CLIENT_SHA256`; QS3D computes the file SHA-256 and requires an exact match.

Runtime API keys are stored through the existing Windows Credential Manager contract after write/read-back verification, while the local QS3D bearer remains a local persisted MCP credential. At tunnel launch, both required secrets are projected into child-process environment values and are not serialized as literal values into generated YAML. The tunnel supervisor captures only a bounded, sanitized stdout/stderr tail plus exit code and trust summary.

## OpenAI tunnel local-origin authentication

OpenAI/ChatGPT `Authorization` and QS3D loopback tunnel-origin authentication are **two separate authentication layers**. They must not share the same HTTP header.

The generated OpenAI tunnel config uses:

```yaml
mcp:
  extra_headers:
    X-QS3D-MCP-Local-Authorization: env:QS3D_TUNNEL_MCP_AUTH
    Content-Type: application/json
  discovery_extra_headers:
    X-QS3D-MCP-Local-Authorization: env:QS3D_TUNNEL_MCP_AUTH
```

`Authorization` is reserved for connector/OAuth or existing direct engineering compatibility. The dedicated `X-QS3D-MCP-Local-Authorization` header carries only the QS3D loopback credential for the selected OpenAI Secure Tunnel provider. This separation is required because connector-forwarded request headers may be applied after static tunnel headers; relying on `Authorization` precedence can overwrite the local bearer and produce origin HTTP 401, which is observed from ChatGPT as an upstream 502.

The embedded server treats `X-QS3D-MCP-Local-Authorization` as a security-sensitive singleton. When the OpenAI Secure Tunnel provider is selected and this header is present, malformed or incorrect Bearer data fails closed and does not fall back to connector `Authorization`. On non-OpenAI/public paths the dedicated header cannot bypass the existing OAuth/bearer policy.

If ChatGPT reports 502 after a tunnel update, check in this order:

1. `GET http://127.0.0.1:8765/healthz` returns 200 on the active embedded server.
2. Direct local initialize with the saved local bearer and `Content-Type: application/json` returns HTTP 200.
3. `%APPDATA%\QS3D\MCP\OpenAiSecureTunnel\tunnel-client.yaml` contains `X-QS3D-MCP-Local-Authorization: env:QS3D_TUNNEL_MCP_AUTH` and does **not** contain `Authorization: env:QS3D_TUNNEL_MCP_AUTH`.
4. The tunnel local `/readyz` endpoint returns 200.
5. After an assembly update, restart BricsCAD so the active `McpEmbeddedServerV2` code is loaded; restart/regenerate the tunnel config as part of the same recovery.
6. Re-test ChatGPT with `connector_info`, then `cad_active_document`.

Do not print the local bearer, Runtime API key, OAuth access token, or environment value while collecting diagnostics.

## Agent Center diagnostics

On the OpenAI Secure Tunnel connection page, Agent Center augments the existing actions with:

- **Copy tunnel diagnostics** — copies the bounded/sanitized diagnostic bundle containing state, trust summary, exit code, last error and sanitized stdout/stderr tail.
- **Open tunnel logs** — materializes that same sanitized bounded bundle on demand under `%LOCALAPPDATA%\QS3D\MCP\OpenAiSecureTunnel\Support\tunnel-diagnostics.log` and opens it with the Windows default text viewer. The file is created only after this explicit user action; Runtime API key/local bearer are not intentionally written to it.
- **Restart tunnel · saved/env key** — re-projects the verified saved Windows Credential Manager Runtime API key when available, with `CONTROL_PLANE_API_KEY`/`OPENAI_API_KEY` as supported environment sources, and restarts only when a usable key is resolved.
- a live diagnostic status line for trust, exit code and last error.

These local diagnostics are troubleshooting evidence only. `READY` is not proof that ChatGPT performed a `tools/call`.

## Qualification

Hosted CI can guard source contracts but cannot produce licensed BricsCAD/OpenAI/Cloudflare/ChatGPT runtime evidence. Run the expanded LOCAL-024 matrix from `MCP-CANONICAL-RUNBOOK.md` on the exact intended merged/release descendant before claiming runtime PASS.
