# MCP CAD runtime response budget

Lane-Key: `issue-5164`
Reservation-Protocol: `v2`
Runtime scope: MCP CAD-direct dispatch only.

## Defect boundary

The core `McpCadAgentRuntime.InvokeCad()` path historically waited 15 seconds for BricsCAD application-context dispatch. That path backs direct reads such as `cad_active_document`, `cad_view_state`, and `cad_sysvar`, plus direct mutations routed through `InvokeCadMutation`. An upstream MCP/tunnel deadline can expire first, surfacing a raw 502 instead of the runtime's structured timeout error.

## Contract

The core CAD dispatch wait is bounded to 8 seconds so the embedded MCP layer retains response budget to serialize a normal MCP error before the edge deadline. If work is still queued, the work item is cancelled before start. If CAD work already started, the error remains completion-uncertain and callers must not retry automatically; they must inspect drawing/audit state first.

This change does not shorten native command workflows after they have been accepted into BricsCAD, does not add retries, and does not change A00/master-layout or QS3D business-domain behavior.

## Validation

`scripts/preflight-mcp-cad-runtime-response-budget.py` pins the 8-second response budget and the existing cancel-before-start / completion-uncertain semantics. Shared CI must pass discovered feature guards, Core checks, and locked BricsCAD V25 compile on the exact PR head.

A licensed test that deliberately blocks the BricsCAD application context and confirms ChatGPT receives a structured MCP timeout instead of raw 502 remains `LOCAL_ONLY` evidence.
