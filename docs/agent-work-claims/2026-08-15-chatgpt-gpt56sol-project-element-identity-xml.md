# Work claim — ProjectElement persisted identity XML representability

- Status: `ACTIVE` — implementation complete, pending review/authorized integration
- Agent: `chatgpt-gpt56sol-element-identity-xml-20260815`
- Registered: `2026-08-15T08:39:58+07:00`
- Baseline main SHA: `e9faeedbf251e5a012168cbb2c964d9f74812fa3`
- Latest reconciled main SHA: `96862d6cdfddd8bb2ea4a0055505005859467ea7`
- Issue: `#1454`
- PR: `#1458` (draft)
- Branch: `agent/chatgpt-gpt56sol/project-element-identity-xml-20260815`
- Priority: Core P1 persistence integrity

## Confirmed defect

`ProjectElement` persists `Id`, optional relation IDs (`FamilyId`, `FloorId`, `ZoneId`) and `DrawingFingerprint` into QSDB XML, but the public validators enforced only blank/control-character rules and trimming. XML-illegal UTF-16 such as an unpaired surrogate could therefore be accepted in canonical in-memory state and rejected only later by `QsdbProjectStore.Save*`.

The earlier `ProjectElement.Id` persistability fix `4414b52fcdccfd98f69f643f4fda781187e23ca1` added control-character rejection but did not preflight XML representability; this lane is the narrow follow-up for that remaining gap plus the adjacent persisted relation/fingerprint fields in the same class.

## Implemented fix

- `src/QS3D.Core/Domain/ProjectElement.cs`: the existing `RequireXmlText(...)` helper is now reused by `RequireId(...)`, `NormalizeOptionalRelationId(...)`, and `NormalizeDrawingFingerprint(...)` after their existing canonical/control-character validation.
- `tests/QS3D.Core.SmokeTests/ProjectElementIdentityXmlPersistabilitySmoke.cs`: rejects lone-surrogate element/relation/fingerprint values, proves rejected setters preserve prior value/Dirty/UpdatedUtc, and covers valid supplementary-Unicode QSDB `SaveNew` → `Load` round-trip.
- `tests/QS3D.Core.SmokeTests/ProjectElementIdentityXmlPersistabilityRegistration.cs`: module-registers the focused smoke.

## Evidence

- Claim commit: `eb9032325bf522d660b9936f0219c3957c947417`
- Source commit: `09f794f68092731e5eeaaa035906885b032ca628`
- Smoke commit: `6982e3895954671cbe4f335d18cc73ac4c7a0894`
- Registration commit: `8de9376f52bcad1ffe4d9f655fdc8d2845ab20ea`
- First non-force reconciliation: `e73b0e5baf95b14ac008e5f37edf967f2fab2a1f` onto `7a80864747acfc2f0057eca57ed332a39d7a4d38`.
- Initial handoff claim update: `9da92ba2ccc0f46321361d6ce8be35aabb383b5c`.
- Latest non-force reconciliation: `b4260fdb22b0aaeda7e848f55a971c3680a1fc7b` onto `96862d6cdfddd8bb2ea4a0055505005859467ea7`; intervening main delta was Curtain docs only.
- PR: `#1458`.
- Production source delta remains exactly 3 additions / 3 deletions, all three return paths delegating to the pre-existing XML validator.
- GitHub source/commit/diff readback: PASS.
- Managed `dotnet` build/smoke: NOT_RUN because this execution environment has no `dotnet` command; no `LOCAL_PASS` is claimed.
- BricsCAD V25/V26 native/runtime: NOT_RUN and outside this Core-only lane.
- No GitHub Actions were manually dispatched or rerun.

## Excluded scope

- No `ProjectState.cs` changes.
- No property, quantity, `SourceHandles`, `DependsOn`, generated-output or dirty-flag behavior changes.
- No RevisionService, adapter/native, workflow/release or product documentation changes.
- No overlap with #1411 or broad #1443.
- No direct write or merge to `main`; normal-agent stop point is branch + PR unless separately authorized.

## Handoff

Implementation and regression are pushed and fully represented in draft PR #1458. The branch has been safely reconciled with latest observed `main` without force-push. This claim remains `ACTIVE` until an authorized integration coordinator merges/resolves the lane. No session-only source change is required for continuation.
