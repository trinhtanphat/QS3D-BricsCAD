# MCP CAD agent DBMOD content semantics

`cad_active_document` and the synchronous fallback `QSAVE` path in `McpCadAgentRuntime` must distinguish persistent drawing-content changes from transient window/view changes reported by BricsCAD `DBMOD`.

The persistent content mask is `1 | 4 | 32`. Residual bits outside that mask, including window/view state such as `8 | 16`, do not by themselves mean drawing content is unsaved.

## Contract

- Read `DBMOD` through one fail-closed integer parser. An unavailable, malformed, or negative value is an error; it must never silently become zero.
- `cad_active_document.modified` is true only when `(DBMOD & (1 | 4 | 32)) != 0`.
- `cad_active_document.saved` requires an existing rooted local path and no persistent content bits.
- Fallback `QSAVE` retains the `CMDACTIVE == 0` idle gate, mutation serialization, and exactly one native `document.Database.Save()` attempt.
- After the native save, fallback `QSAVE` fails closed only when persistent content bits remain. Residual non-content bits are allowed.
- This source-level contract does not claim licensed BricsCAD save/reopen qualification. That evidence remains `LOCAL_ONLY`.

The focused preflight `scripts/preflight-mcp-cad-agent-dbmod-semantics.py` pins these source semantics so active-document status and fallback QSAVE stay aligned with the direct-save DBMOD behavior introduced previously.
