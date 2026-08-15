# Work claim — ProjectZoneService XML persistability

- Status: `ACTIVE` — implementation complete, pending review/authorized integration
- Agent: `chatgpt-gpt56sol-zone-service-xml-20260815`
- Registered: `2026-08-15T08:53:13+07:00`
- Baseline main SHA: `0542131348e6393a4d28d6e0945ec60c2ee3bff6`
- Latest reconciled main SHA: `631c8ea4d4796777a37cc470040ffbade404fe7e`
- Issue: `#1469`
- PR: `#1470` (draft)
- Branch: `agent/chatgpt-gpt56sol/zone-service-xml-persistability-20260815`
- Priority: Core P1 persistence/failure-atomicity

## Confirmed defect

`ProjectZoneService.Required(...)` validated required/length/control-character semantics but not XML character representability. `Create(...)` could therefore admit service input that canonical QSDB cannot represent. `Update(...)` is more dangerous: after service validation and reference resolution it calls `project.Touch()` before assigning `zone.Name`; once the canonical Zone boundary rejects XML-invalid text, the service can leave project revision/timestamp advanced even though the Zone name mutation failed.

## Implemented fix

- `src/QS3D.Core/Domain/ProjectZoneService.cs`: `Required(...)` now calls `XmlConvert.VerifyXmlChars(...)` after all existing required/length/control-character rules and maps `XmlException` to `ArgumentException` before any mutation path.
- `tests/QS3D.Core.SmokeTests/ProjectZoneServiceXmlPersistabilitySmoke.cs`: invalid create id/name and update id/name fail before mutation; failed update preserves exact Zone name, `ChangeVersion`, `UpdatedUtc`, and collection state; valid supplementary Unicode survives service Create/Update and QSDB SaveNew/Load.
- `tests/QS3D.Core.SmokeTests/ProjectZoneServiceXmlPersistabilityRegistration.cs`: module-registers the focused smoke.

## Evidence

- Claim commit: `c7a8cee2316b725e2c40253fff398429f74a878e`
- Source commit: `6bdc80f1830118e8f9288f170745088f417e2a58`
- Smoke commit: `4843bf9834aaa0d0117e4ce2b1effdddfc1f8f87`
- Registration commit: `de7f8315e7de4cb939314bfb58ec6487c7b3cf63`
- First non-force reconciliation: `9a43d14c8c70026bce059897c01a06fbf2491f2a` onto `9830104ae780c7a827c78e8d8290986ff04880dc`.
- Initial handoff claim update: `fe91f883ff9d331531bc3e8d8555e3bdb6d8cb0a`.
- Second non-force reconciliation: `00c0c0c3a9392fb5c7c63254f1d73e7b324002e5` onto `6838001b837338ca70025f07aca57a7652251511`.
- Second handoff claim update: `cd3e7891e1ad1b9a3381d6c5c6dd09d506dd2616`.
- Latest non-force reconciliation: `20e415ed2190aba736def8c6319d6d043d9b89d4` onto `631c8ea4d4796777a37cc470040ffbade404fe7e`; intervening main delta was Source Reconcile claim docs only.
- PR: `#1470`.
- Latest branch-vs-main compare: ahead 9, behind 0; exactly four changed files.
- Production source delta: +9/-0, limited to `System.Xml` import + XML preflight.
- GitHub source/commit/diff readback: PASS.
- Managed `dotnet` build/smoke: NOT_RUN because this execution environment has no `dotnet` command; no `LOCAL_PASS` is claimed.
- BricsCAD runtime: NOT_RUN and outside this Core-only lane.
- No GitHub Actions were manually dispatched or rerun.

## Coordination / exclusions

- #1442 / PR #1446 owns the public `ZoneDefinition` boundary and was merged by the authorized coordinator into `integration/20260815-merge-all`; this lane does not touch `ProjectState.cs`.
- No Floor/Family service, assignment semantics, serializer/schema, adapter/native, workflow/release or product documentation changes.
- No direct write/merge to `main`; normal-agent stop point is branch + PR unless separately authorized.

## Handoff

Implementation and regression are pushed and fully represented in draft PR #1470. The branch has been safely reconciled with latest observed `main` without force-push. This claim remains `ACTIVE` until an authorized integration coordinator merges/resolves the lane. No session-only source change is required for continuation.
