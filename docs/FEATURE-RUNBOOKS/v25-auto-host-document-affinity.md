# V25 Auto Host document-generation affinity

## Scope

This carrier hardens `QS3DAUTOLINKHOSTS` and `LinkSingleOpening` so CAD-read results and retained semantic targets are never handed to HostLink mutation after the originating BricsCAD document/database generation has become stale.

## REMOTE_SAFE contract

The command captures both the exact managed `Document` and `document.Database.UnmanagedObject`. After CAD evaluation and before semantic mutation it requires the same active managed document, the same native database generation, the same canonical project instance/id/change version, and the same opening target set. `LinkSingleOpening` repeats the native-generation and canonical-project fence after its CAD read and before `HostLinkService.LinkOpening`.

Post-commit palette refresh, status publication, editor output, and error publication are best-effort and generation-bound. If MDI focus or the native database generation changes, stale UI publication is suppressed; committed project mutations are not rolled back merely because UI synchronization fails.

Regression guard: `scripts/preflight-v25-auto-host-document-affinity.py`.

## LOCAL_ONLY

Licensed BricsCAD remains required to validate real MDI A→B→A switching, same managed wrapper with native database replacement/reload, disposed native wrappers/ObjectIds, transaction host pumping, palette/editor reentrancy, and visible modeless UI behavior. Hosted CI/static evidence must not be reported as `LOCAL_PASS`.

## Self-review checklist

Verify exact document/native-generation affinity; project identity/version and target-set affinity; CAD transaction ownership; semantic rollback remains scoped to mutation failure; no stale status/error publication; post-commit UI failures do not become false CAD/semantic failures; no new subscriptions or disposal obligations; no raw exception details are published; V25 API compatibility remains intact.
