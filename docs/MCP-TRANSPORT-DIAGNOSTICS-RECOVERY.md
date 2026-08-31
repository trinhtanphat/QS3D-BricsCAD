# QS3D MCP transport diagnostics & recovery

This note is a focused companion to `MCP-CANONICAL-RUNBOOK.md`. The canonical architecture and LOCAL-024 boundary remain authoritative.

## Cloudflare installer

Agent Center keeps the Cloudflare install action single-flight. While the installer is active, the install button is disabled, its label shows live progress, and **Hủy cài Cloudflare Tunnel** is available. Each managed download attempt is bounded to 120 seconds with bounded read/write timeouts and at most three attempts. A cancelled or failed download cleans its temporary file and must not intentionally replace the previous verified binary.

QS3D searches for an already-installed `cloudflared` before downloading another copy. WinGet, Program Files, `QS3D_CLOUDFLARED_PATH` and PATH candidates are accepted only after Windows Authenticode validation with a Cloudflare signer. Manual recovery is:

```powershell
winget install --id Cloudflare.cloudflared
cloudflared --version
```

Then reopen/Refresh Agent Center. A trusted WinGet binary should be reused instead of duplicated.

## OpenAI Secure Tunnel trust

`tunnel-client.exe` is verified when selected and again immediately before launch. A valid Authenticode signature must identify OpenAI. If an official release is intentionally unsigned, set the official release SHA-256 as `QS3D_OPENAI_TUNNEL_CLIENT_SHA256`; QS3D computes the file SHA-256 and requires an exact match.

Runtime API keys and the local QS3D bearer remain child-process environment values and are not persisted into the generated YAML. The tunnel supervisor captures only a bounded, sanitized stdout/stderr tail plus exit code and trust summary.

## Agent Center diagnostics

On the OpenAI Secure Tunnel connection page, Agent Center augments the existing actions with:

- **Copy tunnel diagnostics** — copies the bounded/sanitized diagnostic bundle containing state, trust summary, exit code, last error and sanitized stdout/stderr tail.
- **Restart tunnel · env key** — restarts only when `CONTROL_PLANE_API_KEY` or `OPENAI_API_KEY` already exists in the Windows environment. QS3D does not persist a Runtime API key typed into the UI, so the restart action refuses to stop the current tunnel when no environment key is available.
- a live diagnostic status line for trust, exit code and last error.

These local diagnostics are troubleshooting evidence only. `READY` is not proof that ChatGPT performed a `tools/call`.

## Qualification

Hosted CI can guard source contracts but cannot produce licensed BricsCAD/OpenAI/Cloudflare/ChatGPT runtime evidence. Run the expanded LOCAL-024 matrix from `MCP-CANONICAL-RUNBOOK.md` on the exact intended merged/release descendant before claiming runtime PASS.
