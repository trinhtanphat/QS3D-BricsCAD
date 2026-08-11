# Work claim — Physical opening empty target fail-closed

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:32:00+07:00`
- Baseline main SHA: `fbb7fae24e2fb2086715aba071aa8946e88e47ad`
- Priority: evidence-driven remote-safe Core regression hardening

## Reason

`PhysicalOpeningCutTargetStateCodec.TryRead()` rejects empty serialized target segments and `Normalize()` already rejects duplicate/overlong target IDs, but `Normalize()` silently skips null or whitespace target IDs supplied by callers. Consequently `Write()` and `Resolve()` can silently operate on a smaller target set than the caller supplied, which is unsafe for persisted physical-cut ownership state.

## Reserved scope

Make caller-supplied physical opening target collections fail closed on null/whitespace IDs instead of silently dropping them. Preserve trimming, case-insensitive duplicate detection, deterministic ordering, target-count/length limits, persisted encoding, category/host validation, and valid target behavior. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Services/PhysicalOpeningCutTargetStateCodec.cs`
- `tests/QS3D.Core.SmokeTests/PhysicalOpeningCutTargetEmptyIdSmoke.cs`
- this claim file

## Excluded scope

- No changes to Direct Draw commands, CAD boolean cutting, opening/door host assignment, geometry planners, UI, or BricsCAD V25 runtime.
- No changes to Base64 encoding or existing duplicate/length limits.
- No GitHub Actions dispatch.

## Validation plan

- Assert `Normalize()` rejects a mixed valid + whitespace target collection rather than returning a shortened list.
- Assert `Write()` with a blank target fails before changing an existing target-state property.
- Preserve trimmed, case-insensitive duplicate rejection and deterministic valid ordering.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent Direct Draw Opening bootstrap work is already closed and targeted host-command lifecycle surfaces. No current claim or recent commit was found for empty-ID handling in `PhysicalOpeningCutTargetStateCodec`; this scope stays inside the Core codec and a dedicated smoke.

## Completion condition

Current `main` rejects empty caller-supplied physical opening target IDs, retains valid codec behavior, includes focused regression coverage, and this claim is marked `COMPLETED`.
