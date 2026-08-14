# Work claim — persistence stamp nested dirty detection

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260814-core-persistence-100`
- Registered: `2026-08-14T20:58:00+07:00`
- Baseline main SHA: `b7ca334e862d39f420496ffbab4a2a2279e493ab`
- Implementation branch: `agent/chatgpt-gpt56sol/persistence-stamp-nested-dirty-detection`
- Integration batch: `integration/20260814-core-persistence-data-model-100`
- Priority: close a verified Core/persistence dirty-state correctness gap for the owner-requested Core / persistence / data-model hardening

## Reserved scope

Make `ProjectPersistenceStamp` detect direct mutations of persisted nested Core domain content even when `ProjectState.ChangeVersion` does not advance. The comparison must be deterministic and correctness-preserving rather than relying only on a collision-prone hash.

## Expected surfaces

- `src/QS3D.Core/Persistence/ProjectPersistenceStamp.cs`
- persisted family/element value surfaces read by the stamp, without changing their ownership APIs unless proven necessary
- `tests/QS3D.Core.SmokeTests/*` regression coverage for nested dirty detection
- smoke-test registration only if the existing test harness requires it

## Excluded scope

- `ProjectElement` constructor relation canonicality already owned/integrated by the neighboring constructor-relation lane
- QSDB negative-quantity integrity already owned/integrated by the neighboring persistence lane
- DWG/native BricsCAD runtime, UI, release naming, dispatcher/release workflow work
- unrelated domain or persistence refactors

## Validation plan

- Prove an unchanged captured project remains clean.
- Prove a direct persisted family mutation after capture requires save even if `ChangeVersion` is unchanged.
- Prove a direct persisted element mutation after capture requires save even if `ChangeVersion` is unchanged.
- Prove restoring nested values to the captured persisted state does not leave a false-positive dirty result.
- Preserve existing root-scalar, metadata, recovery-marker, and revision-based dirty semantics.
- Run the relevant Core smoke/regression suite on the combined integration candidate and inspect the exact diff before final landing.

## Coordination

At registration time, recent Core lanes for `ProjectElement` constructor relation canonicality and QSDB negative quantity persistence integrity had already landed/closed. This claim deliberately avoids their source ownership. A repository search at baseline found no existing claim or implementation lane owning `ProjectPersistenceStamp` nested dirty detection.

## Completion condition

The deterministic nested persisted-content dirty detector and regressions are present in the combined integration tree, required remote-safe Core validation passes, the integration candidate lands once to `main`, the resulting exact main SHA is verified, and this claim is then marked `COMPLETED`.
