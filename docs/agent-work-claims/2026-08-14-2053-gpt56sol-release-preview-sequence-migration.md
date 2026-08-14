# Work claim — release preview sequence and historical migration guard

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-review`
- Registered: `2026-08-14T20:53:00+07:00`
- Baseline main SHA: `9f2f7e58ab1f81ad652823c6eada18646ad61f8e`
- Implementation branch: `agent/chatgpt-web-gpt56sol-review/release-preview-sequence-migration-20260814`
- Integration batch: `integration/chatgpt-web-gpt56sol-review-release-preview-sequence-migration-20260814`
- Priority: repository-wide review found a deterministic release-policy gap immediately after the positive-ordinal lane completed.

## Reserved scope

Enforce the canonical preview-series ordering contract during V25 public release preparation. A requested preview tag must be the next ordinal in its exact `vMAJOR.MINOR.PATCH-preview.N` series; if no prior tag exists, `N` must be `1`. This also enforces the documented historical `v0.1.0-preview.10014` migration boundary, so a smaller `v0.1.0-preview.N` cannot be published after `.10014`.

## Expected surfaces

- `.github/workflows/release-v25-cloud.yml` — validate requested preview series against fetched Git tags before release preparation/publishing.
- `scripts/prepare-v25-cloud-release.ps1` — revalidate the same sequence immediately before source identity mutation/commit so direct invocation cannot bypass the public release policy.
- `scripts/preflight-release-preview-sequence.py` — deterministic source guard for first-series, increment, gap, duplicate/older and historical `.10014` migration cases.
- this claim for implementation/integration close-out.

## Exclusions / collision boundaries

- Do not reopen or change the completed positive-ordinal contract except where composing the sequence check requires parsing the already-valid preview tag.
- Do not add alpha/beta/rc publication to the V25 *preview* workflow; this lane only governs the existing preview release path.
- Do not delete, retag, move or mutate historical releases/tags.
- Do not derive public ordinals from Actions run numbers, commit counts, timestamps or other build metadata.
- Do not enter the ACTIVE `slabOpen` lane, issue #1005 Source Reconcile/Undo, BricsCAD native runtime, signing/licensing, drawing-fingerprint or other feature scopes.
- No manual Actions dispatch; final source landing relies on the standing automatic post-integration dispatcher.

## Evidence / reason

`docs/RELEASE-NAMING.md` requires `N` to increase by exactly one per published prerelease series and explicitly states that, because historical `v0.1.0-preview.10014` exists, smaller ordinals such as `v0.1.0-preview.1` must not be published on that same series. Current `scripts/preflight-release-preview-ordinal.py` intentionally treats `v0.1.0-preview.1` as syntactically valid and the completed lane only rejects zero/leading-zero ordinals; current release preparation does not yet enforce series history.

## Validation plan

- Keep the existing positive-ordinal preflight green.
- Add deterministic sequence cases: no prior series -> `.1`; prior `.1` -> `.2`; prior `.1,.2` -> `.3`; reject duplicate/older/gap; historical `.10014` -> require `.10015`.
- Fail closed on malformed matching-series tags instead of silently deriving an unsafe next ordinal.
- Preserve unrelated tags and other base versions.
- Re-read final branch diff, refresh current `main`, reconcile non-overlapping claim/docs deltas, and integrate once through the declared integration branch.
- Report automatic CI separately; do not claim licensed BricsCAD runtime evidence from source/static validation.

## Completion condition

The V25 preview workflow and release-preparation script both enforce exact next-ordinal history from Git tags, deterministic preflight coverage includes the historical `.10014` migration case, the combined integration tree is reviewed, the final source result is reachable from current `main`, and this claim is then marked `COMPLETED` with exact SHAs.
