# LOCAL-002 Curtain Family Save qualification

Status: LOCAL_PASS (bounded Family-editor row)

Lane-Key: `issue-3581`

Parent: #72 / LOCAL-002

Issue: #3581

Canonical branch: `agent/codex/issue3581-v25-curtain-family-save`

Baseline main: `7dfdc8b7d6a0bddb0eabcbd5d7f86487c839b8b8`

Exact runtime candidate: `cf5f4a1938fcff403ffe8e2cd30184a6f1f7ce9d`

Contained `origin/main`: `425738e01c42cbaae02943939092463733fa1f51`

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

## Licensed result

The bounded Family-editor matrix passed on licensed BricsCAD V25.2.10 at the exact clean and pushed candidate above. The V25 Release x64 rebuild completed with zero warnings/errors, all seven focused Family/QSDB guards passed, and Core smoke ended in `ALL PASS`. Exact portable-PDB SourceLink identity was verified for the candidate. The tested V25 plugin SHA-256 was `B34D6203B9F78452DED365B8D784DECE1A6112EDCB0C7D6B96B82E5035076C75`; the colocated Core SHA-256 was `9A16E992924412B5B9CC7293F0499F0B6C3AA45F7C8997BD77B8D175BDA19643`.

The ignored runner used a repository-public disposable DWG with a production-created project, GlassWall Family, two real LINE sources and production semantic capture. It did not load or modify the independently defective shipped sample sidecar tracked by #3585, and no fixture workaround was applied. The real modeless `QS3DCURTAIN` Save path propagated ten accepted numeric/material values to the inherited member, preserved all ten explicit overrides and its timestamp, rejected over-limit `Material` and `CurtainFrameMaterial` values with clean rollback, and performed a clean no-op Save with unchanged project version/timestamp and regen count zero. Production rebuild, `QS3DSAVE`, native save and a fresh-process cold reopen preserved coherent Family/instance/native ownership with two host solids, 21 frame solids, 35 panel solids and zero Health issues.

Both BricsCAD sessions exited gracefully. The repository DWG and QSDB remained byte-identical, no process/script/sidecar/temp residue remained, and the fixed installed V25 DemandLoad registration was restored to its installed loader with `LoadCtrls=2`. The sanitized marker retained `production_local002_qualified=false`: this result closes only the Family-editor row and does not promote broad H.1 or all of LOCAL-002.
