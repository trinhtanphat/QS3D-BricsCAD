# Agent work claim — issue #3495

- Status: ACTIVE
- Lane-Key: issue-3495
- Agent/session: gpt56sol
- Baseline main: `9303fb34109e0b5859d8fc2ff1122afdc3cefa83`
- Canonical branch: `agent/gpt56sol/3495-deferred-local-validation`
- Canonical PR: not opened in this execution; owner requested commit + push and leave the branch for later pickup
- Supersedes: none

## Scope

Document the owner-approved source-first workflow for work with a licensed/local acceptance tail:

1. remote/source agent completes safe source/docs/tests/guards;
2. available source/static/build/CI validation runs;
3. coherent commit is pushed to the canonical branch;
4. unavailable licensed runtime remains `PENDING_LOCAL` / `PENDING_LOCAL_AGENT` rather than blocking source work;
5. when local agents are available they sync Git, check out the exact intended SHA, run the linked local matrix, and record sanitized exact-SHA evidence;
6. source/cloud evidence never manufactures `LOCAL_PASS`.

## Files

- `docs/AGENT-RUNTIME-CONTRACT.md`
- `docs/DEFERRED-LOCAL-VALIDATION.md`
- `docs/agent-work-claims/issue-3495-gpt56sol.md`

## Exclusions

- no source-code/runtime implementation changes;
- no CI workflow/script changes;
- no direct `main` write;
- no licensed BricsCAD runtime claim;
- no merge to `main` in this execution unless the owner changes the explicit stop instruction.

## Validation plan

Push the exact branch commit and observe the repository-selected shared branch CI if triggered. The documentation change itself does not require licensed BricsCAD execution.