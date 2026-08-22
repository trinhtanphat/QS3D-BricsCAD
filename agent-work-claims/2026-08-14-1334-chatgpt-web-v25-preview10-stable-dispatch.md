# V25 preview.10 stable one-shot dispatch

Status: ACTIVE
Owner: chatgpt-web-gpt56sol
Started: 2026-08-14 13:34 +07:00
Baseline main: `49af187faf5a383ed9cdc6af78e8859d77babd6c`

## Scope

- Add one uniquely named temporary GitHub Actions dispatcher file only.
- The dispatcher triggers once when that file is added, immediately invokes `release-v25-cloud.yml` on `--ref main` with `release_tag=v0.1.0-preview.10` and `confirm_release=RELEASE`, and then performs no repository write/cleanup.
- Leave the dispatcher file untouched until the release run reaches a terminal state so this lane itself cannot invalidate the release SHA.

## Evidence / reason

- Release runs #148, #149 and #150 were intentionally aborted by the exact-dispatch/main-moved safety guard, not by source/build defects.
- `cf732f646573e4f5d690f276d63ddec5d35b992a` moved release preparation before toolchain setup.
- `229ecbd108211775c9802e726b2dacdc8ebfd8e8` switched the pre-lock checkout to shallow fetch with tags, reducing the race window further.
- Earlier preview.10 dispatcher cleanup moved `main` before dispatch and contributed to stale qualification. This dispatcher intentionally does not clean itself up before or during the release run.

## Ownership boundary

Only this claim and `.github/workflows/dispatch-v25-preview10-once-20260814-1334.yml`. Do not modify `.github/workflows/release-v25-cloud.yml`, production source, Curtain/#1105/#1106, LOCAL_ONLY/native lanes, or other agents' claims.

## Completion

Commit the one-shot dispatcher to `main`, inspect its Actions run, inspect the resulting V25 cloud release run end-to-end, and only after that run is terminal update this claim/cleanup as appropriate.
