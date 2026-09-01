# CI guide

`CI_POLICY.md` at the repository root is the **single canonical CI policy**.

This file is only a navigation aid. It must not define a second trigger/merge model.

## Current validation model

- `.github/workflows/ci.yml` automatically validates `agent/**`, `integration/**`, and protected PR candidates.
- branch CI is early exact-head evidence;
- protected current-candidate `preflight` + `core`, strict freshness and mergeability are the hard merge gate;
- a canonical PR does not become invalid merely because branch CI completed after PR creation;
- red current-lane CI is remediated on the same carrier;
- publishing/release workflows remain separate from ordinary task validation.

Host-specific build qualification remains defined by the applicable architecture/release runbooks. For BricsCAD V26 those references include `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll`; this note is compatibility/navigation context, not a second CI policy.

For details read:

1. `../CI_POLICY.md`;
2. `PR-CI-LIFECYCLE.md` when branch/PR timing matters;
3. `MAIN-WRITE-AUTHORIZATION.md` for merge authorization;
4. the applicable release/local runbook only when release or licensed runtime is actually in scope.

Historical CI notes and recorded runs qualify only the exact historical SHAs they tested.