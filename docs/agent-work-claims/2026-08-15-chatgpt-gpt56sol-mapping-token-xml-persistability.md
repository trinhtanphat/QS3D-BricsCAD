# Work claim — Measurement/work-item mapping token XML persistability

- Status: `ACTIVE` — implementation complete, pending review/authorized integration
- Agent: `chatgpt-gpt56sol-mapping-token-xml-20260815`
- Registered: `2026-08-15T08:46:51+07:00`
- Baseline main SHA: `96862d6cdfddd8bb2ea4a0055505005859467ea7`
- Latest reconciled main SHA: `96862d6cdfddd8bb2ea4a0055505005859467ea7`
- Issue: `#1460`
- PR: `#1464` (draft)
- Branch: `agent/chatgpt-gpt56sol/mapping-token-xml-persistability-20260815`
- Priority: Core P1 persistence integrity

## Confirmed defect

`MeasurementWorkItemMappingContract.RequireToken(...)` validated blank/trim/control-character semantics but not XML character representability. Mapping tokens are persisted through reserved project metadata: `MappingId` becomes part of the metadata key and the other three identifiers become fields in the metadata value. The owned metadata write path deliberately bypasses generic public metadata XML validation, so XML-illegal UTF-16 could enter canonical reserved metadata after the project revision was touched and fail only during later QSDB serialization.

## Implemented fix

- `src/QS3D.Core/Mapping/MeasurementWorkItemMapping.cs`: after the existing canonical/control-character rules, `RequireToken(...)` now validates with `XmlConvert.VerifyXmlChars(...)` and maps `XmlException` to `ArgumentException`.
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemMappingTokenXmlPersistabilitySmoke.cs`: rejects XML-invalid values for all four constructor tokens and invalid `Resolve(...)` input, then proves valid supplementary Unicode advances mapping revision exactly once and round-trips through QSDB.
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemMappingTokenXmlPersistabilityRegistration.cs`: module-registers the focused smoke.

## Evidence

- Claim commit: `4ce16350c70a3e4025daa003f335d48b8d3b083c`
- Source commit: `d1d0c9feae667808466a618a9c09f50a5cefae17`
- Smoke commit: `9887316406ba9065ddc6be650fab9a4cbeca6252`
- Registration commit: `1a5037a39379873af299e4b4a0e4d07854c86a03`
- PR: `#1464`.
- Branch-vs-main compare before PR: ahead 4, behind 0; exactly four changed files.
- Production source delta: +9/-0, limited to `System.Xml` import + canonical XML validation.
- GitHub source/commit/diff readback: PASS.
- Managed `dotnet` build/smoke: NOT_RUN because this execution environment has no `dotnet` command; no `LOCAL_PASS` is claimed.
- BricsCAD V25/V26 native/runtime: NOT_RUN and outside this Core-only lane.
- No GitHub Actions were manually dispatched or rerun.

## Excluded scope

- No metadata dictionary, mapping codec/collection mutation/version changes.
- No ProjectElement/ProjectState, measurement calculation, adapter/native, workflow/release or product documentation changes.
- No direct write/merge to `main`; normal-agent stop point is branch + PR unless separately authorized.

## Handoff

Implementation and regression are pushed and fully represented in draft PR #1464. The claim remains `ACTIVE` until an authorized integration coordinator merges/resolves the lane. No session-only source change is required for continuation.
