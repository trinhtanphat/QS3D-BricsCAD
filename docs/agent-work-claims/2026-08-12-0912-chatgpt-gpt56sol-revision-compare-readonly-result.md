# Agent Work Claim — RevisionService Compare readonly result

- Agent: `chatgpt-gpt56sol-revision-compare-readonly-result`
- Owner: OpenAI ChatGPT
- Status: `ACTIVE`
- Registered: 2026-08-12 09:12 +07:00
- Baseline main SHA observed: `9035da9b36a11e5d6d6673bbddc467f6c4a503e2`
- Task key: `CORE-REVISION-COMPARE-READONLY-RESULT`

## Confirmed defect

`RevisionService.Compare(...)` declares `IReadOnlyList<RevisionDelta>` but returns its mutable backing `List<RevisionDelta>` directly. A caller can cast the returned object to `IList<RevisionDelta>` and add/remove deltas despite the readonly API contract. The same defect class was just fixed in `QuantityRevisionReport.Build(...)` / `Summarize(...)` by returning `AsReadOnly()`.

## Reserved scope

- `src/QS3D.Core/Revisions/RevisionService.cs`
- one focused Core smoke source for outer result immutability
- this claim file

## Excluded scope

- Quantity Revision Report/Review lanes
- mutation of `RevisionDelta` or `RevisionFieldDelta` DTOs themselves (deep immutability)
- revision snapshot persistence/backup/schema
- Regeneration Preview behavior
- Actions/build/release/BricsCAD runtime

## Plan

1. Re-fetch moving `main` and confirm `RevisionService.Compare(...)` still returns the mutable list directly.
2. Return a read-only wrapper without changing comparison/order/tolerance semantics.
3. Add focused smoke coverage proving non-empty Compare results cannot be mutated through `IList<RevisionDelta>` while contents remain correct.
4. Review exact moving-main diff, squash-merge with expected head SHA, read back source/test, then close the claim.

No GitHub Actions/build/release is authorized by this lane. No BricsCAD V25/V26 runtime PASS will be claimed remotely.
