# Work claim — Model Health baseline stale identity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-model-health-baseline-stale-identity`
- Registered: `2026-08-12T08:07:00+07:00`
- Last Updated: `2026-08-12T08:11:00+07:00`
- Baseline main SHA: `31944a83e766204a73ad94f023561d3df5b5dbda`
- Priority: deterministic Core diagnostics defect found during owner-requested continue-all audit
- Task Key: `CORE-MODEL-HEALTH-BASELINE-STALE-IDENTITY`

## Confirmed defect

`ComprehensiveModelHealthService` deliberately de-duplicates `*_STALE` diagnostics by severity + code + element id, excluding mutable stale-reason message text. `ModelHealthBaselineService`, however, included `Message` in every identity key. When a stale reason changed while the same element remained stale, baseline diff therefore reported the old diagnostic as resolved and the new wording as a regression instead of one persistent issue.

## Implemented scope

- `ModelHealthBaselineService.Key(...)` now uses severity + normalized code + normalized element id for `*_STALE` issues, matching the existing comprehensive aggregation identity.
- Non-stale diagnostics remain message-sensitive, preserving the original distinction for ordinary diagnostics whose message is part of their identity.
- Focused smoke coverage proves a stale reason-message change remains one persistent issue while an ordinary warning message change remains one resolved + one new issue.
- The existing static preflight now pins both branches of the identity contract and the focused smoke tokens.

## Committed evidence

- Claim registration: `ff0030d631379071b62d69ae2e238dcd5c5ce387` — `chore(agent): claim model health baseline stale identity`
- Source fix: `caade3b8e536ac568e45cd77a707a4ca2ad20df5` — `fix(core): stabilize stale model health baseline identity`
- Focused smoke: `7af48e7bdbd78dd91b823b27d401d8efca2845f2` — `test(core): guard stale model health baseline identity`
- Static source gate: `f797d3c4e70f5db6a91ca09412d158fac867ab68` — `test(core): pin stale baseline identity source gate`
- Moving-main readback at `a767ae5e2f6838f4f5e86b5c937e681b2a0b6417` confirmed all three commits are ancestors of `main` and the source, smoke, and preflight contents remain intact after concurrent writes.

## Validation boundary

- GitHub source readback and commit ancestry were verified on current `main`.
- The static preflight contract was updated, but no executable Core smoke/preflight run is claimed from this remote session.
- No GitHub Actions, build, release, BricsCAD V25/V26 runtime, or native DWG qualification was dispatched or claimed.

## Completion condition

Satisfied: stale diagnostic reason-message drift no longer creates a false Model Health baseline regression, ordinary diagnostic message identity remains unchanged, focused regression/source-gate coverage is committed on `main`, and the lane has been read back after concurrent updates.