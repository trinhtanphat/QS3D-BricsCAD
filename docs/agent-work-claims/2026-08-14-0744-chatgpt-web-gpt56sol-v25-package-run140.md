# Work claim — V25 preview package run #140

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T07:44:00+07:00`
- Baseline main SHA: `ebf41473af72970d7911b3b7ad5e3b9297b604ff`
- Priority: fresh V25 workflow run #140 passed deterministic Core smoke and plugin build, then failed at `Build V25 preview package`.

## Reserved scope

Triage and fix only the fresh CAD-independent packaging failure from V25 workflow run #140 (`31757912057`, job `94637682238`) after all earlier Core/build gates passed. Preserve release/version binding and do not weaken packaging validation to force green status.

## Expected surfaces

- GitHub Actions run #140 packaging-step evidence
- `.github/workflows/release-v25-cloud.yml` — read/diagnose and edit only if the failure is proven to be workflow-side
- `scripts/package-v25.ps1` — read/diagnose and edit only if the failure is proven to be package-script-side
- this claim file and issue/handoff status tied only to the package failure

## Excluded scope

- Core health/source-handle code and smoke tests from completed #1092 lane
- LOCAL_ONLY BricsCAD native qualification lanes
- unrelated feature preflights, V26 packaging, updater/product UX, or existing MAP/selection claims
- release publication/tag creation unless separately authorized by CI policy and required after source is proven green

## Validation plan

- obtain the exact run #140 package-step error before changing source/script/workflow
- preserve successful run #140 Core smoke and GitHub-hosted V25 plugin build gates
- run/read the narrowest deterministic validation available for the corrected packaging contract
- require a fresh exact-SHA workflow run before claiming end-to-end release success

## Coordination

Recent `main` commits and open PR review did not show a packaging claim/PR collision; open PR #1083 is MAP-01B and outside this lane. Recheck current `main` and exact-path claims immediately before every write. If another agent claims or writes the same packaging surface, stop rather than stack a duplicate patch.

## Completion condition

The exact packaging defect is fixed with the minimal justified change pushed to `main`, fresh evidence is recorded, and the claim becomes `COMPLETED`; otherwise remain `ACTIVE`/`BLOCKED` with exact evidence. If another source/test path is needed, publish a claim-only amendment before reading/diagnosing/editing/testing that added surface.
