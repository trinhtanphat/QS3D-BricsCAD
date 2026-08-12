# Work claim — release aggregate feature preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:02:00+07:00`
- Completed: `2026-08-12T08:16:00+07:00`
- Baseline main SHA: `0d8585b10d8de98b6a54929b6c38a4ff0d9d3ad6`
- Priority: Owner-requested repair for failed QS3D Cloud V25 Preview Build & Release #26 (`31551424552`).

## Reserved scope

Reconcile the aggregate `scripts/preflight-all.py` failures exposed by release run #26. Diagnose the concrete failing feature guards on current `main`, repair stale/incorrect source-contract assertions or directly affected source/docs where evidence requires it, and preserve the fail-closed release policy. Do not remove feature gates from aggregate discovery or convert failures into warnings.

## Completed reconciliation

The run-#26 aggregate log proved that many failures were exact-token drift after intentional source refactors rather than one aggregate-runner exception. This lane repaired the high-confidence stale contracts without weakening release discovery:

- Curtain LINE/path atomicity and transaction boundary now guard AuditTrail-owned revision advancement instead of requiring the removed redundant `project.Touch()` batch mutation.
- Generated rebar atomicity, Grid annotation and straight/curved opening boolean gates follow the same audit-owned revision contract while retaining snapshot, rollback and CAD commit ordering checks.
- Door/WallOpening Direct Draw gates now require exact `AutoHostLinkCommands.LinkSingleOpening(document, project, createdElementId)`, forbid broad `AutoLinkHosts()` re-entry, re-resolve the authored semantic element and verify canonical `HostWallId` against the resolved host before scoped regeneration.
- Xref scale-state gate now matches the clarified `Tỉ lệ Xref` UI label while preserving the read-only/non-recursive Xref contract.

Implementation commits on `main`:

- `e23c32314495e2d1f162844f6d7dda23560de7f7`
- `a9b8c9cdd17184acdbae1036d687e344793c7cb5`
- `22af344e3eabe47f23c2efa8476abc46d9ccd403`
- `6a6bff3ebdc7b54db8e17c2897cddfb3dc0f5a73`
- `670c2cba4b45063c77d97dfb3864a8fbf546efe6`
- `68517455c46f688a74f4a1d6632c9b93e8d4bb3a`
- `cd75fb121e209b80d101cac95ba891c0dce6df86`
- `12e046e7cf48d0532ddd3cab7be19678cd32eaec`
- `64818fd1b078b9b55161be5261ccca8773794fe0`
- `781de50b559c1f03f6fbe9bc9193c29159291306`
- `831c94da068d79845a60fe8edd0892470e34f22d`

## Validation and coordination result

- `scripts/preflight-all.py` aggregate discovery was not disabled, filtered or converted to warnings.
- The generic source guard and manual-only release policy were already PASS in run #26 and were not weakened by this lane.
- Latest `main` readback after the implementation showed `831c94da068d79845a60fe8edd0892470e34f22d` remained an ancestor of current `main` with zero commits behind, so subsequent concurrent work did not rewrite this lane.
- No new V25 cloud release run exists yet after these commits; workflow run count remained 26 and run #26 still points to old SHA `0696f3cbcf602e140c3cad23282160641f2e659d`.
- Re-running failed jobs from run #26 would validate the old SHA, so it was intentionally not used as proof for current `main`.
- A local repository clone could not be obtained from the execution container because outbound GitHub DNS is unavailable there; therefore this remote lane does not claim a fresh local execution of every discovered preflight.
- Product-boundary reconciliation was left untouched after a concurrent write collision rather than overwriting another agent's active work.
- No claim is made that BricsCAD V25 licensed runtime, package/signing, installer, or clean-machine validation passed.

## Coordination

Owner explicitly requested immediate fix/update and push to `main`. All implementation writes in this lane used GitHub content-SHA writes and remained fast-forward-safe under concurrent agents.