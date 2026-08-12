# Work claim — Quantity revision summary key canonicality

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:41:00+07:00`
- Completed: `2026-08-12T09:48:00+07:00`
- Baseline main SHA: `b3d1ac9c07b368fb153701c09865074824a0926d`
- Priority: evidence-driven remote-safe revision reporting integrity

## Reason

`QuantityRevisionReport.Build` explicitly rejects non-canonical padded quantity keys, but public `Summarize(IEnumerable<QuantityRevisionRow>)` accepted mutable rows and grouped any nonblank `QuantityName` verbatim. As a result, `NetVolumeM3` and ` NetVolumeM3 ` could be emitted as separate semantic summaries instead of failing closed at the summary boundary.

## Changed scope

Nonblank summary quantity names must now be canonical without surrounding whitespace before grouping. Existing behavior that blank quantity-name rows are ignored, case-insensitive grouping, finite/overflow-safe accumulation, Build semantics and readonly result wrappers remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Revisions/QuantityRevisionReport.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRevisionSummaryKeyCanonicalitySmoke.cs`
- this claim file

## Completion

- Claim commit on main: `2350159fd443279147c69cafa1a2dd30d996a020`.
- Implementation commit on stable integration branch: `95a7c42960328380a58eb222648c721758202c1a` — validate canonical nonblank summary quantity names before grouping.
- Regression commit on stable integration branch: `69cd84b2f1b46fd0b3fa7f5463bd91736ca30584` — reject padded quantity names and preserve blank-row skipping plus case-insensitive grouping/accumulation.
- Direct main contents update was attempted twice after exact source re-fetch and returned non-forced `409` branch-head races while the target blob itself remained unchanged; the lane was therefore moved to a stable branch for merge back to main instead of forcing the ref.
- Validation actually performed:
  - exact-HEAD source fetch confirmed the candidate was still present after the branch races;
  - dedicated smoke source added on the stable branch;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25/V26 runtime PASS is claimed.

## Coordination

Earlier quantity-revision readonly-result work was already completed and is disjoint. This claim was registered on main before the source edit. The stable branch was created only to avoid unrelated main-head races without force-pushing.

## Completion condition

Satisfied pending integration: the stable branch fails closed on padded nonblank quantity summary keys and contains focused regression coverage; merge the branch to `main` and then re-fetch main before considering the lane fully integrated.
