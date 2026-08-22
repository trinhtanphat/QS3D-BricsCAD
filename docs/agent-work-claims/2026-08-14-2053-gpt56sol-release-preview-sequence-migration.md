# Work claim — release preview sequence and historical migration guard

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-review`
- Registered: `2026-08-14T20:53:00+07:00`
- Expanded: `2026-08-14T21:10:00+07:00`
- Baseline main SHA: `9f2f7e58ab1f81ad652823c6eada18646ad61f8e`
- Expansion evidence main SHA: `0b2f11c95fd0f6ebf4fabf0a1687b173923ecfe4`
- Implementation branch: `agent/chatgpt-web-gpt56sol-review/release-preview-sequence-migration-20260814`
- Initial implementation commit: `f05786cfb9c8497de91d6483315bb13ddc066ac8`
- Integration batch: `integration/chatgpt-web-gpt56sol-review-release-preview-sequence-migration-20260814`
- Priority: repository-wide review found a deterministic release-policy gap immediately after the positive-ordinal lane completed.

## Reserved scope

Enforce the canonical preview-series ordering contract end-to-end for automatic V25 public releases. A requested preview tag must be the next ordinal in its exact `vMAJOR.MINOR.PATCH-preview.N` series; if no prior tag exists, `N` must be `1`. This also enforces the documented historical `v0.1.0-preview.10014` migration boundary, so a smaller or skipped `v0.1.0-preview.N` cannot be published after `.10014`.

The automatic post-integration dispatcher is now explicitly part of this reservation. It must derive the next public preview ordinal from the repository's published Git-tag history, not from `GITHUB_RUN_NUMBER` or any build/run counter. The release-preparation path independently revalidates that exact next-ordinal contract immediately before source identity mutation.

## Expected surfaces

- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` — derive the automatic `v0.1.0-preview.N` request from published matching-series Git tags and fail closed on malformed/overflowing history; do not use Actions run numbering as public version state.
- `scripts/prepare-v25-cloud-release.ps1` — revalidate the requested sequence immediately before source identity mutation/commit so direct invocation or stale dispatch cannot bypass public release policy.
- `scripts/validate-preview-release-sequence.ps1` — deterministic runtime helper for exact matching-series tag history.
- `scripts/preflight-release-preview-sequence.py` — source guard covering dispatcher derivation, prepare-before-mutation ordering, first-series/increment/gap/duplicate/older cases, and the historical `.10014` migration case.
- this claim for implementation/integration close-out.

## Exclusions / collision boundaries

- Do not reopen or weaken the completed positive-ordinal syntax contract; sequence validation composes on top of it.
- Do not add alpha/beta/rc publication to the V25 *preview* workflow; this lane only governs the existing preview release path.
- Do not delete, retag, move or mutate historical releases/tags.
- Do not derive public ordinals from Actions run numbers, commit counts, timestamps or other build metadata.
- Do not enter the ACTIVE `slabOpen` lane, issue #1005 Source Reconcile/Undo, BricsCAD native runtime, signing/licensing, drawing-fingerprint or other feature scopes.
- No manual Actions dispatch; final source landing relies on the standing automatic post-integration dispatcher.

## Evidence / reason

`docs/RELEASE-NAMING.md` requires `N` to increase by exactly one per published prerelease series and explicitly states that, because historical `v0.1.0-preview.10014` exists, smaller ordinals such as `v0.1.0-preview.1` must not be published on that same series.

Repository evidence after the initial implementation proved the generator side is also defective:

- published matching-series tags observed by the V25 runner stop at `v0.1.0-preview.10014`;
- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` currently computes `preview=$((10000 + GITHUB_RUN_NUMBER))`;
- automatic attempts consequently requested/prepared `v0.1.0-preview.10016`, `.10017`, then `.10018` even though those ordinals were not the next published ordinal;
- V25 #185 correctly failed closed for a separate concurrent-main move, proving the release-preparation freshness guard is working but does not solve public sequence derivation.

The initial branch commit `f05786cfb9c8497de91d6483315bb13ddc066ac8` added prepare-time sequence validation and deterministic regression coverage. This expansion reserves the automatic dispatcher correction required so the system chooses the compliant tag instead of merely rejecting its own run-number-derived request.

## Validation plan

- Keep the existing positive-ordinal preflight green.
- Add deterministic sequence cases: no prior series -> `.1`; prior `.1` -> `.2`; prior `.1,.2` -> `.3`; reject duplicate/older/gap; historical `.10014` -> require `.10015`.
- Fail closed on malformed matching-series tags instead of silently deriving an unsafe next ordinal.
- Preserve unrelated tags and other base versions.
- Guard that the automatic dispatcher does not reference `GITHUB_RUN_NUMBER` for public preview derivation and derives from Git-tag history instead.
- Guard that release preparation revalidates sequence before `sync-preview-release-version.ps1` mutates source identity.
- Re-read final branch diff, refresh current `main`, reconcile non-overlapping claim/docs/release-bot deltas, and integrate once through the declared integration branch.
- Report automatic CI separately; do not claim licensed BricsCAD runtime evidence from source/static validation.

## Completion condition

The automatic dispatcher derives the next `v0.1.0-preview.N` from published tag history, release preparation independently enforces the same exact next-ordinal history, deterministic preflight coverage includes the historical `.10014` migration case and rejects run-number derivation, the combined integration tree is reviewed, the final source result is reachable from current `main`, and this claim is then marked `COMPLETED` with exact implementation/integration/main SHAs.
