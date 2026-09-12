# BLT3D parity P9 — AI and direct MCP-to-CAD

## Scope

P9 continues the approved clean-room BLT3D parity program after P8. It binds the observed BLT3D LUNA/AI reference and the QS3D direct MCP-to-CAD requirement to current QS3D-owned source without copying a proprietary LUNA model, prompts, implementation, assets, keys, or runtime dependencies.

The manifest remains `# catalog-complete=false`. P9 advances only `ai.luna` and `mcp.direct-cad` to `CommandWired`; it does not claim `SemanticBehaviorPass`, `SaveReopenPass`, `V25V26ParityPass`, licensed ChatGPT runtime, or full BLT3D parity.

## AI reference mapping

`ai.luna` is the canonical reference identifier retained from the observed BLT3D product surface. QS3D's supported equivalent is not an embedded clone of LUNA. It is the existing AI/chat workflow exposed through the QS3D AI Dashboard / MCP Agent Center and connected to ChatGPT through the embedded MCP contract.

Current source wiring includes:

- the BLT-style `AI_DASHBOARD` ribbon entry;
- the MCP ribbon override routing that entry to `QS3DMCPAGENTCENTER`;
- the modal Agent Center command and its explicit distinction between tunnel readiness and proof of an actual `tools/call`.

That wiring is sufficient for `CommandWired` evidence only.
## Direct MCP-to-CAD evidence

`mcp.direct-cad` maps to the embedded QS3D MCP runtime, not to a second repository or a BLT3D runtime dependency. Current source exposes JSON-RPC `tools/call`, `connector_info`, `cad_active_document`, bounded `cad_audit_tail`, and direct CAD/QS3D dispatch through the existing host runtime.

`ParityAiMcpCatalog` records two host-neutral infrastructure bindings:

- `ai.luna`: `Ui | Mcp`, with no document requirement because opening/connecting the conversational integration is infrastructure state;
- `mcp.direct-cad`: `Mcp`, requiring `ActiveDocument` for the represented CAD-facing workflow.

The catalog is routing/evidence metadata only. It does not grant a mutation bypass. Individual MCP mutations remain subject to their existing admission, semantic/transaction, audit, confirmation, document-affinity, and emergency-stop contracts.

## Runtime evidence ceiling

Hosted/source CI can prove these routes exist and compile, but it cannot prove a real ChatGPT/OpenAI/Cloudflare/licensed-BricsCAD session. `MCP-CANONICAL-RUNBOOK.md` therefore remains authoritative for `PENDING_LOCAL` / `LOCAL_ONLY` runtime state.

No P9 source result may be cited as `LOCAL-024` PASS. LOCAL-024 still requires the exact intended merged/release descendant to be exercised on real Windows with licensed BricsCAD V25/V26 and the required ChatGPT/OpenAI/transport matrix.

P9 specifically does not claim model-response equivalence with BLT3D LUNA, prompt equivalence, autonomous semantic mutation parity, save/reopen parity, or V25/V26 runtime parity.