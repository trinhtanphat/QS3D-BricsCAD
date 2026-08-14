# Work claim — V25 preview.10 release-source preparation failure

- Status: `ACTIVE`
- Agent: `gpt56sol-v25-preview10-prepare-failure-20260814-1333`
- Registered: `2026-08-14T13:33:00+07:00`
- Baseline observed main SHA: `1edfd985b366b52992a3ac28ca3018f7ed569dd4`
- Failure run: `#150` / `31776510479`
- Failure job: `94692954595`
- Failure head SHA: `8bad1dc3430230279f54dd03d181b456789ab1a4`
- Failing step: `Prepare exact release source commit`
- Priority: `P0 / cloud release blocker`

## Confirmed fresh evidence

Run #150 completed after the predecessor V25 release-order claim had already been closed. Request validation succeeded, then `Prepare exact release source commit` failed; setup, source guards, build, package and publish were skipped. This is therefore fresh post-closeout evidence and requires a new claim rather than reusing the completed lane.

## Initial reserved scope

- `.github/workflows/release-v25-cloud.yml` — only the `Prepare exact release source commit` step and data/control flow directly required for that step.
- this claim file.

The exact helper script invoked by that step is intentionally not guessed. After reading the claimed workflow step, if a helper outside the current reservation must be inspected or changed, this claim will be amended in a claim-only commit **before** reading/editing that helper implementation.

## Intended diagnostic sequence

1. Read the current and failed-head version of the claimed preparation step.
2. Identify the exact helper/command and inputs used by the step.
3. Amend this claim before expanding into any helper/test surface.
4. Reconstruct the deterministic failure from source/contracts; prefer a narrow regression/preflight guard over a broad workflow rewrite.
5. Push only evidence-backed changes.

## Non-scope

- no product Core/UI/native behavior changes;
- no #1005/#1106/#1125/#79/#982 work;
- no unrelated workflow refactor;
- no weakening manual-only CI policy, release confirmation, version binding, source cleanliness, exact-SHA or package integrity gates;
- no licensed BricsCAD runtime claim.

## CI boundary

This claim does not itself authorize a new manual workflow dispatch. Source repair and deterministic static/regression validation may be committed; a new Actions dispatch will only be performed if separately authorized by the user/policy in this session.

## Completion condition

The exact post-#150 preparation defect is either fixed with a focused guard and remote readback, or the claim is closed `BLOCKED` with precise evidence showing why source-only diagnosis cannot safely proceed.
