# Zone Manager native-database single-instance / veto-safe close qualification

Status: SOURCE_PREPARED / LOCAL_ONLY runtime matrix

This runbook qualifies the `QS3DZONES` modeless publication contract introduced by issue #4710. Hosted CI proves source ordering and V25 compile compatibility; it does **not** constitute licensed BricsCAD runtime evidence.

## Artifact boundary

Use an exact built artifact from the candidate SHA. Record candidate SHA, V25 host version, DLL SHA-256, drawing fixture identity, and whether the command observed the same managed `Document` wrapper or a reacquired wrapper for the same native database.

The command uses native database identity for logical drawing arbitration, but the window remains intentionally bound to the managed `Document` wrapper supplied at construction. Wrapper drift therefore requires terminal replacement rather than reusing an old wrapper-bound window.

## Matrix

1. **Same-wrapper repeat** — open a QS3D project drawing, run `QS3DZONES`, then invoke `QS3DZONES` again while the exact managed `Document` wrapper remains current. Expected: the existing Zone Manager is activated/reused; there is exactly one live Zone Manager and Zone state is unchanged.
2. **Native database / wrapper drift** — with the first manager live, reacquire the same drawing through a host lifecycle that yields a different managed `Document` wrapper while native database identity remains unchanged, then run `QS3DZONES`. Expected: the old wrapper-bound manager is requested to close; only after terminal `Closed` is a replacement manager bound to the current wrapper shown. The old window is not reused and two managers are never simultaneously published.
3. **Wrapper-drift close veto** — repeat cell 2 while a legitimate `Closing` subscriber/lifecycle condition vetoes the old manager close. Expected: old publication remains, no replacement is constructed/shown, and retry is possible after the veto is removed.
4. **Cross-document terminal close** — with manager A live on drawing A, activate drawing B and run `QS3DZONES`. Expected: A receives normal close processing; only after terminal `Closed` may manager B be shown. At no observation point are two published Zone Managers live.
5. **Cross-document close veto** — arrange a legitimate `Closing` cancellation on A, then invoke `QS3DZONES` from B. Expected: A remains published, B is not created/shown, and the command reports that cleanup/close must complete first. Remove the veto and retry; B may replace A only after A reaches `Closed`.
6. **Close exception** — using the local qualification harness if available, make the previous window's close path throw. Expected: failure is surfaced and a second manager is not published.
7. **Normal terminal close/reopen** — close the manager normally and invoke `QS3DZONES` again. Expected: terminal `Closed` releases ownership and a new manager opens normally.
8. **Zone semantics regression** — in the surviving current-wrapper manager, exercise Refresh, Create/Update, Activate, Assign and guarded Delete against an existing project. Expected: existing transaction/rollback/document-affinity behavior remains unchanged.
9. **Document shutdown** — close the bound drawing/host using the normal `DocumentBoundWindowLifetime` path. Expected: manager closes without leaving stale publication that blocks the next valid `QS3DZONES` invocation.

## Evidence to capture

For every cell record exact SHA, host build, native database identity if safely observable, wrapper same/drift classification, initial/final active drawing, number of live Zone Manager windows, whether terminal `Closed` occurred, status/command-line text, and any exception. Preserve sanitized logs/screenshots under the repository's current LOCAL_ONLY evidence policy.

Do not claim `LOCAL_PASS` unless all required licensed-host cells were actually executed against the exact candidate artifact.
