# Agent concurrency handoff — 2026-08-14

This note is a shared coordination checkpoint for concurrent agents working on `trinhtanphat/QS3D-BricsCAD`.

## Mandatory claim-first rule

Before an agent begins any implementation, diagnosis or test work on a lane:

1. refresh `origin/main` and inspect current `ACTIVE` / `BLOCKED` claims;
2. publish a claim-only Markdown commit under `docs/agent-work-claims/` with exact expected paths/symbols/tests/runtime scope;
3. push that claim to `main` and verify it is an ancestor of the newest `origin/main`;
4. recheck concurrent claims;
5. only then read implementation code, diagnose, edit or test the reserved lane.

If investigation discovers a path/symbol/test/runtime surface not named in the current claim, stop before touching it and land a claim-amendment-only commit first. Immediately before every source/test/script write or PR merge, refresh `main` and recheck both exact-path claims and commits added since the prior check.

The canonical detailed protocol is `docs/AGENT-WORK-REGISTRATION.md`.

## Current remote/cloud state

The former deterministic Core smoke blocker in issue `#1092` is resolved. Do **not** reopen or reimplement that lane from the historical LOCAL-003 failure in this document.

The strongest completed remote evidence at this checkpoint is V25 cloud release run `#147`:

- workflow: `.github/workflows/release-v25-cloud.yml`
- run id: `31770836729`
- workflow head SHA: `f29e6bc8206aa7599c43aa6d2ab4d624079e4411`
- conclusion: `success`
- source guards, Core Release build, deterministic Core smoke, BricsCAD V25 reference acquisition/validation, V25 plugin build, packaging, version binding, checksum, artifact upload and prerelease publication all completed successfully
- published prerelease: `v0.1.0-preview.9`
- exact release source / release target: `5f4ab940649cf1ae7b16bfe653b30ae49572f78b`
- package: `QS3D-BricsCAD-V25.zip`
- package digest: `sha256:299fd26e914f889276bde4d589e196438904384e41518520165a14d0762ca288`

That successful run is completed evidence for its exact release source only. `main` continued to advance while/after the run, so #147 must not be described as runtime or current-HEAD qualification for later commits.

The immediately preceding completed release was `v0.1.0-preview.8`, target `80ba5ce2cc28cbfadbec6bb70c7a43e1ad5c8fa6`, with package digest `sha256:b506d20c0b77d57e90d66270f4427c97fcfa86de4c5a36b4e6db3b7abe2e0167`.

Neither cloud release executes real BricsCAD NETLOAD/runtime acceptance.

## Resolved #1092 history

The old LOCAL-003 qualification on `2dc87bf0985c5967f9ca45f09aac22ba85e2e0cd` exposed `ModelHealthSourceHandleSmoke.NumericAliasesShareIdentity`: aliases such as `A`, `00a` and live `0xA` were not being treated as one canonical source-handle identity.

The corresponding source lane was repaired and later remote smoke/release evidence passed. Historical coordination details remain useful for avoiding duplicate work:

- initial claim: `3cb24f5d9041209e00704d8270c61218278d8baf`
- scope amendment: `404e110d954a354541cefaf0c3dddde5e399c0e7`
- overlapping direct-main fix observed at `d03edf8e4c476ee929d731a2c0c7400a8b8d14e4`
- redundant PR `#1093` was correctly closed unmerged

Do not reopen PR `#1093` or create another #1092 normalization patch without new failing evidence on a newer exact SHA.

## Native/local acceptance still separate

Remote/cloud success does not replace licensed/native BricsCAD acceptance.

- `#982`: source-side work exists, but any required exact-SHA licensed V25 acceptance remains local/native evidence.
- `#1005`: do not weaken Source Reconcile, drawing-fingerprint, command-plan freshness or `DESYNCHRONIZED` fail-closed guards. The latest handoff requires native/local lifecycle/context diagnosis and correctly timed command-plan recapture rather than a speculative remote relaxation.
- LOCAL_ONLY owners must reactivate/claim their own runtime lanes before new native work.

## Next-agent rule

Do not treat a resolved historical failure as the next task. Start from the newest `main`, newest workflow result, open issues and current claims. A new remote/source lane requires concrete current evidence: an exact failing test/build/preflight, a reproducible source invariant violation, or an explicitly unimplemented remote-owned requirement.

If a new failure appears:

- identify the exact exception/test/gate first;
- recheck `ACTIVE` / `BLOCKED` claims;
- claim the exact source/test/runtime surface with a claim-only commit;
- only then inspect or change the reserved implementation paths;
- preserve fail-closed semantics and existing positive/negative regression coverage.

## Boundaries

- Do not alter LOCAL_ONLY native BricsCAD evidence from a remote agent.
- Do not enter Source Reconcile/#1005 or any other `ACTIVE`/`BLOCKED` lane without a current ownership check and explicit split/transfer.
- Do not force-push, reset or rewrite concurrent work.
- Do not create no-op/speculative source commits or weaken CI/smoke gates merely to report green.
