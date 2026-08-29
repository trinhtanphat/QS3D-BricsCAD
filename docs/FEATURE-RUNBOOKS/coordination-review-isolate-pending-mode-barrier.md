# Coordination review isolate pending-mode barrier

## Scope

This runbook qualifies the source-side lifecycle contract for starting a new Coordination Manager isolate action while a prior `OBJECTISOLATIONMODE` compensation is still owned by the transient review session.

## Defect boundary

A completed `UNISOLATEOBJECTS` queue can release command-isolation ownership while a transient native failure leaves `_objectIsolationModeBefore` pending. A subsequent isolate must not capture the still-mutated host mode and overwrite the original restore obligation.

## Required source contract

Before a new isolate observes or mutates PICKFIRST, reads or changes `OBJECTISOLATIONMODE`, or queues `ISOLATEOBJECTS`, it must:

1. detect all outstanding isolation ownership through `HasIsolation`;
2. invoke `RestoreIsolation()` to drain the prior command/mode obligations;
3. recheck `HasIsolation`;
4. fail closed if any prior obligation remains.

Successful launch ownership is still published only after native command queue acceptance. Existing synchronous launch compensation, exact prior PICKFIRST restoration, independent mode-retry ownership, and destroyed-document explicit-abandon semantics remain unchanged.

## Deterministic qualification

Run:

```text
python scripts/preflight-coordination-review-isolate-pending-mode-barrier.py
```

The repository aggregate feature-guard discovery must execute this guard automatically. Full branch/protected validation also includes deterministic Core smoke, trusted V25 compile references, V25 plugin compilation, and final build.

## Runtime boundary

This source-safe contract does not claim licensed BricsCAD runtime acceptance. Any native/runtime qualification remains exact-SHA bound under the repository's LOCAL_ONLY process.
