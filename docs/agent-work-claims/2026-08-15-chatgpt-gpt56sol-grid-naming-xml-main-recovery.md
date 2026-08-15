# Work claim — Grid naming XML current-main recovery

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-grid-naming-xml-main-recovery-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `87c38a532673b16f315ab766333870d4200a8677`
- Issue: `#1495`
- Superseded integration-v2 PR: `#1497`
- Branch: `agent/chatgpt-gpt56sol/grid-naming-xml-main-recovery-20260815`
- Priority: Core P1 failure atomicity / persisted Grid naming integrity

## Confirmed current-main defect

`GridNamingService.Optional(...)` still accepts XML-illegal UTF-16 in Grid prefix/suffix text. `Renumber(...)` can therefore call `project.Touch()` before `ProjectElement.SetProperty(...)` rejects the generated XML-invalid Grid label, advancing project revision/timestamp on a failed operation.

## Reserved recovery surfaces

- `src/QS3D.Core/Domain/GridNamingService.cs`
- `tests/QS3D.Core.SmokeTests/GridNamingXmlFailureAtomicitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/GridNamingXmlFailureAtomicityRegistration.cs`
- this claim file

## Recovery contract

- preflight prefix/suffix with `XmlConvert.VerifyXmlChars(...)` during existing option normalization;
- preserve trim/length/parameter-name semantics;
- reject XML-invalid affixes before project/element mutation;
- preserve project revision/timestamp, Grid label/sequence, dirty state and element timestamps after rejection;
- preserve valid supplementary-Unicode affixes through Grid renumber and canonical QSDB round-trip;
- no Grid capture/intersection/system/native annotation, ProjectElement contract, schema, UI/adapter, workflow/release or product-boundary changes;
- no direct main merge and no manual GitHub Actions dispatch/rerun.

## Prior reviewed evidence

- fixed source blob: `f1d6191e0c908a7f0c813b19bb0f3c81603b86bb`
- focused smoke blob: `7a4084990e6c30a8a460f77be12e5eeeeb22860d`
- registration blob: `030c214715d192a56c2317765a0102caa0b49048`

Implementation begins only after this claim is published.
