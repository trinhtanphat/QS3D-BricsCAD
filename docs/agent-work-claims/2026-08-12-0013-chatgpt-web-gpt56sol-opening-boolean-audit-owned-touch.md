# Work claim — Physical opening boolean audit-owned Touch

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:13:00+07:00`
- Baseline main SHA: `c61c5a4e74743426882403b28bd072f1eb987698`
- Priority: evidence-driven remote-safe native lifecycle correctness

## Reason

Both straight and curved physical-opening boolean services applied host metadata through `CommitSemanticUpdate(...)` and recorded a dedicated `AuditTrail` mutation while the CAD transaction was still rollback-capable, then performed an additional batch-level `project.Touch()`. Because `AuditTrail.Record(...)` owns revision advancement, successful batches advanced `ChangeVersion` once beyond their audited host mutations.

## Reserved scope

Remove only the redundant explicit batch-level Touch from `OpeningBooleanService` and `CurvedOpeningBooleanService`, preserving cutter geometry, host/opening placement, fingerprints/target state, native ownership, rollback, document lock/transaction and post-commit regen. Add one shared auto-discovered static preflight.

## Completion evidence

- PR #567 merged to `main` as `531f73b17f6a515692619b1158965668f6d97716`.
- PR scope was exactly three files: both reserved services plus `scripts/preflight-opening-boolean-audit-owned-touch.py`.
- The PR reported `+62/-2`; the two source changes are exactly the two removed batch-level Touch lines and the additions are the new static preflight.
- Compare against 27 concurrent commits after the implementation branch baseline showed no overlap with either reserved service.
- Straight service retains `PhysicalOpeningCutTargetState.Write(...)` and audit action `geometry.opening.boolean`; curved service retains target-state write and `geometry.opening.boolean.curved`.
- Rollback snapshot, document lock/native transaction, semantic update before CAD commit and post-commit best-effort `TryRegen` remain intact.
- Post-merge exact blob verification: straight `3fa8e0bb4332c370eefa800f26d9b1d388f8039c`; curved `5a448c2215821df96e5607423befa6cfdf3e4c60`; preflight `bb13a4bf573457b3fc45c3549d2e8d67fe482a14`.
- No force-push, GitHub Actions dispatch, release workflow, or licensed BricsCAD V25 runtime claim.

## Excluded scope

No `OpeningCutPlanner`, curved footprint geometry, BooleanOperation behavior, host-category support, source resolution, placement, generated/native ownership, fingerprint/target-set policy, UI/command or `AuditTrail` semantics changed.

## Completion condition

Completed: straight and curved physical-opening boolean services now advance project revision only through their existing per-host audit records and the shared static regression gate is on `main`.