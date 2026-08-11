# Work claim — Quantity Insight modeless document/row affinity hardening

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-insight-affinity-hardening`
- Registered: `2026-08-11T20:56:00+07:00`
- Completed: `2026-08-11T20:59:00+07:00`
- Baseline main SHA: `12159590d88afd2127f49404d254184883e4f0b5`
- Priority: P1

## Reserved scope

- Audit and harden the newly added docked `QuantityInsightPanel` so modeless rows cannot locate/select CAD objects after the active DWG/project or live quantity row has changed.
- Preserve the existing read-only quantity tree, selection highlight, native Handle selection + `QS3DZOOMSELECTED`, and the completed `QS3DBQ` detail/reveal lane.
- Add deterministic source/preflight coverage for the document-affinity and stale-row fail-closed contract.

## Implemented

- `a13ff0bbc79d3c6bd6a4e6d8f1bdd33f45564a3e` — binds each quantity insight refresh to its exact BricsCAD `Document`, ProjectId and drawing fingerprint; stores the displayed grouped `QuantityReportRow` snapshot per tree item; rejects cross-DWG/project locate; rebuilds current grouped rows read-only and requires one exact semantic identity plus unchanged quantity/provenance before resolving Handles.
- Selection highlighting now also refuses a project/document mismatch instead of resolving the stale tree against another project.
- `39259a39e4bba3c2fe2e11ae72d3bebc4912aa19` — adds `scripts/preflight-quantity-insight-affinity.py` guarding DWG -> project -> live-row -> Handle -> native CAD selection ordering and forbidding creating/mutating project binds or direct stale item-ID Handle resolution.

## Source validation

- Re-fetched `QuantityInsightPanel.xaml.cs` after the implementation commit and confirmed the current `main` blob retains the affinity guards, full live-row equality check, and current-row Handle resolution.
- The new path remains read-only: it uses `ProjectContextCoordinator.TryGetReadOnly(...)` and `ProjectQuantityReportBuilder.Group(project)`; it does not use `GetOrCreate` or `ExistingProjectMutationContext.Require`.
- Existing `QS3DZOOMSELECTED` behavior is preserved after successful current-row revalidation.
- GitHub exposes no combined status checks for the preflight commit, and this lane did not dispatch GitHub Actions.

## LOCAL_ONLY disposition

- Native BricsCAD V25 modeless mouse selection, implied-selection highlight and viewport zoom still require the existing local interactive qualification. No remote runtime PASS is claimed.
- No duplicate local inbox item was added because the repository already retains the modeless/private-DWG BQ interaction matrix under the existing local qualification queue.

## Completion evidence

- Quantity Insight locate now fails closed when the user switches DWG, the canonical project identity changes, the displayed grouped row disappears/becomes ambiguous, or any compared quantity/provenance field changes.
- Only a revalidated live current row reaches `SourceHandleResolver.Resolve(...)`, `CadHandleService.Select(...)`, and `QS3DZOOMSELECTED`.
- Implementation/test tip for this lane: `39259a39e4bba3c2fe2e11ae72d3bebc4912aa19`; concurrent main commits were preserved and no force push was used.
