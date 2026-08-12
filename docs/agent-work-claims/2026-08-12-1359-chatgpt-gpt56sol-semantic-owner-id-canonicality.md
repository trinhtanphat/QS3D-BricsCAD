# Work claim — Semantic documentation owner-ID canonicality regression

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-semantic-owner-id-canonicality-20260812-1359`
- Registered: `2026-08-12T13:59:00+07:00`
- Baseline main SHA: `c3856e7be20714a74ada42ed7cdc09f6b2703771`
- Priority: P1 semantic documentation fail-closed parity
- Task Key: `CORE-SEMANTIC-DOC-OWNER-ID-CANONICALITY`
- Source fix: `27e86677e9a895c4a5ea625cfb22e22e0548daa9`
- Regression: `930a1fed0d240f8312f496eb82300bc22bda52aa`

## Confirmed defect

`SemanticTagRenderContext.Add(...)` trimmed IDs of project-owned elements/Families/Floors/Zones before indexing. The pre-index renderer compared owner IDs and references directly with `StringComparison.OrdinalIgnoreCase`, so an owner ID such as `" F1 "` did not satisfy canonical reference `"F1"`. The indexing optimization therefore introduced a semantic regression: malformed in-memory owner IDs could be silently normalized and rendered as canonical owners. Later reference hardening rejected non-canonical reference IDs but left owner-side trimming intact.

## Completed contract

- Preserved case-insensitive ID matching.
- Preserved fail-closed duplicate detection and lazy Family/Floor/Zone indexing.
- Project-owned IDs are now trimmed only to validate canonical spelling; surrounding whitespace fails closed instead of changing identity.
- Preserved canonical reference validation and generated/native property protections.
- Added focused Family/Floor/Zone owner-ID regression coverage to `SemanticTagRendererSmoke`.

## Validation boundary

Source and smoke were re-read from `main` after both commits and contained the intended changes. GitHub returned no combined status checks for regression commit `930a1fed0d240f8312f496eb82300bc22bda52aa`; no GitHub Actions, full executable smoke, or licensed BricsCAD runtime PASS is claimed.