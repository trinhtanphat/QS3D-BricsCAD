# Work claim — drawing identity scalar revision ordering

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-drawing-identity-scalar-revision-20260813`
- Registered: `2026-08-13T20:16:00+07:00`
- Baseline main SHA: `e51d19df145f576b9f3f2e12a68d01fa926076c4`
- Priority: P0 persisted drawing-identity revision/atomicity regression.

## Confirmed defect

`ProjectContextCoordinator` still carries the old explicit `project.Touch()` ordering introduced by `74b7c4e3c9fcc3def7d3f4f32436f887aa8eb6be`, when `ProjectState.DrawingPath` / `DrawingFingerprint` were plain setters. Later `0c7dbe5612c0db6e5252f3ec1db6385b06771e0e` moved persisted scalars onto `SetPersistedScalar(...)`, where every changed scalar now performs its own checked `ChangeVersion + 1` before mutation. The old adapter `Touch()` is therefore redundant: a path-only identity sync advances the revision twice, and legacy identity adoption can advance once for `Touch` plus once per changed project scalar. Near `long.MaxValue`, the old one-step Touch preflight also no longer proves that all subsequent scalar-owned revision increments can complete before any identity mutation.

## Reserved scope

- `src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs`
- `scripts/preflight-project-context-drawing-identity-touch-order.py`
- this claim file

## Intended bounded change

- remove obsolete explicit `project.Touch()` ownership from drawing path/fingerprint synchronization now that the persisted scalar setters own revision advances;
- preserve path-only sync as exactly one scalar revision advance;
- before legacy adoption mutates either project scalar, preflight checked revision capacity for the exact number of changed project scalar assignments, so overflow cannot leave only one of path/fingerprint updated;
- retain prevalidation of the element snapshot before mutation and retain existing element drawing-fingerprint adoption behavior;
- update the existing focused static preflight from the obsolete “Touch before assignment” contract to the current scalar-owned revision/atomicity contract.

## Excluded scope

- no edits to `ProjectState`, persistence schema/store/stamp, sidecar freshness, recovery behavior, drawing-fingerprint algorithm, element identity policy, RevisionMath, UI/native geometry, or local V25 runtime work;
- no GitHub Actions or licensed runtime PASS claim.

## Coordination

- current `main` immediately before claim was `e51d19df145f576b9f3f2e12a68d01fa926076c4`;
- the active RevisionMath signed-zero claim reserves only `RevisionMath.cs` and `RevisionRegressionSmoke.cs` and is disjoint;
- targeted commit searches for `drawing identity redundant touch` and `ProjectContextCoordinator ChangeVersion drawing scalar` found no competing current lane;
- there were no open pull requests immediately before claim;
- chronology was verified from the historical touch-order source/static commits and the later persisted-scalar versioning commit.

## Validation plan

Refresh `main` after claim, read current reserved blobs, make the smallest source/preflight changes, read back exact remote content and diffs, and close only with static/source validation actually performed. No native BricsCAD runtime qualification is implied.
