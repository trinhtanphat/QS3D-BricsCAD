# Work claim — Semantic handle selection structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T10:38:00+07:00`
- Baseline main SHA: `cefe3a8c834d62dcc8dfaf3a77bfc33d5e285ab7`
- Priority: P1 — semantic handle ownership resolution must not continue across caller-driven project element membership changes that bypass `ChangeVersion`.
- Task Key: `CORE-SEMANTIC-HANDLE-STRUCTURAL-FRESHNESS`

## Confirmed defect

`SemanticHandleOwnershipResolver.Resolve(ProjectState, IEnumerable<string>)` validates unique project element IDs before materializing caller-provided selected handles, then pins only `ProjectState.ChangeVersion` across that lazy enumeration. `ProjectState.Elements` is a public mutable list. A side-effecting handle enumerable can remove, add, or replace semantic element instances directly without calling `project.Touch()`, leaving `ChangeVersion` unchanged. Ownership scanning then runs against a different element membership/identity set than the one validated before input enumeration.

This is distinct from the completed semantic-handle input-freshness lane, whose regression explicitly covers lazy inputs that call `project.Touch()`. The resolver feeds mutation workflows such as semantic untrack, so stale/ambiguous ownership resolution can influence which semantic elements are targeted.

## Reserved scope

- `src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs` — `Resolve(...)` structural element-set freshness only
- one focused Core smoke/regression for remove/replace/add during lazy selected-handle enumeration plus stable control
- this claim file

## Intended contract

- preserve existing malformed-project validation and `ChangeVersion` freshness precedence;
- snapshot canonical element ID → exact instance ownership before caller handle enumeration;
- after materialization/version validation and before empty-selection no-op or ownership scan, require current project element membership and exact instances to match the snapshot;
- fail closed on element removal, addition, same-ID replacement, null/duplicate identity introduced without `Touch()`;
- preserve selected-handle count/normalization/deduplication, source/generated ownership diagnostics, deterministic ordering and existing stable behavior;
- do not modify `SourceHandleResolver`, `SemanticUntrackService`, generated-handle policies/health, CAD/UI/native adapters or concurrent claims.

## Validation boundary

Source and focused regression will be committed/read back from `main`. No force-push, GitHub Actions dispatch, executable full-smoke/build PASS or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.
