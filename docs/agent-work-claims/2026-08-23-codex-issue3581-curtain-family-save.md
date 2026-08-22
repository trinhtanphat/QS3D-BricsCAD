# LOCAL-002 Curtain Family Save qualification

Status: ACTIVE

Lane-Key: `issue-3581`

Parent: #72 / LOCAL-002

Issue: #3581

Canonical branch: `agent/codex/issue3581-v25-curtain-family-save`

Baseline main: `7dfdc8b7d6a0bddb0eabcbd5d7f86487c839b8b8`

## Boundary

This lane qualifies only the remaining modeless `QS3DCURTAIN` Family Save row on licensed BricsCAD V25.2.10. It does not rerun or broaden the bounded P01-P12 Curtain matrix and cannot promote broad H.1 or full LOCAL-002 parity.

The exact runtime matrix covers inherited versus explicit instance overrides, representative numeric and material/frame-material propagation, over-1000-character rejection with rollback, a clean no-op Save, and save/cold-reopen coherence with existing generated Curtain output.

## Safety and evidence

- Use only a repository-public disposable fixture and ignored local runtime helpers/evidence.
- Keep private/customer drawings, proprietary binaries, object handles, project IDs, raw DWGs, and stack traces out of Git and GitHub.
- Require exact local/remote SHA identity, a clean tracked tree, zero pre-existing BricsCAD processes, focused source guards, Core smoke, and V25 Release x64 build before runtime.
- Keep the fixed V25 DemandLoad registration at `LoadCtrls=2`; do not change `SECURELOAD` or `TRUSTEDPATHS`.
- If runtime exposes a production defect, record the smallest sanitized reproduction and hand source work to a separate remote-safe issue.

Only a sanitized exact-SHA licensed result may change this claim from `ACTIVE / PENDING_LOCAL` to a bounded `LOCAL_PASS` or a precise blocked handoff.
