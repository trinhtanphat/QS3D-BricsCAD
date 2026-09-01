# MCP cad_wait_idle Schema Default Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish the existing 5000 ms `cad_wait_idle` runtime default in the active MCP tool JSON Schema without changing runtime behavior.

**Architecture:** Keep `McpCadAgentRuntime` unchanged because its 5000 ms default / 7000 ms maximum are already correct. Add a focused auto-discovered source guard that binds the runtime dispatch and the `McpEmbeddedServerV2` descriptor to the same 5000/7000 contract, then make the minimal descriptor-only correction.

**Tech Stack:** C# (.NET/BricsCAD V25 adapter source), Python 3 preflight guards, GitHub Actions Shared CI.

**Spec:** GitHub issue #5254 (`fix(mcp): publish cad_wait_idle default in tool schema`).

## Global Constraints

- Preserve `cad_wait_idle` runtime dispatch default `5000`, minimum `100`, maximum `7000`.
- Preserve CMDACTIVE polling, `Thread.Sleep(100)`, structured timeout result, and transport response-budget behavior.
- Change no other MCP tool schema.
- Hosted/source evidence is authoritative only for this schema contract; do not claim licensed BricsCAD `LOCAL_PASS`.
- Require Reservation Protocol v2, `Lane-Key: issue-5254`, fresh exact-head `preflight + core`, latest-main freshness, and expected-head merge.

---

### Task 1: Bind and publish the `cad_wait_idle` schema default

**Files:**
- Create: `scripts/preflight-mcp-cad-wait-idle-schema-default.py`
- Modify: `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs`
- Create: `docs/FEATURE-RUNBOOKS/mcp-cad-wait-idle-schema-default.md`

**Interfaces:**
- Consumes: runtime dispatch `case "cad_wait_idle": return WaitUntilIdle(Integer(args, "timeoutMs", 5000, 100, 7000));` from `McpCadAgentRuntime.cs`.
- Produces: MCP schema fragment `"timeoutMs":{"type":"integer","minimum":100,"maximum":7000,"default":5000}` for `cad_wait_idle`.

- [ ] **Step 1: Write the failing source guard**

```python
expected_dispatch = 'case "cad_wait_idle": return WaitUntilIdle(Integer(args, "timeoutMs", 5000, 100, 7000));'
expected_schema = '\\"timeoutMs\\":{\\"type\\":\\"integer\\",\\"minimum\\":100,\\"maximum\\":7000,\\"default\\":5000}'
expected_tool = 'Tool("cad_wait_idle", "Wait until BricsCAD CMDACTIVE becomes zero.", "' + expected_schema + '")'
```

The guard must fail if the runtime dispatch drifts, if the descriptor omits `default:5000`, or if the old no-default descriptor remains.

- [ ] **Step 2: Run the guard to verify RED**

Run:

```text
python scripts/preflight-mcp-cad-wait-idle-schema-default.py
```

Expected on baseline `be2feea4...`: FAIL with `cad_wait_idle schema must publish default 5000 ms` because the descriptor currently ends at `maximum:7000`.

- [ ] **Step 3: Make the minimal production correction**

Change only the `cad_wait_idle` descriptor in `McpEmbeddedServerV2.cs` from:

```csharp
Tool("cad_wait_idle", "Wait until BricsCAD CMDACTIVE becomes zero.", "\"timeoutMs\":{\"type\":\"integer\",\"minimum\":100,\"maximum\":7000}")
```

to:

```csharp
Tool("cad_wait_idle", "Wait until BricsCAD CMDACTIVE becomes zero.", "\"timeoutMs\":{\"type\":\"integer\",\"minimum\":100,\"maximum\":7000,\"default\":5000}")
```

- [ ] **Step 4: Verify GREEN and preserved semantics**

Run:

```text
python scripts/preflight-mcp-cad-wait-idle-schema-default.py
python scripts/preflight-mcp-cad-wait-idle-response-budget.py
```

Expected: both PASS. Shared CI must also produce fresh exact-head `preflight` and `core` SUCCESS.

- [ ] **Step 5: Document and integrate**

Add the focused runbook with RED/GREEN evidence, reconcile current protected `main` non-force if it moved, open one canonical PR with `Lane-Key: issue-5254` and `Closes #5254`, then expected-head merge only after fresh required checks succeed. Verify protected `main` contains the merge and close/release the reservation.