# Work claim — Family usage badge parity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-usage-badge`
- Registered: `2026-08-11T22:20:00+07:00`
- Completed: `2026-08-11T22:28:00+07:00`
- Baseline main SHA: `25fe5508dc49089fd29112c4fa4e998def3d6444`
- Priority: P1 screenshot/reference parity

## Implemented

- `69478a0e1e9f8371746647a137c700718ec68226` — added `FamilyUsageTextConverter`, a read-only WPF `IMultiValueConverter` that receives the current row `ProjectFamily`, obtains only the existing active project through `ProjectContextCoordinator.TryGetReadOnly(...)`, verifies that the Family object is the current project's canonical Family, counts semantic `ProjectElement` rows by `FamilyId`, and returns `N cấu kiện`. Missing/stale project/family state returns `—`.
- `192c2501e41585f9678c7cb61b68ab538a0fc786` — added isolated `WorkspacePanel.FamilyUsageBadge.cs`. It registers a class-level Loaded hook, hooks only `FamilyList` container/layout generation, walks generated Family row visuals, identifies the original `Properties.Count` TextBlock binding, and replaces that one badge with a `MultiBinding` using the converter plus Workspace `Status` as a passive invalidation signal.
- Upgraded Family row TextBlocks are marked through an attached property so repeated load/layout events remain idempotent. Property-panel count badges and unrelated TextBlocks are not modified.
- `WorkspacePanel.xaml`, `WorkspaceViewModel.cs`, `WorkspacePanel.xaml.cs`, Core Family/domain and persistence files were deliberately left unchanged, preserving concurrent Workspace and semantic-model winners.
- `591105bae02209712e61f147c7a59928d32aef53` plus token correction `333a79921ba80237de09682c0ae4e3d0594d917f` — added/fixed `scripts/preflight-family-usage-badge.py`, covering read-only project acquisition, canonical Family ownership, semantic element count, idempotent FamilyList-only binding upgrade, MultiBinding invalidation, preserved Family actions/search/selection and preserved property-panel count.

## Source validation

- Re-fetched the converter, Family usage partial and current Workspace XAML/VM contracts from `main`. The Family list still binds canonical `ProjectFamily` rows, still exposes Add/Delete/Bóc chọn/Vẽ 3D/search/selection behavior, and the original property-count badge remains the uniquely targeted source binding for runtime upgrade.
- `compare_commits` from `69478a0e1e9f8371746647a137c700718ec68226` to current `main` reports `behind_by: 0` with that implementation as merge base while preserving 58 concurrent commits. No force push/reset was used.
- GitHub exposes no combined status checks for `333a79921ba80237de09682c0ae4e3d0594d917f`; no GitHub Actions were dispatched.
- Remote validation is source/preflight inspection only. No licensed BricsCAD runtime PASS is claimed.

## LOCAL_ONLY disposition

- Physical FamilyList virtualization/recycling, click selection and live badge refresh in licensed BricsCAD V25 remain within the existing Workspace/palette runtime qualification boundary. No duplicate local inbox item was added.

## Completion evidence

The screenshot-style Family badge now reports actual semantic usage (`N cấu kiện`) rather than the number of Family property definitions, without changing Family objects, selection semantics or project/CAD state.
