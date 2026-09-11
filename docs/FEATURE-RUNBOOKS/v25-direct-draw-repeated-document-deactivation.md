# V25 repeated Direct Draw document-deactivation containment

Issue: #6416
Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring
Lane-Key: `issue-6416`
Ownership-Key: `v25.direct-draw-repeated-document-deactivation-containment-v1`

## Defect contract

`DirectDrawRepeatedCommands.RepeatedDocumentLifecycleGuard` owns a native
`DocumentCollection.DocumentToBeDeactivated` subscription while repeated Wall/Beam authoring is active.
The event add/remove accessors are already treated as fallible native boundaries. The callback must apply
the same containment rule to `DocumentCollectionEventArgs.Document`: native wrapper access during MDI
teardown can fail, and no exception may escape the BricsCAD event frame.

A payload-access failure is treated conservatively as document deactivation. Repeated authoring therefore
stops before another segment can be committed. A disposed/retained callback generation remains cleanup-only
and retries exact native detach without consulting stale event/document state.

## REMOTE_SAFE verification

- `python scripts/preflight-v25-direct-draw-repeated-document-deactivation.py`
- aggregate discovered feature/source guards
- repository deterministic smoke suite
- trusted/admitted BricsCAD V25 reference validation
- V25 plugin build against locked admitted reference generations

Remote green proves source topology, deterministic behavior covered by hosted tests, and compile compatibility.
It does **not** prove native BricsCAD teardown timing.

## LOCAL_ONLY qualification

Requires a real licensed BricsCAD V25 host. Exercise repeated Wall and Beam authoring while switching/closing
the owning DWG, including fault-injected or naturally failing `DocumentCollectionEventArgs.Document` access
where the harness permits it. Verify:

1. no exception escapes the native `DocumentToBeDeactivated` callback;
2. accessor failure fails closed and prevents another segment commit;
3. Enter/ESC normal completion is unchanged;
4. retained callback after failed native remove only retries detach and performs no document/UI mutation;
5. whole-command checkpoint/Undo and rollback remain coherent for already accepted segments;
6. no duplicate handler or rooted command generation remains after teardown.

Until those checks execute in licensed V25, classification is `LOCAL_ONLY / NO_RESULT`; hosted CI must never
be reported as `LOCAL_PASS`.

## Self-review checklist

- exact document switch/deactivation semantics;
- subscribe/unsubscribe ownership before partial native add/remove;
- dispose/retained callback cleanup;
- detach reentrancy;
- event-payload native-wrapper exception containment;
- cancellation and DrawJig termination;
- semantic/native transaction and checkpoint ownership;
- rollback of accepted segments when checkpoint registration fails;
- stale project/document wrappers;
- post-commit reporting isolation;
- UI/native thread affinity and V25 compatibility.
