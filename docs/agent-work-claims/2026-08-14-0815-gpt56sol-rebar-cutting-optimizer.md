# Work claim — REB-02A deterministic rebar cutting optimizer

- Status: `COMPLETED`
- Agent: `gpt56sol-rebar-cutting-optimizer-20260814-0815`
- Baseline main SHA: `2024eb0616a5162a76aaf07dbee3e6e4cc5ca1fa`
- Priority: `REB-02` specialist rebar depth; dependency REB-01A completed first.

## Confirmed gap

The current Core rebar surface had canonical stock/cut demand plus BBS cutting lengths/quantities, but no canonical stock-bar allocation result or deterministic cutting planner. The roadmap requires REB-02 to consume actual QS3D constraints with deterministic output/tie-breaking/resource bounds and explicitly does not mandate a competitor/research algorithm.

## Implemented scope

- `src/QS3D.Core/Rebar/RebarCuttingOptimizer.cs`
  - deterministic `BestFitDecreasingV1` heuristic; this is not presented as a proof of global optimum;
  - expands at most 10,000 required pieces and fails closed above that explicit resource bound;
  - sorts by effective piece length descending, then cut identity and instance index for stable tie-breaking;
  - best-fit selection minimises remaining off-cut and preserves the earliest stock bar on ties;
  - consumes REB-01 required length + per-required-cut allowance and stock length/kerf policy;
  - explicit planned cut instances, per-stock-bar cut-operation count, kerf quantity and off-cut quantity;
  - kerf semantics: a piece requires a separating cut while a positive stock tail remains; if the final piece reaches the stock end within the one-sided fit tolerance, no final cut is charged;
  - numeric tolerance may snap only a small underfill to the stock end and never accepts any overrun;
  - aggregate procurement quantities use the REB-01 canonical procurement model.
- `tests/QS3D.Core.SmokeTests/RebarCuttingOptimizerSmoke.cs`
  - deterministic two-bar best-fit plan;
  - exact stock-end kerf semantics;
  - tail cut + off-cut accounting;
  - requirement-order independence;
  - expanded-piece resource bound;
  - allowance-induced infeasibility;
  - sub-tolerance stock overrun rejection;
  - material conservation checks for focused examples.
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
  - registers `RebarCuttingOptimizerSmoke.Run()`.

## Coordination / commits

- Claim-first: `dff2ce5b608f39688fca29d104dd35cc21dcce31`.
- Core optimizer: `5369b54011bba74d15c739a74c66cc7a482347ff`.
- Focused smoke: `d5d5766e1b6afbec640462b3a3cd31b098dfd187`.
- Smoke registration: `271ccd69d9ce4b998ff5b6dfa92a709375866a0b`.
- One-sided tolerance / overrun fix: `1e0e5ddf2df41a0df233f2155d07006544c97674`.
- Sub-tolerance overrun regression: `e40a1f0f987815c2b4bcc6ca593299491995acbe`.
- Concurrent IFC/family-preflight work landed afterward and remained in lineage; compare from `e40a1f0...` to live `eeec7895...` was ahead by 2 and behind by 0.

## Excluded scope

BBS/report/Excel projection (REB-03), persistence/schema, CAD host application, multi-stock-length purchasing strategy, remnant inventory reuse, lap/splice/anchorage rules and claims of mathematical global optimality are not part of this lane.

## Validation actually executed

- Refreshed current `main`, searched for REB-02/cutting overlap and found none before claiming.
- Claim-only commit was published before source/test writes and rechecked on remote `main`.
- Read current BBS contract: canonical cutting length already includes lap/anchor/hook allowances before BBS row creation, while stock optimisation was absent; the optimizer therefore remains downstream of canonical REB-01 rather than duplicating BBS/export math.
- Read back source/test/registration after writes and self-reviewed numeric fit semantics; the review found and fixed the initial two-sided tolerance overrun defect before completion.
- Verified `e40a1f0f987815c2b4bcc6ca593299491995acbe` remains an ancestor of later live `main` (`eeec7895f8bae3dcbbabe85588f4ee697f903f10`, ahead 2 / behind 0).
- GitHub exposes no combined status checks for the final source/test SHA. No Actions were dispatched.
- This execution environment has no `dotnet`, `csc` or `mcs`, so no executable managed smoke/build is reported as PASS. No licensed BricsCAD/native validation was executed.

## Completion condition

Satisfied for this bounded REB-02 Core/static lane: deterministic source and focused regressions are on remote `main`, REB-01/REB-02/REB-03 boundaries remain explicit, concurrent work is preserved, the claim is closed, and unavailable runtime/native gates are explicitly unclaimed.
