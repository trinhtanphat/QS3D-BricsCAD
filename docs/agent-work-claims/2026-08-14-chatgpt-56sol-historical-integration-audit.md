# Work claim — historical multi-agent integration audit

- Status: `COMPLETED`
- Agent: `chatgpt-20260814-historical-integration-audit-56sol`
- Registered: `2026-08-14T17:11:00+07:00`
- Baseline main SHA: `c2b4e2a49bbfa5690adc5da96b6c80a4bb30ab09`
- Implementation branch: `agent/chatgpt-zone-update-atomicity-20260814`
- Implementation commits: `d918c8348011548beae8b314cc6d57efa59a726e`, `3a9889e4d5bec71c2989bf0c20b361ecb2745a74`
- Integration branch: `integration/chatgpt-zone-update-atomicity-20260814`
- Integration commit: `66c56579cc7f237ba2faac2481d71cd26dde7b49`
- Final source main landing: PR `#1307`, merge `a69e9a34da00f96d495463412795d8348db10c13`
- Closeout branch: `agent/chatgpt-56sol/historical-integration-audit-closeout`
- Audit report: `docs/HISTORICAL-INTEGRATION-AUDIT-2026-08-14.md`
- Priority: Owner-requested retrospective audit of prior multi-agent direct-to-main / PR integrations for lost code, semantic overwrites, stranded work, and missing regression protection.

## Reserved scope

Read-only historical/integration audit across Git commit history, merge/revert evidence, PR/branch state, current source contracts, tests and preflights. A preliminary Floor/Zone active-id regression hypothesis was investigated and disproved after tracing the later canonicalization contract: `ProjectState.ActiveFloorId` / `ActiveZoneId` now trim and version persisted changes, `ProjectFloorService.SetActive` / `ProjectZoneService.SetActive` canonicalize case aliases through those setters, and `ActiveFloorZoneCanonicalRegressionSmoke` locks single-version canonical repair plus exact canonical no-op.

A separate current defect was confirmed in `ProjectZoneService.Update`: its former local `Required(...)` validation accepted control characters, so the method could call `project.Touch()` and only then fail when `ZoneDefinition.Name` rejected the same name. The rejected update therefore mutated `ChangeVersion` / `UpdatedUtc`, violating failure atomicity. That defect is now fixed and integrated.

## Expected surfaces

- `docs/agent-work-claims/2026-08-14-chatgpt-56sol-historical-integration-audit.md`
- `docs/HISTORICAL-INTEGRATION-AUDIT-2026-08-14.md`
- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectZoneUpdateFailureAtomicitySmoke.cs`
- read-only Git history / PR / branch / issue / current source / tests / existing CI evidence

## Excluded scope

- every other product source/test/script/runtime file unless a concrete defect was proven and the claim was amended first with exact surfaces
- every source/test/script/runtime surface reserved by another `ACTIVE` or `BLOCKED` claim
- LOCAL_ONLY BricsCAD runtime qualification and private-machine evidence
- unrelated GitHub Actions/release changes
- arbitrary backlog feature development unrelated to a proven integration-loss finding

## Validation and evidence

- Historical Floor/Zone active-id cluster around `0ce741622c31fe794aa3784ac45c304309d8c2a4`, `2d59c7e11f156387b452e86077a23a6f0f8a8db0`, `9e65b58d40b0d0937c4de4dc7dbfbd6bbb55838b`, and `191e0509dcad66c9c5029bfd512a795d97f1486f` classified `SUPERSEDED / SAFE`; current canonical alias repair semantics are intentionally later and regression-protected.
- Zone update failure atomicity classified `CONFIRMED_REGRESSION`, then fixed by service-level pre-mutation control-character validation.
- `ProjectZoneUpdateFailureAtomicitySmoke` captures name, `ChangeVersion`, `UpdatedUtc`, active Zone, and Zone count; rejected control-character rename leaves all captured state unchanged.
- Claim amendment: PR `#1305`.
- Agent implementation: PR `#1306`, head `3a9889e4d5bec71c2989bf0c20b361ecb2745a74`, merged to integration as `66c56579cc7f237ba2faac2481d71cd26dde7b49`.
- Final source landing: PR `#1307`, merge `a69e9a34da00f96d495463412795d8348db10c13`.
- Later ancestry comparison from `a69e9a34...` to then-current `main@cc1423875ede7593286d80b0e0148da9f5cdc950` returned `ahead`, `ahead_by=4`, `behind_by=0`, proving the Zone landing remained reachable rather than overwritten.
- Closeout refresh later observed `main@74c7362dea6b98e6171dca43f19156153e5cc049`; no takeover was made of concurrent Floor/Family/V25 smoke lanes added after the Zone landing.
- Open-PR search at closeout returned no open pull requests.
- Current repository search found no unresolved `<<<<<<<` merge markers.
- Representative closed-unmerged high-risk cases were reconciled rather than treated as loss by PR state alone: #1279 was superseded by #1280/#1283; #1273 records winning #1269; #1102's intended `scripts/preflight-se-closed-polyline-solid.py` exists on current `main`.
- GitHub reports `main` unprotected with no required status checks enforced at the branch-protection layer. This is recorded as a process risk, not a source regression; no repository-setting mutation is performed by this audit.
- No manual GitHub Actions run/rerun was dispatched by this audit. Standing automatic post-integration CI remains governed by `CI_POLICY.md`; no fresh V25 cloud green or LOCAL_ONLY BricsCAD runtime PASS is claimed here.

## Coordination

The audit deliberately avoided active neighboring source/runtime claims. The earlier temporary Floor/Zone active-id write reservation was released after that hypothesis was disproved. The narrower Zone update failure-atomicity reservation was implemented through the canonical agent/integration flow. Concurrent Floor update failure-atomicity and later Family/V25 smoke lanes were left to their registered owners.

## Completion

`COMPLETED`: the audit report is prepared on the closeout branch; the one confirmed remote-safe source regression is integrated into `main` with deterministic regression protection; representative stale/unmerged-risk cases were reconciled against current source/history; no open PR or unresolved merge marker remained at closeout inspection; and no additional safely reservable current regression was proven before closeout.

Future HEADs must be re-audited under the same claim/integration protocol; this completion record proves the inspected history/current tree only, not arbitrary later commits.
