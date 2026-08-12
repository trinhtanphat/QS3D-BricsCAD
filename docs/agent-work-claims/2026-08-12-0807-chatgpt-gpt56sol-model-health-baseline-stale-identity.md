# Work claim — Model Health baseline stale identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-model-health-baseline-stale-identity`
- Registered: `2026-08-12T08:07:00+07:00`
- Last Updated: `2026-08-12T08:07:00+07:00`
- Baseline main SHA: `31944a83e766204a73ad94f023561d3df5b5dbda`
- Priority: deterministic Core diagnostics defect found during owner-requested continue-all audit
- Task Key: `CORE-MODEL-HEALTH-BASELINE-STALE-IDENTITY`

## Confirmed defect

`ComprehensiveModelHealthService` deliberately de-duplicates `*_STALE` diagnostics by severity + code + element id, excluding mutable stale-reason message text. `ModelHealthBaselineService`, however, includes `Message` in every identity key. When a stale reason changes while the same element remains stale, baseline diff therefore reports the old diagnostic as resolved and the new wording as a regression instead of one persistent issue.

## Intended scope

- Align baseline identity for `*_STALE` codes with the existing comprehensive aggregation identity: severity + normalized code + normalized element id.
- Preserve message-sensitive identity for all non-stale diagnostics.
- Add focused smoke coverage proving stale message changes stay persistent and ordinary message changes still classify as resolved + new.
- No BricsCAD adapter/UI/runtime changes and no GitHub Actions dispatch.

## Completion condition

Source and focused regression are committed on current `main`, read back after concurrent updates, and this claim is closed with exact commit evidence.