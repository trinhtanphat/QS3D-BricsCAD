# Work claim — Project Browser query Family/category integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-browser-family-category-integrity`
- Registered: `2026-08-12T00:53:00+07:00`
- Completed: `2026-08-12T00:57:00+07:00`
- Baseline main SHA: `5c3e297a02dab395247ed2bb880b6299f9495da3`
- Reservation commit: `23cbb3b45eaf37fa67aee7c121ebbcb06fa77410`
- Priority: P1 — fail closed before semantic search uses an incompatible Family relation.

## Defect fixed

`ProjectBrowserQueryPlanner` explicitly indexes Family definitions and searches Family ID/name for each element, and already failed closed for missing Family references. However `ValidateElementReferences(...)` only checked that the referenced Family ID existed; it did not verify that `family.Category == element.Category`. A corrupt Beam → Column-Family relation could therefore participate in browser Family-name search and produce misleading semantic query results instead of being rejected.

The filtered query boundary now resolves the referenced Family and rejects a category mismatch before Floor/Zone checks and before any filter/search result is produced. This aligns the browser query/read path with semantic selection and Family mutation integrity without changing grouping or mutation behavior.

## Published commits

- `592a3773bbdd3f7395b8849ac4f18db180de905b` — `fix(browser): reject family category mismatch in queries`.
- `25434e8d5beb6c84ddefbb404f0d9df9f7dc4abd` — `test(browser): guard family category integrity`.
- `c38f582952a8721e9060a856f3a55526dd77cca2` — `test(browser): pin family category query integrity`.

## Preserved contract

- Missing Family/Floor/Zone reference checks remain unchanged.
- Valid Family-name search/filter behavior remains unchanged.
- The focused smoke injects a Beam referencing the valid Column Family and applies a filter that would otherwise exclude that corrupt Beam, proving whole-project reference validation remains fail-closed before filtering.
- No `ProjectBrowserPlanner` grouping/reference-definition bound, Family mutation, Workspace/UI or native file was modified.

## Validation notes

Current query-planner source and focused smoke were re-fetched around publication, and the dedicated static gate is committed under the repository preflight naming convention. `main` advanced concurrently in unrelated lanes and those changes were preserved through current-blob writes; no force-push or overwrite was used. This connector-only lane did not execute Core smoke or Python preflights, so no executable PASS is claimed. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.

## Completion condition

Satisfied for the remote-safe source/static contract: filtered Project Browser queries fail closed on Family/category corruption, focused regression/static coverage is on current `main`, and this reservation is released.
