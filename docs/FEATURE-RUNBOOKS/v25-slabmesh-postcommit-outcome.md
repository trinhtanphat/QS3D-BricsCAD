# V25 Slab Mesh post-commit outcome / document-affinity qualification

Scope: `QS3DSLABREBAR3D` in licensed BricsCAD V25. Remote/source agents may validate source guards, deterministic checks and locked-reference compile, but must not report native runtime scenarios as `LOCAL_PASS` without executing them in licensed V25.

## Source contract

The command captures the invocation `Document` and its native database identity before selection/project work. Native generation is revalidated immediately before geometry mutation and before every post-commit palette/editor publication step. A managed `Document` wrapper whose native database has changed is stale for this operation.

`SlabMeshSolidBuilder.BuildSelected(...)` returns a `SlabMeshBuildResult` containing committed element/bar counts and a bounded `PostCommitCleanupWarning` discriminator. Project semantic/audit state remains published before `transaction.Commit()`. Any failure before successful native commit restores the project snapshot; a rollback failure preserves both the operation and restore failures. Once `transaction.Commit()` has returned and `cadCommitted` is true, later transaction/document-lock cleanup failures do not roll back durable project state and are not rethrown as a generic mutation failure.

The warning surface is intentionally boolean/bounded: raw host exception text is not exposed to palette/editor output. Palette refresh, Regen, status and editor publication are each generation-fenced; isolated UI failures do not redefine a successful native commit.

PICKFIRST/current selection acquisition, existing-project-only binding, ProjectId/ChangeVersion and semantic target-set freshness, generated ownership, geometry/batch bounds and audit-before-CAD-commit behavior remain unchanged.

## REMOTE_SAFE validation

Run at minimum:

- `python scripts/preflight-v25-slabmesh-postcommit-outcome.py`
- `python scripts/preflight-slab-mesh-native.py`
- the repository aggregate feature-source-guard suite
- deterministic smoke required by protected CI
- admitted locked-reference BricsCAD V25 compile/build

Require fresh exact-head required CI after any replay/reconciliation with protected `main`.

## LOCAL_ONLY licensed V25 matrix

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| SMP01 | Valid selected Slab, normal mesh build | Bars/project semantic/audit commit once; success element/bar count is shown. |
| SMP02 | Force geometry/ownership failure before native commit | CAD transaction aborts and project snapshot is restored; generic operation failure is allowed. |
| SMP03 | Force project rollback failure after a pre-commit operation failure | Diagnostic evidence preserves both operation and restore failures; no false success is shown. |
| SMP04 | Force transaction cleanup failure after successful `Commit()` | Durable CAD/project result remains; user sees committed success plus bounded cleanup warning, not mutation failure. |
| SMP05 | Force document-lock cleanup failure after successful commit | Same committed-warning semantics as SMP04; retry is not invited as if mutation failed. |
| SMP06 | Switch MDI document before geometry mutation | Mutation fails closed; no slab mesh is authored into the stale/new drawing. |
| SMP07 | Switch MDI document after commit before palette/Regen/status publication | Stale invocation does not publish palette/status/editor output into the successor document. |
| SMP08 | Reuse managed wrapper with replaced native database generation | Native identity fence rejects stale mutation/publication. |
| SMP09 | Palette refresh or Regen throws after successful commit | Durable state remains; bounded UI-sync warning is used without raw host exception text. |
| SMP10 | Cold reopen after SMP01/SMP04 | Generated handles/count/diameter/cover/spacing/faces/vertical snapshot and audit remain coherent. |

## Runtime classification

`REMOTE_SAFE`: source/static guards, deterministic checks, repository CI and admitted locked-reference V25 compile.

`LOCAL_ONLY`: actual Teigha transaction/document-lock cleanup timing, MDI/native-generation switching during command execution, and visible palette/editor behavior. Absence of a licensed runtime is `NO_RESULT`, never `LOCAL_PASS`.
