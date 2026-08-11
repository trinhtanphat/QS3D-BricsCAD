# Work claim — Physical opening boolean audit-owned Touch

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:13:00+07:00`
- Baseline main SHA: `c61c5a4e74743426882403b28bd072f1eb987698`
- Priority: evidence-driven remote-safe native lifecycle correctness

## Reason

Both straight and curved physical-opening boolean services apply host metadata through `CommitSemanticUpdate(...)` and record a dedicated `AuditTrail` mutation while the CAD transaction is still rollback-capable. `AuditTrail.Record(...)` owns `ProjectState.Touch()`, but both batch lifecycles then call `if (pending.Count > 0) project.Touch();` before `transaction.Commit()`. A successful N-host operation therefore advances `ChangeVersion` N+1 times rather than exactly once per audited host mutation, creating unnecessary freshness churn.

## Reserved scope

Remove only the redundant batch-level explicit Touch from:

- `OpeningBooleanService`
- `CurvedOpeningBooleanService`

Preserve all cutter geometry, host/opening placement, fingerprint/target-state behavior, generated ownership validation, rollback, document lock/native transaction, best-effort viewport regen and existing audit records. Add one auto-discovered static preflight guarding the audit-owned revision invariant for both services.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs`
- `src/QS3D.BricsCAD.V25/Cad/CurvedOpeningBooleanService.cs`
- `scripts/preflight-opening-boolean-audit-owned-touch.py`
- this claim file

## Excluded scope

- No changes to `OpeningCutPlanner`, curved footprint geometry, BooleanOperation behavior, host category support, source resolution, opening placement, generated/native ownership, physical-opening fingerprints/target sets, UI/commands, or `AuditTrail` semantics.
- No GitHub Actions dispatch or release workflow.
- No claim of licensed BricsCAD V25 runtime qualification.

## Validation plan

- Re-fetch claim and both target blob SHAs after registration; never force-push.
- Preserve `ProjectStateSnapshot.Capture(project)`, native transaction, per-host semantic commit before CAD commit, rollback restore and post-commit `TryRegen` behavior.
- Preserve audit actions `geometry.opening.boolean` and `geometry.opening.boolean.curved`.
- Remove only the explicit batch-level `project.Touch()` following the audited update loop.
- Add a shared static preflight requiring these ordering/ownership-of-revision invariants and rejecting reintroduction of explicit Touch in the `CutLinkedOpenings` lifecycle.
- Record source/static verification only; exact V25 behavior remains LOCAL_ONLY.

## Coordination

The older cross-layer atomicity commit intentionally moved physical-opening metadata/audit/touch under the native transaction. This claim retains that atomic ordering while eliminating only the now-redundant explicit Touch because AuditTrail owns revision advancement. No current claim or recent commit was found reserving this exact revision-semantics lane.

## Completion condition

Both physical-opening boolean services advance project revision only through their existing per-host audit records, retain their rollback/native/geometry contracts, include a shared static regression gate, are merged to `main`, and this claim is marked `COMPLETED`.