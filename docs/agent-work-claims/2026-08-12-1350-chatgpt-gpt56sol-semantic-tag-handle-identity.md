# Work claim — Semantic Tag standalone numeric handle identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-semantic-tag-handle-identity-20260812-1350`
- Registered: `2026-08-12T13:50:00+07:00`
- Baseline main SHA: `9f70598a5683394e55dcd245a23a567870aec024`
- Priority: P1 generated health identity parity
- Task Key: `CORE-SEMANTIC-TAG-STANDALONE-HANDLE-IDENTITY`

## Confirmed defect

`GeneratedSemanticTagHealthService.ParseHandles(...)` validates hexadecimal tokens but stores trimmed raw text in its duplicate set, while `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` defines shared logical CAD-handle identity. Numeric aliases such as `A` and `0A` can therefore evade `SEMANTIC_TAG_HANDLE_DUPLICATE`. The subsequent `SourceHandles` overlap check also compares raw trimmed spelling and can miss the same logical handle under an alias.

Earlier Semantic Tag handle canonicality/empty-token lanes are completed. Current open-PR/history checks found no Semantic Tag numeric-handle identity lane.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs`
- `tests/QS3D.Core.SmokeTests/SemanticTagHandleIdentitySmoke.cs`
- this claim file

## Intended contract

- Preserve existing hexadecimal validity and whitespace diagnostics.
- Normalize only valid generated handles before duplicate-set insertion.
- Compare `SourceHandles` using the same shared logical identity.
- Preserve persisted spelling, existing prefixed-hex validity behavior, owner/template/render/numeric-position/rotation behavior.

## Validation boundary

Focused auto-registered Core smoke + source/readback only. No GitHub Actions, full executable smoke, or licensed BricsCAD runtime PASS will be claimed unless actually executed.
