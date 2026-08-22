# Work claim — LOCAL-002/P02 licensed Curtain opening-clipping qualification

- Status: `COMPLETED`
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

## Licensed result — 2026-08-12

- The first V2 run on clean exact SHA `7c160de66de68c811282f4cd460e927370e454cd` failed only at allowlisted `DOOR_NATIVE_GEOMETRY / STATE_REJECTED`; disposable-DWG hash, launched-process, private-script, sidecar, backup and metadata cleanup all passed. That evidence supported separately published claim `#845` and centered-box fix `#850`, closed by `#851`.
- The fresh post-fix run passed on clean exact SHA `7b4a379da15c8c0bed60536bc0ccca7334eb4712`, BricsCAD `25.2.10`, and exact x64 Release plugin SHA-256 `25B6A40F120028CED160F5F04362FFAE1FBEA25E0A850CEE45860E761559B53F`.
- Door partial/full-cell clipping: 15 source cells, 16 positive fragments, one fully removed cell, five partially clipped cells, 16/16 native/Core-plan matches and zero positive native/opening intersections.
- WallOpening complete-empty: 15/15 cells removed, zero output pieces/handles, healthy canonical `Complete` and opening-aware metadata.
- Ownership sets were disjoint, Health issue count was zero, Locate resolved one panel to one canonical owner, and source geometry was preserved.
- The repository-generated disposable drawing hash remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; launched-process exit, private-script deletion and sidecar/backup absence were independently verified. No customer/private/BLT artifact or GitHub Actions workflow was used.
- This proves only bounded P02. P03-P12 and overall LOCAL-002 remain `PENDING_LOCAL`.

## Close-out — 2026-08-12

- Claim-only PR `#841` merged as `79782b0882f6e144f0549ce143d5364830b87eb4` before the licensed run.
- The first sanitized V2 failure supported separately claimed centered-box correction PRs `#845`, `#850` and `#851`; the fresh post-fix exact-SHA run then satisfied every bounded P02 marker and cleanup invariant.
- Evidence/docs PR `#859` merged as `ccd2196ad7d5727b17ba59d5a2e5bd7093c75547` and records the exact runtime identity, aggregate counts and narrow status boundary.
- P02 is bounded `LOCAL_PASS`; P03-P12 and overall LOCAL-002 remain open and `PENDING_LOCAL`. No GitHub Actions, release, signing, package publication or private/customer/BLT artifact was used.
