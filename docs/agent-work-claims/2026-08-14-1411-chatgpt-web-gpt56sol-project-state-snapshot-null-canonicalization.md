# Work claim — ProjectState snapshot null canonicalization smoke

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-state-snapshot-null-canonicalization-20260814`
- Registered: `2026-08-14T14:11:00+07:00`
- Baseline main SHA: `32ae87836ca010891728bdaf5bdc87c09705ad32`
- Priority: first independent Core smoke blocker reported after merged PR #1182 / Project Quantity fixture reconciliation

## Reserved scope

Reconcile `ProjectStateSnapshotNullFidelitySmoke` with the current domain contract: persisted scalar setters on `ProjectState` and `ProjectElement` canonicalize `null` to `string.Empty`, while raw nullable values inserted into dictionaries/lists/audit payloads must remain fidelity-preserved by detached copy and rollback restore.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/ProjectStateSnapshotNullFidelitySmoke.cs`
- read-only contract inspection of `src/QS3D.Core/Domain/ProjectState.cs`, `src/QS3D.Core/Domain/ProjectElement.cs`, and `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`

## Excluded scope

- No production/domain/persistence behavior changes unless a separate defect is proven and separately reserved.
- No Level, Curtain, Beam/Rebar native preflight, BricsCAD adapter/runtime, UI, packaging, release, private DWG, or LOCAL_ONLY qualification work.
- No GitHub Actions dispatch/rerun.

## Validation plan

- Keep production null-to-empty normalization intact.
- Assert canonical empty-string fidelity for scalar properties that normalize on assignment.
- Continue asserting raw null fidelity for dictionary/list/audit backing values that remain nullable.
- Preserve rollback object identity assertion.
- Review the final diff against current `main`; any subsequent full-smoke execution is evidence only when actually run by an authorized execution lane.

## Coordination

PR #1182 explicitly reported this Snapshot smoke as the next independent blocker and its claim was closed on current `main`. No open issue search result currently reserves `ProjectStateSnapshotNullFidelitySmoke`; this claim owns only the named smoke correction and does not overlap the active native/runtime lanes.

## Completion condition

A pushed `main` commit updates only the reserved smoke contract, the change is read back from current `main`, and this claim is marked `COMPLETED` with exact commit evidence and any remaining validation boundary.