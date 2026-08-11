# Work claim — Dependency Impact input-enumeration freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-dependency-impact-input-freshness`
- Registered: `2026-08-11T23:58:00+07:00`
- Baseline main SHA: `f3dc5be32f3bd86d1e8e617c788f50a59af24896`
- Reservation commit: `148419d9ee86e0219022b94317933fbf287b2520`
- Priority: P1 — make the existing read-only `ChangeVersion` freshness contract cover caller root enumeration as well as graph traversal.

## Defect fixed

`DependencyImpactPlanner.Plan(...)` captured `project.ChangeVersion` only after `CanonicalRoots(...)` had enumerated the caller-provided `IEnumerable<string>`. If project state changed while that potentially lazy input was being enumerated, the planner recorded the post-change version and its final freshness check could not detect that the project changed during the operation. The root cardinality bound also read project element count before that window, so root validation and graph planning could observe different project revisions without failing closed.

The planner now captures `ChangeVersion` first, captures semantic element cardinality next, and only then enumerates caller roots against that captured cardinality. The existing final version guard therefore covers the entire caller-controlled input-enumeration and graph-planning window.

## Published commits

- `81c8cf3992405edc8255185f12a7fbc946d293c6` — `fix(review): start dependency impact freshness before input enumeration`.
- `21c35ee977f3844d8c70164672e2fa9b25c7f5df` — `test(review): cover dependency impact input freshness`.
- `83277642e8047f38c89d0b6113506779e560b3cf` — `test(review): pin dependency impact input freshness`.

## Preserved contract

- Canonical root validation, duplicate/missing-root rejection and deterministic breadth-first impact traversal are unchanged.
- The project-cardinality enumeration bound from the preceding lane remains intact and now uses the captured cardinality.
- Normal planning remains read-only.
- The focused regression uses a lazy root enumerable that advances `ProjectState.ChangeVersion` during enumeration; the operation must now fail through the existing stale-plan exception rather than silently rebasing its freshness token after the change.
- The static preflight requires `ChangeVersion -> element count -> CanonicalRoots` ordering and rejects both prior root-materialization patterns.

## Validation notes

Current `main` source, focused smoke and preflight were re-fetched after publication and contain the intended ordering/regression/static contract. One preflight write encountered a concurrent-main 409 and was retried only after re-fetching current state; no force-push or overwrite was used. The smoke/preflight were not executed from a full repository checkout in this connector-only lane, so no executable Core PASS is claimed. No GitHub Actions were dispatched and no licensed BricsCAD V25 runtime PASS is claimed.

## Excluded scope

No DependencyGraph rewrite, no mutation workflow, no BricsCAD/native/UI changes and no release workflow changes.

## Completion condition

Satisfied for the remote-safe source/static contract: the planner freshness window begins before caller root enumeration, focused regression/static coverage is on `main`, and this reservation is released.
