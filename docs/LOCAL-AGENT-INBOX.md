# QS3D local-agent inbox

**Updated:** 2026-08-27 (UTC+7)

This file is the **single live queue for LOCAL_ONLY work**. Detailed runbooks remain in the linked local qualification/handoff documents, but a local agent should start here before opening those longer files.

## Mandatory handoff contract

- A remote/hybrid agent that discovers a new LOCAL_ONLY requirement must add or update the matching item in this file **in the same source/docs batch that introduced or exposed the requirement**.
- Do not create a second live queue. Historical `docs/LOCAL-AGENT-*.md` files are supporting detail/evidence; this inbox is the current priority index.
- Every `OPEN`, `IN_PROGRESS`, or `BLOCKED` LOCAL_ONLY item in this inbox has implicit remote disposition **`DO_NOT_RETRY_REMOTE`**. Subsequent remote/non-local agents must skip its execution/re-audit unless current source materially changes the scenario, the owner explicitly asks for a fresh remote source investigation, or the agent actually gains the missing local capability.
- Before adding an item, remote agents must search this inbox and update the existing matching item instead of duplicating the same unavailable work. Lack of local capability is a handoff condition, not a reason to retry from another equivalent remote agent.
- Local agents work `P0` before `P1` before `P2`, always from a clean checkout of the newest intended SHA.
- `LOCAL_PASS` requires real evidence tied to the exact tested SHA. Source review, static preflight, mock tests, `-SkipRuntime`, or a remote build cannot manufacture `LOCAL_PASS`.
- Never commit proprietary BricsCAD DLLs, private/customer DWGs, signing keys, credentials, or unsanitized runtime captures.
- When an item passes, set `Status: PASS`, replace `Evidence: PENDING_LOCAL` with a sanitized evidence summary, and record the exact SHA under `Evidence`.
- When source changes alter a local scenario, update this inbox immediately instead of relying on an older handoff paragraph.

Valid priorities: `P0`, `P1`, `P2`.  
Valid statuses: `OPEN`, `IN_PROGRESS`, `PASS`, `BLOCKED`.

## LOCAL-024 — #4352 ChatGPT MCP full-agent qualification

- Priority: P0
- Status: OPEN
- Area: issue #4352; ChatGPT MCP full-agent production qualification across the embedded QS3D runtime, Cloudflare public endpoint, and BricsCAD-hosted CAD/UI tool surface.
- Remote disposition: PENDING_LOCAL / DO_NOT_RETRY_REMOTE
- Runtime gate: Run only from a clean exact candidate SHA after all remote/source gates are green; hosted/static checks are source-readiness evidence only.
- Why local: Requires licensed BricsCAD V25/V26 on Windows, real Cloudflare browser-login/Named Tunnel behavior, ChatGPT MCP discovery, process-confined UI input, save/reopen, and cleanup against the exact candidate SHA.
- Scenario: Follow `docs/agent-work-claims/issue-4352-chatgpt-mcp-session-handoff.md` and `docs/MCP-FULL-CAD-AGENT.md`. Prove V25/V26 plugin load, authenticated loopback protocol, browser-login Cloudflare onboarding and reconnect, ChatGPT tool discovery, direct CAD read/mutation tools, bounded command workflows, BricsCAD-process-only mouse/keyboard fallback, timeout/no-auto-retry truth, emergency stop/cancel/resume, save/cold-reopen, and zero task-owned process residue on disposable drawings.
- Evidence required: Exact candidate SHA; matching V25/V26 host/plugin identity; sanitized loopback/tunnel/ChatGPT results; mutation/UI/recovery/save-reopen matrix; cleanup. Never publish bearer tokens, Cloudflare credentials, private paths, customer drawings, proprietary binaries, or unsanitized captures.
- Evidence: `PENDING_LOCAL`
- Related source/docs: `docs/agent-work-claims/issue-4352-chatgpt-mcp-session-handoff.md`; `docs/MCP-FULL-CAD-AGENT.md`; issue #4352; PR #4425.
- Updated: 2026-08-29

## Close-out rule

Closing all `OPEN` P0/P1 items does not automatically mean the product is commercially released. Release publication still follows `CI_POLICY.md` and requires the owner's separate explicit release authorization. This inbox only records local engineering qualification truth.
