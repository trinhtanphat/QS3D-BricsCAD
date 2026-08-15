# Work claim — ZoneDefinition XML persistability

- Status: `ACTIVE` — implementation complete, pending review/authorized integration
- Agent: `chatgpt-gpt56sol-zone-xml-20260815`
- Registered: `2026-08-15T08:30:19+07:00`
- Baseline main SHA: `bbf44df6f5440566758122866289ea60973e155c`
- Latest reconciled main SHA: `bbf44df6f5440566758122866289ea60973e155c`
- Issue: `#1442`
- PR: `#1446` (draft)
- Branch: `agent/chatgpt-gpt56sol/zone-xml-persistability-20260815`
- Priority: Core P1 persistence integrity

## Confirmed defect

`ZoneDefinition` accepted XML-illegal UTF-16 such as an unpaired surrogate because its public constructor and `Name` setter validated only blank/control-character input. `QsdbProjectStore.Save*` serializes Zone `id`/`name` to XML and rejects that accepted state later, so the public mutation boundary was not aligned with canonical persistence and an invalid `Name` assignment was not fail-fast.

## Implemented fix

- `src/QS3D.Core/Domain/ProjectState.cs`: `ZoneDefinition.Require(...)` now preflights XML representability with `XmlConvert.VerifyXmlChars(...)` and converts `XmlException` into `ArgumentException` before constructor/setter state acceptance.
- `tests/QS3D.Core.SmokeTests/ZoneDefinitionXmlPersistabilitySmoke.cs`: covers invalid Zone id/name rejection, `Name` setter failure atomicity, and valid supplementary-Unicode QSDB `SaveNew` → `Load` round-trip.
- `tests/QS3D.Core.SmokeTests/ZoneDefinitionXmlPersistabilityRegistration.cs`: registers the focused smoke using the existing `ModuleInitializer` pattern.

## Evidence

- Claim commit: `fef641b8f29cbdd095e1bb7b6adab6a4e0bda3b6`
- Source commit: `920615ada6242d7d0c9dcbc2b1bdf7226384f437`
- Smoke commit: `4cae2e140ff88f77cc327328df9a842b9862f117`
- Registration commit / pre-handoff head: `4c0a978b2354394ff4cc33da570714df738c3b60`
- PR: `#1446`
- Branch-vs-main compare before handoff: ahead 4, behind 0; exactly four changed files, with the source delta limited to +9 lines in `ProjectState.cs`.
- GitHub source/test readback completed successfully.
- `dotnet --info`: NOT_RUN/UNAVAILABLE for managed validation because this execution environment reports `dotnet: command not found`; no `LOCAL_PASS` is claimed.
- BricsCAD V25/V26 native/runtime: NOT_RUN and outside this Core-only lane.
- No GitHub Actions were manually dispatched or rerun.

## Excluded scope

- No `FloorDefinition`, `ProjectFamily`, other `ProjectState` scalar/identity fields, elements, quantities, adapter/native, release, workflow, or documentation behavior.
- No overlap with #1401 / PR #1424.
- No direct write or merge to `main`; normal-agent stop point is branch + PR unless separately authorized for integration.

## Handoff

Implementation and regression are pushed and fully represented in draft PR #1446. The claim remains `ACTIVE` until an authorized integration coordinator merges/resolves the lane. No session-only source change is required to continue review/integration.
