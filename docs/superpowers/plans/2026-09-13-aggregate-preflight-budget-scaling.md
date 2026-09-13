# Aggregate Preflight Budget Scaling

Issue: #6799
Base: `main` at `cd9e6420d83cef12db3a27aed7465bf52c7bdc2c`
Carrier: `agent/gpt56sol-20260913-aggregate-budget-scale/issue-6799-aggregate-budget-scale`

## Root cause

The aggregate runner retained a fixed 15-minute wall-clock cap created near a 1,024-gate repository scale while the admitted inventory grew to roughly 1.9k gates. Healthy late gates therefore received only the residual global budget and failed at different positions depending on host load.

## Contract

- Keep the historical 900-second aggregate baseline through 1,024 admitted gates.
- Add 0.5 seconds for each admitted gate above 1,024.
- Keep an explicit 25-minute hard ceiling below the enclosing 30-minute hosted preflight job.
- Keep the existing 180-second child timeout ceiling.
- Preserve the public `remaining_child_timeout(started_at, now=None)` compatibility contract.
- Preserve bounded output/input handling and process-tree cleanup.
- Do not change Basic Drawing, Update Center, runtime, MCP, installer, or release behavior.

## TDD and verification

1. Extend `preflight-preflight-all-aggregate-timeout.py` and verify RED on unmodified runner behavior.
2. Implement the bounded gate-count budget derivation and compatibility wrapper.
3. Verify focused timeout regression, discovery compatibility, generic preflight, and `git diff --check`.
4. Run the complete aggregate preflight on the exact branch tree.
5. Only after fresh local GREEN: commit, push, open PR, require fresh protected preflight/core GREEN, merge, verify main, close #6799, and clean owned worktrees when safe.
