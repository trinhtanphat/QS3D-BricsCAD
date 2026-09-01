# MCP `cad_wait_idle` Schema Default

## Scope

Issue #5254 aligns the active V2 MCP tool descriptor with the already-correct `cad_wait_idle` runtime contract. The runtime remains default `5000` ms, minimum `100` ms, maximum `7000` ms. This lane changes only the schema so clients inspecting `tools/list` can discover that default.

## TDD RED evidence

Protected baseline: `main@be2feea4de3a788c7f9b10a99fe0c92f775eb939`.

RED guard commit/head: `a75fab3b902a8fcf63e3a081e4268d2b66214b40`.

GitHub Actions push run `33490444040`, preflight job `99800440017`:

- Reservation ownership validation: PASS.
- Exact checkout: `a75fab3b902a8fcf63e3a081e4268d2b66214b40`.
- Auto-discovered `preflight-mcp-cad-wait-idle-schema-default.py`: FAIL.
- Expected failure messages:
  - `cad_wait_idle schema must publish default 5000 ms`
  - `cad_wait_idle descriptor still omits the runtime default`

This is a valid source RED rather than reservation/CI infrastructure noise.

## GREEN implementation

Production commit `1ae348bca5ba2ca9d0820acbbc31aa57c20bacc7` makes exactly one source-line change in `McpEmbeddedServerV2.cs`:

```text
cad_wait_idle.timeoutMs: minimum=100, maximum=7000, default=5000
```

The runtime dispatch, CMDACTIVE polling loop, structured timeout result, transport response budgets, and all other tool descriptors remain unchanged.

## Verification

Required source/hosted checks:

```text
python scripts/preflight-mcp-cad-wait-idle-schema-default.py
python scripts/preflight-mcp-cad-wait-idle-response-budget.py
```

The canonical branch/PR must also obtain fresh exact-head protected `preflight` and `core` SUCCESS after any latest-main reconciliation. This is a schema/source contract lane; do not claim licensed BricsCAD `LOCAL_PASS` from hosted CI evidence.

## Merge discipline

- Reservation Protocol v2
- `Lane-Key: issue-5254`
- one canonical branch/PR only
- non-force latest-main reconciliation if protected `main` advances
- expected-head merge only after fresh required checks
- verify merged protected `main`, then mark issue `MERGED_MAIN / COMPLETED / RELEASED`
