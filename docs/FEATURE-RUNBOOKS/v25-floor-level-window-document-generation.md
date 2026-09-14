# V25 Floor/Level modeless native-generation affinity

## Scope
`QS3DLEVELS` is modeless. Managed `Document` identity is insufficient when BricsCAD replaces/reloads the native `Database` behind the same wrapper. Selection/ObjectId state, project mutation authority, first-save bootstrap and UI publication therefore belong to the captured non-zero `Database.UnmanagedObject` generation.

## Contract
- Every read/mutation/bootstrap boundary must prove both active managed document identity and the captured native database identity.
- Selection preview must be revalidated before mutation; stale selection/project/default context must fail closed.
- A first-save project context created just before generation drift is forgotten before control returns to the stale window.
- Rollback and post-commit UI publication must never touch or report into a successor generation.
- Failure/warning/success status publication is generation-safe; exception-derived public text is not emitted from first-save handling.
- Same-generation MDI inactivity and native-generation replacement remain distinct conditions; this carrier closes only for stale generation/wrapper ownership.

## Verification
Run `python scripts/preflight-v25-floor-level-window-document-generation.py`, the existing Floor/Level focused preflights, generic preflight, deterministic smoke and an admitted locked-reference V25 Release compile. Licensed same-wrapper database replacement, routed-input MDI switching and visible modeless behavior remain `LOCAL_ONLY / NO_RESULT` unless executed in a real licensed V25 host.