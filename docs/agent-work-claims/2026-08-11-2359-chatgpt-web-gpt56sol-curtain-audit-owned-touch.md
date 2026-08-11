# Work claim — Curtain generated audit-owned Touch

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:59:00+07:00`
- Baseline main SHA: `64f375afa565a86e73bc0dd1cdfade2b642b9d59`
- Priority: evidence-driven remote-safe native lifecycle correctness

## Reason

Four existing native Curtain generated-output builders already recorded one dedicated `AuditTrail` mutation per successfully updated GlassWall element, and `AuditTrail.Record(...)` owns `ProjectState.Touch()`. Each batch nevertheless performed an additional `if (pending.Count > 0) project.Touch();` immediately after its audited semantic-update loop and before CAD transaction commit, advancing `ChangeVersion` once beyond its audited mutations.

## Reserved scope

Remove only the redundant batch-level explicit Touch from `CurtainWallFrameSolidBuilder`, `CurtainWallPanelSolidBuilder`, `CurtainWallPathFrameSolidBuilder`, and `CurtainWallPathPanelSolidBuilder`; preserve all geometry/opening/path/ownership/rollback/transaction behavior; add one shared auto-discovered static preflight.

## Completion evidence

- PR #554 merged to `main` as `cb440ecbc92e282c76c7c9497e9048e7f5379df3`.
- PR scope was exactly five files: the four reserved Curtain builders plus `scripts/preflight-curtain-audit-owned-touch.py`.
- Compare against 36 concurrent commits after the implementation branch baseline showed no reserved-file overlap.
- All four semantic commit helpers retain their dedicated audit actions: `geometry.curtain.frames`, `geometry.curtain.panels`, `geometry.curtain.path.frames`, and `geometry.curtain.path.panels`.
- The redundant Build-level `project.Touch()` was removed from all four builders; rollback snapshots, native transactions, ownership, layout/opening/path logic, and CAD commit order were otherwise preserved.
- Post-merge exact blob verification: straight frame `8258a7a1b7a763e46500a41777ebfac7c0ea0d9d`; straight panel `08d5cb810fe6b30945ec53906327a47c45ab855d`; path frame `7ec5a8a7eb20325c169c424bd2164b276df10ea0`; path panel `4bb90430108cb14e88907aa70a1ce2d2727f7572`; preflight `7270e987da323464eea092a0b987ea937ef8e155`.
- No force-push, GitHub Actions dispatch, release workflow, or licensed BricsCAD V25 runtime claim.

## Excluded scope

No Curtain geometry/layout formulas, opening interruption, panel/frame dimensions, path tessellation/sagitta, generated ownership, source binding, UI/commands, `AuditTrail` semantics, or runtime certification changed.

## Completion condition

Completed: all four Curtain generated-output builders now advance project revision only through their existing per-element audit records and the shared static regression gate is on `main`.