# Work claim — Family assignment canonical no-op smoke

- Status: `COMPLETED`
- Agent: `gpt56sol-family-assignment-noop-smoke-agent`
- Registered: `2026-08-14T19:47:00+07:00`
- Baseline main SHA: `da2c8c58c5e24a901eb73de23e6c93674241b706`
- Trigger: V25 Preview #181 / run `31801274134`, deterministic Core smoke job `94769593393`
- Completed evidence: V25 Preview #182 / run `31801978835`, exact release SHA `ab940318e2b8885dc4ec1682617ae2bbd8e4c461`

## Evidence

The #181 CI annotation reported `ProjectFamilyAssignmentAtomicitySmoke.SemanticallyIdenticalTargetAssignmentIsNoOp()` failing because the fixture assigned `"  target  "` through `ProjectElement.FamilyId`, whose public setter canonicalized the stored value to `"target"` before `ProjectFamilyService.Assign()` ran. Historical commit `22bcb9e738672a1680ed2633d8436bce7e070c1b` intentionally defines padded/case-varied references that already resolve to the target Family as true assignment no-ops that preserve stored identity and persistence state. Production `ProjectFamilyService.Assign()` already implemented that contract and was left unchanged.

## Integrated fix

- Claim-only commit: `ae15c9bc66942a53aa27050373b3594d0fbbcf5c`
- Agent implementation: `61294523490e4e1651ad9e2ace58997a9851f30b`
- Agent → integration PR: #1332
- Integration candidate: `2f623b036167f4917b654ea46d4f39fe4bb95561`
- Final integration → main PR: #1334
- Final main landing: `c23f5f061287290e1db8d0061866a4e600ab688c`
- Duplicate later claim #1331 was explicitly released by docs commit `4abf48a91ff3db4a92d671beae6020a01398eafa`; no second implementation was merged.

The focused smoke now asserts the public FamilyId setter canonicalizes padded input, then injects only the intended legacy/raw padded `_familyId` backing value via narrow reflection before invoking `ProjectFamilyService.Assign()`. Existing zero-change, identity, property, dirty/timestamp and project persistence assertions remain in place.

## Validation

- `61294523490e4e1651ad9e2ace58997a9851f30b` is an ancestor of final landing `c23f5f061287290e1db8d0061866a4e600ab688c` (`behind_by = 0`).
- Read-back from final main confirms the raw-fixture repair is present.
- Automatic dispatcher #14 / run `31801898913` dispatched the final landing; no manual release dispatch was used.
- V25 Preview #182 / run `31801978835` prepared exact release SHA `ab940318e2b8885dc4ec1682617ae2bbd8e4c461`.
- On #182, generic source guard, all discovered feature source guards, Core restore, Core Release build, Core smoke harness and **Deterministic Core smoke tests all passed**. The run advanced to BricsCAD V25 compile-reference acquisition, proving the #181 Family no-op smoke blocker is closed.
