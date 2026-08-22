# Work claim — semantic view catalog read-only result

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-view-catalog-readonly`
- Registered: `2026-08-12T09:31:00+07:00`
- Completed: `2026-08-12T09:35:00+07:00`
- Baseline main SHA: `cd398601829106d2e4c1dc9b90398cf21297b14a`
- Claim commit: `63ac503d6ef73cb4aa88b8c1c2cf1c4628356704`
- Implementation commit: `304ebd6cd8eefd9d64e68044d5e877af5fee6bc7`
- Regression-test commit: `13219765d9940c9ede67cdc554cd24f6216bd04e`
- Final observed main during verification: `13219765d9940c9ede67cdc554cd24f6216bd04e`
- Priority: `Documentation/Core API integrity discovered during requested continue-all review.`

## Confirmed defect

`SemanticViewPlanner.BuildCatalog()` advertised `IReadOnlyList<SemanticViewPlan>` but returned a sorted array. Arrays implement `IList<T>` and permit indexed replacement, so callers could replace published catalog entries after duplicate identity validation and deterministic ordering.

## Implemented

- `BuildCatalog()` now sorts into a list and publishes it through `AsReadOnly()`.
- View name/ID ordering is unchanged.
- Filtering, category/reference semantics, duplicate ID/name validation, bounded materialization, plan contents, and single-view `Build()` behavior are unchanged.

## Regression coverage

New `SemanticViewCatalogReadonlySmoke` proves:

- a two-view catalog retains deterministic `Alpha`, `Zulu` name ordering;
- indexed replacement through `IList<SemanticViewPlan>` throws `NotSupportedException`;
- `Add` and `Remove` also throw `NotSupportedException`;
- rejected mutation attempts preserve count and stable view IDs.

This specifically catches the previous array implementation because indexed replacement on an array-backed `IList<T>` succeeds.

## Changed surfaces

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs` — only `BuildCatalog` returned collection wrapper
- `tests/QS3D.Core.SmokeTests/SemanticViewCatalogReadonlySmoke.cs`

## Coordination

Earlier semantic-view catalog work bounded lazy definition enumeration. This completed follow-up did not modify bounds, filters, category/reference semantics, or project element indexing.

## Validation performed

- Re-read current main after source and regression publication.
- Current source shows `ToList().AsReadOnly()` on the `BuildCatalog()` result.
- Current focused smoke remains present and checks indexed replacement/Add/Remove rejection plus deterministic order.
- Source write used the exact current source blob SHA; no force-push or concurrent-work overwrite occurred.
- No GitHub Actions workflow was dispatched or rerun.
- No local .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote source-only lane.

## Outcome

Semantic view catalog results now satisfy their advertised read-only outer collection contract. The lane is closed with no dangling ownership.
