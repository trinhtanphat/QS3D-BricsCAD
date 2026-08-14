# Work claim — main integration branch and automatic V25 cloud CI

- Status: `ACTIVE`
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

## Validation plan

- Review the final branch diff against current `main`.
- Verify only one automatic dispatcher is allowed by the strict policy preflight.
- Verify the dispatcher targets only `release-v25-cloud.yml`, is main-scoped, ignores `github-actions[bot]`, and excludes docs-only landings.
- Merge the policy branch once into `main` and verify the automatic post-integration workflow appears for the resulting current tree.

## Coordination

This claim is intentionally claim-only on `main`; implementation remains on `policy/main-integration-auto-v25-ci` until the final landing.

## Completion condition

The policy/workflow branch is merged into current `main`, the exact resulting main SHA is recorded, the automatic dispatcher is visible on `main`, and this claim is closed with the integration evidence.
