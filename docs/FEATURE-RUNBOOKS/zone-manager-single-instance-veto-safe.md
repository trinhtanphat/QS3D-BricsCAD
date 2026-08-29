# Zone Manager native-database single-instance / veto-safe close qualification

Status: SOURCE_PREPARED / LOCAL_ONLY runtime matrix

This runbook qualifies the `QS3DZONES` modeless publication contract introduced by issue #4710. Hosted CI proves source ordering and V25 compile compatibility; it does **not** constitute licensed BricsCAD runtime evidence.

## Artifact boundary

Use an exact built artifact from the candidate SHA. Record candidate SHA, V25 host version, DLL SHA-256, drawing fixture identity, and whether the run used the same managed `Document` wrapper or a reacquired wrapper for the same native database.

## Matrix

1. **Same-document repeat** — open a QS3D project drawing, run `QS3DZONES`, then invoke `QS3DZONES` again without closing the first manager. Expected: the existing Zone Manager is activated/reused; there is exactly one live Zone Manager window and Zone state is unchanged.
2. **Native database / wrapper drift** — with the first manager still live, reacquire/activate the same drawing through the host lifecycle so the command may observe a different managed `Document` wrapper while the native database identity is unchanged, then run `QS3DZONES`. Expected: the original manager is reused; no second window is published.
3. **Cross-document terminal close** — with manager A live on drawing A, activate drawing B and run `QS3DZONES`. Expected: A receives normal close processing; only after terminal `Closed` may manager B be shown. At no observation point are two published Zone Managers live.
4. **Cross-document close veto** — arrange a legitimate `Closing` subscriber/lifecycle condition that cancels A's close, then invoke `QS3DZONES` from B. Expected: A remains published, B is not created/shown, and the command reports that cleanup/close must complete first. Remove the veto and retry; B may then replace A after A reaches `Closed`.
5. **Close exception** — using the local qualification harness if available, make the previous window's close path throw. Expected: failure is surfaced and a second manager is not published.
6. **Normal terminal close/reopen** — close the manager normally and invoke `QS3DZONES` again. Expected: terminal `Closed` releases ownership and a new manager opens normally.
7. **Zone semantics regression** — in the surviving manager, exercise Refresh, Create/Update, Activate, Assign and guarded Delete against an existing project. Expected: existing transaction/rollback/document-affinity behavior remains unchanged.
8. **Document shutdown** — close the bound drawing/host using the normal `DocumentBoundWindowLifetime` path. Expected: manager closes without leaving a stale publication that blocks the next valid `QS3DZONES` invocation.

## Evidence to capture

For every cell record exact SHA, host build, initial/final active drawing, number of live Zone Manager windows, whether terminal `Closed` occurred, status/command-line text, and any exception. Preserve sanitized logs/screenshots under the repository's current LOCAL_ONLY evidence policy.

Do not claim `LOCAL_PASS` unless all required licensed-host cells were actually executed against the exact candidate artifact.
