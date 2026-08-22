# Agent work claim — issue #3495

- Status: COMPLETED
- Lane-Key: issue-3495
- Agent/session: gpt56sol
- Baseline main: `9303fb34109e0b5859d8fc2ff1122afdc3cefa83`
- Canonical branch: `agent/gpt56sol/3495-deferred-local-validation`
- Canonical PR: #3497 — MERGED
- Final source-ready head: `d78e42be2b0c76b54d14c01a094b7a9f9910379b`
- Merged main: `939e0d1dec4f436a9ec26b8b5e42e0e9c019b120`
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
- no licensed BricsCAD runtime claim.

## Validation and landing

Owner first requested commit + push only, then explicitly changed the endpoint to merge `main`. The canonical carrier was synchronized with then-current `main@65e789a406a2208bb1c2de5fe501ed333a858e72` and revalidated on exact head `d78e42be2b0c76b54d14c01a094b7a9f9910379b`.

Shared PR CI run `32558530870` / run number `12483` completed `SUCCESS`: protected `preflight` and `core` both succeeded. The candidate was current and mergeable, then PR #3497 merged through the protected PR path to `main@939e0d1dec4f436a9ec26b8b5e42e0e9c019b120`.

Licensed BricsCAD runtime evidence remains outside this documentation-only task and is not claimed here.
