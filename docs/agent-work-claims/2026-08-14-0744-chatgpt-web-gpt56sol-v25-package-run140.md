# Work claim — V25 preview package run #140

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T07:44:00+07:00`
- Baseline main SHA: `ebf41473af72970d7911b3b7ad5e3b9297b604ff`
- Priority: fresh V25 workflow run #140 passed deterministic Core smoke and plugin build, then failed at `Build V25 preview package`.

## Reserved scope

Triage and fix only the fresh CAD-independent packaging failure from V25 workflow run #140 (`31757912057`, job `94637682238`) after all earlier Core/build gates passed. Preserve release/version binding and do not weaken packaging validation to force green status.

Exact failure evidence: the workflow was dispatched with `RELEASE_TAG=v0.1.0-preview.6`, while the checked-out V25/V26/Core product identity remained `0.1.0-preview.5`; `scripts/package-v25.ps1` correctly rejected that mismatch. The already-published `v0.1.0-preview.5` release means the justified source-side correction is to advance the aligned product identity to preview.6, not to weaken the package guard or dispatch preview.5 again.

## Expected surfaces

- GitHub Actions run #140 packaging-step evidence
- `.github/workflows/release-v25-cloud.yml` — read/diagnose and edit only if the failure is proven to be workflow-side
- `scripts/package-v25.ps1` — read/diagnose; preserve its exact source/tag version guard
- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` — aligned preview product identity
- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj` — aligned preview product identity required by runtime-version guard
- `src/QS3D.Core/QS3D.Core.csproj` — aligned preview product identity required by package/runtime-version guards
- `scripts/preflight-runtime-product-version-identity.py` — read/validate only; preserve V25/V26/Core identity equality
- this claim file and issue/handoff status tied only to the package failure

## Excluded scope

- Core health/source-handle code and smoke tests from completed #1092 lane
- LOCAL_ONLY BricsCAD native qualification lanes
- unrelated feature preflights, V26 packaging behavior, updater/product UX, or existing MAP/selection claims
- release publication/tag creation unless separately authorized by CI policy and required after source is proven green

## Validation plan

- preserve successful run #140 Core smoke and GitHub-hosted V25 plugin build gates
- advance V25/V26/Core `Version`, `FileVersion`, and `InformationalVersion` together to `0.1.0-preview.6` / `0.1.0.6`
- keep `AssemblyVersion` stable at `0.1.0.0`
- preserve the strict package source/tag version check
- require a fresh exact-SHA workflow run with `release_tag=v0.1.0-preview.6` before claiming end-to-end release success

## Coordination

Recent `main` commits and open PR review did not show a packaging claim/PR collision; open PR #1083 is MAP-01B and outside this lane. Recheck current `main` and exact-path claims immediately before every write. If another agent claims or writes the same packaging/version surfaces, stop rather than stack a duplicate patch.

## Completion condition

The exact packaging defect is fixed with the minimal justified change pushed to `main`, fresh evidence is recorded, and the claim becomes `COMPLETED`; otherwise remain `ACTIVE`/`BLOCKED` with exact evidence.
