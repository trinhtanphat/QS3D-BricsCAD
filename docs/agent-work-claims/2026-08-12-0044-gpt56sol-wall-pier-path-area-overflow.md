# Work claim — Wall-pier path area overflow

- Status: `COMPLETED_NO_CHANGE`
- Agent: `chatgpt-web-gpt56sol-wall-pier-path-area-overflow-20260812-0044`
- Registered: `2026-08-12T00:44:00+07:00`
- Baseline main SHA: `ad4f2f304fc449ba7ce59b5b904675a68d1fdc48`
- Priority: evidence-driven Core numeric hardening during owner-requested `continue all`

## Reserved scope

Audit whether `WallPierPathProfilePlanner` needs an independent scale-safe footprint-area fix.

## Finding

The private `PolygonArea` helper does duplicate raw determinant arithmetic, but deeper call-chain validation showed the proposed failure is not independently reachable through `WallPierPathProfilePlanner.Plan`: `WallFootprintEngine.Build` computes the same footprint area earlier, using its own raw determinant path, before the wall-pier private area helper runs. A large-coordinate determinant-cancellation fixture therefore fails upstream first.

Applying a Wall-pier-only refactor would not fix the user-visible failure and would create a source change without an independently demonstrable regression. Per the repository's evidence-driven/no-speculative-change rule, no Wall-pier source edit was made under this claim.

## Follow-up

The actual upstream defect is in `WallFootprintEngine` (`SignedAreaRelative` and determinant-based segment intersection math). That upstream scope must be claimed separately before any source edit.

## Validation performed

- Re-fetched `WallPierPathProfilePlanner.PolygonArea` and confirmed the duplicate raw cross path.
- Re-fetched `WallFootprintEngine.Build`/`SignedAreaRelative` and confirmed the same determinant class is evaluated earlier on the generated footprint.
- No source/test files changed under this claim.
- No GitHub Actions dispatched and no BricsCAD V25 runtime/build/NETLOAD PASS claimed.

## Completion

Claim closed with no source change because the initially suspected Wall-pier-only defect is upstream-dominated; the correct fix surface is `WallFootprintEngine`.
