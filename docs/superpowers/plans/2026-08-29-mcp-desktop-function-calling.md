# MCP desktop function-calling implementation plan

Issue: #4629

1. Add a dedicated auto-discovered preflight that fails until the server advertises the complete desktop tool catalog, the CAD runtime routes desktop calls through existing mutation gates, and the desktop runtime contains bounded Win32/WPF implementations without shell/process execution.
2. Prove the new preflight RED against baseline `main`.
3. Add `McpDesktopAutomationRuntime.cs` with descriptor generation, read-only cursor/window inspection, target-window focus, mouse move/click/scroll, Unicode typing, named-key/hotkey input, text clipboard read/write and bounded in-memory PNG screenshots.
4. Route desktop tools from `McpCadAgentRuntime.Call`; all mutation tools use existing `Mutation(...)`, emergency-stop epoch checks and existing audit writer.
5. Append desktop descriptors from `McpEmbeddedServerV2.ToolsListResponse` so standard MCP `tools/list` discovers them; keep `tools/call` unchanged except for normal runtime delegation.
6. Extend `preflight-mcp-full-agent.py` and `docs/MCP-FULL-CAD-AGENT.md` to make the expanded contract canonical.
7. Run focused source guards, aggregate preflight where practical, inspect the exact branch diff, open a PR, wait for exact-head protected CI, reconcile non-force if `main` moves, then merge only with expected-head protection.
8. Keep real Windows/BricsCAD/ChatGPT desktop qualification explicitly LOCAL_ONLY until executed on the licensed host.
