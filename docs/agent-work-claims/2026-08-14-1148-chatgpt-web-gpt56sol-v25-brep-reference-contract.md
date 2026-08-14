# Agent work claim — V25 BREP compile-reference contract

- Agent: `chatgpt-web-gpt56sol-v25-brep-reference-contract-20260814-1148`
- Date: 2026-08-14
- Status: `ACTIVE`
- Base observed before claim: `10a412288ecc13b12416d64ee99c7af0f7cc50eb`

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

Current V25 project declares and validates `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll`. The self-hosted V25 integration workflow and manual V25 release workflow validate only the first two managed assemblies, while the cloud workflow locates/selects a runtime directory using only `BrxMgd.dll` + `TD_Mgd.dll` and its later compile-reference gate also checks only those two. This lets workflow preconditions report healthy even when the build-required BREP assembly is absent or the selected extracted directory is incomplete.

## Boundaries

- Do not change geometry/quantity behavior or the V25 project reference itself.
- Do not weaken any compile-reference requirement.
- Do not cancel, rerun, or otherwise interfere with release V25 run `#147`; it is independent evidence on an already-prepared release snapshot.
- Add a static regression guard so future V25 project reference additions cannot silently drift from workflow gates.
- Refresh `main` before every write and stop/re-scope if another agent touches any reserved path.
