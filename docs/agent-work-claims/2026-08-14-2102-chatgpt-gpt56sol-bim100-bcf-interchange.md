# Agent work claim: BCF issue interchange core

Status: ACTIVE
Agent: chatgpt-gpt56sol-bim100
Branch: agent/chatgpt-gpt56sol-bim100/bcf-interchange
Purpose: Close the remote-safe BCF portion of #84 by adding deterministic topic/comment/viewpoint payloads, stable QS3D↔IFC component identity mapping, and round-trip smoke coverage.
Claim-only commit: yes

## Source files

- `src/QS3D.Core/Export/BcfIssueExchange.cs`
- `src/QS3D.Core/Export/BcfIssueExchangeSerializer.cs`
- `tests/QS3D.Core.SmokeTests/BcfIssueExchangeSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`

## Acceptance

- BCF topics preserve stable topic identity, title, status, type, and description.
- Comments preserve stable identity, author, UTC timestamp, text, and optional viewpoint reference.
- Viewpoints preserve stable identity plus canonical component references.
- Every BCF component reference carries the QS3D element identity and IFC GlobalId together so the #84 identity bridge is explicit and round-trippable.
- Serialization is deterministic for semantically equivalent input ordering and deserialization fails closed on malformed, duplicate, or dangling references.
- Core-only smoke coverage proves round-trip identity, canonical ordering, determinism, and invalid-reference rejection.

## Verification

- `pwsh ./scripts/build.ps1`
- `pwsh ./scripts/run-tests.ps1`

## Local-only gate

None for this Core-only deterministic interchange contract. This claim does not certify BricsCAD-native UI/adapters or third-party `.bcfzip` compatibility; those remain separate acceptance surfaces for full product qualification.
