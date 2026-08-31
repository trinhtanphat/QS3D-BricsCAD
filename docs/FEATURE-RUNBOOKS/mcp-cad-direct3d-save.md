# MCP deterministic CAD 3D and save runbook

Issue: #4797

## Exposed tools

The embedded MCP server publishes these bounded mutation tools from `McpCadDirectModelRuntime`:

- `cad_create_box`
- `cad_extrude`
- `cad_boolean_union`
- `cad_boolean_subtract`
- `cad_boolean_intersect`
- `cad_save`
- `cad_save_as`

`cad_command_sequence` also has one command-specific multi-stage grammar for `EXTRUDE`, allowing a blank selection terminator followed by at most two finite numeric post-selection values while rejecting command-like injection.

## Safety boundary

All direct mutations are dispatched by `McpCadAgentRuntime.Mutation`. `confirmMutation=true` is required before entry, and `McpCadAgentRuntime.EnsureCurrentMutationRunning()` re-checks the shared mutation epoch inside the direct runtime before CAD dispatch and mutation. Emergency stop therefore invalidates in-flight work even if the agent is resumed before a later mutation checkpoint.

No shell, process-launch, arbitrary script/eval, generic file-reader or unrestricted command surface is added by this lane.

## Save semantics

`cad_save` requires an existing rooted drawing path, idle CAD state, synchronous `Database.Save()`, and `DBMOD==0` before reporting completion.

`cad_save_as` requires an absolute `.dwg` target under an existing writable directory. Windows/application installation directories are rejected. Existing files require explicit `overwrite=true`; the active drawing path must use `cad_save` instead. After `Database.SaveAs`, the active database path and `DBMOD==0` are verified before success is returned.

## Source qualification

Run:

```text
python scripts/preflight-mcp-cad-direct3d-save.py
```

The guard verifies QSAVE truthfulness, DBMOD-aware document status, direct tool descriptors, canonical mutation routing, shared epoch checks, bounded EXTRUDE grammar, SaveAs writability/overwrite guards and embedded-server exposure.

Repository CI must also pass the aggregate preflight lane and BricsCAD V25 compile/plugin build on the exact PR head before merge.

## Licensed host qualification

Runtime qualification remains LOCAL_ONLY until tested on the licensed BricsCAD V25 host. Minimum checks:

1. Create two boxes, exercise union/subtract/intersect and inspect resulting handles.
2. Extrude a closed planar curve with `cad_extrude`.
3. Exercise the multi-stage EXTRUDE selection terminator and confirm a second command token is rejected.
4. Modify a rooted drawing, call `cad_save`, confirm completion and `cad_active_document.modified=false`.
5. Verify `cad_save_as` rejects a protected/unwritable destination and an existing file without overwrite opt-in, then succeeds to a writable project path.
6. Trigger `cad_agent_stop` during a mutation opportunity and confirm stale mutation work cannot continue after resume.
