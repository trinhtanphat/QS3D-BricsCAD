# Historical multi-agent integration audit — 2026-08-14

## Scope

This audit reviews the high-concurrency QS3D-BricsCAD integration history for evidence of lost code, semantic overwrite, stale/superseded contracts, stranded pull-request work, and missing regression protection. It is a repository/source audit only. LOCAL_ONLY BricsCAD runtime qualification, private DWG evidence, signing, and manual GitHub Actions operations are outside this lane.

Canonical coordination policy is `docs/AGENT-WORK-REGISTRATION.md`: claim-only reservations are visible on `main`; implementation stays on agent branches; participating changes are assembled on `integration/<batch-id>`; one final source landing reaches `main`; closing/status documentation may then land without triggering another source-path V25 dispatch.

## Executive result

No current evidence was found that a still-required reviewed implementation is stranded only in an open pull request. At closeout inspection there were no open pull requests, and current-tree search found no unresolved Git conflict markers.

The audit did find one concrete current source regression in its own reservable lane: rejected Zone rename text containing a control character could call `ProjectState.Touch()` before `ZoneDefinition.Name` rejected the value, changing `ChangeVersion` / `UpdatedUtc` on a failed update. That defect is now integrated with deterministic smoke protection through the claim -> agent branch -> integration branch -> final main flow.

Historical closed-unmerged PRs must not be interpreted as lost work by state alone. Representative high-risk examples inspected in this audit are either explicitly superseded/duplicated or have their intended current artifact present on `main`.

## Classification table

| Finding | Classification | Evidence / outcome |
|---|---|---|
| Active Floor/Zone canonical-id history around `0ce7416`, `2d59c7e`, `9e65b58`, `191e050` | `SUPERSEDED / SAFE` | Later canonicalization intentionally supersedes the older trimmed/case-insensitive no-op interpretation. Current regression coverage preserves canonical alias repair with single versioning and exact canonical no-op. No rollback to the older semantic contract is warranted. |
| `ProjectZoneService.Update` control-character rename | `CONFIRMED_REGRESSION -> FIXED` | Claim amendment merged via PR #1305. Agent implementation PR #1306 used `agent/chatgpt-zone-update-atomicity-20260814` and merged into `integration/chatgpt-zone-update-atomicity-20260814`. Final PR #1307 merged the integration candidate to `main` as `a69e9a34da00f96d495463412795d8348db10c13`. Production validation now rejects control characters before mutation; `ProjectZoneUpdateFailureAtomicitySmoke` proves rejected updates preserve name, version, timestamp, active Zone, and Zone count. |
| Closed-unmerged PR #1279 Quantity Rule raw FamilyId fixture | `SUPERSEDED / SAFE` | Its own history records that equivalent reviewed implementation #1280 landed first; closeout #1283 records reuse rather than duplicate overwrite. |
| Closed-unmerged PR #1273 QSDB relation fixture | `SUPERSEDED / SAFE` | PR body records earlier claim #1267 and merged implementation #1269 as the winning lane; #1273 was intentionally closed to avoid overwrite. |
| Closed-unmerged PR #1102 SE closed-polyline preflight | `INTEGRATED / SAFE` | Although the PR itself is closed without merge, `scripts/preflight-se-closed-polyline-solid.py` exists on current `main` and guards the intended SE atomic batch/source-retention contract. PR state therefore does not indicate lost work. |
| Current open-PR inventory | `SAFE` | No open pull requests were returned at closeout inspection. |
| Current merge-marker search | `SAFE` | No `<<<<<<<` merge-conflict marker was found in the current repository search. |
| Historical agent branches remaining after merge/supersession | `PROCESS_NOTE` | Branch existence or deletion is not integration proof. Per canonical policy, current-tree behavior and commit reachability are authoritative. Old branches may remain for history without implying missing code. |
| `main` branch protection | `PROCESS_RISK` | GitHub reports `main` as not protected and with no required status checks enforced at the branch-protection layer. The repository-level claim/integration/automatic-dispatch protocol therefore remains important; enabling server-side protection would further reduce accidental direct-main writes, but this audit does not mutate repository settings. |

## Zone failure-atomicity integration evidence

1. Claim-only amendment PR #1305 reserved only `src/QS3D.Core/Domain/ProjectZoneService.cs` and `tests/QS3D.Core.SmokeTests/ProjectZoneUpdateFailureAtomicitySmoke.cs`.
2. Source commit `d918c8348011548beae8b314cc6d57efa59a726e` added service-level control-character rejection before mutation.
3. Test commit `3a9889e4d5bec71c2989bf0c20b361ecb2745a74` added deterministic failure-atomicity smoke coverage.
4. PR #1306 merged the two commits from `agent/chatgpt-zone-update-atomicity-20260814` into `integration/chatgpt-zone-update-atomicity-20260814` as `66c56579cc7f237ba2faac2481d71cd26dde7b49`.
5. PR #1307 merged that integration branch to `main` as `a69e9a34da00f96d495463412795d8348db10c13`.
6. A later ancestry comparison showed current `main` ahead of `a69e9a34...` with zero commits behind, proving the integrated Zone fix remains reachable rather than having been overwritten.

## Concurrent findings intentionally not taken over

While this audit was active, other agents registered and integrated neighboring failure-atomicity work, including Floor update handling, and later registered Family rename / V25 smoke annotation lanes. Those surfaces were not taken over by this audit. This is intentional collision avoidance, not incomplete audit work.

## CI / runtime boundary

No manual GitHub Actions operation is performed by this audit. The repository's standing policy owns the automatic post-source-integration dispatcher. At the latest inspected source `main` during closeout, the automatic dispatcher run for the then-current Floor integration SHA was in progress. This report therefore does not claim a fresh V25 cloud green result, licensed BricsCAD runtime PASS, private-DWG PASS, or signing PASS.

The Zone fix itself is source-integrated and regression-protected independently of those LOCAL_ONLY/runtime gates.

## Multi-agent integration rules reinforced by this audit

- Publish the claim on `main` before substantive implementation.
- Re-read current `main` and every active/blocking claim before touching source.
- Keep implementation on a dedicated agent branch.
- Do not infer that a closed-unmerged PR means lost work; compare the winning implementation and current tree.
- Assemble participating owner-request lanes on one integration branch and perform one final source landing to `main`.
- Never resolve semantic conflicts with blind whole-file `ours` / `theirs` choices.
- Verify exact commit reachability and current-tree behavior after landing; branch/PR state alone is not proof.
- Do not force-push or reset shared `main` backwards.

## Closeout

The audit's one confirmed remote-safe source regression is fixed and integrated; representative stale/unmerged-risk cases were reconciled against current source/history; no open PR or unresolved merge marker remains at closeout inspection; and no additional safely reservable current regression was proven by this audit before closeout.

Any later source change can create a new integration risk, so future agents must continue to follow current claim and batch-integration policy rather than treating this report as permanent proof for future HEADs.
