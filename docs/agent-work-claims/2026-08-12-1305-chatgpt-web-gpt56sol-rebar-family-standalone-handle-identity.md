# Work claim — Rebar standalone numeric handle identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-rebar-family-standalone-handle-identity`
- Registered: `2026-08-12T13:05:00+07:00`
- Scope refined: `2026-08-12T13:10:00+07:00`
- Completed: `2026-08-12T13:16:00+07:00`
- Baseline main SHA: `6d3eb76a2c08216a58284db524338e5172de2c1e`
- Priority: P0 — standalone longitudinal/shape and Tie Rebar health must use the same numeric handle identity as shared ownership.
- Task Key: `CORE-REBAR-STANDALONE-HANDLE-IDENTITY`

## Confirmed defect

The shared ownership policy canonicalizes valid positive CAD-hex identities, so `A` and `0A` refer to the same ownership identity. `GeneratedRebarHealthService` and `GeneratedTieRebarHealthService` still hashed/compared provider-valid handles by trimmed text. Because both `A` and `0A` pass the providers' existing hexadecimal validity check, a persisted list could contain aliases of the same CAD object without a duplicate issue, inflate the valid-handle count, and use the wrong identity for local ownership/source/live checks.

The same pattern was confirmed in Beam Stirrup and mesh providers during audit, but those files were released from this atomic lane for separate follow-up claims.

## Completed implementation

- Original claim commit: `a17a4c8d0a73b462ec1ece6a5552cd7f1a7d31bc`.
- Scope-refinement commit: `14412456376a50d96caaa4ef9f9e29228a41a581`.
- Generic/Shape Rebar source commit: `abeeea360042b11402b942166a67fbe06650eef7`.
- Tie Rebar source commit: `118a84579c70456448df7326ec14975c910163cf`.
- Smoke commit: `fa1292a231e668f52a5911bd819131c4327321d4`.
- PR #912 squash merge: `580de6555d87bbb73512813513a86eb96a683022`.
- Merged Generic/Shape Rebar blob: `9c9570c102ea65ae8bd0f79d12cb0deb4fabc8bd`.
- Merged Tie Rebar blob: `804e5d44e517b924e8d7ea4e6cde3eabfc492e03`.
- Merged smoke blob: `e6dcfcb7f0de3c3a2677720f41d7e210e3392c8c`.

## Final contract

- Once a provider handle token has passed its existing hexadecimal validity rule, duplicate/local ownership/source/live checks use `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)`.
- `A` and `0A` are one logical CAD object for standalone Generic/Shape Rebar and Tie Rebar duplicate/conflict checks.
- Existing invalid-token behavior remains unchanged, including current treatment of optional `0x` text in these providers.
- Existing whitespace canonicality diagnostics remain unchanged.
- Count checks use the count of unique valid numeric identities, preventing alias inflation.
- Beam Stirrup and Slab/Wall/Foundation Mesh standalone providers remain separate follow-up scope.

No GitHub Actions were dispatched. No full local .NET build PASS, executable smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed for this lane.
