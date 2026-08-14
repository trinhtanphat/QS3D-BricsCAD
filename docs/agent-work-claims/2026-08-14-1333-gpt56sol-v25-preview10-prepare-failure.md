# Work claim — V25 preview.10 release-source preparation failure

- Status: `BLOCKED`
- Agent: `gpt56sol-v25-preview10-prepare-failure-20260814-1333`
- Registered: `2026-08-14T13:33:00+07:00`
- Blocked: `2026-08-14T13:36:00+07:00`
- Baseline observed main SHA: `1edfd985b366b52992a3ac28ca3018f7ed569dd4`
- Claim commit: `9f606c92c9b61933186ba15434b8a77e73da312f`
- Failure run: `#150` / `31776510479`
- Failure job: `94692954595`
- Failure head SHA: `8bad1dc3430230279f54dd03d181b456789ab1a4`
- Failing step: `Prepare exact release source commit`
- Priority: `P0 / cloud release blocker`

## Confirmed evidence

Run #150 completed after the predecessor V25 release-order claim had already been closed. Request validation succeeded, then `Prepare exact release source commit` failed; setup, source guards, build, package and publish were skipped.

Under this claim, the owned workflow step was read at failed head `8bad1dc3430230279f54dd03d181b456789ab1a4`. It invokes `scripts/prepare-v25-cloud-release.ps1` and validates the returned exact SHA/HEAD identity. The helper itself was **not read or edited**, because it was not yet included in the initial reservation.

## Collision discovered after claim publication

A concurrent CI lane modified the same workflow surface after this claim landed:

- `f9466f3400e0c85b4702646ecf62b4d11d8f86fe` — changed the V25 release checkout to `fetch-depth: 1` plus `fetch-tags: true` to reduce the pre-lock race window;
- a one-shot dispatcher launched run `#151` (`31776786359`) on head `06311079fd16a2ecefe5a7d52d911e02b7892404`;
- run #151 again failed at `Prepare exact release source commit` after request validation succeeded;
- subsequent concurrent commit `de2d032f62caacd9583fa4e61db7dcf2d39c5523` created another preview.10 dispatcher intended to remove a cleanup race.

Because the concurrent lane is actively modifying/dispatching the exact release-preparation workflow, this claim stops before any implementation/helper expansion. No overlapping patch is created.

## Reserved scope status

- `.github/workflows/release-v25-cloud.yml`: **released to active concurrent CI lane; no write performed by this claim**.
- `scripts/prepare-v25-cloud-release.ps1`: **never claimed/read/edited by this claim**.
- this claim file: coordination status only.

## Non-scope preserved

- no product Core/UI/native behavior changes;
- no #1005/#1106/#1125/#79/#982 work;
- no unrelated workflow refactor;
- no weakening manual-only CI policy, release confirmation, version binding, source cleanliness, exact-SHA or package integrity gates;
- no licensed BricsCAD runtime claim.

## CI boundary

No GitHub Actions were dispatched or rerun by this claim. Runs #151 and later dispatchers were created by the concurrent lane.

## Unblock condition

This claim may only be reopened after the concurrent release-preparation lane stops/closes and a fresh failing run still proves the same step remains broken. At that point a new collision scan and claim amendment for the exact helper/test paths are required before implementation diagnosis.
