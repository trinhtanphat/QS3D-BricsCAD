# Source Reconcile failure redaction qualification

Issue: #5214  
Lane-Key: `issue-5214`  
Runtime class: `LOCAL_ONLY` for licensed BricsCAD V25 command/UI execution.  
Remote/static source and CI evidence is never `LOCAL_PASS`.

## Defect and source contract

On protected main `f1545b8d7a4baa31e5f1c1b3942028f07ea52d0f`, `QS3DSYNCSOURCE` surfaced caught host/native exception detail through its command failure status. Its post-reconcile UI path also grouped Palette refresh, editor Regen, Palette status and Editor output in one try/catch and appended the caught exception message to a user-visible UI warning.

This carrier does not change `SourceReconcileService` mutation semantics. Selection/ownership validation, read-only preview, single canonical mutation bind, project/target freshness, generated-output invalidation, bounded affected-closure regeneration, project rollback before CAD commit, AuditTrail revision ownership and explicit native rebuild boundaries remain guarded by `scripts/preflight-source-reconcile.py`.

The command adapter now:

- reports operation failure using stable text without caught exception detail;
- enters `FinalizeUi` only after `SourceReconcileService.ReconcileSelection` has returned;
- independently fail-isolates Palette refresh, editor Regen, Palette status and normal Editor output;
- emits only a stable post-commit warning when any UI synchronization cell fails;
- keeps Palette/Editor failure reporting best-effort and non-escaping.

Deterministic remote acceptance includes `scripts/preflight-source-reconcile.py` and the auto-discovered `scripts/preflight-source-reconcile-failure-redaction.py`, followed by fresh exact-head protected `preflight` + `core`.

## Licensed V25 matrix — SR01–SR12

Execute only against the exact candidate authorized for local qualification; record plugin SHA-256/ProductVersion and use a disposable project/DWG.

| Cell | Action | Required evidence |
| --- | --- | --- |
| SR01 | Start V25, NETLOAD exact plugin, open disposable tracked project | Exact artifact identity and clean startup captured |
| SR02 | Run `QS3DSYNCSOURCE` with no selection | Stable no-op guidance; no mutation and no raw exception detail |
| SR03 | Reconcile one valid tracked LINE/open-POLYLINE source after a live CAD edit | Source-derived semantic state refreshes and command reports stable success |
| SR04 | Select QS3D-generated output rather than authoritative source | Fail closed before mutation with stable user-facing failure |
| SR05 | Exercise ambiguous/invalid source ownership fixture | Ownership rejection remains fail closed; no generated/native mutation |
| SR06 | Change project state between preview and canonical mutation bind | Freshness rejection occurs before mutation |
| SR07 | Change selected target identity between preview and bind | Target-set freshness rejection occurs before mutation |
| SR08 | Reconcile source with generated host/rebar/curtain dependents | Owned generated output invalidates/removes according to existing source-reconcile contract; no implicit rebuild |
| SR09 | Force controlled Palette refresh failure after successful reconcile | Reconcile remains committed; later UI cells still run best-effort; stable warning only |
| SR10 | Force controlled Editor Regen/status/output failure cells independently | No UI failure escapes or exposes host exception detail; stable post-commit warning only |
| SR11 | Run explicit rebuild workflow after successful reconcile | New native output is produced only by the explicit build workflow, not by sync itself |
| SR12 | QSAVE, close, fresh-process reopen and inspect/reconcile again | Semantic/native ownership persists; cleanup leaves no owned BricsCAD process/residue |

## Verdict

`LOCAL_PASS` requires SR01–SR12 on one exact artifact identity with sanitized evidence and cleanup. Any licensed/native defect is `RUNTIME_FAIL` or `NO_RESULT`; hosted CI must not be promoted to runtime evidence.
