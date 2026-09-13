# MCP native QSAVE database-generation affinity

## Scope

`cad_save_current` queues BricsCAD's native `QSAVE` for the active drawing. The operation must remain bound to the exact managed `Document`, local path, and native `Database` generation that were admitted before the command was queued.

## Invariants

- Capture `Database.UnmanagedObject` before handler attachment / native QSAVE queueing and reject a zero native identity.
- A terminal `QSAVE` event is not success evidence unless the active managed document still owns that exact native database identity.
- Post-terminal `DBMOD` settling must revalidate the same native database generation before publishing success.
- A generation mismatch is fail-closed: completion is not confirmed and callers must not automatically retry or replay the native save.
- The application-context mutation ownership introduced by #6623 remains authoritative; this guard does not add a second writer, queue, timeout, or retry path.

## Failure model

Managed `Document` reference equality and an unchanged filename are insufficient if BricsCAD reloads/replaces the underlying native database while keeping the wrapper/path stable. In that case, stale terminal events or a clean `DBMOD` from a replacement database must not be attributed to the original QSAVE.

## Validation

Run `python scripts/preflight-mcp-native-qsave-database-generation.py` plus the MCP production/capability guards and repository reservation checks. Licensed BricsCAD testing must exercise database replacement/reload timing; without that evidence classify native runtime behavior as `LOCAL_ONLY / NO_RESULT` rather than PASS.
