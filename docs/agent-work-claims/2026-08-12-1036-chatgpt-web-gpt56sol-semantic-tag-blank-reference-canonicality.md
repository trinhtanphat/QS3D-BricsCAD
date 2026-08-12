# Work claim — Semantic tag blank-reference canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-tag-blank-reference-canonicality`
- Registered: `2026-08-12T10:36:00+07:00`
- Baseline main SHA: `05f0a28c5ff7da622da86ce729d48271057f2460`
- Priority: P1 — semantic tag rendering currently lets whitespace-only relation ids bypass the completed fail-closed reference-canonicality contract.

## Confirmed defect

Commit `310c238cfaf17d623028c4c540a3df0e37649016` made semantic tag Family/Floor/Zone references fail closed when their raw token is not canonical, and regression `ae8f98a96b400d908acfde2629274fafcc2a6eff` pins whitespace-padded references as invalid. Current `SemanticTagRenderContext.ResolveFamily`, `ResolveFloor`, and `ResolveZone` still return an empty rendered value for any `string.IsNullOrWhiteSpace(...)` token before `ResolveReference(...)` runs. A raw relation id containing only spaces or tabs therefore bypasses the canonicality guard and renders as if it were the canonical unassigned empty string.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticTagRenderContext.cs`
- `tests/QS3D.Core.SmokeTests/SemanticTagRendererSmoke.cs`
- this claim file for close-out

## Intended contract

- Preserve the canonical empty string as an unassigned Family/Floor/Zone reference that renders empty.
- Reject whitespace-only non-empty Family/Floor/Zone relation tokens as non-canonical instead of normalizing them to absence.
- Preserve the completed behavior for valid canonical references, padded nonblank references, missing/ambiguous references, exact project-owned element identity, and rendering output.

## Excluded scope

- No Reporting schedule/identity-guard changes; LOCAL-003 currently owns adjacent schedule fixture reconciliation.
- No Project Browser, persistence, project-domain mutation, BricsCAD UI/runtime, or other documentation planner changes.
- No GitHub Actions dispatch and no licensed BricsCAD runtime qualification claim.

## Validation plan

- Refresh `main` and re-fetch the exact context/smoke blobs before each write.
- Narrow the unassigned fast path to the exact empty string; route every non-empty token through the existing canonicality guard.
- Extend the existing semantic-tag canonicality smoke with whitespace-only Family/Floor/Zone cases plus canonical empty-reference sanity coverage.
- Re-read integrated source/test, close this claim with exact SHAs, and verify final main ancestry.
