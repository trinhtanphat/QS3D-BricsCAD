# BricsCAD V25 modeless initial Attach retry safety

## Scope

This runbook covers the per-window registration created by `DocumentBoundWindowLifetime.Attach(Window, Document)` when the *first* attach attempt fails before a modeless window becomes successfully attached.

The defect fixed by issue #6916 is distinct from managed-wrapper drift after a successful attach: a failed initial `Registration` used to remain cached in the `ConditionalWeakTable`, carrying constructor-captured native database and managed-wrapper affinity into a later retry.

## Production contract

- registration lookup, initial attach and failed-registration eviction are serialized by a weak per-`Window` gate;
- same-thread reentrant `Attach` is rejected while an attach attempt is in progress, so monitor reentrancy cannot replace the cached registration behind an outer failure path;
- a failed initial registration records a cleanup obligation before rollback starts;
- if host quiescence or a document close boundary prevents rollback from completing, the failed registration remains canonical and retry fails closed instead of orphaning its lifecycle subscription;
- a later retry first asks that same failed registration to complete deferred cleanup when the host is safe; only after it is detached and its per-window native lifecycle subscription has been released may the table evict it and construct a fresh registration from the retry `Document`;
- the caller captures whether the selected fresh/current registration was already attached before invoking `Registration.Attach`;
- when an attach call that began with a never-attached registration throws, eviction is allowed only after rollback completed and only when the table still maps the `Window` to that exact failed instance;
- when a repeated attach or wrapper rebind begins from an already-successful registration and then fails, the existing registration remains canonical so the failure cannot create a second lifecycle owner on the next retry;
- the original exception is rethrown after any safe initial-registration eviction;
- the weak gate remains keyed by the `Window`, so it does not introduce a process-lifetime strong root;
- successful repeated attach keeps the wrapper-rebind and semantic project/drawing affinity contract established by #6872;
- coordinator-native handler cleanup remains owned by `DocumentBoundNativeLifecycleCoordinator`; remote/static checks do not claim host unsubscription succeeded.

## REMOTE_SAFE verification

Run:

```text
python scripts/preflight-v25-modeless-initial-attach-retry.py
python scripts/preflight-all.py
```

Then require repository deterministic smoke and the BricsCAD V25 compile using trusted/admitted locked references on the exact candidate SHA. Remote/static/build green is `REMOTE_SAFE` evidence only.

## LOCAL_ONLY licensed BricsCAD matrix

The following require a real licensed BricsCAD V25 host and remain `NO_RESULT` until such evidence exists:

1. Force an initial native lifecycle registration/subscription failure for a modeless window and verify the visible window does not remain partially interactive.
2. After that failed attach, cause the original managed `Document` wrapper/native generation to become stale and retry the same `Window` with the current legitimate wrapper; verify retry starts from the retry document rather than constructor state from the failed attempt.
3. Race the initial attach failure against host quit/quiescence; verify the failed registration remains owned while cleanup is unsafe, retry fails closed during quiescence, and a retry after quiescence abort completes cleanup before creating a fresh registration.
4. Race the initial attach failure against document close/close-abort; verify any retained native subscription is not orphaned and a fresh registration is admitted only after the close-abort cleanup releases it.
5. Retry after an initial failure using a different project/drawing affinity and verify the normal fail-closed project/document checks still reject it.
6. Trigger two concurrent attach requests for the same `Window`; verify only one initial registration proceeds and no duplicate managed/native handlers are installed.
7. Trigger same-thread reentrant attach during an injected attach callback; verify the nested call fails without evicting/replacing the outer registration.
8. Force a repeated attach/rebind to fail after a window was already successfully attached; verify the original registration remains the sole lifecycle owner and a later retry cannot create duplicate handlers.
9. Close/abort/quit after a successful retry and verify subscription disposal, quiescence and window-close behavior remain compatible with the #6872 wrapper-rebind lifecycle contract.

Do not label any scenario `LOCAL_PASS` without a licensed host execution artifact tied to the exact tested SHA.
