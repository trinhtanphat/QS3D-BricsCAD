# Work claim — main integration branch and automatic V25 cloud CI

- Status: `COMPLETED`
- Agent: `chatgpt/github-integration`
- Registered: `2026-08-14T17:55:00+07:00`
- Baseline main SHA: `fd18d18d268513cc07d0f90a82c4c4fe23ad7d67`
- Implementation branch: `policy/main-integration-auto-v25-ci`
- Integration batch: `policy/main-integration-auto-v25-ci`
- Priority: owner explicitly requested branch-based multi-agent integration followed by automatic V25 cloud CI after the final main merge.

## Reserved scope

Define and enforce the repository-wide landing model where agent implementation stays off `main`, participating work is combined before one final main landing, and that landing automatically dispatches the V25 cloud release workflow.

## Expected surfaces

- `CI_POLICY.md`
- `docs/AGENT-WORK-REGISTRATION.md`
- `scripts/preflight-ci-manual-only.py`
- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`
- integration/CI policy only; no product feature implementation.

## Excluded scope

- BricsCAD product feature/source changes.
- V25/V26 native runtime qualification.
- Changes to the implementation of `release-v25-cloud.yml` itself unless a verified integration-policy defect requires it.
- Unrelated CI workflows.

## Validation performed

- Reviewed the final branch diff against current `main`; the implementation changed only the two canonical policy Markdown files, the strict CI-policy preflight and the single automatic dispatcher workflow.
- PR `#1302` was squash-merged once into `main`.
- Final policy landing SHA: `8dc9c4012ff7f980837bfdb6a71529fc57178344`.
- Automatic dispatcher workflow was read back from `main` and GitHub created push run `31794250414` / run number `1` for that exact policy landing.
- The dispatcher is scoped to `main`, ignores `github-actions[bot]`, excludes docs-only landings and may dispatch only `release-v25-cloud.yml` with `confirm_release=RELEASE`.

## Coordination

The claim-only reservation was published separately on `main`; implementation remained on `policy/main-integration-auto-v25-ci` until PR `#1302` performed the single final landing.

## Completion

Repository policy now requires agent implementation branches to be combined through `integration/<batch-id>` and landed to `main` once. That landing automatically starts the approved V25 cloud CI path. Documentation-only claim/close-out commits do not retrigger it.
