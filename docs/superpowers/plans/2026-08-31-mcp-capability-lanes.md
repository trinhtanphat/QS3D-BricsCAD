# MCP Capability Lanes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Separate native BricsCAD/CAD function calling from QS3D business-domain function calling while preserving compatibility and explicit routing modes.

**Architecture:** Put lane/mode/error policy in host-neutral QS3D.Core, keep `McpCadAgentRuntime` as the outer safety dispatcher, keep native mutations in `McpCadDirectModelRuntime`, and move QS3D business execution into `McpQs3dDomainRuntime`. The MCP transport publishes split statuses, injects execution-mode schema fields, and emits structured error contracts.

**Tech Stack:** C# / .NET Standard 2.0 Core, .NET Framework 4.8 BricsCAD V25 adapter, .NET 8 BricsCAD V26 shared adapter, Python source preflights, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-08-31-mcp-capability-lanes-design.md`

## Global Constraints

- Existing MCP tool names remain compatible.
- `qs3d_status` becomes a deprecated domain-only alias and must not expose active-document/current-layer host fields.
- `AUTO`, `CAD_DIRECT`, and `QS3D_DOMAIN` are the only execution modes.
- Both `executionMode` and `execution_mode` are accepted; conflicting values fail closed.
- QS3D failure state must never disable native CAD capability.
- Existing confirmation and emergency-stop semantics remain authoritative.

---

### Task 1: Host-neutral capability contract

**Files:**
- Create: `src/QS3D.Core/Agent/McpToolCapabilityContract.cs`
- Create: `tests/QS3D.Core.SmokeTests/McpToolCapabilityContractSmoke.cs`

**Interfaces:**
- Produces: `McpExecutionMode`, `McpToolLane`, `McpToolFailure`, `McpToolContractException`, and `McpToolCapabilityContract` lane/mode/error APIs.

- [ ] **Step 1: Write the failing smoke test** covering lane classification, mode aliases, mode violations, emergency/read-only exceptions, and representative error mappings.
- [ ] **Step 2: Run `dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release` and verify RED because the contract does not exist.**
- [ ] **Step 3: Add the minimal Core contract implementation.**
- [ ] **Step 4: Run the smoke suite again and verify GREEN.**

### Task 2: Separate QS3D domain runtime

**Files:**
- Create: `src/QS3D.BricsCAD.V25/McpQs3dDomainRuntime.cs`
- Modify: `src/QS3D.BricsCAD.V25/McpCadDirectModelRuntime.cs`
- Modify: `src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs`

**Interfaces:**
- Consumes: `McpToolCapabilityContract`.
- Produces: `McpQs3dDomainRuntime.BuildStatusJson`, `Call`, `RequiresMutation`, and reset semantics.

- [ ] **Step 1: Move `qs3d_place_single_footing` and `qs3d_run_command` business execution into `McpQs3dDomainRuntime`.**
- [ ] **Step 2: Remove QS3D placement ownership from `McpCadDirectModelRuntime`.**
- [ ] **Step 3: Route modes before dispatch and preserve confirmation/emergency-stop gates in `McpCadAgentRuntime`.**
- [ ] **Step 4: Split `mcp_status`, `bricscad_status`, `qs3d_domain_status`, and compatibility `qs3d_status`.**

### Task 3: Transport schema and structured errors

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs`

**Interfaces:**
- Consumes: Core capability/error contract and runtime results.
- Produces: mode-aware schemas and structured MCP error envelopes.

- [ ] **Step 1: Publish split status tools and QS3D placement descriptor.**
- [ ] **Step 2: Inject `executionMode` and `execution_mode` into fixed and dynamic tool schemas.**
- [ ] **Step 3: Return `code`, `lane`, and `message` in structured error content.**

### Task 4: Regression/source guards and documentation

**Files:**
- Create: `scripts/preflight-mcp-capability-lanes.py`
- Modify: `scripts/preflight-mcp-single-footing-direct.py`
- Create: `docs/MCP-CAPABILITY-LANES.md`

- [ ] **Step 1: Add source guards proving status separation, ownership separation, mode schema, structured errors, and CAD independence from QS3D health.**
- [ ] **Step 2: Update the single-footing preflight to require domain ownership and shared authoring semantics.**
- [ ] **Step 3: Document routing/fallback/error behavior for ChatGPT function calling.**

### Task 5: Verification and integration

- [ ] **Step 1: Run the Core smoke suite.**
- [ ] **Step 2: Run `preflight-mcp-capability-lanes.py`, `preflight-mcp-single-footing-direct.py`, `preflight-mcp-full-agent.py`, and `preflight-embedded-mcp.py`.**
- [ ] **Step 3: Open a PR with `Lane-Key: issue-4997`.**
- [ ] **Step 4: Require normal PR CI, including aggregate source preflight and V25 compile validation, to pass.**
- [ ] **Step 5: Merge to `main` only after exact-head verification succeeds.**
