# MCP credential persistence and listener generation

## Scope

This lane hardens the active V25 embedded MCP transport and local Runtime API-key persistence without claiming licensed BricsCAD runtime validation.

## Bearer token contract

- Reuse an existing valid `mcp-bearer-token.txt` token.
- Generate a token only when no valid saved token is available.
- Persist a generated token through a same-directory temporary file using write-through I/O.
- Replace/move atomically, then re-read the final file and compare the exact token before publishing it to the MCP server process.
- Any persistence/read-back failure prevents token publication/server startup. There is no ephemeral process-token fallback.

## Runtime API-key contract

- Store the key in Windows Credential Manager.
- Immediately re-read the credential and require exact ordinal equality.
- Only after verification may `CONTROL_PLANE_API_KEY` be published into the current process.
- Agent Center treats persistence failure as blocking the newly entered key for the current tunnel session.

## Preview updater boundary

The verified preview installer remains limited to `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll`. Bearer-token files, Windows Credential Manager state, and Runtime API-key environment state are outside the updater payload surface.

## Listener generation contract

- `Start()` captures the exact `TcpListener` in the listener-thread delegate.
- `ServeLoop(TcpListener listener)` accepts only on that captured listener.
- Before accept and on retry/error paths, the loop verifies that the captured listener is still the globally owned generation.
- A stale loop from a prior Stop→Start cycle exits instead of adopting the replacement listener.
- Existing bounded `thread.Join(1000)` shutdown remains; no `Thread.Abort` is introduced.

## Deterministic source verification

```text
python scripts/preflight-mcp-credential-persistence.py
python scripts/preflight-mcp-listener-generation.py
```

Normal protected PR CI, source preflights and V25 build validation remain required before merge. Licensed local MCP/BricsCAD runtime PASS is intentionally not claimed by this source-only lane.
