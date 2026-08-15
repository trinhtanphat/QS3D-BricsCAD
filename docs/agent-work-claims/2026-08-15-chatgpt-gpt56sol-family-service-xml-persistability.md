# Work claim — ProjectFamilyService XML persistability

- Status: `ACTIVE` — implementation complete, pending authorized integration
- Agent: `chatgpt-gpt56sol-family-service-xml-20260815`
- Registered: `2026-08-15T09:11:37+07:00`
- Current main SHA at claim: `1eb5a757845ac1e978b3a9dccb33f439f9dfa46f`
- Integration-v2 baseline: `db571da244531213d986617220996740f4c5b878`
- Latest reconciled integration-v2 SHA: `f3191e79c8c66fcb43431c95282e4a25641c8f9f`
- Issue: `#1491`
- PR: `#1493` (draft)
- Branch: `agent/chatgpt-gpt56sol/family-service-xml-persistability-20260815`
- Priority: Core P1 persistence/failure-atomicity

## Confirmed defect

On integration v2, `ProjectFamilyService.Required(...)` validated required/trimmed-length/control-character semantics but not XML character representability. The helper guards Family id/name inputs for Create/Duplicate/Rename/lookup and Family property keys. `Rename(...)` calls `project.Touch()` before assigning `family.Name`, so once the public ProjectFamily boundary is XML-safe under #1468, an XML-invalid service rename can advance project revision/timestamp and then throw. Before that public-boundary hardening, the same service input can enter state canonical QSDB cannot represent.

## Implemented fix

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`: add `XmlConvert.VerifyXmlChars(...)` to `Required(...)` after existing canonical/control-character checks and map `XmlException` to `ArgumentException`.
- Existing `Value(...)` property-value XML validation imported through #1479 is unchanged.
- Focused smoke rejects XML-invalid Create/Duplicate/Rename/lookup/property-key input before mutation, pins exact Family name / project ChangeVersion / UpdatedUtc failure atomicity, and proves supplementary-Unicode Family id/name/property service mutation through QSDB SaveNew/Load.
- Focused smoke is module-registered.

## Evidence

- claim: `70341c960910b7245bbb82af28699abba526001f`
- source: `d9c391ad1cfd159d806b7a268afc153ffce45487`
- smoke: `a62d897c8f7190c983bec21fcefcbdff88dd25c8`
- registration: `0f62ba02ef41a770039f3a255874a4156bcf2a49`
- first non-force integration reconciliation: `e19d8a5e07963843a58cd48a91e52d4926011776` onto `58f0262612f19e90fc78160571fa2e3b0e69e9a8`
- second reconciliation: `d1dace8a4c4d1f6bd99595fe7a69c35f7bdf4566` onto `931276e5205b2047da475ba7274e9a386806afb3`
- latest reconciliation: `10106cf530bb9046356eb7ca1583454e597748a8` onto `f3191e79c8c66fcb43431c95282e4a25641c8f9f`
- PR #1493: mergeable=true at latest readback; exactly 4 task files
- production source delta: +8/-0 only in `Required(...)`; no deletions
- GitHub source/diff/readback: PASS
- managed build/smoke: NOT_RUN because this session has no `dotnet`; no `LOCAL_PASS` claimed
- BricsCAD runtime: NOT_RUN and outside this Core-only lane
- no GitHub Actions manually dispatched/rerun by this session

## Coordination / exclusions

- #1468 owns `ProjectState.cs` and the public ProjectFamily id/name contract; no `ProjectState.cs` edits here.
- #1422/#1479 owns Family property-value XML safety; preserved unchanged.
- #1474/#1483 owns Floor service; #1469/#1470 owns Zone service and is integrated into v2.
- No Family assignment business-rule changes, element semantics, serializer/schema, adapter/native, workflow/release or unrelated product changes.
- No direct mutation/merge of integration or main refs by this normal-agent session.

## Handoff

Implementation/regression are fully represented by draft PR #1493 against the owner-authorized integration-v2 branch. No session-only source change remains. Claim stays ACTIVE until coordinator import/release.