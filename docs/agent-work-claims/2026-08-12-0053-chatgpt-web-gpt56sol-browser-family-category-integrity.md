# Work claim — Project Browser query Family/category integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-browser-family-category-integrity`
- Registered: `2026-08-12T00:53:00+07:00`
- Baseline main SHA: `5c3e297a02dab395247ed2bb880b6299f9495da3`
- Priority: P1 — fail closed before semantic search uses an incompatible Family relation.

## Confirmed defect

`ProjectBrowserQueryPlanner` explicitly indexes Family definitions and searches Family ID/name for each element, and it already fails closed for missing Family references. However `ValidateElementReferences(...)` only checks that the referenced Family ID exists; it does not verify that `family.Category == element.Category`. A corrupt Beam → Column-Family relation can therefore participate in browser Family-name search and produce misleading semantic query results instead of being rejected.

`SemanticSelectionInspector` and Family mutation paths already enforce the category relation. This lane aligns the browser query/read boundary without changing browser grouping or mutation behavior.

## Reserved scope

- `src/QS3D.Core/Navigation/ProjectBrowserQueryPlanner.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserQueryPlannerSmoke.cs`
- `scripts/preflight-project-browser-family-category-integrity.py` (new)
- this claim file for close-out

## Intended contract

- A non-empty Family reference must resolve to a Family whose category exactly matches the semantic element category before any filtered browser result is produced.
- Missing Family/Floor/Zone reference checks remain unchanged.
- Valid Family-name search/filter behavior remains unchanged.
- The focused smoke proves an incompatible relation fails even if that element would otherwise be excluded by the chosen filter/query.

## Excluded scope

No ProjectBrowserPlanner grouping/ref-bound changes, no Family mutation, Workspace/UI/native changes, no Actions dispatch and no V25 runtime claim.

## Completion condition

Filtered Project Browser queries fail closed on Family/category corruption, focused smoke/static coverage is on current `main`, and this claim is closed with exact SHAs and truthful validation boundaries.
