# Work claim — Grid naming XML current-main recovery

- Status: `RELEASED` — implementation complete; pending authorized review/integration on current main
- Agent: `chatgpt-gpt56sol-grid-naming-xml-main-recovery-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `87c38a532673b16f315ab766333870d4200a8677`
- Latest reconciled main: `1660c817f7d376357453744e79a95ebe1830cd4d`
- Issue: `#1495`
- Replacement current-main PR: `#1568` (`ready for review`, latest readback `mergeable=true`)
- Superseded integration-v2 PR: `#1497` (`closed`, not merged)
- Branch: `agent/chatgpt-gpt56sol/grid-naming-xml-main-recovery-20260815`
- Priority: Core P1 failure atomicity / persisted Grid naming integrity

## Confirmed current-main defect

`GridNamingService.Optional(...)` accepted XML-illegal UTF-16 in Grid prefix/suffix text. `Renumber(...)` could therefore call `project.Touch()` before `ProjectElement.SetProperty(...)` rejected the generated XML-invalid Grid label, advancing project revision/timestamp on a failed operation.

## Recovered implementation

- prefix/suffix are preflighted with `XmlConvert.VerifyXmlChars(...)` during existing option normalization;
- existing trim/length/parameter-name behavior is preserved;
- focused regression rejects XML-invalid prefix and suffix before project/element mutation;
- rejected renumber preserves project `ChangeVersion` / `UpdatedUtc`, Grid label/sequence properties, dirty flags and element timestamps;
- valid supplementary-Unicode affixes survive Grid renumber and canonical QSDB SaveNew/Load exactly;
- numeric/alphabetic sequencing, batch bounds, target ownership, duplicate-label and capacity behavior remain unchanged.

## Evidence

- claim-only: `edb75f2cb62fea5d3fff2951cf9a0a78d88cc9c1`
- implementation: `75aced099a75218fcffb9556e6c21fcb673b896b`
- reconciliation onto `f10c1fed5af58e2a0f3be1d63637c190696eb605`: `7ad6d4aebe7590c714ed2dc7c5b407b6f0235549`
- latest reconciliation onto `1660c817f7d376357453744e79a95ebe1830cd4d`: `c71367125c7bdfaaef019261074707a3b0cfadd1`
- replacement PR: `#1568`
- task diff: exactly four files; production source delta `+9/-0`
- exact GitHub source/diff readback: PASS
- prior reviewed source/smoke/registration blobs: `f1d6191e0c908a7f0c813b19bb0f3c81603b86bb` / `7a4084990e6c30a8a460f77be12e5eeeeb22860d` / `030c214715d192a56c2317765a0102caa0b49048`
- managed build/smoke in this recovery session: NOT_RUN; no `dotnet` execution available and no PASS claimed
- BricsCAD runtime: not applicable to this Core-only lane
- GitHub Actions: not manually dispatched/rerun

## Coordination / exclusions

- #1497 was closed only after #1568 was verified clean/mergeable; old branch/history remains intact.
- No Grid capture/intersection/system/native annotation, ProjectElement contract, schema, UI/adapter, workflow/release or product-boundary changes.
- No direct main merge by this normal-agent session.

## Handoff / release

All recovery source/regression state is represented by ready PR #1568 against `main`. Reservation ownership is released from this session. Keep Issue #1495 open until an authorized coordinator integrates #1568 and remote ancestry/source readback confirms the failure-atomicity fix on `main`.
