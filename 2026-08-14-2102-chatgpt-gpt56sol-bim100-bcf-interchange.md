# Agent work claim: BCF 3.0 issue interchange and package core

Status: ACTIVE
Agent: chatgpt-gpt56sol-bim100
Branch: agent/chatgpt-gpt56sol-bim100/bcf-interchange
Purpose: Close the remote-safe BCF portion of #84 by adding deterministic topic/comment/viewpoint semantics, stable QS3D↔IFC component identity mapping, and a bounded buildingSMART BCF 3.0 package subset (`bcf.version`, `extensions.xml`, per-topic `markup.bcf`, and `.bcfv` component-selection viewpoints) with round-trip smoke coverage.
Claim-only commit: yes

## Source files

- `src/QS3D.Core/Export/BcfIssueExchange.cs`
- `src/QS3D.Core/Export/BcfIssueExchangeSerializer.cs`
- `src/QS3D.Core/Export/BcfZipPackage.cs`
- `tests/QS3D.Core.SmokeTests/BcfIssueExchangeSmoke.cs`
- `tests/QS3D.Core.SmokeTests/BcfZipPackageSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`

## Acceptance

- BCF topics preserve canonical lowercase GUID identity, title, status, type, description, required creation author, and required UTC creation timestamp.
- Comments preserve canonical GUID identity, author, UTC timestamp, text, and optional viewpoint reference.
- Viewpoints preserve canonical GUID identity, a bounded explicit camera, and canonical component references.
- Every BCF component reference carries the QS3D element identity and IFC GlobalId together; `.bcfv` writes IFC identity as `IfcGuid`, QS3D identity as `AuthoringToolId`, and `OriginatingSystem=QS3D`.
- The package emits `bcf.version` with BCF 3.0, `extensions.xml` containing deterministic TopicType/TopicStatus vocabularies used by the package, one lowercase topic-GUID directory per topic, schema-shaped `markup.bcf`, and one `.bcfv` per materialized viewpoint.
- Package entries use deterministic ordering/timestamps, bounded counts/sizes, safe relative paths only, and strict duplicate/missing/dangling/mismatched-identity rejection on read.
- Package round-trip preserves topic/comment/viewpoint/QS3D↔IFC identity and camera values without inventing BricsCAD-native state.
- Core-only smoke coverage proves deterministic logical package content, round-trip identity, canonical ordering, malformed archive rejection, path/entry safety, and unsupported-version rejection.

## Verification

- `pwsh ./scripts/build.ps1`
- `pwsh ./scripts/run-tests.ps1`

## Local-only gate

None for this Core-only deterministic BCF 3.0 subset. This claim does not certify BricsCAD-native BCF UI/adapters, snapshots/bitmaps, arbitrary third-party extension schemas, BCF API/cloud sync, or licensed V25 runtime behavior; those remain separate acceptance surfaces for full product qualification.
