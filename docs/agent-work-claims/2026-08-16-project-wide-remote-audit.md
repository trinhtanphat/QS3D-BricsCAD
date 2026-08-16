# Project-wide remote-safe audit — 2026-08-16

Status: ACTIVE
Agent/session: chatgpt-gpt56sol
Baseline: `main@a4bc784e1e46d77d1e384ca851cc9fb97268608a`
Branch: `agent/chatgpt-gpt56sol/project-wide-audit-20260816-r3`
Supersedes construction branches: `agent/chatgpt-gpt56sol/project-wide-audit-20260816`, `agent/chatgpt-gpt56sol/project-wide-audit-20260816-r2`

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

## Validation
The superseded r2 candidate `090a3a0d573c20f14ef8bbfef4b105d751030c44` passed automatic shared branch CI run `31932922758` completely. This r3 branch is a clean reapplication on newer non-overlapping `main` and requires its own exact-head branch CI before PR creation.
