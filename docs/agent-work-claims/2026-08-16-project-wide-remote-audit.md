# Project-wide remote-safe audit — 2026-08-16

Status: ACTIVE
Agent/session: chatgpt-gpt56sol
Original r4 baseline: `main@ce0a77575497062da49c5e4b82d591af434a7324`
Latest reconciled main: `main@755266dcf09a5a7d8e47cec9ad677c1cb615cdd2`
Branch: `agent/chatgpt-gpt56sol/project-wide-audit-20260816-r4`
Supersedes construction branches: `agent/chatgpt-gpt56sol/project-wide-audit-20260816`, `agent/chatgpt-gpt56sol/project-wide-audit-20260816-r2`, `agent/chatgpt-gpt56sol/project-wide-audit-20260816-r3`

## Owner request
Review the whole repository again, identify verified remote-safe defects, fix them, update regression coverage, and push the work on a dedicated branch.

## Collision boundary
This audit must not duplicate or take over currently reserved/open lanes. Existing active/open PR and issue scopes remain excluded unless the owner separately authorizes integration/review of that exact lane. If a discovered defect overlaps an existing reservation, record/skip it and move to a non-overlapping defect.

## Audit focus
- deterministic Core correctness and numeric edge cases;
- parser/serialization strictness and culture invariance;
- path/archive/input fail-closed behavior;
- stale-state/document-affinity safety in remote-verifiable source;
- deterministic regression coverage and source guards where appropriate.

## Verified defect in this lane
`CurtainWallLayoutPlanner.Divide(...)` accepted a positive nonzero input whose division rounded to `0d`. In multi-division curtain layouts, `double.Epsilon / 2d` therefore erased a positive mullion/transom half-width and silently changed clear-panel geometry. The fix fails closed on nonzero division underflow while preserving legitimate zero internal-frame widths and ordinary layouts.

## Exclusions
- no direct `main` writes or merge;
- no force push;
- no takeover of other agent branches/PRs;
- no licensed BricsCAD runtime claims;
- no manual GitHub Actions dispatch unless separately authorized.

## Validation / reconciliation
Exact r4 head `a8d7a4666e09a8765646ff972c01b418974d262e` passed automatic shared branch CI run `31933470384` completely: preflight, all discovered feature guards, Core Release build, deterministic smoke, trusted BricsCAD V25 compile-reference validation, and V25 plugin build. While that run completed, `main` advanced only through non-overlapping ColumnTieLayout scaling-underflow work (#1909). The branch is being reconciled non-force with `main@755266dcf09a5a7d8e47cec9ad677c1cb615cdd2`; the resulting merge head requires fresh automatic branch CI before PR creation.
