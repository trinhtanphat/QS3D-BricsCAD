# Project-wide remote-safe audit — 2026-08-16

Status: ACTIVE
Agent/session: chatgpt-gpt56sol
Baseline: `main@db4b19531e26e0a717214a1066e94132ec728670`
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
`CurtainWallLayoutPlanner.Divide(...)` accepted a positive nonzero numerator whose division rounded to `0d`. In multi-division curtain layouts, `double.Epsilon / 2d` therefore erased a positive mullion/transom half-width and silently changed clear-panel geometry. The fix fails closed on nonzero division underflow while preserving legitimate zero internal-frame widths and ordinary layouts.

## Concurrent main reconciliation
The lane has been repeatedly reconciled with advancing `main` without force-push or takeover. The current baseline includes concurrent fixes landed by other lanes, including rebar underflow hardening, geometry numeric hardening, progress-range hardening, BCF strictness, and canonical Ribbon icon coverage. Those changes are retained; this lane overlays only the curtain-layout defect and its deterministic smoke coverage.

## Exclusions
- no direct `main` writes;
- no force push;
- no takeover of other agent branches/PRs;
- no licensed BricsCAD runtime claims;
- no manual GitHub Actions dispatch unless separately authorized.

## Validation
Candidate `0e17d08e380d843d5f7f74d508eca7491ba33949` passed automatic Shared Branch and Integration CI run `31933971181`: preflight, Core Release build, deterministic smoke, trusted V25 compile-reference validation, and BricsCAD V25 plugin build all succeeded. The branch is being reconciled onto `main@db4b19531e26e0a717214a1066e94132ec728670`; the resulting exact head requires a fresh automatic CI run before merge eligibility is claimed.
