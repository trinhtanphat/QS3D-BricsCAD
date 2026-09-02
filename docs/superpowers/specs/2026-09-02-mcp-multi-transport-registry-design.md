# MCP Concurrent Transport Profile Registry Design

Issue: #5299  
Lane-Key: `issue-5299`

## Goal

Replace the current singleton transport-ownership assumption with a versioned, secret-free profile registry so one embedded QS3D MCP can eventually be reached by multiple OpenAI and Cloudflare transports concurrently. This carrier establishes registry, migration, and status semantics only; provider multi-process supervisors and Agent Center profile UI land in follow-up carriers.

## Invariants

1. There remains exactly one loopback `McpEmbeddedServer` per BricsCAD/QS3D process.
2. `McpCadMutationCoordinator` remains the only process-global DWG write boundary. Multi-transport must never create a second writer lane or bypass `writerToken` ownership.
3. Read-only MCP work can remain multi-session under existing admission limits.
4. Registry persistence must contain no API keys, bearer tokens, tunnel credentials, OAuth credentials, writer tokens, or raw diagnostic output.
5. Legacy users keep their existing provider preference/configuration after upgrade.
6. Malformed registry state fails closed to a reconstructed legacy/default profile rather than deleting legacy settings.

## Registry model

Create `McpTransportProfileRegistry.cs` with:

- schema version `1`;
- a stable profile ID using lowercase 32-hex identifiers;
- `McpTransportProvider Provider`;
- `string DisplayName` bounded to 120 characters and stripped of control characters;
- `bool Enabled`;
- `bool AutoStart`;
- `bool IsLegacyDefault`;
- `string RegistrationIdentity` containing only the non-secret OpenAI tunnel ID or canonical Cloudflare public URL identity already used by the current registration acknowledgement contract;
- per-profile registration acknowledgement state stored separately from provider credentials.

The persistent registry is written atomically under `%APPDATA%/QS3D/MCP/Transport/profiles-v1.txt`. Use a deterministic line-oriented format with escaped values so the V25 target does not require a new JSON package. A temporary file is flushed then moved/replaced into place.

## Migration

On first registry load when no registry exists:

1. Read current `McpTransportCoordinator.SelectedProvider` through a migration-only helper that does not recurse back into the registry.
2. Create one enabled legacy-default profile using that provider.
3. Preserve existing OpenAI tunnel ID / Cloudflare hostname files unchanged; the profile points to provider identity, not secrets.
4. Preserve `provider.txt` as the preferred UI provider compatibility setting.
5. Persist the new registry only after validation succeeds.

If a registry exists but is malformed, return a bounded recovery profile in memory, publish a sanitized status error, and leave the malformed file untouched for diagnosis. Never silently discard user transport metadata.

## Compatibility semantics

`SelectedProvider` and `SetSelectedProvider` remain available because Agent Center and older call sites depend on them. Their semantic meaning changes to **preferred provider for UI/onboarding**, not exclusive transport owner. `TryAutoStartPreferred()` remains legacy compatibility in this carrier; follow-up supervisor work will introduce `TryAutoStartEnabledProfiles()` and remove provider exclusivity from process lifecycle.

`CurrentRegistrationIdentity()` remains the legacy selected-provider identity. New registry APIs provide per-profile registration acknowledgement and identity. Follow-up UI work will consume those APIs.

## Public API surface

`McpTransportProfileRegistry` exposes bounded internal methods:

- `IReadOnlyList<McpTransportProfile> LoadProfiles()`
- `McpTransportProfile EnsureLegacyDefaultProfile()`
- `McpTransportProfile UpsertProfile(McpTransportProfile profile)`
- `bool RemoveProfile(string profileId)` with refusal to remove the last enabled/legacy recovery profile unless explicitly disabled first
- `void SetRegistrationAcknowledged(string profileId, string registrationIdentity)`
- `bool IsRegistrationAcknowledged(string profileId, string registrationIdentity)`
- `string StatusJson()` returning only IDs, provider names, booleans, sanitized display names, and sanitized registry errors

Mutation methods use one process-local lock and atomic file replacement.

## Error handling

- Invalid profile IDs/provider values/control characters/overlong display names fail closed before persistence.
- Duplicate IDs are rejected.
- Unknown schema versions are not overwritten.
- Persistence errors leave the previous registry file intact.
- Status output never emits filesystem paths or credentials.

## Testing

Add `scripts/preflight-mcp-multi-transport-registry.py` as a source/model guard. It must first fail against the singleton baseline, then pass only when:

- registry source exists with schema/version/atomic persistence and secret-free contract;
- compatibility `SelectedProvider` remains present but comments/docs define it as UI preference;
- migration preserves legacy provider selection;
- registry model permits at least two enabled profiles with different providers and at least two OpenAI profile IDs;
- no production registry source references API key/bearer/writer-token persistence;
- `McpCadMutationCoordinator` is not modified by this carrier.

Hosted CI proves source/build behavior only. Concurrent live tunnel processes and same-DWG stress remain follow-up LOCAL_ONLY qualification.