# Work claim — LOCAL-002/P02 licensed Curtain opening-clipping qualification

- Status: `ACTIVE`
- Agent: `codex-local-curtain-p02-runtime-qualification-20260812` (`/root`)
- Registered: `2026-08-12T12:10:00+07:00`
- Baseline main SHA: `ef093faa058b39d9c246d90ca55c0b22591f5f76`
- Priority: `LOCAL-002 / P0 / P02` — run the prepared BLT-style Curtain panel opening-clipping matrix in the installed BricsCAD V25 environment.

## Reserved scope

- Run the merged guarded P02 runner against a fresh ordinary copy of the repository-generated synthetic DWG, an empty artifact directory outside the repository, and the exact clean `main` SHA/DLL built locally for V25 `Release|x64`.
- Record only the sanitized V2 marker/metadata, exact Git/plugin/V25 identity, source-copy hash preservation, process/script/sidecar cleanup booleans and aggregate PASS fields. Never publish raw paths, Handles, semantic IDs, drawing content, private scripts or workbook data.
- If the run passes, update only the bounded `LOCAL-002/P02` evidence in `docs/CURTAIN-NATIVE-PANELS.md` and `docs/LOCAL-AGENT-INBOX.md`, then close this claim through normal PRs.
- If the run fails, report only the allowlisted `failure_phase`, `failure_code` and cleanup booleans. Production source remains read-only until that evidence supports a separately published, non-overlapping claim expansion.
- This claim for close-out.

## Excluded scope

- No customer/private/BLT drawing or workbook; no modification of the owner synthetic sample.
- No production Curtain planner/builder/Health/ownership/Level edit in this claim.
- No P03-P12, release, packaging, signing or GitHub Actions dispatch.
- No `LOCAL_PASS` or broad LOCAL-002 completion claim unless every P02 assertion and cleanup invariant passes on the exact candidate.

## Validation and completion

- Claim-only reservation must be merged and visible on `origin/main` before the licensed run.
- Re-pin a clean exact `main`, confirm no BricsCAD process or sidecar, run the focused P02 gate and installed-reference V25 build, then execute the guarded runner once on a new disposable copy.
- Verify the disposable DWG hash is unchanged, the launched PID exited, the private script was deleted, no sidecar remains, and the marker/metadata contain only the allowlisted aggregate schema.
- Deliver any evidence/doc change and claim close-out through normal commit/push/PR/merge without force-push or workflow dispatch.
