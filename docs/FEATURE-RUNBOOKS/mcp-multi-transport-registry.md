# MCP Multi-Transport Registry

Issue: #5299

## Scope

This carrier adds a versioned, secret-free transport profile registry. It can represent multiple enabled OpenAI Secure MCP Tunnel and Cloudflare profiles at the same time while preserving `McpTransportCoordinator.SelectedProvider` as the legacy preferred UI/onboarding provider.

It does **not** claim that multiple provider child processes are already supervised concurrently. Provider multi-process supervisors, Agent Center profile controls, and same-DWG concurrent stress qualification belong to follow-up carriers.

## Invariants

- Exactly one loopback `McpEmbeddedServer` remains canonical per BricsCAD/QS3D process.
- `McpCadMutationCoordinator` remains the only process-global DWG write boundary.
- Multiple transports may expose the same embedded MCP, but they must never create independent DWG writer lanes.
- Registry persistence contains profile metadata only; provider credentials remain in their existing protected stores or process environment.
- `SelectedProvider` is a compatibility preference for UI/onboarding and legacy autostart behavior, not registry ownership.

## Persistence

Registry path:

`%APPDATA%/QS3D/MCP/Transport/profiles-v1.txt`

Registration acknowledgement path:

`%APPDATA%/QS3D/MCP/Transport/profiles-v1-registration.txt`

Both files use schema version 1, deterministic line-oriented escaped records, and atomic same-directory temporary-file replacement/move.

The profile registry stores only:

- 32-lowercase-hex profile ID;
- provider enum value;
- sanitized display name;
- enabled/autostart/legacy-default booleans;
- non-secret registration identity.

## Migration

When no versioned registry exists, the first load creates one enabled `legacy-default` profile from the existing selected provider. Existing provider preference/configuration files are left unchanged.

If an existing registry is malformed or uses an unsupported schema, the file is not overwritten. The runtime returns an in-memory bounded recovery profile and a sanitized registry status error.

## Registration acknowledgement

Acknowledgement is per profile ID and exact non-secret registration identity. Changing the profile identity causes the previous acknowledgement to stop matching automatically.

## Hosted verification

Run:

`python scripts/preflight-mcp-multi-transport-registry.py`

Hosted/source verification proves registry contract, legacy compatibility, secret-free persistence, and the unchanged single-writer invariant. It does not prove concurrent live external tunnel processes.

## LOCAL_ONLY follow-up

Live qualification must later cover:

1. two or more OpenAI tunnel processes plus at least one Cloudflare transport reaching the same embedded MCP;
2. concurrent read-only requests;
3. serialized mutation behavior through the existing process-global writer;
4. save/reopen evidence on the same DWG;
5. shutdown/restart recovery and per-profile registration state.
