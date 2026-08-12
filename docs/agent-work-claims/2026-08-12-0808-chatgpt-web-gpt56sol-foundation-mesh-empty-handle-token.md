# Work claim — Foundation mesh empty generated-handle token

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:08:00+07:00`
- Baseline main SHA: `781de50b559c1f03f6fbe9bc9193c29159291306`
- Priority: evidence-driven Core health fail-visible regression

## Reason

`GeneratedFoundationMeshHealthService.Inspect()` currently splits `GeneratedFoundationMeshHandles` with `StringSplitOptions.RemoveEmptyEntries`. Malformed persisted metadata such as `A;;B`, `;A` or `A;` therefore discards empty handle tokens before validation even though the loop explicitly treats an empty token as `INVALID_FOUNDATION_MESH_GENERATED_HANDLE`. Wall mesh has already moved to `StringSplitOptions.None` for the same contract. Foundation mesh should fail visible instead of silently normalizing malformed generated ownership metadata.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs`
- focused CAD-independent regression coverage for empty Foundation mesh generated-handle tokens
- this claim file

## Excluded scope

- No Foundation mesh generation/engineering policy changes.
- No ownership-index normalization changes beyond the inspected metadata token stream.
- No wall/slab mesh changes; those lanes are owned/completed separately.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Validation plan

- Preserve empty tokens in the inspected `GeneratedFoundationMeshHandles` stream so the existing invalid-handle branch becomes reachable.
- Cover leading, interior and trailing empty-token forms without changing valid handle/count semantics.
- Re-fetch current source before every write and avoid overlapping concurrent Foundation claims.
- Record only source/static evidence available remotely.

## Completion condition

Current `main` reports malformed empty Foundation mesh handle tokens as `INVALID_FOUNDATION_MESH_GENERATED_HANDLE`, focused regression coverage prevents `RemoveEmptyEntries` from returning, valid handle/count behavior remains unchanged, and this claim is marked `COMPLETED` with exact SHAs.
