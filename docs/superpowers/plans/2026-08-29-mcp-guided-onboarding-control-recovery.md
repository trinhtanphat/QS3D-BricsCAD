# QS3D MCP guided onboarding/control/recovery implementation plan

1. Add a source-level contract test before production code. It requires local desktop consent, active blue overlay + Esc×2 emergency stop, guided Control Center tabs, browser-owned auth wording, autosave/recovery service and V25/V26 startup lifecycle integration.
2. Add `McpAgentExperience` as a bounded in-process status/event timeline. Keep secrets and user document contents out of event payloads.
3. Add `McpDesktopControlSession` and overlay UI. Consent resets on every host start. Guard desktop mutation plus clipboard/screenshot sensitive reads. Use a low-level keyboard hook so Esc×2 works even when another Windows application is foreground.
4. Update `McpDesktopAutomationRuntime` so guarded desktop tools fail closed unless local consent is enabled and wrap active calls with the visible session overlay/status scope.
5. Add `McpProjectRecoveryService`: preserve shorter existing BricsCAD autosave settings, enforce an upper autosave interval of five minutes plus `.bak`, snapshot stable on-disk DWGs while CAD is idle, cap retention, and recover only to a new copy.
6. Add `McpFirstRunExperience` toast prompting missing cloudflared/onboarding and linking directly to the Control Center without capturing credentials.
7. Replace the current flat Agent Center with a tabbed guided UX. Primary flow is embedded MCP -> install cloudflared -> provider browser login -> Named Tunnel -> copy OAuth MCP URL/open ChatGPT -> local protocol verification. Keep bearer/Quick Tunnel under Advanced.
8. Split the local loopback test client into its own source file so the UI remains maintainable.
9. Start/stop the recovery and first-run experience from both V25 and V26 host entries; V26 must also start the embedded MCP and persistent Cloudflare path just like V25.
10. Update the runbook/docs to describe system-browser ownership, local consent, recovery model and current ChatGPT remote-MCP boundary.
11. Do source inspection/contract verification only in this session. CI is intentionally left to the repository owner per request; do not rerun or repair CI here.
