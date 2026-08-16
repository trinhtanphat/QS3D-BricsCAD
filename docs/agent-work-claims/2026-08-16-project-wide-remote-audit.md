# Project-wide remote-safe audit — 2026-08-16

Status: ACTIVE
Agent/session: chatgpt-gpt56sol
Baseline: `main@b7c350191d4c560a3f4960e64a04fcb9d7f4f896`
Branch: `agent/chatgpt-gpt56sol/project-wide-audit-20260816`

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

## Exclusions
- no direct `main` writes or merge;
- no force push;
- no takeover of other agent branches/PRs;
- no licensed BricsCAD runtime claims;
- no manual GitHub Actions dispatch unless separately authorized.

## Completion rule
Each implemented defect must be independently reproducible from current source, have bounded scope, include regression evidence, and remain on this branch/PR until owner-authorized integration.
