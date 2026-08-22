# Work claim — V25 preview.10 release-source preparation failure

- Status: `COMPLETED / RESOLVED_BY_CONCURRENT_LANE`
- Agent: `gpt56sol-v25-preview10-prepare-failure-20260814-1333`
- Registered: `2026-08-14T13:33:00+07:00`
- Blocked on collision: `2026-08-14T13:36:00+07:00`
- Resolved: `2026-08-14T13:41:00+07:00`
- Baseline observed main SHA: `1edfd985b366b52992a3ac28ca3018f7ed569dd4`
- Claim commit: `9f606c92c9b61933186ba15434b8a77e73da312f`
- Collision/block commit: `3872a6a4ef8840895d48058c61872ce331560b49`
- Original failure run: `#150` / `31776510479`
- Original failure job: `94692954595`
- Original failure head SHA: `8bad1dc3430230279f54dd03d181b456789ab1a4`
- Original failing step: `Prepare exact release source commit`
- Priority: `P0 / cloud release blocker`

## Original evidence

Run #150 completed after the predecessor V25 release-order claim had already been closed. Request validation succeeded, then `Prepare exact release source commit` failed; setup, source guards, build, package and publish were skipped.

Under this claim, the owned workflow step was read at failed head `8bad1dc3430230279f54dd03d181b456789ab1a4`. It invokes `scripts/prepare-v25-cloud-release.ps1` and validates the returned exact SHA/HEAD identity. The helper itself was **not read or edited**, because it was not yet included in the initial reservation.

## Collision and concurrent resolution

A concurrent CI lane modified the release-preparation path after this claim landed. This claim therefore stopped without overlapping implementation work.

Relevant concurrent changes included:

- `f9466f3400e0c85b4702646ecf62b4d11d8f86fe` — reduced the pre-lock checkout to shallow history plus tags;
- `1314621e6f7e22e2bb177f76ba55bae725c4da0f` — allowed an immutable prepared release SHA to remain valid after `main` advances;
- `bb16cd200ac11ecfa2be82ca3bd37600820667f7` — synchronized source identity to `v0.1.0-preview.10` before the immutable dispatch path.

Fresh run `#154` (`31777027282`, job `94694612999`, dispatched SHA `2c7a1dd73b14902768f2b7138312e1c6778f0f4d`) proves the original blocker is resolved: `Validate cloud prerelease request` passed and **`Prepare exact release source commit` passed**. The helper reported that source identity already matched `v0.1.0-preview.10` and retained the immutable release commit `2c7a1dd73b14902768f2b7138312e1c6778f0f4d` even if `main` advanced later.

Run #154 subsequently failed at a **different** gate, `Manual-only CI policy gate`, because the dispatched immutable SHA still contained the temporary push-trigger dispatcher `dispatch-v25-preview10-once-20260814-1334.yml`. That gate correctly reported four manual-only policy violations for the temporary dispatcher. The temporary dispatcher was later removed from `main` by concurrent commit `8f6df2feba46b75426ea2ce539f5d1775613fab6`.

The new manual-only dispatcher failure is outside this claim's original release-source-preparation defect and remains owned by the active stable-dispatch coordination lane. This claim does not take over or weaken that policy gate.

## Reserved scope status

- `.github/workflows/release-v25-cloud.yml`: no write performed by this claim.
- `scripts/prepare-v25-cloud-release.ps1`: never claimed/read/edited by this claim.
- this claim file: coordination closeout only.

## Validation boundary

- Original `Prepare exact release source commit` failure: **RESOLVED**, proven by run #154 step success.
- Full V25 preview.10 release: **NOT claimed successful here**; run #154 failed later at the manual-only policy gate.
- No GitHub Actions were dispatched or rerun by this claim.
- No product Core/UI/native behavior changed by this claim.
- No licensed BricsCAD runtime PASS is claimed.

## Completion

Complete as `RESOLVED_BY_CONCURRENT_LANE`: the exact defect reserved by this claim is no longer failing, collision rules were honored, and the newly exposed manual-only dispatcher blocker is kept separate under its existing active ownership.
