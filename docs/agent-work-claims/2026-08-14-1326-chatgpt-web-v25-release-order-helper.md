# V25 release ordering one-shot helper repair

Status: ACTIVE
Owner: chatgpt-web-gpt56sol
Started: 2026-08-14 13:26 +07:00
Baseline main: `35507fe35b2c49fe41400167d0e28b67aab29c65`

## Evidence

- One-shot workflow run `31776101565`, job `94691733963` failed in `Move release preparation before toolchain setup` with `Expected exactly one toolchain setup block, got 0`.
- The helper embeds `${{ ... }}` workflow expressions inside a `run:` script string. GitHub Actions expands those expressions before Python runs, so the expected source block no longer matches the literal workflow text.

## Scope

- Only `.github/workflows/fix-v25-release-order-once-20260814.yml` and this claim.
- Replace brittle exact-text matching with bounded step-boundary extraction/reinsertion so the helper moves setup-python/setup-dotnet/NuGet cache after `Prepare exact release source commit` without evaluating workflow expressions.
- Preserve `release-v25-cloud.yml` stale-main/concurrency guard and release semantics.

## Exclusions

Do not modify Curtain/#1105/#1106, LOCAL_ONLY/native qualification lanes, `prepare-v25-cloud-release.ps1`, or unrelated workflows/source.

## Completion

Commit/push helper fix to `main`, inspect its fresh push-triggered run, verify the intended release-workflow ordering commit if produced, then close SOURCE_FIXED with exact SHAs.
