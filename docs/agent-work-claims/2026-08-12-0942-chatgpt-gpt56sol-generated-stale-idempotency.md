# Work claim — ProjectElement generated stale idempotency

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-generated-stale-idempotency`
- Registered: `2026-08-12T09:42:00+07:00`
- Last Updated: `2026-08-12T09:42:00+07:00`
- Baseline main SHA: `2d59c7e11f156387b452e86077a23a6f0f8a8db0`
- Priority: deterministic generated-output freshness defect found during owner-requested continue-all audit
- Task Key: `CORE-PROJECT-ELEMENT-GENERATED-STALE-IDEMPOTENCY`

## Confirmed defect

`ProjectElement.MarkGeneratedGeometryStale(...)`, `MarkGeneratedCurtainFrameStale(...)`, and `MarkGeneratedCurtainPanelStale(...)` currently rewrite stale state/snapshot and `UpdatedUtc` whenever a generated output exists, even when the same output signature is already stale for the same reason. This turns a semantic no-op into a freshness mutation. Conversely, a simple `already stale => return` fix would be wrong: if generated handles/signature change while the stale marker remains, the stale snapshot must refresh to the new output so health continues to describe the current generated object.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs`
- `tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs`
- this claim file

## Intended contract

- same generated signature + same normalized stale reason => true no-op; `UpdatedUtc` unchanged;
- generated signature/handles change while stale => refresh stale snapshot and `UpdatedUtc`;
- stale reason changes while output snapshot is unchanged => update reason and `UpdatedUtc`;
- no generated output => do not create aggregate stale state;
- specialized Curtain Frame/Panel marking follows the same contract;
- preserve existing stale query purity, explicit-clear behavior, health semantics, output signature normalization and dirty-flag behavior.

## Validation plan

Extend the existing registered generated stale smoke to prove repeated same-signature/same-reason marking is timestamp-idempotent, changed handles refresh the stale snapshot and remain stale, changed reason advances freshness, and no-output behavior remains fresh. Use timestamp spacing only to distinguish real writes; do not rely on timing equality by accident.

## Validation boundary

No GitHub Actions, full build, executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

## Completion condition

Generated stale marking mutates freshness only when stale state/snapshot/reason changes, while changed generated output is never hidden behind an `already stale` early return, with regression evidence merged to current `main` and claim closed.
