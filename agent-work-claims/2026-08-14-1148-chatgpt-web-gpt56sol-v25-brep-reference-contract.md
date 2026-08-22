# Agent work claim — V25 BREP compile-reference contract

- Agent: `chatgpt-web-gpt56sol-v25-brep-reference-contract-20260814-1148`
- Date: 2026-08-14
- Status: `COMPLETED`
- Base observed before claim: `10a412288ecc13b12416d64ee99c7af0f7cc50eb`
- Claim commit: `ae11d27c8b6b1905ef02123aeb451d0fb7d3fcc9`

## Goal

Keep every V25 build/release reference gate synchronized with the V25 project after `TD_MgdBrep.dll` became a required compile reference. The project must not require an assembly that the workflow acquisition/validation contracts ignore.

## Reserved paths

- `.github/workflows/bricscad-v25.yml`
- `.github/workflows/release-v25.yml`
- `.github/workflows/release-v25-cloud.yml`
- `scripts/preflight-v25-compile-reference-contract.py`
- `docs/agent-work-claims/2026-08-14-1148-chatgpt-web-gpt56sol-v25-brep-reference-contract.md`

Read-only evidence surface:

- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj`

## Evidence

The V25 project declares and validates `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll`. Before this lane, the self-hosted V25 integration workflow and manual V25 release workflow validated only the first two managed assemblies, while the cloud workflow located/selected a runtime directory using only `BrxMgd.dll` + `TD_Mgd.dll` and its later compile-reference gate also checked only those two. That allowed workflow preconditions to report healthy even when the build-required BREP assembly was absent or the selected extracted directory was incomplete.

## Result

- `54048742d14fba72eac42612ca893e5df4d82bff` — `fix(v25): validate BREP compile reference`
  - self-hosted V25 integration now fails fast when `TD_MgdBrep.dll` is missing.
- `2191050d573075dfe07efa4e57741ce1384bf706` — `fix(release): require V25 BREP reference`
  - manual V25 release reference validation now includes `TD_MgdBrep.dll` with the existing BricsCAD/managed-reference gate.
- `674aa692e92255a112dc1ea906614d54183af33a` — `fix(cloud): resolve complete V25 compile references`
  - cloud MSI extraction now discovers `TD_MgdBrep.dll`, requires Brx/TD/BREP availability, only selects a directory containing all three managed references, and revalidates all three before the V25 build.
- `b1040d42581b60161af426f90a5645dcfa1e8344` — `test(preflight): guard V25 compile reference contract`
  - added `scripts/preflight-v25-compile-reference-contract.py`.
  - the guard derives managed `$(BRICSCAD_V25_DIR)` references from the V25 project, requires all project-managed references to remain represented in each V25 workflow, and locks the current BREP discovery/co-location/validation contract.

## Validation

- Exact-source GitHub read-back: `PASS` for all three workflow changes and the new preflight source.
- Aggregate discovery contract: `PASS` by source inspection; current `scripts/preflight-all.py` discovers every `scripts/preflight-*.py` except itself, so the new guard is included automatically.
- Fresh execution of `scripts/preflight-v25-compile-reference-contract.py`: `NOT_RUN` in this web-only connector lane; no local checkout/toolchain is available here.
- GitHub Actions run `#147` (`31770836729`) completed `SUCCESS`, but it ran on the earlier release snapshot (`f29e6bc8206aa7599c43aa6d2ab4d624079e4411` / prepared preview.9 source) before these BREP workflow commits. It is historical release evidence only and is not claimed as validation of this descendant patch.
- No workflow was dispatched, canceled, or rerun by this lane.
- Licensed/native BricsCAD runtime validation was not required for this workflow-reference contract and was not claimed.

## Boundaries preserved

- Geometry/quantity behavior and the V25 project reference were not changed.
- No compile-reference requirement was weakened.
- `#982` / `#1005` native acceptance boundaries were untouched.
- The concurrent V25 MOTW/manual-NETLOAD package lane uses different reserved paths and remains independent.
