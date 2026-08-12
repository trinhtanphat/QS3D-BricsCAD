# Work claim — Physical opening empty target fail-closed

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:32:00+07:00`
- Baseline main SHA: `fbb7fae24e2fb2086715aba071aa8946e88e47ad`
- Priority: evidence-driven remote-safe Core regression hardening

## Reason

`PhysicalOpeningCutTargetStateCodec.TryRead()` rejected empty serialized target segments and `Normalize()` already rejected duplicate/overlong target IDs, but `Normalize()` silently skipped null or whitespace target IDs supplied by callers. Consequently `Write()` and `Resolve()` could silently operate on a smaller target set than the caller supplied, which was unsafe for persisted physical-cut ownership state.

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

Recent Direct Draw Opening bootstrap work is already closed and targeted host-command lifecycle surfaces. No current claim or recent commit was found for empty-ID handling in `PhysicalOpeningCutTargetStateCodec`; this scope stayed inside the Core codec and a dedicated smoke.

## Completion

- Implementation commits:
  - `044c84903fab09428533bf526bb9e6e99bb3437b` — make `Normalize()` reject null/whitespace target entries instead of silently dropping them.
  - `51e95e000e5f2113fd833c7b974031536aab77bd` — add regression coverage for mixed blank input, failed-write state preservation, valid trimming/order, and duplicate rejection.
- Final observed `main` before claim close: `c6a522afdf004877fc3f154d19596cf730625d57`.
- Validation actually performed:
  - re-fetched `PhysicalOpeningCutTargetStateCodec.cs` from current `main` and confirmed the empty-ID guard runs before trimming/encoding/writes;
  - re-fetched the new smoke and confirmed a failed `Write()` leaves the existing serialized target-state unchanged;
  - confirmed valid trimming, deterministic ordering, and case-insensitive duplicate rejection remain covered;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is a CAD-independent Core target-state validation hardening change.

## Completion condition

Satisfied: current `main` rejects empty caller-supplied physical opening target IDs, retains valid codec behavior, includes focused regression coverage, and this claim is released as `COMPLETED`.
