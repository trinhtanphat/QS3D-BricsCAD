# LOCAL-002 modeless Curtain Family Save qualification

Status: ACTIVE
Agent: codex-local003-curtain-family-save-20260815
Issue: #1675
Branch: `agent/local003/local002-curtain-family-save-20260815`
Baseline: `6d0bde12266f3839752818ffeeb261852b73ae4e`
Inbox item: `LOCAL-002`

## Reserved scope

- Qualify the existing production `QS3DCURTAIN` modeless Family Save workflow on interactive Windows x64 with licensed BricsCAD V25.2.10.
- Use a disposable public fixture/current project containing a `GlassWall` Family, at least one inherited instance and one explicit instance override.
- Change representative numeric values plus `Material` and `CurtainFrameMaterial`; prove inherited values follow the Family while explicit overrides stay unchanged and are not dirtied solely by the Family change.
- Attempt a material/frame-material value longer than the canonical 1000-character Family-property limit and prove Save refuses or rolls back without semantic/native partial mutation.
- Repeat Save with a clean project and unchanged form values; prove no `ChangeVersion` or `UpdatedUtc` advance attributable only to the no-op Family write.
- Save, close and reopen the disposable drawing/project; verify accepted Family values and existing generated Curtain output remain coherent.
- Use only ignored local automation/probe files as needed and publish sanitized aggregate evidence.

## Explicit boundary

- Do not rerun, audit, broaden or promote the current-main P01-P12 bounded evidence.
- Do not claim broad LOCAL-002 parity or full H.1 closure.
- No production, Core, adapter, test, runner, workflow, packaging or release source changes are reserved.
- Any product/source defect returns to a non-local source-fix issue with the smallest sanitized reproduction.
- No private/customer drawing, proprietary BLT binary/API inspection, GitHub Actions dispatch/re-run/cancel, release, direct `main` write, force-push or merge.

## Validation plan

1. Push this claim and open a draft PR; verify local/remote exact-SHA identity before any build or licensed execution.
2. Require a clean worktree, zero pre-existing BricsCAD processes, full Core smoke `ALL PASS`, and installed-reference V25 `Release|x64` build with zero warnings/errors using the portable SDK sequentially.
3. Run only the bounded Family Save matrix through current production commands/UI on disposable public state.
4. Capture sanitized before/after Family, inherited/explicit instance, dirty/version/time, native Curtain count/ownership, rejection, no-op and save/reopen markers.
5. Require exact-PID graceful exit, unchanged canonical fixture and zero process/script/sidecar/lock/private residue.
6. Commit and push only this claim plus bounded inbox/handoff evidence, update Issue/PR, and stop before merge.

## Expected repository surfaces

- `docs/agent-work-claims/2026-08-15-codex-local002-curtain-family-save.md`
- `docs/LOCAL-AGENT-INBOX.md` (bounded sanitized result only)
- a focused existing LOCAL-002/Curtain handoff document only if current inbox detail is insufficient
