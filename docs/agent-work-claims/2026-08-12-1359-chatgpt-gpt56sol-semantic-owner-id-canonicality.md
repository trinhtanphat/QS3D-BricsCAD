# Work claim — Semantic documentation owner-ID canonicality regression

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-semantic-owner-id-canonicality-20260812-1359`
- Registered: `2026-08-12T13:59:00+07:00`
- Baseline main SHA: `c3856e7be20714a74ada42ed7cdc09f6b2703771`
- Priority: P1 semantic documentation fail-closed parity
- Task Key: `CORE-SEMANTIC-DOC-OWNER-ID-CANONICALITY`

## Confirmed defect

`SemanticTagRenderContext.Add(...)` trims IDs of project-owned elements/Families/Floors/Zones before indexing. The pre-index renderer compared owner IDs and references directly with `StringComparison.OrdinalIgnoreCase`, so an owner ID such as `" F1 "` did not satisfy canonical reference `"F1"`. The indexing optimization therefore introduced a semantic regression: malformed in-memory owner IDs can be silently normalized and rendered as canonical owners. Later reference hardening rejects non-canonical reference IDs but left owner-side trimming intact.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticTagRenderContext.cs`
- focused Core semantic documentation regression coverage
- this claim file

## Intended contract

- Preserve case-insensitive ID matching.
- Preserve fail-closed duplicate detection and lazy Family/Floor/Zone indexing.
- Reject non-canonical project-owned IDs rather than trimming them into canonical identity.
- Preserve canonical reference validation and all generated/native property protections.
- Cover Family/Floor/Zone owner IDs; do not broaden into unrelated persistence normalization.

## Validation boundary

Focused Core regression/source readback and available repository checks only. No GitHub Actions, full executable smoke, or licensed BricsCAD runtime PASS will be claimed unless actually observed.