# V25 cloud release Platform dependency materialization

Status: ACTIVE
Lane-Key: issue-3482
Issue: #3482
Agent: chatgpt-gpt56sol
Session: v25-release-platform-submodule
Baseline main: `18b19a2bbdc0701f3cc1655d984c7a94330c2475`
Canonical branch: `agent/chatgpt-gpt56sol/issue-3482-v25-release-platform-submodule`
Canonical PR: pending
Supersedes: none

## Scope
- Fix `release-v25-cloud.yml` so the exact pinned `external/QS3D-Platform` gitlink is materialized after source guards and before Core restore/build.
- Keep source guards scoped to the BricsCAD repository rather than recursively scanning the external dependency.
- Preserve exact release source SHA/tag validation and immutable action pins.

## Evidence
- Failing cloud release run: `32550842654`
- Failing job: `96977193417`
- Failure: missing `external/QS3D-Platform/src/QS3D.Platform.Parity/QS3D.Platform.Parity.csproj`, followed by cascading missing `QS3D.Platform`, `CoordinationIssue`, and `CadReference` compiler errors.
