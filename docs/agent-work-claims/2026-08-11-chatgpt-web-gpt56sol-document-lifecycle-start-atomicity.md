# Work claim — document lifecycle start atomicity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-document-lifecycle-start-atomicity`
- Registered: `2026-08-11T22:12:00+07:00`
- Baseline main SHA: `4f4cc84f3248e94cd6b7a9686d8ce490619b7f83`
- Priority: make plugin lifecycle subscription startup fail atomically instead of leaving half-attached handlers that a retry cannot safely reconcile.

## Confirmed defects

Two deterministic initialization paths currently mutate subscription ownership before a throwing operation can complete:

1. `DocumentLifecycleCoordinator.Start()` subscribes four `DocumentManager` events, then attaches active-document persistence/selection handlers, and sets `_started = true` only at the end. If either active-document attach throws, the collection handlers remain subscribed while `_started` stays false. A later retry can subscribe them again and produce duplicate lifecycle callbacks.
2. `SelectionSyncCoordinator.Attach(document)` adds the document to `Attached` before subscribing `ImpliedSelectionChanged`. If event subscription throws, the document remains marked attached and all later retries return early, permanently suppressing selection sync for that document.

These are source-level ownership/rollback defects; no native rendering behavior is required to establish them.

## Reserved scope

- `src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs`
- `src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs`
- `scripts/preflight-document-lifecycle.py`
- this claim file

## Intended contract

- `DocumentLifecycleCoordinator.Start()` either completes all subscriptions/active-document attachments and marks started, or rolls back manager events, project-persistence attachments and selection-sync ownership before rethrowing.
- `SelectionSyncCoordinator.Attach()` must not retain `Attached` membership when native event subscription fails; retry remains possible.
- Existing exact-Document destruction cleanup, save/close persistence, selection refresh semantics and Stop behavior remain unchanged.
- No exceptions are swallowed at startup merely to claim success.

## Excluded scope

- No ProjectContext persistence semantics, modeless window redesign, updater, Ribbon, Quantity/BQ, Direct Draw, Core, installer/signing/release or LOCAL inbox edits.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

Re-fetch every reserved file immediately before writes. Add explicit rollback/fail-retry contracts without weakening existing fail-closed behavior. Extend the auto-discovered document lifecycle preflight to require rollback ordering and reject add-before-subscribe selection ownership. Inspect exact diffs and verify ancestry after concurrent integration without force push.

## Completion condition

Lifecycle startup no longer leaves manager/document handlers in a half-attached state after failure, selection-sync attachment remains retryable, focused static coverage is merged, and native V25 failure-injection qualification remains local-only.